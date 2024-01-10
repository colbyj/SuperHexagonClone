using SH.LevelScripting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ThreatEditPanel : MonoBehaviour
{
    private Pattern pattern
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
    [SerializeField] private Slider sSide;
    [SerializeField] private TMP_InputField iThickness;
    [SerializeField] private TMP_InputField iDistance;
    [SerializeField] private Button bDelete;

    // Start is called before the first frame update
    void Start()
    {
        sSide.onValueChanged.AddListener(OnSideChanged);
        iThickness.onValueChanged.AddListener(OnThicknessChanged);
        iDistance.onValueChanged.AddListener(OnDistanceChanged);

        SHLine.SelectedThreatChanged += () =>
        {
            if (SHLine.selectedLine == null)
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
        sSide.value = CurrentWall.Side;
        iThickness.text = CurrentWall.Height.ToString();
        iDistance.text = CurrentWall.Distance.ToString();
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
