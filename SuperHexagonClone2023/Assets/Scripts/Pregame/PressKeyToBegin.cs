using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PressKeyToBegin : MonoBehaviour
{

    private bool begun = false;

    // Use this for initialization
    void Start()
    {
        if (!begun) StartPause();
    }

    // Update is called once per frame
    void Update()
    {
        if (!begun)
        {
            Time.timeScale = 0;
            if (Input.anyKey)
            {
                begun = true;
                Time.timeScale = 1;
            }
        }
    }

    public void StartPause()
    {
        FindObjectOfType<DisplayMessage>().AddMessageToTop("Press any key to begin", 0.1f);
        begun = false;
    }

    public void StopPause()
    {
        FindObjectOfType<DisplayMessage>().ClearMessage();
        begun = true;
        Time.timeScale = 1;
    }
}
