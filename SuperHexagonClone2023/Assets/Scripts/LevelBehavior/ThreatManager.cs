using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.LevelVisuals;
using Assets.Scripts.SHPlayer;
using UnityEngine;
using UnityEngine.Pool;

namespace Assets.Scripts.LevelBehavior
{
    public class ThreatManager : MonoBehaviour
    {
        public const float SpawnPatternsUntilRadius = 100f;

        public static ThreatManager Instance;

        // These will get used for logging
        public Action<LevelPattern> PatternIsPastPlayer;
        public Action<LevelPattern> PatternIsAtPlayer;
        public Action<LevelPattern> PatternIsOffScreen;

        public List<LevelPattern> PatternsOnScreen = new();
        private ParsedLevel Level => LevelManager.Instance.Level;


        [Header("Object Pooling Config")] private ObjectPool<SHLine> _threatPool;
        public SHLine ThreatPrefab;
        public List<SHLine> ActiveThreats = new();
        
        [SerializeField] private int _poolStartingSize = 100;
        [SerializeField] private float _firstPatternRadius = 50f;

        public static bool IsEditScene = false;

        /// <summary>
        /// Note that this is the inner radius, not the outer radius.
        /// </summary>
        public float FurthestThreatRadius
        {
            get
            {
                if (PatternsOnScreen.Count > 0)
                {
                    return PatternsOnScreen.Last().FurthestThreat.Radius;
                }
                else
                {
                    return 0;
                }
            }
        }

        private float SpawnRadius
        {
            get
            {
                if (PatternsOnScreen.Count == 0)
                {
                    return _firstPatternRadius;
                }

                return PatternsOnScreen.Last().FurthestThreat.RadiusOuter() + DifficultyManager.Instance.PatternRadiusOffset;
            }
        }


        // Start is called before the first frame update
        private void Awake()
        {
            Instance = this;
            _threatPool = new ObjectPool<SHLine>(CreateThreat, OnGetThreatFromPool, OnReleaseThreatToPool,
                defaultCapacity: _poolStartingSize);

            if (FindObjectOfType<PatternPreview>() != null)
            {
                IsEditScene = true;
            }
        }

        #region Threat Pooling

        private SHLine CreateThreat()
        {
            if (ThreatPrefab != null)
            {
                try
                {
                    SHLine spawned = Instantiate(ThreatPrefab, transform);

                    spawned.transform.position = new Vector3(0f, 0f, -0.2f);
                    spawned.transform.rotation = gameObject.transform.rotation;

                    return spawned;
                }
                catch (UnityException e)
                {
                    Debug.Log(e.StackTrace);
                }
            }
            else
            {
                throw new UnityException("The threat prefab on the lane: " + gameObject.name +
                                         " is not set! This will prevent spawning from happening on this lane.");
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
            ActiveThreats.Add(retrievedLine);
        }

        /// <summary>
        /// Called when we release a threat via Release
        /// </summary>
        /// <param name="releasedLine"></param>
        private void OnReleaseThreatToPool(SHLine releasedLine)
        {
            releasedLine.gameObject.SetActive(false);
            ActiveThreats.Remove(releasedLine);
        }

        private SHLine SpawnThreatFromPool(float thickness, float radius, float rotation = 0f, bool isFirstPattern = false)
        {
            SHLine threat = _threatPool.Get();

            if (threat != null)
            {
                threat.Radius = radius;
                threat.Thickness = thickness;

                // Reset the position and rotation of the line/threat.
                threat.transform.position = new Vector3(0f, 0f, -0.2f);
                threat.transform.rotation = Quaternion.Euler(0, 0, rotation);

                threat.UpdatePolygon();
                
                if (isFirstPattern && !IsEditScene)
                {
                    threat.StartFadeIn();
                }
            }

            return threat;
        }

        #endregion

        // Update is called once per frame
        private void Update()
        {
            UpdateThreatRadii();
        }

        private void UpdateThreatRadii()
        {
            if (PlayerBehavior.IsDead)
            {
                return;
            }

            LevelPattern patternPastPlayer = null;
            LevelPattern patternAtPlayer = null;
            LevelPattern patternOffScreen = null;

            foreach (LevelPattern patternOnScreen in PatternsOnScreen)
            {
                for (int i = 0; i < patternOnScreen.Threats.Count; i++)
                {
                    patternOnScreen.Threats[i].Radius -= DifficultyManager.Instance.ThreatSpeed * Time.deltaTime;
                    patternOnScreen.Threats[i].UpdatePolygon();
                }

                if (patternOnScreen.FurthestThreat.HasJustPassedRadius(GameParameters.PlayerRadius))
                {
                    patternPastPlayer = patternOnScreen;
                }

                if (patternOnScreen.ClosestThreat.HasJustPassedRadius(GameParameters.PlayerRadius))
                {
                    patternAtPlayer = patternOnScreen;
                }

                if (patternOffScreen == null && patternOnScreen.FurthestThreat.RadiusOuter() <= 0)
                {
                    patternOffScreen = patternOnScreen;
                }
            }

            // These are invoked after iterating through the list to avoid modifying the list.
            if (patternPastPlayer != null)
            {
                PatternIsPastPlayer?.Invoke(patternPastPlayer);
            }

            if (patternAtPlayer != null)
            {
                PatternIsAtPlayer?.Invoke(patternAtPlayer);
            }

            // Remove any patterns that are now fully off the screen.
            if (patternOffScreen != null)
            {
                PatternIsOffScreen?.Invoke(patternOffScreen);
                FinishWithLevelPattern(patternOffScreen);
            }
        }

        private void FinishWithLevelPattern(LevelPattern patternFinished)
        {
            Debug.Log($"FinishWithLevelPattern({patternFinished.Name}), made up of {patternFinished.Threats.Count} threats.");
            foreach (SHLine line in patternFinished.Threats)
            {
                line.ResetLine();
                _threatPool.Release(line);
            }

            patternFinished.Threats = new List<SHLine>(); // These patterns get reused, so make sure the threat list is ready to go again.
            PatternsOnScreen.Remove(patternFinished);
        }

        public void SpawnLevelPattern(LevelPattern patternToSpawn)
        {
            const int lanesRequired = 6; // TODO
            float spawnRadius = SpawnRadius;
            Debug.Log($"Spawning {patternToSpawn.Name} at {spawnRadius}.");

            foreach (Pattern.Wall wall in patternToSpawn.Pattern.Walls)
            {
                int sideIndex = (wall.Side + patternToSpawn.RotationOffset) % lanesRequired;
                if (patternToSpawn.Mirrored)
                {
                    sideIndex = lanesRequired - 1 - sideIndex;
                }

                float wallDistance = spawnRadius + wall.Distance + patternToSpawn.DistanceOffset;
                float rotation = sideIndex * (360f / lanesRequired);

                SHLine line = SpawnThreatFromPool(wall.Height, wallDistance, rotation, spawnRadius == _firstPatternRadius);
                patternToSpawn.Threats.Add(line);
            }

            patternToSpawn.UpdateClosestAndFurthestThreats();
            PatternsOnScreen.Add(patternToSpawn);
        }

        public void Clear()
        {
            for (int i = PatternsOnScreen.Count - 1; i >= 0; i--)
            {
                FinishWithLevelPattern(PatternsOnScreen[i]);
            }
        }
    }
}