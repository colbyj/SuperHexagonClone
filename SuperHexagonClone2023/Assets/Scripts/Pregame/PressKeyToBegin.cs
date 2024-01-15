using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PressKeyToBegin : MonoBehaviour
{

    private bool _begun = false;

    // Use this for initialization
    void Start()
    {
        if (!_begun)
        {
            StartPause();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!_begun)
        {
            Time.timeScale = 0;
            if (Input.anyKey)
            {
                _begun = true;
                Time.timeScale = 1;
            }
        }
    }

    public void StartPause()
    {
        FindObjectOfType<DisplayMessage>().AddMessageToTop("Press any key to begin", 0.1f);
        _begun = false;
    }

    public void StopPause()
    {
        FindObjectOfType<DisplayMessage>().ClearMessage();
        _begun = true;
        Time.timeScale = 1;
    }
}
