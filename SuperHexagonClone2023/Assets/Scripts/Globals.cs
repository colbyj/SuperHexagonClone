public static class GameParameters
{
    private const bool SolverEnabled = false;
    public const float PlayerRadius = 5f;
    public const float PlayerWidth = 1f;
    public const float PlayerHeight = 1f;
    public const bool OverrideInput = SolverEnabled;
    public const bool SolverDebuggingOn = SolverEnabled;
    public const bool EnableCollisions = !SolverEnabled;
}

public static class DefaultDifficulty
{
    public const float PlayerRotationRate = 300; // Degrees per second
    public const float CameraRotation = 1f;
    public const float ThreatVelocity = 20f;
    public const float PatternRadiusOffset = 20f; // Minimum distance between threats
}
