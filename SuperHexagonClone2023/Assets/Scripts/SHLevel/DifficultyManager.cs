using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomExtensions;
using System;

public class DifficultyManager : MonoBehaviour
{

    [Serializable]
    public class DifficultyAccelerator
    {
        [SerializeField]
        private float value;
        [SerializeField]
        public float increaseBy = 0f;
        [SerializeField]
        public float startingValue = 1f;
        [SerializeField]
        public float ceilingValue = 10f;

        public void Initialize()
        {
            value = startingValue;
        }

        public void Initialize(int increases)
        {
            value = startingValue + (increaseBy * increases);
        }

        public void IncreaseDifficulty()
        {
            if (value + increaseBy >= ceilingValue)
            {
                value = ceilingValue;
                return;
            }
            value = value + increaseBy;
        }

        public void SetStartingValue(float value)
        {
            startingValue = value;
            Initialize();
        }

        public void SetIncreaseBy(float increaseBy)
        {
            this.increaseBy = increaseBy;
        }

        public void SetCeiling(float ceiling)
        {
            this.ceilingValue = ceiling;
        }

        public float GetValue()
        {
            //if (value == 0)
            //    value = startingValue;

            return value;
        }
    }

    public static DifficultyManager Instance { get; private set; }

    /// <summary>
    /// The overall rate in which the difficulty increases
    /// </summary>
    public float UpdateRate = 1f;

    /// <summary>
    /// Increase the speed of the camera rotation over time.
    /// </summary>
    [SerializeField] public DifficultyAccelerator RotationDifficultyAccelerator = new DifficultyAccelerator() 
    { 
        startingValue = DefaultDifficulty.CameraRotation 
    }; 

    /// <summary>
    /// Increase the speed of the threats over time.
    /// </summary>
    [SerializeField] public DifficultyAccelerator ThreatDifficultyAccelerator = new DifficultyAccelerator()
    {
        startingValue = DefaultDifficulty.ThreatVelocity
    };

    public float RotationSpeed
    {
        get
        {
            return RotationDifficultyAccelerator.GetValue();
        }
    }

    public float ThreatSpeed
    {
        get
        {
            return ThreatDifficultyAccelerator.GetValue();
        }
    }

    public float PlayerRotationRate = DefaultDifficulty.PlayerRotationRate;
    public float PatternRadiusOffset = DefaultDifficulty.PatternRadiusOffset;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        RotationDifficultyAccelerator.startingValue = ConstantWorldRotation.Instance.currentRotationRate;

        ResetDifficulty();

        InvokeRepeating(nameof(UpdateDifficulty), 0f, UpdateRate); // Starts the difficulty increases
    }

    void UpdateDifficulty()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.Started)
        {
            RotationDifficultyAccelerator.IncreaseDifficulty();
            ThreatDifficultyAccelerator.IncreaseDifficulty();
        }

        ConstantWorldRotation.Instance.currentRotationRate = RotationDifficultyAccelerator.GetValue();
    }

    public void ResetDifficulty()
    {
        RotationDifficultyAccelerator.Initialize();
        ThreatDifficultyAccelerator.Initialize();
    }
}
