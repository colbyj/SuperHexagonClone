using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.SHPlayer;
using UnityEngine;

public class Playback : MonoBehaviour
{

    public string MovementsCsv;
    private string _movementsCsvDecrypted;
    public bool MovementsIsCompressed = true;

    private List<Experiment.Movement> _movements;
    private float _startTime = 0f;

    private PlayerBehavior _controls;
    private PlayerPolygon _playerPolygon;

    // Use this for initialization
    void Start()
    {
        if (MovementsIsCompressed)
        {
            _movementsCsvDecrypted = StringCompressor.DecompressString(MovementsCsv);
        }
        else
        {
            _movementsCsvDecrypted = MovementsCsv;
        }

        _movements = new List<Experiment.Movement>();

        string[] movementsCsvSplit = _movementsCsvDecrypted.Split(';');

        for (int i = 0; i < movementsCsvSplit.Length; i++)
        {
            if (movementsCsvSplit[i].Length == 0)
            {
                continue;
            }

            _movements.Add(new Experiment.Movement(movementsCsvSplit[i]));
        }

        //movements = Array.ConvertAll(movementsCSV.Split(','), s => int.Parse(s));

        _controls = FindObjectOfType<PlayerBehavior>();
        _playerPolygon = FindObjectOfType<PlayerPolygon>();

        _playerPolygon.GetComponent<CircleCollider2D>().enabled = false;
    }

    // Update is called once per frame
    void Update()
    {

        if (!PlayerBehavior.IsDead)
        {
            return;
        }

        if (_startTime == 0f)
        {
            _startTime = Time.time;
        }

        //Debug.Log(Time.deltaTime);


        // Iterate through the log and stop when we've found the index we should be on now.
        float indexTimeSum = 0f;
        int index = 0;

        for (int i = 0; i < _movements.Count; i++)
        {
            indexTimeSum += _movements[i].DeltaTime;

            if (indexTimeSum > Time.time - _startTime)
            {
                index = i;
                break;
            }
        }

        // Make sure we aren't at the end of the data
        if (index + 1 >= _movements.Count)
        {
            FindObjectOfType<DisplayMessage>().AddMessage("It's over", 2.0f);
            enabled = false;
            return;
        }

        float currentRotation = _movements[index].Rotation;
        float nextRotation = _movements[index + 1].Rotation;

        float timeSinceCurrent = indexTimeSum - (Time.time - _startTime);
        float progress = (_movements[index + 1].DeltaTime - timeSinceCurrent) / _movements[index + 1].DeltaTime;
        float lerpedRotation = Mathf.LerpAngle(currentRotation, nextRotation, progress);

        //Debug.Log("indexTimeSum = " + indexTimeSum + ", timeSinceCurrent = " + timeSinceCurrent + ", progress = " + progress);

        Quaternion currentQuat = Quaternion.Euler(new Vector3(0f, 0f, lerpedRotation));
        _controls.transform.SetPositionAndRotation(Vector3.zero, currentQuat);

    }
}
