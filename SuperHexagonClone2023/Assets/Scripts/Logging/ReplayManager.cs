using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts.LevelBehavior;
using Assets.Scripts.LevelVisuals;
using Assets.Scripts.SHPlayer;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.PlayerLoop;
using Event = UnityEngine.Event;

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
        
        public ReplayEvent(ReplayEventType type)
        {
            Type = type;
            SessionNumber = Experiment.Instance.SessionNumber;
            TrialNumber = Experiment.Instance.TrialNumber;
            TrialTime = Experiment.Instance.TimerTrial.Value;
            GameTime = Time.time;
            PlayerRotation = PlayerBehavior.Instance.GetAngle();
            RotationInput = PlayerBehavior.Instance.Input;
            CameraRotationSpeed = DifficultyManager.Instance.CameraRotationSpeed;
            ThreatSpeed = DifficultyManager.Instance.ThreatSpeed;
            PlayerRotationRate = DifficultyManager.Instance.PlayerRotationRate;
        }
        
        public string ToCsv()
        {
            return $"{Type}|{SessionNumber}|{TrialNumber}|{TrialTime}|{GameTime}|{PlayerRotation}|{RotationInput}|{CameraRotationSpeed}|" +
                   $"{ThreatSpeed}|{PlayerRotationRate}|{PatternName}|{EventRadius}|{PatternOuterRadius}|{PatternInnerRadius}";
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

            PlayerBehavior.OnInputStart += () => AddBasicLog(ReplayEventType.MoveStart);
            PlayerBehavior.OnInputEnd += () => AddBasicLog(ReplayEventType.MoveEnd);

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
            re.PatternOuterRadius = patternInstance.FurthestThreat.RadiusOuter();
            
            _events.Add(re);
        }

        private void AddThreatLog(SHLine threat, ReplayEventType type)
        {
            ReplayEvent re = new(type);
            re.PatternName = threat.AssociatedPatternInstance.Name;
            re.EventRadius = threat.Radius;
            re.PatternInnerRadius = threat.AssociatedPatternInstance.ClosestThreat.Radius;
            re.PatternOuterRadius = threat.AssociatedPatternInstance.FurthestThreat.RadiusOuter();

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
                    "ThreatSpeed|PlayerRotationRate|PatternName|EventRadius|PatternOuterRadius|PatternInnerRadius\n");
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
