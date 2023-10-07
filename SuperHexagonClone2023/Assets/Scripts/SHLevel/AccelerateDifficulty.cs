using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomExtensions;
using System;

public class AccelerateDifficulty : MonoBehaviour
{

    [Serializable]
    public class DifficultyArgument
    {
        [SerializeField]
        private float value;
        [SerializeField]
        public float increaseBy = 0.1f;
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

        public float GetValue()
        {
            //if (value == 0)
            //    value = startingValue;

            return value;
        }
    }

    public float updateRate = 1f; // The overall rate in which the difficulty increases
    [SerializeField] public DifficultyArgument rotationDifficulty; // Note: initial rotation comes from ConstantWorldRotation.
    [SerializeField] public DifficultyArgument spawningDifficulty;

    private SHLevelManager level;
    private ConstantWorldRotation rotation;

    void Start()
    {
        level = gameObject.FindRequiredGameObjectWithTag("LevelManager").GetRequiredComponent<SHLevelManager>();
        rotation = gameObject.FindRequiredGameObjectWithTag("MainCamera").GetRequiredComponent<ConstantWorldRotation>();

        rotationDifficulty.startingValue = rotation.currentRotationRate;

        ResetDifficulty();

        InvokeRepeating("UpdateDifficulty", 0f, updateRate); // Starts the difficulty increases
    }

    void UpdateDifficulty()
    {
        if (level.started)
        {
            rotationDifficulty.IncreaseDifficulty();
            spawningDifficulty.IncreaseDifficulty();
        }

        rotation.currentRotationRate = rotationDifficulty.GetValue();
    }

    public float GetSpawningDifficulty()
    {
        return spawningDifficulty.GetValue();
    }

    public float GetRotationRate()
    {
        return rotationDifficulty.GetValue();
    }

    public void ResetDifficulty()
    {
        rotationDifficulty.Initialize();
        spawningDifficulty.Initialize();
    }
}
