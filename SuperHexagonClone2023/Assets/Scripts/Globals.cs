public static class GameParameters
{
    public const float PlayerRadius = 5f;
    public const bool OverrideInput = false;
    public const bool SolverDebuggingOn = false;
    public const bool EnableCollisions = true;
}

public static class DefaultDifficulty
{
    public const float PlayerRotationRate = 300; // Degrees per second
    public const float CameraRotation = 1f;
    public const float ThreatVelocity = 20f;
    public const float PatternRadiusOffset = 20f; // Minimum distance between threats
}
