using SH.LevelScripting;
using System.Collections;
using System.Linq;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public ParsedLevel Level;
    public TextAsset LevelDefinition;

    public bool Started = true;
    public bool ShowInstructions = true;
    private Experiment experiment;

    private void Awake()
    {
        Instance = this;
        experiment = FindObjectOfType<Experiment>();

        Level = new ParsedLevel();
        Level.ParseLevelXml(LevelDefinition.text);
        StartCoroutine(nameof(StartLevel));
    }

    private IEnumerator StartLevel()
    {
        if (ShowInstructions)
        {
            DisplayMessage displayMessage = FindObjectOfType<DisplayMessage>();
            displayMessage.AddMessage("Move clockwise with the right arrow", 2);
            displayMessage.AddMessage("Move counter-clockwise with the left arrow", 2);
            displayMessage.AddMessage("Avoid the obtacles", 2);
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
        if (experiment != null && experiment.state != Experiment.SHGameState.Playing)
        {
            // We shouldn't be here... Try again in a second
            Invoke(nameof(BeginLevel), 1.0f);
            return;
        }

        Started = true;
        //timeLastObstacle = Time.time;
        Level.ResetLevel();
        Debug.Log("BeginLevel");
    }

    /// <summary>
    /// Stop new threats from spawning.
    /// </summary>
    public void StopLevel()
    {
        Started = false;
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

        if (Started && LaneManager.Instance.NeedToSpawnThreats())
        {
            if (nextCommand.CommandType == LevelCommandType.SpawnOne)
            {
                var spawnCommand = nextCommand as LevelSpawnOneCommand;
                LaneManager.Instance.SpawnThreats(spawnCommand.GetRandomToSpawn, LaneManager.Instance.GetThreatSpawnRadius());
                Level.CommandHandled();
            }
            else if (nextCommand.CommandType == LevelCommandType.SpawnGroup)
            {
                var spawnCommand = nextCommand as LevelSpawnGroupCommand;
                var threats = spawnCommand.GetRandomGroupToSpawn;

                foreach (var threat in threats)
                {
                    // Need to update spawn radius each time
                    LaneManager.Instance.SpawnThreats(threat, LaneManager.Instance.GetThreatSpawnRadius());
                }

                Level.CommandHandled();
            }
        }
    }
}
