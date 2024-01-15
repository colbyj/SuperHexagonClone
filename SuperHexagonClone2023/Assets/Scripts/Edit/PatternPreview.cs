using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.LevelBehavior;
using Assets.Scripts.SHPlayer;
using UnityEngine;

public class PatternPreview : MonoBehaviour
{
    public static PatternPreview Instance;
    public string PatternFileName;
    public Pattern CurrentPattern;

    private void Awake()
    {
        Instance = this;
        PlayerBehavior.IsDead = false;
        ThreatManager.Instance.PatternIsOffScreen += PatternIsOffScreen;
        PlayerBehavior.OnPlayerDied += PlayerDied;

        if (!string.IsNullOrEmpty(PatternFileName))
        {
            LoadPatternFromFileName();
        }
    }

    private void PatternIsOffScreen(LevelPattern obj)
    {
        PlayerBehavior.IsDead = true;
        ThreatManager.Instance.SpawnLevelPattern(new LevelPattern(CurrentPattern));
    }

    private void PlayerDied()
    {
        ThreatManager.Instance.Clear();
        ThreatManager.Instance.SpawnLevelPattern(new LevelPattern(CurrentPattern));
    }

    public void LoadPatternFromFileName()
    {
        CurrentPattern = new Pattern(PatternFileName);
        //LaneManager.Instance.ResetLanes();
        //LaneManager.Instance.SpawnThreats(CurrentPattern, 20);
        ThreatManager.Instance.SpawnLevelPattern(new LevelPattern(CurrentPattern));
    }

    public void Update()
    {
    }
}
