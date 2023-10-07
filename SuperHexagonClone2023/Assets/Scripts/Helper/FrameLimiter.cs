using UnityEngine;
using System.Collections;

public class FrameLimiter : MonoBehaviour 
{
    public int targetFps = 60;

    void Awake() 
    {
        Application.targetFrameRate = targetFps;
    }
}