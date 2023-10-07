using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Playback : MonoBehaviour
{

    public string movementsCSV;
    private string movementsCSVDecrypted;
    public bool movementsIsCompressed = true;

    private List<Experiment.Movement> movements;
    private float startTime = 0f;

    private SHLevelManager level;
    private SHControls controls;
    private SHPlayer player;

    // Use this for initialization
    void Start()
    {
        if (movementsIsCompressed)
        {
            movementsCSVDecrypted = StringCompressor.DecompressString(movementsCSV);
        }
        else
        {
            movementsCSVDecrypted = movementsCSV;
        }

        movements = new List<Experiment.Movement>();

        string[] movementsCSVSplit = movementsCSVDecrypted.Split(';');

        for (int i = 0; i < movementsCSVSplit.Length; i++)
        {
            if (movementsCSVSplit[i].Length == 0) continue;
            movements.Add(new Experiment.Movement(movementsCSVSplit[i]));
        }

        //movements = Array.ConvertAll(movementsCSV.Split(','), s => int.Parse(s));

        level = FindObjectOfType<SHLevelManager>();
        controls = FindObjectOfType<SHControls>();
        player = FindObjectOfType<SHPlayer>();

        player.GetComponent<CircleCollider2D>().enabled = false;
    }

    // Update is called once per frame
    void Update()
    {

        if (!level.started) return;

        if (startTime == 0f) startTime = Time.time;

        //Debug.Log(Time.deltaTime);


        // Iterate through the log and stop when we've found the index we should be on now.
        float indexTimeSum = 0f;
        int index = 0;

        for (int i = 0; i < movements.Count; i++)
        {
            indexTimeSum += movements[i].deltaTime;

            if (indexTimeSum > Time.time - startTime)
            {
                index = i;
                break;
            }
        }

        // Make sure we aren't at the end of the data
        if (index + 1 >= movements.Count)
        {
            FindObjectOfType<DisplayMessage>().AddMessage("It's over", 2.0f);
            enabled = false;
            return;
        }

        float currentRotation = movements[index].rotation;
        float nextRotation = movements[index + 1].rotation;

        float timeSinceCurrent = indexTimeSum - (Time.time - startTime);
        float progress = (movements[index + 1].deltaTime - timeSinceCurrent) / movements[index + 1].deltaTime;
        float lerpedRotation = Mathf.LerpAngle(currentRotation, nextRotation, progress);

        //Debug.Log("indexTimeSum = " + indexTimeSum + ", timeSinceCurrent = " + timeSinceCurrent + ", progress = " + progress);

        Quaternion currentQuat = Quaternion.Euler(new Vector3(0f, 0f, lerpedRotation));
        controls.transform.SetPositionAndRotation(Vector3.zero, currentQuat);

    }
}
