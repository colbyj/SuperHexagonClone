﻿using System;
using System.Collections.Generic;
using Assets.Scripts.LevelBehavior;
using Assets.Scripts.LevelVisuals;
using Assets.Scripts.SHPlayer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Logging
{
    public partial class Experiment : MonoBehaviour
    {
        public enum FeedbackMode
        {
            Default,
            None,
            Meaningless,
            Meaningful
        }

        public delegate void TimerDelegate();

        [Serializable]
        public class ShTimer
        {
            public Text GuiText;
            public float Value;
            public float InitialValue = 0f; // What the timer gets set to on Reset()
            public bool Paused = false;
            public bool CountUp = true;
            public float? MaxValue = null;
            public float? MinValue = null;
            public int TickLength = 0; // Used for logging between sessions

            public event TimerDelegate TimerFinished;
            public event TimerDelegate TimerTick;

            public void Update(float seconds)
            {
                if (Paused)
                {
                    return;
                }

                if (CountUp)
                {
                    // Because this is on Update(), only check this when we are crossing to a new integer (e.g., 4.99 to 5.00).
                    if ((int)Value != (int)(Value + seconds) && TickLength != 0 && (int)Value % TickLength == 0)
                    {
                        if (TimerTick != null)
                        {
                            TimerTick();
                        }
                    }

                    Value += seconds;

                    if (MaxValue.HasValue && Value >= MaxValue)
                    {
                        Value = MaxValue.Value;
                        Stop();
                        if (TimerFinished != null)
                        {
                            TimerFinished();
                        }
                    }
                }
                else
                {
                    // Same
                    if ((int)Value != (int)(Value - seconds) && TickLength != 0 && (int)Value % TickLength == 0)
                    {
                        if (TimerTick != null)
                        {
                            TimerTick();
                        }
                    }

                    Value -= seconds;

                    if (MinValue.HasValue && Value <= MinValue)
                    {
                        Value = MinValue.Value;
                        Stop();
                        if (TimerFinished != null)
                        {
                            TimerFinished();
                        }
                    }
                }
            }

            public void Reset()
            {
                Value = InitialValue;
            }

            public void Stop()
            {
                Paused = true;
            }

            public void Start()
            {
                Paused = false;
            }

            public void UpdateDisplay()
            {
                if (GuiText != null)
                {
                    GuiText.text = Value.ToString("#.00");
                }
            }

        }

        [Serializable]
        public class SessionParameters
        {
            /// <summary>
            /// Time until the next trial start
            /// </summary>
            public float InterTrialTime;
            /// <summary>
            /// Time to wait before beginning this session (ignored on first session)
            /// </summary>
            public float InterSessionTime;
            /// <summary>
            /// How long you want the session to last (seconds).
            /// </summary>
            public float SessionTime;
            /// <summary>
            /// What type of feedback are we using? Valid options are Default, None, Meaningless, Meaningful
            /// </summary>
            public string FeedbackType;
        }

        public static Experiment Instance;
        public static Action OnSessionEnd;

        public enum ShGameState { Loading, Ready, Playing, InterTrialBreak, InterSessionBreak };

        public ShGameState State;
        public bool PlaySoundOnSessionStart = true;

        public FeedbackMode CurrentFeedbackMode 
        {
            get
            {
                try 
                {
                    return Enum.Parse<FeedbackMode>(Sessions[SessionNumber].FeedbackType);
                } catch
                {
                    return FeedbackMode.None;
                }
                
            }
        }

        private int _trialFrames = 0;
        private bool _newHighScore = false;

        // Current trial and session
        public ShTimer TimerTrial = new ShTimer();
        public ShTimer TimerBest = new ShTimer();
        public ShTimer TimerSession = new ShTimer();

        public ShTimer InterTrialTimer = new ShTimer();
        public ShTimer InterSessionTimer = new ShTimer();

        [Header("Required Game Objects")]
        public TMP_Text Message;
        public TMP_Text Countdown;
        public TMP_Text InterSessionText;
        public AudioSource Alert;
        public AudioSource Success;

        [Header("Experiment Settings (Editor Only)")]
        public bool StartFullScreen;

        public float InterSessionTimeRemaining
        {
            get { return InterSessionTimer.Value; }
            set { InterSessionTimer.Value = value; }
        }

        public List<SessionParameters> Sessions = new List<SessionParameters>();

        [Header("Server Settings (Editor Only)")]
        public string ServerUrl;
        public int ParticipantId = 1;


        [Header("Experiment State")]
        public int SessionNumber = 0;
        public int TrialNumber = 0;
        private float SessionTimeRemaining
        {
            get { return TimerSession.Value; }
            set { TimerSession.Value = value; }
        }
        private float MaxDuration
        {
            get { return TimerBest.Value; }
            set { TimerBest.Value = value; }
        }

        // Variables for logging player actions.
        [Header("Movement Logging")]
        float _timeLastMovementLogged = 0f; // In-game time indicating when movement was last logged
        public float TimeDeltaMovement = 0.150f; // In seconds
        PlayerBehavior _playerControls;
        public int movementKeysPressedThisTrial = 0;

        private TrialLogging _trialLogging = new();

        [SerializeField] private GameObject _explosion;
        [SerializeField] private GameObject _deathCanvas;

        private void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 120;

            PlayerBehavior.OnInputStart += () => 
            {
                movementKeysPressedThisTrial += 1;
            };
        }

        // Use this for initialization
        void Start()
        {
            if (StartFullScreen)
            {
                Screen.fullScreen = true;
            }

            if (!Debug.isDebugBuild)
            {
                ServerUrl = "";
                ParticipantId = 0;
            }

            _playerControls = FindObjectOfType<PlayerBehavior>();
            State = ShGameState.Loading;
            StartCoroutine(LoadSettings(AfterSetttingsLoaded));
        }

        public void OnPlayerDied()
        {
            if (CurrentFeedbackMode == FeedbackMode.Meaningless)
            {
                _deathCanvas.SetActive(true);
                _explosion.SetActive(true);
            }
        }

        public void OnPlayerRespawn()
        {
            if (CurrentFeedbackMode == FeedbackMode.Meaningless)
            {
                _deathCanvas.SetActive(false);
                _explosion.SetActive(false);
            }
            movementKeysPressedThisTrial = 0;
        }

        // Update is called once per frame
        void Update()
        {
#if UNITY_STANDALONE
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
#endif

            float deltaT = Time.deltaTime;

            if (State == ShGameState.Playing)
            {
                _trialFrames++;
                TimerTrial.Update(deltaT);
                TimerSession.Update(deltaT);

                if (TimeDeltaMovement + _timeLastMovementLogged < Time.time)
                {
                    _trialLogging.Movements.Add(new Movement(Time.time - _timeLastMovementLogged, (int)_playerControls.transform.eulerAngles.z));
                    _timeLastMovementLogged = Time.time;
                }

                if (TimerTrial.Value >= TimerBest.Value)
                {  // Has the player reached a new high score?
                    TimerBest.Value = TimerTrial.Value;

                    if (!_newHighScore && TrialNumber > 0)
                    {
                        _newHighScore = true;
                        FindObjectOfType<DisplayMessage>().AddMessageToTop("New high score!", 2f);
                        Success.Play();
                    }
                }

                TimerTrial.UpdateDisplay();
                TimerBest.UpdateDisplay();
            }
            else if (State == ShGameState.InterTrialBreak)
            {
                Message.enabled = true;
                Message.text = "Be ready in...";

                TimerSession.Update(deltaT);
                InterTrialTimer.Update(deltaT);

                Countdown.text = Mathf.CeilToInt(InterTrialTimer.Value).ToString();
            }
            else if (State == ShGameState.InterSessionBreak)
            {
                Message.enabled = true;
                Message.text = "The next session will begin in...";

                if (InterSessionTime() != InterTrialTime() || SessionNumber == 0)
                {
                    InterSessionText.enabled = true;
                }

                InterSessionTimer.Update(deltaT);

                Countdown.text = Mathf.CeilToInt(InterSessionTimer.Value).ToString();
            }

            TimerSession.UpdateDisplay();
        }

        #region Access to session parameters.
        public int TotalSessions()
        {
            return Sessions.Count;
        }

        public float InterTrialTime()
        {
            if (SessionNumber >= Sessions.Count)
            {
                return 0.0f;
            }

            return Sessions[SessionNumber].InterTrialTime;
        }

        public float InterSessionTime()
        {
            if (SessionNumber >= Sessions.Count)
            {
                return 0.0f;
            }

            return Sessions[SessionNumber].InterSessionTime;
        }

        public float SessionTime()
        {
            if (SessionNumber >= Sessions.Count)
            {
                return 0.0f;
            }

            return Sessions[SessionNumber].SessionTime;
        }
        #endregion

        public void EndGame()
        {
            Application.ExternalCall("EndGame"); // Yes this is deprecated. No, there is no other way to do this.
            Time.timeScale = 0f;
        }

        public void EndSession()
        {
            EndSession(false);
        }

        public void EndSession(bool onGameStart)
        {
            TimerTrial.Stop();
            PlayerBehavior.Instance.ForceDeath();
            ThreatManager.Instance.Clear();

            if (!onGameStart)
            {
                _trialLogging.SaveTrial(_trialFrames, this, true);
                TrialNumber++;
                SaveState();
                SessionNumber++;

                InterSessionTimer.Reset();
            }

            InterSessionTimer.Start();

            // Set message in update loop, due to interference with DisplayMessage
            Countdown.enabled = true;

            if (SessionNumber >= TotalSessions())
            {
                State = ShGameState.Loading;
                Message.text = "Thanks for playing!";
                Message.enabled = true;
                Countdown.enabled = false;
                Invoke(nameof(EndGame), 1.0f);
            }
            else
            {
                PlayerBehavior.Instance.ForceDeath();
                State = ShGameState.InterSessionBreak;
            }

            OnSessionEnd?.Invoke(); // Need to reset the level, like when the player dies
        }

        public void StartSession()
        {
            ReloadTimerSettings();

            if (InterSessionTime() != InterTrialTime() || SessionNumber == 0)
            {
                InterSessionText.enabled = false;
                //FindObjectOfType<PressKeyToBegin>().StartPause();
            }

            Alert.Play();

            TimerSession.Reset();
            TimerSession.Start();

            StartTrial();

            //GetComponent<AudioSource>().Play();
        }

        public void EndTrial()
        {
            TimerTrial.Stop();
            _trialLogging.SaveTrial(_trialFrames, this);
            TrialNumber++;
            SaveState();

            PlayerBehavior.Instance.ForceDeath();

            InterTrialTimer.Reset();
            InterTrialTimer.Start();

            // Message is set in update loop, due to interference with DisplayMessage
            Countdown.enabled = true;

            State = ShGameState.InterTrialBreak;
        }

        public void StartTrial()
        {
            State = ShGameState.Ready;

            _trialLogging.Movements = new List<Movement>();
            _timeLastMovementLogged = Time.time;

            Message.enabled = false;
            Countdown.enabled = false;

            TimerTrial.Reset();
            TimerTrial.Start();
            LevelManager.Instance.BeginLevel();

            _trialFrames = 0;
            _newHighScore = false;
        }

        void ReloadTimerSettings()
        {
            if (SessionNumber < Sessions.Count)
            {
                TimerSession.InitialValue = SessionTime();
                InterTrialTimer.InitialValue = InterTrialTime();
                InterSessionTimer.InitialValue = InterSessionTime();
            }
        }

        void AfterSetttingsLoaded()
        {
            ReloadTimerSettings();

            TimerSession.TimerFinished += EndSession;
            TimerSession.MinValue = 0f;
            TimerSession.CountUp = false;
            TimerSession.Reset();

            InterTrialTimer.Stop();
            InterTrialTimer.TimerFinished += StartTrial;
            InterTrialTimer.MinValue = 0f;
            InterTrialTimer.CountUp = false;
            InterTrialTimer.Reset();

            InterSessionTimer.Stop();
            InterSessionTimer.TimerFinished += StartSession;
            InterSessionTimer.TimerTick += SaveState;
            InterSessionTimer.MinValue = 0f;
            InterSessionTimer.TickLength = 5;
            InterSessionTimer.CountUp = false;
            InterSessionTimer.Reset();

            StartCoroutine(LoadState(AfterStateLoaded));
        }

        void AfterStateLoaded()
        {
            //Debug.Log("After State Loaded. interSessionTimeRemaining = " + interSessionTimeRemaining + "; sessionTimeRemaining = " + sessionTimeRemaining);
            if (InterSessionTimeRemaining != 0 && SessionTimeRemaining == 0)
            {
                //FindObjectOfType<PressKeyToBegin>().StopPause();
                EndSession(true);
            }
            else
            {
                //Debug.Log("AfterStateLoaded");
                State = ShGameState.Ready;
            }
        }

    }
}
