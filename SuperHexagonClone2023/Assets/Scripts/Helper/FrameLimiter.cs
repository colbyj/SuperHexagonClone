using UnityEngine;
using System.Collections;

public class FrameLimiter : MonoBehaviour 
{
    public int TargetFps = 60;

    void Awake() 
    {
        Application.targetFrameRate = TargetFps;
    }
}