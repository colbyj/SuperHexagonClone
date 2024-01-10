using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomExtensions;
using System.Xml;
using SH.LevelScripting;

/* Written by Parker Neufeld under the supervision of Regan Mandryk    
 * Property of The University of Saskatchewan Interaction Lab */

/// <summary>
/// This is used to dynamically build the lane game objects.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
public class LaneManager : MonoBehaviour
{
    public static LaneManager Instance { get; private set; }

    public enum ColorMode
    {
        GREYSCALE,
        FRUITY,
        DARK,
        COSMIC,
    }

    [Range(3, 20)]
    public static int lanesRequired = 6;

    // TODO: Make use of delegates so that the number of lanes can be 
    // changed on the fly without sending messages to individual objects.
    // For now this is not necessary.

    public SHLane basicLane;
    public Material[] mats = new Material[2];

    //Graphical
    public Color[] laneColors;

    public List<SHLane> Lanes = new List<SHLane>();
    private bool spawnedFirstThreat = false;
    public bool Paused = false;

    void Awake()
    {
        Instance = this;

        if (basicLane == null)
        {
            throw new UnityException("There is no basic lane prefab!!! Can't construct the board.");
        }

        if (!HasValidColors())
        {
            throw new UnityException("The current stage configuration will have two lanes of the same color touching!");
        }
        CreateLanes();
    }

    #region Set-up methods
    private void CreateLanes()
    {
        int lanesNeeded = 0;
        if (Lanes.Count != lanesRequired)
        {
            lanesNeeded = lanesRequired - Lanes.Count;
        }
        //Debug.Log("need" + lanesNeeded);


        //levelManager.Lanes = new List<SHLane>();
        Lanes = new List<SHLane>();

        for (int i = lanesRequired - lanesNeeded; i < lanesRequired; i++)
        {
            //Instantiate
            SHLane newLane = Instantiate(basicLane, transform.position, Quaternion.Euler(0, 0, i * 360f / lanesRequired), transform);
            Lanes.Add(newLane);

            //levelManager.Lanes.Add(newLane.GetComponent<SHLane>());
        }

        ConfigLanes();
    }

    /// <summary>
    /// Adjust rotation and colour of lanes.
    /// </summary>
    void ConfigLanes()
    { // This seems to be called more often than necessary?
        for (int i = 0; i < Lanes.Count; i++)
        {
            SHLane currentLane = Lanes[i];
            GameObject currentLaneGo = currentLane.gameObject;

            if (i >= lanesRequired)
            {
                currentLaneGo.SetActive(false);
                continue;
            }
            else if (i < lanesRequired && currentLaneGo.activeInHierarchy == false)
            {
                currentLaneGo.SetActive(true);
            }
            //Instantiate
            //GameObject newLane = Instantiate(basicLane, transform.position, Quaternion.Euler(0, 0, i * 360f / zonesCount), transform);
            currentLane.transform.rotation = Quaternion.Euler(0, 0, i * 360f / lanesRequired);
            currentLane.name = "Lane" + i;

            //Improve access modifiers and whatnot
            currentLane.num = i;
            //lanes[i].GetRequiredComponent<MeshFilter>().mesh = mesh;

            Material matToApply = mats[i % mats.Length];
            //matToApply.SetColor("_TintColor", laneColors[i % laneColors.Length]);
            //matToApply.SetColor(i%laneColors.Length,laneColors[i % laneColors.Length]);
            currentLaneGo.GetRequiredComponent<MeshRenderer>().material = matToApply;
            currentLaneGo.GetRequiredComponent<MeshRenderer>().material.color = laneColors[i % laneColors.Length];
        }
        ThreatParameters.CurrentStartingRadius = ThreatParameters.FirstThreatRadius;
        spawnedFirstThreat = false;
        //Debug.Log("ConfigLanes");
    }

    private bool HasValidColors()
    {
        return (lanesRequired % laneColors.Length) != 1;
    }

    public void ResetLanes()
    {
        foreach (SHLane lane in Lanes)
        {
            lane.ClearThreats();
        }

        ConfigLanes();
    }
    #endregion

    public void SpawnThreats(LevelPattern nextPattern, float atRadius)
    {
        SpawnThreats(nextPattern.Pattern, atRadius, nextPattern.RotationOffset, nextPattern.RotationOffset, nextPattern.Mirrored);
    }

    public void SpawnThreats(Pattern nextPattern, float atRadius, int rotationOffset = 0, int distanceOffset = 0, bool mirrored = false)
    {
        //Debug.Log("Spawning threat: " + nextPattern.Pattern.FileName);

        foreach (var wall in nextPattern.Walls)
        {
            int sideIndex = (wall.Side + rotationOffset) % lanesRequired;
            if (mirrored)
            {
                sideIndex = lanesRequired - 1 - sideIndex;
            }

            float wallDistance = atRadius + wall.Distance + distanceOffset;

            Lanes[sideIndex].SpawnThreat(wall.Height, wallDistance);
        }

        if (!spawnedFirstThreat)
        {
            spawnedFirstThreat = true;
            ThreatParameters.CurrentStartingRadius = ThreatParameters.StartingRadius;
        }

        /*if (obstacle.Rot)
        {
            worldRotation.LevelEnducedFlip();
        }*/
    }

    public bool NeedToSpawnThreats()
    {
        float furthestThreat = 0.0f;

        for (int i = 0; i < Lanes.Count; i++)
        {
            if (Lanes[i].GetFurthestThreat() > furthestThreat)
            {
                furthestThreat = Lanes[i].GetFurthestThreat();
            }
        }

        if (furthestThreat < (ThreatParameters.StartingRadius - DifficultyManager.Instance.PatternRadiusOffset))
            return true;
        return false;
    }

    public float GetThreatSpawnRadius()
    {
        float furthestThreat = 0.0f;

        for (int i = 0; i < Lanes.Count; i++)
        {
            if (Lanes[i].GetFurthestThreat() > furthestThreat)
            {
                furthestThreat = Lanes[i].GetFurthestThreat();
            }
        }

        if (furthestThreat < ThreatParameters.FirstThreatRadius)
        {
            return ThreatParameters.CurrentStartingRadius + DifficultyManager.Instance.PatternRadiusOffset;
        }

        return furthestThreat + DifficultyManager.Instance.PatternRadiusOffset;
    }
}
