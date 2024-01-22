using System.IO;
using Assets.Scripts.LevelBehavior;
using Assets.Scripts.LevelVisuals;
using Assets.Scripts.SHPlayer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Edit
{
    public class EditControls : MonoBehaviour
    {
        public static EditControls Instance;

        [SerializeField] private Toggle _togglePause;
        [SerializeField] private Button _testButton;
        [SerializeField] private Slider _zoomSlider;
        [SerializeField] private TMP_InputField _rotationInput; 
        [SerializeField] private TMP_InputField _threatSpeedInput; 
        [SerializeField] private TMP_InputField _playerSpeedInput;
        [SerializeField] private Button _createWallButton;

        // Save And Load
        [SerializeField] private TMP_Dropdown _loadDropdown;
        [SerializeField] private TMP_InputField _saveInput;
        [SerializeField] private Button _loadButton;
        [SerializeField] private Button _saveButton;
        [SerializeField] private GameObject _bottomPanel;
        [SerializeField] private Button _bottomPanelButton;

        // Start is called before the first frame update
        void Start()
        {
            Instance = this;

            _zoomSlider.value = Camera.main.orthographicSize;
            _rotationInput.text = DifficultyManager.Instance.CameraRotationSpeed.ToString();
            _threatSpeedInput.text = DifficultyManager.Instance.ThreatSpeed.ToString();
            _playerSpeedInput.text = DifficultyManager.Instance.PlayerRotationRate.ToString();

            _testButton.onClick.AddListener(OnTestClicked);
            _zoomSlider.onValueChanged.AddListener(OnZoomChanged);
            _rotationInput.onValueChanged.AddListener(OnRotationSpeedChanged);
            _threatSpeedInput.onValueChanged.AddListener(OnThreatSpeedChanged);
            _playerSpeedInput.onValueChanged.AddListener(OnPlayerSpeedChanged);
            _createWallButton.onClick.AddListener(OnAddWallClicked);

            PlayerBehavior.OnPlayerDied += (line) => 
                gameObject.SetActive(true);

            //ThreatManager.Instance.PatternIsOffScreen += (pi) => 
            //    gameObject.SetActive(true);

            LoadLevelOptions();
            _loadButton.onClick.AddListener(OnLoadClicked);
            _saveButton.onClick.AddListener(OnSaveClicked);
            _bottomPanelButton.onClick.AddListener(OnBottomPanelButtonClicked);
        }

        #region Save and Load
        private void LoadLevelOptions()
        {
            _loadDropdown.options.Clear();

            string[] fileNames = Directory.GetFiles($"{Application.streamingAssetsPath}/../Resources/Patterns");
        
            foreach (string path in fileNames)
            {
                if (path.EndsWith(".xml"))
                {
                    string displayName = Path.GetFileName(path.Replace(".xml", ""));
                    _loadDropdown.options.Add(new TMP_Dropdown.OptionData(displayName));

                    if (displayName == PatternPreview.Instance.PatternFileName)
                    {
                        _loadDropdown.value = _loadDropdown.options.Count - 1;
                    }
                }
            }
        }

        private void OnLoadClicked()
        {
            string fileName = _loadDropdown.options[_loadDropdown.value].text;
            _saveInput.text = fileName;

            PatternPreview.Instance.PatternFileName = fileName;
            PatternPreview.Instance.LoadPatternFromFileName();
        }

        private void OnSaveClicked()
        {
            PatternPreview.Instance.SavePattern(_saveInput.text);
            LoadLevelOptions();
        }

        private void OnBottomPanelButtonClicked()
        {
            _bottomPanel.SetActive(!_bottomPanel.activeSelf);
        }
        #endregion

        private void OnTestClicked() 
        {
            gameObject.SetActive(false); // Turn off these edit controls.

            PlayerBehavior.IsDead = false;
        
            if (SHLine.SelectedLine != null)
            {
                SHLine.SelectedLine.IsSelected = false;
            }
        }

        private void OnZoomChanged(float zoom)
        {
            Camera.main.orthographicSize = zoom;
        }

        private void OnRotationSpeedChanged(string rotationSpeedStr)
        {
            if (float.TryParse(rotationSpeedStr, out float rotationSpeed))
            {
                DifficultyManager.Instance.RotationDifficultyAccelerator.StartingValue = rotationSpeed;
                DifficultyManager.Instance.ResetDifficulty();

                if (rotationSpeed == 0)
                {
                    ConstantWorldRotation.Instance.Reset();
                }
            }
        }

        private void OnThreatSpeedChanged(string threatSpeedStr)
        {
            if (float.TryParse(threatSpeedStr, out float threatSpeed))
            {
                DifficultyManager.Instance.ThreatDifficultyAccelerator.StartingValue = threatSpeed;
                DifficultyManager.Instance.ResetDifficulty();
            }
        }

        private void OnPlayerSpeedChanged(string playerSpeedStr)
        {
            if (float.TryParse(playerSpeedStr, out float playerSpeed))
            {
                DifficultyManager.Instance.PlayerRotationRate = playerSpeed;
            }
        }

        private void OnAddWallClicked()
        {
            Pattern.Wall newWall = ThreatEditPanel.Instance.NewWallFromSettings();
            PatternPreview.Instance.CurrentPattern.Walls.Add(newWall);

            SHLine threat = ThreatManager.Instance.SpawnThreat(PatternPreview.Instance.CurrentPatternInstance, newWall);
            threat.SetAssociations(PatternPreview.Instance.CurrentPatternInstance, newWall);
            threat.IsSelected = true;

            PatternPreview.Instance.CurrentPatternInstance.Threats.Add(threat);
        }
    }
}
