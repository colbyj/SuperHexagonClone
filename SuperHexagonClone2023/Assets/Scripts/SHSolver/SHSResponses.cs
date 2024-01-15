using System;
using System.Collections.Generic;
using Assets.Scripts.LevelBehavior;
using Assets.Scripts.SHPlayer;
using UnityEngine;

[Serializable]
public class MovementOption : IComparable<MovementOption>
{
    public bool IsClockwise;
    public float AngleDelta;
    public float StartingRadius;
    public bool IsMovementPossible;
    public GameObject TargetLane;

    public MovementOption(GameObject targetLane, float angleDelta, bool isClockwise, float startingRadius = 0f)
    {
        TargetLane = targetLane;
        AngleDelta = angleDelta;
        IsClockwise = isClockwise;

        if (startingRadius == 0f)
        {
            startingRadius = GameParameters.PlayerRadius;
        }

        StartingRadius = startingRadius;

        CalculateIsMovementPossible();
    }

    // Used to sort movement options by the required delta angle 
    public int CompareTo(MovementOption other)
    {
        var angleToConsider = AngleDelta;
        if (!IsMovementPossible) // We want impossible movements to be treated as higher angles.
        {
            angleToConsider = angleToConsider + 360f;
        }

        if (angleToConsider == other.AngleDelta)
        {
            return 0;
        }
        else if (angleToConsider < other.AngleDelta)
        {
            return -1;
        }

        return 1;
    }

    private int SlicesToTraverse()
    {
        var slices = Mathf.RoundToInt(AngleDelta / 60f); // Hardcoded value of 60 degrees based on number of slices.
        return slices;
    }

    // Use MovementPath calculations to estimate whether an obstacle is hit.
    public bool CalculateIsMovementPossible()
    {
        var playerAngle = SHSBehavior.PlayerPivot.GetComponent<PlayerBehavior>().GetAngle();

        MovementPath lastMovement = null;
        var slices = SlicesToTraverse();

        for (var i = 0; i < slices; i++)
        {
            MovementPath nextMovement = null;

            if (i == 0) // First movement starts from the player's location. 
            {
                nextMovement = new MovementPath(new Vector2(StartingRadius - 0.04f, playerAngle), IsClockwise);
            }
            else // Subsequent movements start at the boundary of two slices.
            {
                nextMovement = new MovementPath(lastMovement.EndPointPolar, IsClockwise);
            }

            if (GameParameters.SolverDebuggingOn) // Draw a line if debugging is enabled.
            {
                nextMovement.DebugMovement(false, true);
            }

            var hits = nextMovement.PerformRaycast();

            if (hits.Count > 0)
            {
                // We found an obstacle in the way!
                IsMovementPossible = false;
                return false;
            }

            lastMovement = nextMovement;
        }

        // Nothing can stop us
        IsMovementPossible = true;
        return true;
    }
}

// Radial movement is approximated by straight lines. One line per slice/lane.
public class MovementPath
{
    public Vector2 StartPointPolar;
    public Vector2 EndPointPolar;

    public MovementPath(Vector2 startPointPolar, bool isClockwise = true)
    {
        CalcEndPointPolar(startPointPolar, isClockwise);
    }

    public static Vector2 ToCartesian(Vector2 polar)
    {
        return new Vector2(
            -polar.x * Mathf.Sin(Mathf.Deg2Rad * polar.y),
            polar.x * Mathf.Cos(Mathf.Deg2Rad * polar.y)
        );
    }

    // Using polar coordinates, work out a raycast clockwise (decreasing angle) or counter-clockwise (increasing angle)
    public Vector2 CalcEndPointPolar(Vector2 startPointPolar, bool isClockwise = true)
    {
        StartPointPolar = startPointPolar;

        // Distance represented as an angle.
        float angleDelta = 0;

        if (isClockwise)
        {
            angleDelta = -1 * (startPointPolar.y - 30) % 60; // TODO: 30 and 60 are hardcoded based on lanes=6

            if (angleDelta == 0) // If the start point is exactly on the intersection of two slices
            {
                angleDelta = -60;
            }
            else if (angleDelta > 0) // Required for 0-30 degree scenario
            {
                angleDelta = angleDelta - 60;
            }
        }
        else
        {
            angleDelta = 60 - (startPointPolar.y - 30) % 60; // TODO: 30 and 60 are hardcoded based on lanes=6

            if (angleDelta == 0) // If the start point is exactly on the intersection of two slices
            {
                angleDelta = 60;
            }
            else if (angleDelta > 60) // Required for 0-30 degree scenario
            {
                angleDelta = angleDelta - 60;
            }
        }

        // Seconds the player needs to rotate to the next lane
        var rotationSeconds =
            Mathf.Abs(angleDelta / (DifficultyManager.Instance.PlayerRotationRate / Time.fixedDeltaTime));

        // The ray needs to be drawn toward this radius value
        var radialDelta = DifficultyManager.Instance.ThreatDifficultyAccelerator.GetValue() * rotationSeconds;

        EndPointPolar = new Vector2(startPointPolar.x + radialDelta, startPointPolar.y + angleDelta);
        return EndPointPolar;
    }

    public List<RaycastHit2D> PerformRaycast()
    {
        var start = ToCartesian(StartPointPolar);
        var end = ToCartesian(EndPointPolar);
        var distance = Vector2.Distance(start, end);

        var results = new List<RaycastHit2D>();

        var filterObstaclesOnly = new ContactFilter2D();
        filterObstaclesOnly.layerMask = LayerMask.GetMask("Threat");
        filterObstaclesOnly.useLayerMask = true;

        Physics2D.CircleCast(start, 0.01f, end - start, filterObstaclesOnly, results, distance);

        return results;
    }

    public void DebugMovement(bool message, bool line)
    {
        if (message)
        {
            Debug.Log(string.Format("RADII  -> Begin: {0}, Target: {1}", StartPointPolar.x, EndPointPolar.x));
            Debug.Log(string.Format("ANGLES -> Begin: {0}, Target: {1}", StartPointPolar.y, EndPointPolar.y));
        }

        if (line)
        {
            var startPoint = ToCartesian(StartPointPolar);
            var endPoint = ToCartesian(EndPointPolar);
            Debug.DrawLine(new Vector3(startPoint.x, startPoint.y, 1f), new Vector3(endPoint.x, endPoint.y, 1f));
        }
    }
}