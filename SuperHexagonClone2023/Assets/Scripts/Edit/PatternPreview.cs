using SH.LevelScripting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatternPreview : MonoBehaviour
{
    public static PatternPreview Instance;
    public string PatternFileName;
    public Pattern CurrentPattern;

    private void Awake()
    {
        Instance = this;

        if (!string.IsNullOrEmpty(PatternFileName))
        {
            LoadPatternFromFileName();
        }
    }

    public void LoadPatternFromFileName()
    {
        CurrentPattern = new Pattern(PatternFileName);
        LaneManager.Instance.ResetLanes();
        LaneManager.Instance.SpawnThreats(CurrentPattern, 20);
    }

    public void Update()
    {
    }
}
