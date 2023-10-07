
using UnityEngine;
using System.Collections;
using CustomExtensions;
using System.Collections.Generic;
using System;

/// <summary>
/// Handles the loading of the level from a text file, and the spawning of threats.
/// </summary>
public class SHLevelManager : MonoBehaviour
{
    public bool showInstructions = true;
    public SHLevel level;
    private Experiment experiment;
    [HideInInspector] public List<SHLane> lanes; // This is set by FindLanesAndBegin()
    [HideInInspector] public ConstantWorldRotation worldRotation; // This is set by Awake()
    [HideInInspector] public AccelerateDifficulty diffSettings; // This is set by Awake()
#pragma warning disable 0649 // Hide "Is never assigned to" warning
    [SerializeField] TextAsset levelDefinition; // Set in editor.
#pragma warning restore 0649

    // Level Status
    public bool started = false;
    private int idxOutermostObstacle = 0;
    private float timeLastObstacle;

    private void Awake()
    {
        if (levelDefinition)
        {
            level.Load(levelDefinition.text);
        }
        worldRotation = gameObject.FindRequiredGameObjectWithTag("MainCamera").GetRequiredComponent<ConstantWorldRotation>();
        diffSettings = gameObject.GetRequiredComponent<AccelerateDifficulty>();
        experiment = FindObjectOfType<Experiment>();
        //DontDestroyOnLoad(gameObject);

        if (showInstructions)
        {
            FindObjectOfType<DisplayMessage>().AddMessage("Move clockwise with the right arrow", 2);
            FindObjectOfType<DisplayMessage>().AddMessage("Move counter-clockwise with the left arrow", 2);
            FindObjectOfType<DisplayMessage>().AddMessage("Avoid the obtacles", 2);
            FindObjectOfType<DisplayMessage>().AddMessage("GO!", 1);

            Invoke("BeginLevel", 7);
        }
        else
        {
            BeginLevel();
        }
    }


    /// <summary>
    /// Call this every time a new trial begins.
    /// </summary>
    public void BeginLevel()
    {
        if (experiment.state != Experiment.SHGameState.Playing)
        {
            // We shouldn't be here... Try again in a second
            Invoke("BeginLevel", 1.0f);
            return;
        }
        started = true;
        timeLastObstacle = Time.time;
        idxOutermostObstacle = 0;
        Debug.Log("BeginLevel");
    }

    /// <summary>
    /// Stop new threats from spawning.
    /// </summary>
    public void StopLevel()
    {
        started = false;
    }

    private void Update()
    {
        if (!started) return;

        SHLevel.Obstacle nextObstacle = level.GetObstacleAt(idxOutermostObstacle);

        if (nextObstacle == null)
        {
            Debug.Log("The level is over!");
            BeginLevel(); // Restart the level
            return;
        }

        float timeDelta = Time.time - timeLastObstacle;

        // The nextObstacle delta is modified by the difficulty level
        float currentDifficulty = diffSettings.GetSpawningDifficulty();
        float modifiedDelta = nextObstacle.Delta / currentDifficulty;

        if (timeDelta >= modifiedDelta)
        {
            timeLastObstacle = Time.time;
            SpawnThreats(nextObstacle);
            idxOutermostObstacle++;
        }
    }

    public void SpawnThreats(SHLevel.Obstacle obstacle)
    {
        Debug.Log("Spawning threats");
        for (int i = 0; i < lanes.Count; i++)
        {
            if (obstacle.Lane[i]) 
            {
                SHLine threat = lanes[i].SpawnThreat(obstacle.Thickness);
            }
        }

        if (obstacle.Rot)
        {
            worldRotation.LevelEnducedFlip();
        }
    }

    public void ClearThreats()
    {
        for (int i = 0; i < lanes.Count; i++)
        {
            lanes[i].ClearThreats();
        }
    }
}