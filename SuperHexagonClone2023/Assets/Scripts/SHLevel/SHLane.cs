using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomExtensions;
using UnityEngine.Pool;
using static SHLevel;



//
// Summary:
//     Spawn threats, and removes those threats when needed.
[RequireComponent(typeof(MeshFilter))]
public class SHLane : MonoBehaviour
{

    public int num;

    [Header("Object Pooling Config")]
    public ObjectPool<SHLine> threatPool;
    public SHLine threatPrefab;
    public List<SHLine> activeThreats = new List<SHLine>();
    public int amountToPool;

    // These "Debug" settings are left over from before loading from levels was implemented.
    [Header("Debug Settings")]

    public bool autoStart;
    [Range(0f, 20f)]
    public float autoStartDelay = 5f;
    [Range(0.0166f, 10f)]
    public float autoStartPeriod = 5f;

    [HideInInspector]
     // Added to build the SHSolver


    void OnDisable()
    {
    }

    void Awake()
    {
        threatPool = new ObjectPool<SHLine>(CreateThreat, OnGetThreatFromPool, OnReleaseThreatToPool);

        /*if (autoStart)
        { // For debugging.
            InvokeRepeating(nameof(SpawnThreat), autoStartDelay, autoStartPeriod);
        }*/
        // Throw an error if Pivot is missing
        gameObject.FindRequiredGameObjectWithTag("Pivot");
    }

    //Spawning Functionality

    public SHLine SpawnThreat(float thickness)
    { 
        SHLine threat = threatPool.Get();

        if (threat != null)
        {
            threat.radius = GameParameters.ThreatStartingRadius;

            // Modify the thickness by current difficult level (as objects spawn faster as the game goes on)
            threat.thickness = thickness / FindObjectOfType<AccelerateDifficulty>().spawningDifficulty.GetValue();

            // Reset the position and rotation of the line/threat.
            threat.transform.position = new Vector3(0f, 0f, -0.2f);
            threat.transform.rotation = gameObject.transform.rotation;
        }

        return threat;
    }

    public SHLine CreateThreat()
    {
        if (threatPrefab != null)
        {
            //Instantiate(threatPrefab, this.transform);

            //Pooler
            try
            {
                //GameObject spawned = ObjectPooler.SharedInstance.GetPooledObject("Threat");
                SHLine spawned = Instantiate(threatPrefab, transform);

                //spawned.transform.parent = gameObject.transform;
                spawned.transform.position = new Vector3(0f, 0f, -0.2f);
                spawned.transform.rotation = gameObject.transform.rotation;
                // Modify the thickness by current difficult level (as objects spawn faster as the game goes on)
                //spawned.GetComponent<SHLine>().thickness = thickness / FindObjectOfType<AccelerateDifficulty>().spawningDifficulty.GetValue();

                //threats.Add(spawned);
                return spawned;
            }
            catch (UnityException e)
            {
                Debug.Log(e.StackTrace);
            }
        }
        else
        {
            throw new UnityException("The threat prefab on the lane: " + gameObject.name + " is not set! This will prevent spawning from happening on this lane.");
        }
        return null;
    }

    /// <summary>
    /// Called when we request a threat via Get
    /// </summary>
    /// <param name="retrievedLine"></param>
    private void OnGetThreatFromPool(SHLine retrievedLine)
    {
        retrievedLine.gameObject.SetActive(true);
        activeThreats.Add(retrievedLine);
    }

    /// <summary>
    /// Called when we release a threat via Release
    /// </summary>
    /// <param name="releasedLine"></param>
    private void OnReleaseThreatToPool(SHLine releasedLine)
    {
        releasedLine.gameObject.SetActive(false);
        activeThreats.Remove(releasedLine);
    }


    void Update()
    {
        if (activeThreats.Count == 0)
        {
            return;
        }

        // Use ToArray to avoid having the collection modifed by the loop errors.
        foreach (SHLine threat in activeThreats.ToArray())
        {
            if (threat.RadiusOuter() <= 0)
            {
                threatPool.Release(threat);
            }
        }

        //GameObject nextThreat = threats[0];

        /*if (nextThreat.GetComponent<SHLine>().RadiusOuter() <= 0f)
        {
            threats.RemoveAt(0);
            ObjectPooler.SharedInstance.RecycleObject(nextThreat);
        }*/
    }

    public void ClearThreats()
    {
        // Use ToArray to avoid having the collection modifed by the loop errors.
        foreach (SHLine threat in activeThreats.ToArray()) 
        { 
            threatPool.Release(threat);
        }
        //threatPool.Clear();
        /*GameObject[] threats = GameObject.FindGameObjectsWithTag("Threat");

        foreach (GameObject threat in threats)
        {
            Destroy(threat);
        }
        this.threats = new List<GameObject>();*/
    }

    /*public float GetAngle()
    {
        return gameObject.transform.rotation.eulerAngles.z;
    }*/
}
