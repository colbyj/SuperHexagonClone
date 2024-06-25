using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Assets.Scripts.Edit;
using Assets.Scripts.LevelVisuals;
using Assets.Scripts.Logging;
using Assets.Scripts.SHPlayer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Pool;

namespace Assets.Scripts.LevelBehavior
{
    public class ThreatManager : MonoBehaviour
    {
        private const bool DebuggingEnabled = false;
        public const int LanesRequired = 6; // TODO
        public const float SpawnPatternsUntilRadius = 100f;

        public static ThreatManager Instance;

        // These will get used for logging
        public Action<PatternInstance> PatternIsPastPlayer;
        public Action<PatternInstance> PatternIsAtPlayer;
        public Action<PatternInstance> PatternIsOffScreen;
        public Action<PatternInstance> PatternHasSpawned;

        public List<PatternInstance> PatternsOnScreen = new();
        private ParsedLevel Level => LevelManager.Instance.Level;


        [Header("Object Pooling Config")] private ObjectPool<SHLine> _threatPool;
        public SHLine ThreatPrefab;
        public List<SHLine> ActiveThreats = new();
        
        [SerializeField] private int _poolStartingSize = 100;
        [SerializeField] private float _firstPatternRadius = 50f;
        [SerializeField] private bool _areTriggersVisible;

        public static bool IsEditScene = false;
        public static bool AreTriggersVisible => Instance != null && Instance._areTriggersVisible;


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

                return PatternsOnScreen.Last().FurthestThreat.RadiusOuter + DifficultyManager.Instance.PatternRadiusOffset;
            }
        }


        // Start is called before the first frame update
        private void Awake()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

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
        #endregion

        // Update is called once per frame
        private void Update()
        {
            UpdateThreatRadii();
        }

        private void UpdateThreatRadii()
        {
            if (PlayerBehavior.IsDead) // Threats don't move if player is dead!
            {
                return;
            }

            PatternInstance patternInstancePastPlayer = null;
            PatternInstance patternInstanceAtPlayer = null;
            PatternInstance patternInstanceOffScreen = null;

            foreach (PatternInstance patternOnScreen in PatternsOnScreen)
            {
                for (int i = 0; i < patternOnScreen.Threats.Count; i++)
                {
                    patternOnScreen.Threats[i].Radius -= DifficultyManager.Instance.ThreatSpeed * Time.deltaTime;
                    patternOnScreen.Threats[i].UpdatePolygon();
                }

                if (patternOnScreen.FurthestThreat.HasJustPassedRadius(GameParameters.PlayerRadius))
                {
                    patternInstancePastPlayer = patternOnScreen;
                }

                if (patternOnScreen.ClosestThreat.HasJustPassedRadius(GameParameters.PlayerRadius))
                {
                    patternInstanceAtPlayer = patternOnScreen;
                }

                if (patternInstanceOffScreen == null && patternOnScreen.FurthestThreat.RadiusOuter <= 0)
                {
                    patternInstanceOffScreen = patternOnScreen;
                }
            }

            // These are invoked after iterating through the list to avoid modifying the list.
            if (patternInstancePastPlayer != null)
            {
                PatternIsPastPlayer?.Invoke(patternInstancePastPlayer);

                // Did the player win?
                if (patternInstancePastPlayer.LastBeforeRestart)
                {
                    //Experiment.Instance?.Success.Play();
                    //FindObjectOfType<DisplayMessage>().AddMessageToTop("Level completed!", 2f);
                }
            }

            if (patternInstanceAtPlayer != null)
            {
                PatternIsAtPlayer?.Invoke(patternInstanceAtPlayer);
            }

            // Remove any patterns that are now fully off the screen.
            if (patternInstanceOffScreen != null)
            {
                FinishWithLevelPattern(patternInstanceOffScreen);
                PatternIsOffScreen?.Invoke(patternInstanceOffScreen);
            }
        }

        public SHLine SpawnThreat(PatternInstance patternInstance, Pattern.Wall wall, float? spawnRadius = null, bool isFirstPattern = false)
        {
            SHLine threat = _threatPool.Get();

            if (threat == null)
            {
                return null;
            }

            if (!spawnRadius.HasValue)
            {
                spawnRadius = _firstPatternRadius;
            }

            threat.SetAssociations(patternInstance, wall, spawnRadius.Value);

            if (isFirstPattern && !IsEditScene)
            {
                threat.StartFadeIn();
            }

            return threat;
        }

        public void RemoveThreat(SHLine line)
        {
            line.ResetLine();
            _threatPool.Release(line);
        }

        private void FinishWithLevelPattern(PatternInstance patternInstanceFinished)
        {
            if (DebuggingEnabled) Debug.Log($"FinishWithLevelPattern({patternInstanceFinished.Name}), made up of {patternInstanceFinished.Threats.Count} threats.");
            foreach (SHLine line in patternInstanceFinished.Threats)
            {
                RemoveThreat(line);
            }

            patternInstanceFinished.Threats = new List<SHLine>(); // These patterns get reused, so make sure the threat list is ready to go again.
            patternInstanceFinished.Triggers = new List<SHLine>();
            PatternsOnScreen.Remove(patternInstanceFinished);
        }

        public void SpawnLevelPattern(PatternInstance patternInstanceToSpawn)
        {
            float spawnRadius = SpawnRadius;
            if (DebuggingEnabled) Debug.Log($"Spawning {patternInstanceToSpawn.Name} at {spawnRadius}.");

            foreach (Pattern.Wall wall in patternInstanceToSpawn.Pattern.Walls)
            {
                SHLine line = SpawnThreat(patternInstanceToSpawn, wall, spawnRadius, spawnRadius == _firstPatternRadius);
                patternInstanceToSpawn.Threats.Add(line);
            }

            patternInstanceToSpawn.UpdateClosestAndFurthestThreats();
            PatternsOnScreen.Add(patternInstanceToSpawn);
            
            PatternHasSpawned?.Invoke(patternInstanceToSpawn);
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