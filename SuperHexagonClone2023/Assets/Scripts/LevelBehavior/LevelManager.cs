using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.Logging;
using Assets.Scripts.SHPlayer;
using UnityEngine;

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

        private bool _levelIsActive = false;
        private bool _canSpawnThreats = false;

        private void Awake()
        {
            Instance = this;
            _experiment = FindObjectOfType<Experiment>();

            string levelXmlStr = File.ReadAllText($"{Application.streamingAssetsPath}/Levels/{LevelName}.xml");

            Level = new ParsedLevel();
            Level.ParseLevelXml(levelXmlStr);
            StartCoroutine(nameof(FirstBeginLevel));

            PlayerBehavior.OnPlayerDied += OnPlayerDied;
            PlayerBehavior.OnPlayerRespawn += OnPlayerRespawn;
        }

        private void OnPlayerDied()
        {
            _levelIsActive = false;
        }

        private void OnPlayerRespawn()
        {
            ThreatManager.Instance.Clear();
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
            if (_experiment != null && _experiment.State != Experiment.ShGameState.Playing)
            {
                // We shouldn't be here... Try again in a moment
                Invoke(nameof(BeginLevel), 0.1f);
                return;
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
