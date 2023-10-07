using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomExtensions;
using System.Xml;

/* Written by Parker Neufeld under the supervision of Regan Mandryk    
 * Property of The University of Saskatchewan Interaction Lab */


/* Code Reviewed by Parker Neufeld on Sunday May 27th, 2018 */

//
// Summary:
//         This is used to dynamically build the lane game objects.
[RequireComponent(typeof(MeshFilter))]
public class StageConstructor : MonoBehaviour
{

    public enum ColorMode
    {
        GREYSCALE,
        FRUITY,
        DARK,
        COSMIC,
    }

    [Range(3, 20)]
    public static int laneCount = 6; // I think because this is static that it doesn't show up in the inspector
    private int lastZoneCount;

    // TODO: Make use of delegates so that the number of lanes can be 
    // changed on the fly without sending messages to individual objects.
    // For now this is not necessary.

    public GameObject basicLane;
    public Material[] mats = new Material[2];

    //Graphical
    public Color[] laneColors;

    private List<GameObject> lanes = new List<GameObject>();
    private SHLevelManager levelManager;

    void Awake()
    {
        //mesh = gameObject.GetRequiredComponent<MeshFilter>().mesh;
        lastZoneCount = laneCount;
        levelManager = FindObjectOfType<SHLevelManager>();
    }

    void Start()
    {
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


    // Update is called once per frame
    void Update()
    {
        if (lastZoneCount != laneCount)
        {
            CreateLanes();
        }
        lastZoneCount = laneCount;
    }

    //Creates lanes if needed, disables 
    void CreateLanes()
    {
        int lanesNeeded = 0;
        if (lanes.Count != laneCount)
        {
            lanesNeeded = laneCount - lanes.Count;
        }
        //Debug.Log("need" + lanesNeeded);


        levelManager.lanes = new List<SHLane>();

        for (int i = laneCount - lanesNeeded; i < laneCount; i++)
        {
            //Instantiate
            GameObject newLane = Instantiate(basicLane, transform.position, Quaternion.Euler(0, 0, i * 360f / laneCount), transform);
            lanes.Add(newLane);

            levelManager.lanes.Add(newLane.GetComponent<SHLane>());
        }
        ConfigLanes();

    }

    /// <summary>
    /// Adjust rotation and colour of lanes.
    /// </summary>
    void ConfigLanes()
    { // This seems to be called more often than necessary?
        for (int i = 0; i < lanes.Count; i++)
        {
            if (i >= laneCount)
            {
                lanes[i].SetActive(false);
                continue;
            }
            else if (i < laneCount && lanes[i].activeInHierarchy == false)
            {
                lanes[i].SetActive(true);
            }
            //Instantiate
            //GameObject newLane = Instantiate(basicLane, transform.position, Quaternion.Euler(0, 0, i * 360f / zonesCount), transform);
            lanes[i].transform.rotation = Quaternion.Euler(0, 0, i * 360f / laneCount);
            lanes[i].name = "Lane" + i;

            //Improve access modifiers and whatnot
            SHLane laneScript = lanes[i].GetRequiredComponent<SHLane>();
            laneScript.num = i;
            //lanes[i].GetRequiredComponent<MeshFilter>().mesh = mesh;

            Material matToApply = mats[i % mats.Length];
            //matToApply.SetColor("_TintColor", laneColors[i % laneColors.Length]);
            //matToApply.SetColor(i%laneColors.Length,laneColors[i % laneColors.Length]);
            lanes[i].GetRequiredComponent<MeshRenderer>().material = matToApply;
            lanes[i].GetRequiredComponent<MeshRenderer>().material.color = laneColors[i % laneColors.Length];
        }
        //Debug.Log("ConfigLanes");
    }



    public void Reset()
    {
        foreach (GameObject lane in lanes)
        {
            lane.GetRequiredComponent<SHLane>().ClearThreats();
        }

        ConfigLanes();
    }

    private bool HasValidColors()
    {
        return (laneCount % laneColors.Length) != 1;
    }
}
