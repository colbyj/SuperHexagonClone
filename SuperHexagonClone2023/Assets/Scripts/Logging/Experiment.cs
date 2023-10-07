using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using UnityEngine.UI;


public partial class Experiment : MonoBehaviour
{

    public delegate void TimerDelegate();

    [Serializable]
    public class SHTimer
    {
        public Text guiText;
        public float value;
        public float initialValue = 0f; // What the timer gets set to on Reset()
        public bool paused = false;
        public bool countUp = true;
        public float? maxValue = null;
        public float? minValue = null;
        public int tickLength = 0; // Used for logging between sessions

        public event TimerDelegate TimerFinished;
        public event TimerDelegate TimerTick;

        public void Update(float seconds)
        {
            if (paused)
            {
                return;
            }

            if (countUp)
            {
                // Because this is on Update(), only check this when we are crossing to a new integer (e.g., 4.99 to 5.00).
                if ((int)value != (int)(value + seconds) && tickLength != 0 && (int)value % tickLength == 0)
                {
                    if (TimerTick != null)
                    {
                        TimerTick();
                    }
                }

                value += seconds;

                if (maxValue.HasValue && value >= maxValue)
                {
                    value = maxValue.Value;
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
                if ((int)value != (int)(value - seconds) && tickLength != 0 && (int)value % tickLength == 0)
                {
                    if (TimerTick != null)
                    {
                        TimerTick();
                    }
                }

                value -= seconds;

                if (minValue.HasValue && value <= minValue)
                {
                    value = minValue.Value;
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
            value = initialValue;
        }

        public void Stop()
        {
            paused = true;
        }

        public void Start()
        {
            paused = false;
        }

        public void UpdateDisplay()
        {
            if (guiText != null)
            {
                guiText.text = value.ToString("#.00");
            }
        }

    }

    public class Movement
    {
        public float deltaTime;
        public int rotation;

        public Movement(float deltaTime, int rotation)
        {
            this.deltaTime = deltaTime;
            this.rotation = rotation;
        }

        public Movement(string csv)
        {
            if (csv.EndsWith(";"))
            {
                csv = csv.Remove(csv.Length, 1); // Remove trailing delimiter.
            }
            string[] movementParts = csv.Split(',');
            this.deltaTime = float.Parse(movementParts[0]);
            this.rotation = int.Parse(movementParts[1]);
        }

        public string CSV()
        {
            return string.Format("{0},{1};", deltaTime, rotation);
        }
    }

    [Serializable]
    public class SessionParameters
    {
        /// <summary>
        /// Time until the next trial start
        /// </summary>
        public float interTrialTime;
        /// <summary>
        /// Time to wait before beginning this session (ignored on first session)
        /// </summary>
        public float interSessionTime;
        /// <summary>
        /// How long you want the session to last (seconds).
        /// </summary>
        public float sessionTime;
    }

    public enum SHGameState { Loading, Playing, InterTrialBreak, InterSessionBreak };

    public SHGameState state;
    public bool playSoundOnSessionStart = true;

    private int trialFrames = 0;
    private bool newHighScore = false;

    public SHLevelManager levelManager;
    //public StageConstructor stageConstructor;

    // consider a "SHTimer" class with value, text, direction

    // Current trial and session
    public SHTimer timerTrial = new SHTimer();
    public SHTimer timerBest = new SHTimer();
    public SHTimer timerSession = new SHTimer();

    public SHTimer interTrialTimer = new SHTimer();
    public SHTimer interSessionTimer = new SHTimer();

    [Header("Required Game Objects")]
    public Text message;
    public Text countdown;
    public Text interSessionText;
    public AudioSource alert;

    [Header("Experiment Settings (Editor Only)")]
    public bool startFullScreen;

    public float interSessionTimeRemaining
    {
        get { return interSessionTimer.value; }
        set { interSessionTimer.value = value; }
    }

    public List<SessionParameters> sessions = new List<SessionParameters>();

    [Header("Server Settings (Editor Only)")]
    public string serverUrl;
    public int participantID = 1;


    [Header("Experiment State")]
    public int sessionNumber = 0;
    public int trialNumber = 0;
    private float sessionTimeRemaining
    {
        get { return timerSession.value; }
        set { timerSession.value = value; }
    }
    private float maxDuration
    {
        get { return timerBest.value; }
        set { timerBest.value = value; }
    }

    // Variables for logging player actions.
    [Header("Movement Logging")]
    List<Movement> movements = new List<Movement>();
    float timeLastMovementLogged = 0f; // In-game time indicating when movement was last logged
    public float timeDeltaMovement = 0.150f; // In seconds
    SHControls playerControls;

    private void Awake()
    {
        Application.targetFrameRate = 120;
    }

    // Use this for initialization
    void Start()
    {
        if (startFullScreen)
        {
            Screen.fullScreen = true;
        }

        if (!Debug.isDebugBuild)
        {
            serverUrl = "";
            participantID = 0;
        }

        playerControls = FindObjectOfType<SHControls>();
        state = SHGameState.Loading;
        StartCoroutine(LoadSettings(AfterSetttingsLoaded));
    }

    // Update is called once per frame
    void Update()
    {
        float deltaT = Time.deltaTime;

        if (state == SHGameState.Playing)
        {
            if (!levelManager.started) return;

            trialFrames++;
            timerTrial.Update(deltaT);
            timerSession.Update(deltaT);

            if (timeDeltaMovement + timeLastMovementLogged < Time.time)
            {
                movements.Add(new Movement(Time.time - timeLastMovementLogged, (int)playerControls.transform.eulerAngles.z));
                timeLastMovementLogged = Time.time;
            }

            if (timerTrial.value >= timerBest.value)
            {  // Has the player reached a new high score?
                timerBest.value = timerTrial.value;

                if (!newHighScore && trialNumber > 0)
                {
                    newHighScore = true;
                    FindObjectOfType<DisplayMessage>().AddMessageToTop("New high score!", 2f);
                }
            }

            timerTrial.UpdateDisplay();
            timerBest.UpdateDisplay();
        }
        else if (state == SHGameState.InterTrialBreak)
        {
            message.enabled = true;
            message.text = "Be ready in...";

            timerSession.Update(deltaT);
            interTrialTimer.Update(deltaT);

            countdown.text = Mathf.CeilToInt(interTrialTimer.value).ToString();
        }
        else if (state == SHGameState.InterSessionBreak)
        {
            message.enabled = true;
            message.text = "The next session will begin in...";

            if (InterSessionTime() != InterTrialTime() || sessionNumber == 0)
            {
                interSessionText.enabled = true;
            }

            interSessionTimer.Update(deltaT);

            countdown.text = Mathf.CeilToInt(interSessionTimer.value).ToString();
        }

        timerSession.UpdateDisplay();
    }

    #region Access to session parameters.
    public int TotalSessions()
    {
        return sessions.Count;
    }

    public float InterTrialTime()
    {
        if (sessionNumber >= sessions.Count) return 0.0f;
        return sessions[sessionNumber].interTrialTime;
    }

    public float InterSessionTime()
    {
        if (sessionNumber >= sessions.Count) return 0.0f;
        return sessions[sessionNumber].interSessionTime;
    }

    public float SessionTime()
    {
        if (sessionNumber >= sessions.Count) return 0.0f;
        return sessions[sessionNumber].sessionTime;
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
        timerTrial.Stop();
        levelManager.ClearThreats();
        levelManager.StopLevel();

        if (!onGameStart)
        {
            SaveTrial(true);
            trialNumber++;
            SaveState();
            sessionNumber++;

            interSessionTimer.Reset();
        }

        interSessionTimer.Start();

        // Set message in update loop, due to interference with DisplayMessage
        countdown.enabled = true;

        levelManager.started = false;

        if (sessionNumber >= TotalSessions())
        {
            state = SHGameState.Loading;
            message.text = "Thanks for playing!";
            message.enabled = true;
            countdown.enabled = false;
            Invoke("EndGame", 1.0f);
        }
        else
        {
            state = SHGameState.InterSessionBreak;
        }
    }

    public void StartSession()
    {
        ReloadTimerSettings();

        if (InterSessionTime() != InterTrialTime() || sessionNumber == 0)
        {
            interSessionText.enabled = false;
            FindObjectOfType<PressKeyToBegin>().StartPause();
        }

        alert.Play();

        timerSession.Reset();
        timerSession.Start();

        StartTrial();

        //GetComponent<AudioSource>().Play();
    }

    public void EndTrial()
    {
        timerTrial.Stop();
        levelManager.ClearThreats();
        levelManager.StopLevel();

        SaveTrial();
        trialNumber++;
        SaveState();

        interTrialTimer.Reset();
        interTrialTimer.Start();

        // Message is set in update loop, due to interference with DisplayMessage
        countdown.enabled = true;

        state = SHGameState.InterTrialBreak;
    }

    public void StartTrial()
    {
        if (state != SHGameState.Playing)
        {
            state = SHGameState.Playing;
        }

        movements = new List<Movement>();
        timeLastMovementLogged = Time.time;

        message.enabled = false;
        countdown.enabled = false;

        timerTrial.Reset();
        timerTrial.Start();
        levelManager.diffSettings.ResetDifficulty();
        levelManager.BeginLevel();

        trialFrames = 0;
        newHighScore = false;
    }

    void ReloadTimerSettings()
    {
        if (sessionNumber < sessions.Count)
        {
            timerSession.initialValue = SessionTime();
            interTrialTimer.initialValue = InterTrialTime();
            interSessionTimer.initialValue = InterSessionTime();
        }
    }

    void AfterSetttingsLoaded()
    {
        ReloadTimerSettings();

        timerSession.TimerFinished += EndSession;
        timerSession.minValue = 0f;
        timerSession.countUp = false;
        timerSession.Reset();

        interTrialTimer.Stop();
        interTrialTimer.TimerFinished += StartTrial;
        interTrialTimer.minValue = 0f;
        interTrialTimer.countUp = false;
        interTrialTimer.Reset();

        interSessionTimer.Stop();
        interSessionTimer.TimerFinished += StartSession;
        interSessionTimer.TimerTick += SaveState;
        interSessionTimer.minValue = 0f;
        interSessionTimer.tickLength = 5;
        interSessionTimer.countUp = false;
        interSessionTimer.Reset();

        StartCoroutine(LoadState(AfterStateLoaded));
    }

    void AfterStateLoaded()
    {
        //Debug.Log("After State Loaded. interSessionTimeRemaining = " + interSessionTimeRemaining + "; sessionTimeRemaining = " + sessionTimeRemaining);
        if (interSessionTimeRemaining != 0 && sessionTimeRemaining == 0)
        {
            FindObjectOfType<PressKeyToBegin>().StopPause();
            EndSession(true);
        }
        else
        {
            //Debug.Log("AfterStateLoaded");
            state = SHGameState.Playing;
        }
    }

}
