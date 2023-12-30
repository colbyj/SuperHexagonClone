using System;
using UnityEngine;

public static class GameParameters 
{
    public const float PlayerRadius = 5f;
    public const bool OverrideInput = false;
    public const bool SolverDebuggingOn = false;
    public const bool EnableCollisions = true;
}

public static class ThreatParameters
{
    public const float DefaultThickness = 3f;
    public static float CurrentStartingRadius = FirstThreatRadius;
    public const float StartingRadius = 75f;
    public const float FirstThreatRadius = 50f;
}

public static class DefaultDifficulty
{
    public const float PlayerRotationRate = 360 / 60f; // Specifically written this way due to frame limit;
    public const float CameraRotation = 1f;
    public const float ThreatVelocity = 20f;
    public const float PatternRadiusOffset = 10f; // Minimum distance between threats
}
