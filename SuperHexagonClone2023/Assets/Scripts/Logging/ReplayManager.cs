using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts.LevelBehavior;
using Assets.Scripts.LevelVisuals;
using Assets.Scripts.SHPlayer;
using Assets.Scripts.Solver;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.PlayerLoop;

namespace Assets.Scripts.Logging
{
    public enum ReplayEventType
    {
        TouchedPattern, 
        PatternSpawned, 
        CheckpointTrigger,
        PatternIsPastPlayer, 
        PatternIsAtPlayer, 
        PatternIsOffScreen,
        MoveStart,
        MoveEnd,
        SolverTriggersChanged,
        SessionEnded
    }

    [Serializable]
    public class ReplayEvent
    {
        public ReplayEventType Type;
        public int SessionNumber;
        public int TrialNumber;
        public float TrialTime;
        public float GameTime;
        public float PlayerRotation;
        public float RotationInput;
        public float CameraRotationSpeed;
        public float ThreatSpeed;
        public float PlayerRotationRate;
        public string PatternName;
        public float EventRadius;
        public float PatternOuterRadius;
        public float PatternInnerRadius;
        public float ThreatAngularPosition;
        public List<float> NextTriggerAngles;
        public bool CanMoveCw;
        public bool CanMoveCcw;
        public bool IsTriggerAlignedWithPlayer;
        public float ClosestCwTriggerAngle;
        public float ClosestCcwTriggerAngle;
        public Solver.MovementOption BestMovementOption = Solver.MovementOption.None;

        public ReplayEvent(ReplayEventType type)
        {
            Type = type;
            SessionNumber = Experiment.Instance.SessionNumber;
            TrialNumber = Experiment.Instance.TrialNumber;
            TrialTime = Experiment.Instance.TimerTrial.Value;
            GameTime = Time.time;
            PlayerRotation = PlayerBehavior.Instance.CurrentAngle;
            RotationInput = PlayerBehavior.Instance.Input;
            CameraRotationSpeed = DifficultyManager.Instance.CameraRotationSpeed;
            ThreatSpeed = DifficultyManager.Instance.ThreatSpeed;
            PlayerRotationRate = DifficultyManager.Instance.PlayerRotationRate;

            if (ThreatManager.Instance.PatternsOnScreen.Count > 0)
            {
                PatternInstance pi = ThreatManager.Instance.PatternsOnScreen[0];
                PatternName = pi.Name;
                PatternInnerRadius = pi.ClosestThreat.Radius;
                PatternOuterRadius = pi.FurthestThreat.RadiusOuter;
            }

            if (HexagonSolver.Instance != null)
            {
                NextTriggerAngles = new List<float>(HexagonSolver.Instance.NextTriggers.Count);

                foreach (SHLine trigger in HexagonSolver.Instance.NextTriggers)
                {
                    NextTriggerAngles.Add(trigger.Angle);
                }
                CanMoveCw = HexagonSolver.Instance.CanMoveCw;
                CanMoveCcw = HexagonSolver.Instance.CanMoveCcw;
                IsTriggerAlignedWithPlayer = HexagonSolver.Instance.TriggerAlignedWithPlayer != null;
                ClosestCwTriggerAngle = HexagonSolver.Instance.ClosestCwTriggerAngle;
                ClosestCcwTriggerAngle = HexagonSolver.Instance.ClosestCcwTriggerAngle;
                BestMovementOption = HexagonSolver.Instance.BestMovementOption;
            }
        }
        
        public string ToCsv()
        {
            string nextTriggerAngles = string.Join(';', NextTriggerAngles);

            return Type + "|" +
                   SessionNumber + "|" +
                   TrialNumber + "|" +
                   TrialTime.ToString("#.0000") + "|" +
                   GameTime.ToString("#.0000") + "|" +
                   PlayerRotation.ToString("#.0000") + "|" +
                   RotationInput.ToString("#.0000") + "|" +
                   CameraRotationSpeed.ToString("#.0000") + "|" +
                   ThreatSpeed.ToString("#.0000") + "|" +
                   PlayerRotationRate.ToString("#.0000") + "|" +
                   PatternName + "|" +
                   EventRadius.ToString("#.0000") + "|" +
                   PatternOuterRadius.ToString("#.0000") + "|" +
                   PatternInnerRadius.ToString("#.0000") + "|" +
                   ThreatAngularPosition.ToString("#.0000") + "|" +
                   nextTriggerAngles + "|" +
                   CanMoveCw + "|" +
                   CanMoveCcw + "|" +
                   IsTriggerAlignedWithPlayer + "|" +
                   ClosestCwTriggerAngle.ToString("#.0000") + "|" +
                   ClosestCcwTriggerAngle.ToString("#.0000") + "|" +
                   BestMovementOption;

            //return $"{Type}|{SessionNumber}|{TrialNumber}|{TrialTime}|{GameTime}|{PlayerRotation}|{RotationInput}|{CameraRotationSpeed}|" +
            //       $"{ThreatSpeed}|{PlayerRotationRate}|{PatternName}|{EventRadius}|{PatternOuterRadius}|{PatternInnerRadius}|{ThreatAngularPosition}|" +
            //       $"{nextTriggerAngles}|{CanMoveCw}|{CanMoveCcw}|{IsTriggerAlignedWithPlayer}|{ClosestCwTriggerAngle}|{ClosestCcwTriggerAngle}|{BestMovementOption}";
        }
    }

    public class ReplayManager : MonoBehaviour
    {
        public static ReplayManager Instance;
        [SerializeField]
        private List<ReplayEvent> _events;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            ThreatManager.Instance.PatternIsPastPlayer += (PatternInstance pi) =>
                AddPatternInstanceLog(pi, ReplayEventType.PatternIsPastPlayer);

            ThreatManager.Instance.PatternIsOffScreen += (PatternInstance pi) =>
                AddPatternInstanceLog(pi, ReplayEventType.PatternIsOffScreen);

            ThreatManager.Instance.PatternIsAtPlayer += (PatternInstance pi) =>
                AddPatternInstanceLog(pi, ReplayEventType.PatternIsAtPlayer);

            ThreatManager.Instance.PatternHasSpawned += (PatternInstance pi) =>
                AddPatternInstanceLog(pi, ReplayEventType.PatternSpawned);

            PlayerBehavior.OnPlayerDied +=
                (SHLine threat) =>
                {
                    AddThreatLog(threat, ReplayEventType.TouchedPattern);
                    SaveEvents();
                };

            PlayerBehavior.OnCheckpointTrigger +=
                (SHLine trigger) =>
                {
                    AddThreatLog(trigger, ReplayEventType.CheckpointTrigger);
                };

            Experiment.OnSessionEnd += 
                () =>
                {
                    AddBasicLog(ReplayEventType.SessionEnded);
                    SaveEvents();
                };

            PlayerBehavior.OnInputStart += () => AddBasicLog(ReplayEventType.MoveStart);
            PlayerBehavior.OnInputEnd += () => AddBasicLog(ReplayEventType.MoveEnd);

            HexagonSolver.OnNextTriggersChanged += (pi) =>
                AddPatternInstanceLog(pi, ReplayEventType.SolverTriggersChanged);

            //PlayerBehavior.OnInputStart += 
        }

        private void AddBasicLog(ReplayEventType type)
        {
            _events.Add(new ReplayEvent(type));
        }

        private void AddPatternInstanceLog(PatternInstance patternInstance, ReplayEventType type)
        {
            ReplayEvent re = new(type);
            re.PatternName = patternInstance.Name;
            re.PatternInnerRadius = patternInstance.ClosestThreat.Radius;
            re.PatternOuterRadius = patternInstance.FurthestThreat.RadiusOuter;
            
            _events.Add(re);
        }

        private void AddThreatLog(SHLine threat, ReplayEventType type)
        {
            ReplayEvent re = new(type);
            re.PatternName = threat.AssociatedPatternInstance.Name;
            re.EventRadius = threat.Radius;
            re.PatternInnerRadius = threat.AssociatedPatternInstance.ClosestThreat.Radius;
            re.PatternOuterRadius = threat.AssociatedPatternInstance.FurthestThreat.RadiusOuter;
            re.ThreatAngularPosition = threat.Angle;

            if (_events.Count() > 0)
            {
                ReplayEvent lastEvent = _events[_events.Count() - 1];

                if (!(re.Type == lastEvent.Type && re.GameTime == lastEvent.GameTime))
                    _events.Add(re); // Only add the event if it wasn't just logged
            }
            else 
            {
                _events.Add(re);
            }
        }

        private void SaveEvents()
        {
#if UNITY_WEBGL
            string allEvents = "";
            foreach (var replayEvent in _events)
            {
                allEvents += replayEvent.ToCsv() + "\n";
            }

            var data = new List<IMultipartFormSection> {
                new MultipartFormFileSection("replay", Encoding.UTF8.GetBytes(allEvents), "replay.csv", "text/plain")
            };

            StartCoroutine(PostReplayEvents(data));

#elif UNITY_STANDALONE
            string logFile = Path.Combine(Application.streamingAssetsPath, "event_log.csv");

            if (!File.Exists(logFile))
            {
                using var file = File.Create(logFile);
                file.Close();

                File.AppendAllText(logFile,
                    "Type|SessionNumber|TrialNumber|TrialTime|GameTime|PlayerRotation|RotationInput|CameraRotationSpeed|"+
                    "ThreatSpeed|PlayerRotationRate|PatternName|EventRadius|PatternOuterRadius|PatternInnerRadius|ThreatAngularPosition\n");
            }

            List<string> lines = new();

            foreach (ReplayEvent logEvent in _events)
            {
                lines.Add(logEvent.ToCsv());
            }

            try
            {
                File.AppendAllLines(logFile, lines);
            }
            catch (Exception ex)
            {
                Debug.Log("Could not save events! Exception was: " + ex);
            }
#endif

            _events.Clear();
        }

        private IEnumerator PostReplayEvents(List<IMultipartFormSection> data)
        {
            using UnityWebRequest request =
                UnityWebRequest.Post(Experiment.Instance.ServerUrl + "/sh_post_replay", data);
            yield return request.SendWebRequest();
            request.Dispose();
        }
    }
}
