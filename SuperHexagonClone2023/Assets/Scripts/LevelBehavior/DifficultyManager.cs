using System;
using Assets.Scripts.SHPlayer;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.Scripts.LevelBehavior
{
    public class DifficultyManager : MonoBehaviour
    {

        [Serializable]
        public class DifficultyAccelerator
        {
            [SerializeField]
            private float _value;
            [SerializeField]
            public float IncreaseBy = 0f;
            [SerializeField]
            public float StartingValue = 1f;
            [SerializeField]
            public float CeilingValue = 10f;

            public void Initialize()
            {
                _value = StartingValue;
            }

            public void Initialize(int increases)
            {
                _value = StartingValue + (IncreaseBy * increases);
            }

            public void IncreaseDifficulty()
            {
                if (_value + IncreaseBy >= CeilingValue)
                {
                    _value = CeilingValue;
                    return;
                }
                _value = _value + IncreaseBy;
            }

            public void SetStartingValue(float value)
            {
                StartingValue = value;
                Initialize();
            }

            public void SetIncreaseBy(float increaseBy)
            {
                this.IncreaseBy = increaseBy;
            }

            public void SetCeiling(float ceiling)
            {
                this.CeilingValue = ceiling;
            }

            public float GetValue()
            {
                //if (value == 0)
                //    value = startingValue;

                return _value;
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
            StartingValue = DefaultDifficulty.CameraRotation 
        }; 

        /// <summary>
        /// Increase the speed of the threats over time.
        /// </summary>
        [SerializeField] public DifficultyAccelerator ThreatDifficultyAccelerator = new DifficultyAccelerator()
        {
            StartingValue = DefaultDifficulty.ThreatVelocity
        };

        public float CameraRotationSpeed => RotationDifficultyAccelerator.GetValue();

        public float ThreatSpeed => ThreatDifficultyAccelerator.GetValue();

        public float PlayerRotationRate = DefaultDifficulty.PlayerRotationRate;
        public float PatternRadiusOffset = DefaultDifficulty.PatternRadiusOffset;

        private void Awake()
        {
            Instance = this;

            //PlayerBehavior.OnPlayerDied += (threat) => ResetDifficulty();
        }

        void Start()
        {
            RotationDifficultyAccelerator.StartingValue = ConstantWorldRotation.Instance.CurrentRotationRate;

            ResetDifficulty();

            InvokeRepeating(nameof(UpdateDifficulty), 0f, UpdateRate); // Starts the difficulty increases
        }

        void UpdateDifficulty()
        {
            if (LevelManager.Instance != null && !PlayerBehavior.IsDead)
            {
                RotationDifficultyAccelerator.IncreaseDifficulty();
                ThreatDifficultyAccelerator.IncreaseDifficulty();
            }

            ConstantWorldRotation.Instance.CurrentRotationRate = RotationDifficultyAccelerator.GetValue();
        }

        public void ResetDifficulty()
        {
            RotationDifficultyAccelerator.Initialize();
            ThreatDifficultyAccelerator.Initialize();
        }
    }
}
