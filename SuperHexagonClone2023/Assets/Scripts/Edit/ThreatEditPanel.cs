using System;
using System.Linq;
using Assets.Scripts.LevelBehavior;
using Assets.Scripts.LevelVisuals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Edit
{
    public class ThreatEditPanel : MonoBehaviour
    {
        public static ThreatEditPanel Instance;
    
        public Pattern.Wall SelectedWall => SHLine.SelectedLine.AssociatedWall;

        private int _currentWallIndex;
        [SerializeField] private Slider _sSide;
        [SerializeField] private TMP_InputField _iThickness;
        [SerializeField] private TMP_InputField _iDistance;
        [SerializeField] private Button _bDelete;

        // Start is called before the first frame update
        private void Awake()
        {
            Instance = this;

            _sSide.onValueChanged.AddListener(OnSideChanged);
            _iThickness.onValueChanged.AddListener(OnThicknessChanged);
            _iDistance.onValueChanged.AddListener(OnDistanceChanged);

            SHLine.SelectedThreatChanged += () =>
            {
                if (SHLine.SelectedLine == null || SHLine.SelectedLine.AssociatedPatternInstance == null)
                {
                    gameObject.SetActive(false);
                }
                else
                {
                    gameObject.SetActive(true);
                    _sSide.value = SelectedWall.Side;
                    _iThickness.text = SelectedWall.Height.ToString();
                    _iDistance.text = SelectedWall.Distance.ToString();
                }
            };

            _bDelete.onClick.AddListener(OnDeleteClicked);

            gameObject.SetActive(false);
        }

        private void OnSideChanged(float side)
        {
            SelectedWall.Side = (int)side;
            SHLine.SelectedLine.RebuildFromAssociations();
        }

        private void OnThicknessChanged(string thickness)
        {
            if (!thickness.All(char.IsDigit))
                return;

            SelectedWall.Height = Convert.ToInt32(thickness);
            SHLine.SelectedLine.RebuildFromAssociations();
        }

        private void OnDistanceChanged(string distanceStr)
        {
            if (!distanceStr.All(char.IsDigit))
                return;

            if (int.TryParse(distanceStr, out int distance))
            {
                SelectedWall.Distance = Convert.ToInt32(distance);
                SHLine.SelectedLine.RebuildFromAssociations();
            }
        }

        private void OnDeleteClicked()
        {
            SHLine.SelectedLine.AssociatedPatternInstance.Pattern.Walls.Remove(SHLine.SelectedLine.AssociatedWall);
            ThreatManager.Instance.RemoveThreat(SHLine.SelectedLine);
        }

        public Pattern.Wall NewWallFromSettings()
        {
            int side = (int)_sSide.value;

            if (!int.TryParse(_iDistance.text, out int distance))
            {
                distance = 0;
            }

            if (!int.TryParse(_iThickness.text, out int height))
            {
                height = 3;
            }

            return new Pattern.Wall(side, distance, height);
        }
    }
}
