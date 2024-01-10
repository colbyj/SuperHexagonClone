using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditControls : MonoBehaviour
{
    public Toggle TogglePause;

    // Start is called before the first frame update
    void Start()
    {
        LaneManager.Instance.Paused = TogglePause.isOn;
        TogglePause.onValueChanged.AddListener(OnPauseToggled);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPauseToggled(bool toggled)
    {
        LaneManager.Instance.Paused = toggled;
    }
}
