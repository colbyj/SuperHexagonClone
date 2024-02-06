using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.Logging;
using Assets.Scripts.SHPlayer;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.LevelBehavior
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }
        public static Action OnFirstBegin;
        private static bool FirstBegun;

        public ParsedLevel Level;
        public string LevelName;

        public bool ShowInstructions = true;
        private Experiment _experiment;

        private bool _levelIsLoaded = false;
        private bool _levelIsActive = false;

        private void Awake()
        {
            Instance = this;

            if (Application.isEditor)
            {
                ShowInstructions = false;
            }

            _experiment = FindObjectOfType<Experiment>();

            PlayerBehavior.OnPlayerDied += (threat) => OnPlayerDied();
            PlayerBehavior.OnPlayerRespawn += OnPlayerRespawn;

            StartCoroutine(ParseLevel());
        }

        private IEnumerator ParseLevel()
        {

#if UNITY_WEBGL //&& !UNITY_EDITOR
            string url = $"{Application.streamingAssetsPath}/Levels/{LevelName}.xml";
            //string url = "http://127.0.0.1:5001/superhexagon/StreamingAssets/Levels/original.xml";
            using UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            string levelXmlStr = www.downloadHandler.text;
            www.Dispose();
#else
            string levelXmlStr = File.ReadAllText($"{Application.streamingAssetsPath}/Levels/{LevelName}.xml");
#endif

            Level = new ParsedLevel();
            Level.ParseLevelXml(levelXmlStr);
            StartCoroutine(nameof(FirstBeginLevel));
            
            yield return null;
            _levelIsLoaded = true;
        }

        private void OnPlayerDied()
        {
            _levelIsActive = false;

            if (Experiment.Instance?.CurrentFeedbackMode == Experiment.FeedbackMode.Meaningless ||
                Experiment.Instance?.CurrentFeedbackMode == Experiment.FeedbackMode.None)
            {
                ThreatManager.Instance.Clear();
            }
        }

        private void OnPlayerRespawn()
        {
            if (Experiment.Instance?.CurrentFeedbackMode != Experiment.FeedbackMode.Meaningless &&
                Experiment.Instance?.CurrentFeedbackMode != Experiment.FeedbackMode.None)
            {
                ThreatManager.Instance.Clear();
            }
        }

        private IEnumerator FirstBeginLevel()
        {
            if (ShowInstructions)
            {
                DisplayMessage displayMessage = FindObjectOfType<DisplayMessage>();
                displayMessage.AddMessage("Move clockwise with the right arrow", 2);
                displayMessage.AddMessage("Move counter-clockwise with the left arrow", 2);
                displayMessage.AddMessage("Avoid the obstacles", 2);
                displayMessage.AddMessage("GO!", 1);

                yield return new WaitForSeconds(8f);
            }
            yield return null;
            BeginLevel();
        }

    
        /// <summary>
        /// Call this every time a new trial begins.
        /// </summary>
        public void BeginLevel()
        {
            if (_experiment != null)
            {
                if (_experiment.State != Experiment.ShGameState.Ready)
                {
                    // We shouldn't be here... Try again in a moment
                    Invoke(nameof(BeginLevel), 0.1f);
                    return;
                }
                _experiment.State = Experiment.ShGameState.Playing;
            }


            PlayerBehavior.IsDead = false;
            //timeLastObstacle = Time.time;
            Level.ResetLevel();
            _levelIsActive = true;

            if (!FirstBegun)
            {
                OnFirstBegin?.Invoke();
                FirstBegun = true;
            }

            Debug.Log("BeginLevel");
        }

        /// <summary>
        /// Stop new threats from spawning.
        /// </summary>
        public void StopLevel()
        {
            PlayerBehavior.IsDead = true;
        }

        public void Update()
        {
            if (!_levelIsLoaded)
                return;

            LevelCommand nextCommand = Level.NextCommand;

            // Try to handle any non-spawning events immediately. 
            while (nextCommand.CommandType != LevelCommandType.SpawnOne && nextCommand.CommandType != LevelCommandType.SpawnGroup)
            {
                if (LevelCommand.FloatCommandEnums.Contains(nextCommand.CommandType))
                {
                    LevelFloatCommand floatCommand = (LevelFloatCommand)nextCommand;
                    switch (nextCommand.CommandType)
                    {
                        case LevelCommandType.RotationDifficulty:
                            DifficultyManager.Instance.RotationDifficultyAccelerator.SetStartingValue(floatCommand.Argument);
                            break;
                        case LevelCommandType.RotationDifficultyRate:
                            DifficultyManager.Instance.RotationDifficultyAccelerator.SetIncreaseBy(floatCommand.Argument);
                            break;
                        case LevelCommandType.RotationDifficultyMax:
                            DifficultyManager.Instance.RotationDifficultyAccelerator.SetCeiling(floatCommand.Argument);
                            break;
                        case LevelCommandType.ThreatSpeed:
                            DifficultyManager.Instance.ThreatDifficultyAccelerator.SetStartingValue(floatCommand.Argument);
                            break;
                        case LevelCommandType.ThreatSpeedRate:
                            DifficultyManager.Instance.ThreatDifficultyAccelerator.SetIncreaseBy(floatCommand.Argument);
                            break;
                        case LevelCommandType.ThreatSpeedMax:
                            DifficultyManager.Instance.ThreatDifficultyAccelerator.SetCeiling(floatCommand.Argument);
                            break;
                        case LevelCommandType.PlayerSpeed:
                            Debug.Log($"Set PlayerRotationRate to {floatCommand.Argument}");
                            DifficultyManager.Instance.PlayerRotationRate = floatCommand.Argument;
                            break;
                        case LevelCommandType.PatternRadiusOffset:
                            DifficultyManager.Instance.PatternRadiusOffset = floatCommand.Argument;
                            break;
                    }
                }
                else if (nextCommand.CommandType == LevelCommandType.CWCamera)
                {
                    ConstantWorldRotation.Instance.Clockwise();
                }
                else if (nextCommand.CommandType == LevelCommandType.CCWCamera)
                {
                    ConstantWorldRotation.Instance.CounterClockwise();
                }

                Level.CommandHandled();
                nextCommand = Level.NextCommand;
            }

            bool needToSpawnThreats =
                ThreatManager.Instance.FurthestThreatRadius < ThreatManager.SpawnPatternsUntilRadius;

            if (_levelIsActive && needToSpawnThreats)
            {
                if (nextCommand.CommandType == LevelCommandType.SpawnOne)
                {
                    if (nextCommand is LevelSpawnOneCommand spawnCommand)
                    {
                        var patternInstance = spawnCommand.GetRandomToSpawn;

                        if (Level.LevelCommands.Last() == nextCommand)
                        {
                            patternInstance.LastBeforeRestart = true;  // Mark this as the last pattern!
                        }

                        ThreatManager.Instance.SpawnLevelPattern(patternInstance);
                    }

                    Level.CommandHandled();
                }
                else if (nextCommand.CommandType == LevelCommandType.SpawnGroup)
                {
                    if (nextCommand is LevelSpawnGroupCommand spawnCommand)
                    {
                        List<PatternInstance> patterns = spawnCommand.GetRandomGroupToSpawn;

                        for (int i = 0; i < patterns.Count; i++) 
                        {
                            if (i == patterns.Count - 1)
                            {
                                patterns[i].LastBeforeRestart = true;  // Mark this as the last pattern!
                            }
                            ThreatManager.Instance.SpawnLevelPattern(patterns[i]);
                        }
                    }

                    Level.CommandHandled();
                }
            }
        }
    }
}
