using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SHResponse
{
    public double startTime;
    public double endTime;
    public double startAngle;
    public double endAngle;
    public bool isClockwise;
}

[Serializable]
public class SHAction
{
    public SHResponse manditoryResponse;
    public SHResponse playerResponse;
    public bool wasSuccessful;

    public SHAction()
    {
        manditoryResponse = new SHResponse();
        playerResponse = new SHResponse();
    }
}


//.timerTrial.value

public class PlayerProgressLog : MonoBehaviour
{
    Experiment experiment;
    SHSBehavior solver;
    SHControls controls;

    void Start()
    {
        experiment = GameObject.FindObjectOfType<Experiment>();
        solver = GameObject.FindObjectOfType<SHSBehavior>();
        controls = GameObject.FindObjectOfType<SHControls>();
    }

    float previousInput;

    public List<SHAction> actions;
    private SHAction currentAction;

    void Update()
    {
        if (controls.input != previousInput)
        { // The user's input has changed!
            if (currentAction != null)
            {
                currentAction.playerResponse.endAngle = controls.GetAngle();
                currentAction.playerResponse.endTime = experiment.timerTrial.value;

                actions.Add(currentAction);
                currentAction = null;
            }
            else if (controls.input != 0)
            {
                currentAction = new SHAction();
                currentAction.playerResponse.isClockwise = controls.input > 0;
                currentAction.playerResponse.startAngle = controls.GetAngle();
                currentAction.playerResponse.startTime = experiment.timerTrial.value;
            }
            previousInput = controls.input;
        }

    }
}