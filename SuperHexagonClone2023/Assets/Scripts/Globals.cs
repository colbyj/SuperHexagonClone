using System;
using UnityEngine;

public static class GameParameters 
{
    public const float PlayerRadius = 7f;
    public const float PlayerRotationRate = 360 / 60f; // Specifically written this way due to frame limit;
    public const bool OverrideInput = false;
    public const bool SolverDebuggingOn = false;    

    // Threat-related.
    public const float ThreatRadialRate = 20f;
    public const float ThreatStartingRadius = 75f;
    public const bool EnableCollisions = true;
}
