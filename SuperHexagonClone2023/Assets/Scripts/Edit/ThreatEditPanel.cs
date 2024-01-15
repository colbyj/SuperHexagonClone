using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Assets.Scripts.LevelBehavior;
using Assets.Scripts.LevelVisuals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ThreatEditPanel : MonoBehaviour
{
    private Pattern Pattern
    {
        get { return PatternPreview.Instance.CurrentPattern; }
    }
    public int CurrentWallIndex {
        get { return _currentWallIndex; }
        set 
        {
            _currentWallIndex = value;
            OnSelectedIndexChanged();
        }
    }
    public Pattern.Wall CurrentWall
    {
        get { return PatternPreview.Instance.CurrentPattern.Walls[CurrentWallIndex]; }
        set { PatternPreview.Instance.CurrentPattern.Walls[CurrentWallIndex] = value; }
    }

    private int _currentWallIndex;
    [SerializeField] private Slider _sSide;
    [SerializeField] private TMP_InputField _iThickness;
    [SerializeField] private TMP_InputField _iDistance;
    [SerializeField] private Button _bDelete;

    // Start is called before the first frame update
    void Start()
    {
        _sSide.onValueChanged.AddListener(OnSideChanged);
        _iThickness.onValueChanged.AddListener(OnThicknessChanged);
        _iDistance.onValueChanged.AddListener(OnDistanceChanged);

        SHLine.SelectedThreatChanged += () =>
        {
            if (SHLine.SelectedLine == null)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
            }
        };
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnSelectedIndexChanged()
    {
        _sSide.value = CurrentWall.Side;
        _iThickness.text = CurrentWall.Height.ToString();
        _iDistance.text = CurrentWall.Distance.ToString();
    }

    public void OnSideChanged(float side)
    {
        Pattern.Wall newWall = new Pattern.Wall((int)side, CurrentWall.Distance, CurrentWall.Height);
        CurrentWall = newWall;
    }

    public void OnThicknessChanged(string thickness)
    {
        Pattern.Wall newWall = new Pattern.Wall(CurrentWall.Side, CurrentWall.Distance, Convert.ToInt32(thickness));
        CurrentWall = newWall;
    }

    public void OnDistanceChanged(string distance)
    {
        Pattern.Wall newWall = new Pattern.Wall(CurrentWall.Side, Convert.ToInt32(distance), CurrentWall.Height);
        CurrentWall = newWall;
    }
}
