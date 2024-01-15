using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.SHPlayer;
using UnityEngine;
using UnityEngine.UI;

public class EditControls : MonoBehaviour
{
    [SerializeField] private Toggle _togglePause;
    [SerializeField] private Button _testButton;

    // Start is called before the first frame update
    void Start()
    {
        //LaneManager.Instance.Paused = _togglePause.isOn;
        //_togglePause.onValueChanged.AddListener(OnPauseToggled);

        //ThreatManager.Instance.
        _testButton.onClick.AddListener(OnTestClicked);
    }

    private void OnTestClicked() 
    {
        PlayerBehavior.IsDead = false;
    }

    public void OnPauseToggled(bool toggled)
    {
    }
}
