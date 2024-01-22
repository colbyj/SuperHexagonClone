using System;
using System.Collections.Generic;
using Assets.Scripts.SHPlayer;
using UnityEngine;

namespace Assets.Scripts.Logging
{
    [Serializable]
    public class ShResponse
    {
        public double StartTime;
        public double EndTime;
        public double StartAngle;
        public double EndAngle;
        public bool IsClockwise;
    }

    [Serializable]
    public class ShAction
    {
        public ShResponse ManditoryResponse;
        public ShResponse PlayerResponse;
        public bool WasSuccessful;

        public ShAction()
        {
            ManditoryResponse = new ShResponse();
            PlayerResponse = new ShResponse();
        }
    }


//.timerTrial.value

    public class PlayerProgressLog : MonoBehaviour
    {
        Experiment _experiment;
        SHSBehavior _solver;
        PlayerBehavior _controls;

        void Start()
        {
            _experiment = GameObject.FindObjectOfType<Experiment>();
            _solver = GameObject.FindObjectOfType<SHSBehavior>();
            _controls = GameObject.FindObjectOfType<PlayerBehavior>();
        }

        float _previousInput;

        public List<ShAction> Actions;
        private ShAction _currentAction;

        void Update()
        {
            if (_controls.Input != _previousInput)
            { // The user's input has changed!
                if (_currentAction != null)
                {
                    _currentAction.PlayerResponse.EndAngle = _controls.CurrentAngle;
                    _currentAction.PlayerResponse.EndTime = _experiment.TimerTrial.Value;

                    Actions.Add(_currentAction);
                    _currentAction = null;
                }
                else if (_controls.Input != 0)
                {
                    _currentAction = new ShAction();
                    _currentAction.PlayerResponse.IsClockwise = _controls.Input > 0;
                    _currentAction.PlayerResponse.StartAngle = _controls.CurrentAngle;
                    _currentAction.PlayerResponse.StartTime = _experiment.TimerTrial.Value;
                }
                _previousInput = _controls.Input;
            }

        }
    }
}