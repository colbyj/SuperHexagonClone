using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class MovementOption : IComparable<MovementOption> {
    public bool isClockwise;
    public float angleDelta;
    public float startingRadius;
    public bool isMovementPossible;
    public GameObject targetLane;

    public MovementOption(GameObject targetLane, float angleDelta, bool isClockwise, float startingRadius = 0f) {
        this.targetLane = targetLane;
        this.angleDelta = angleDelta;
        this.isClockwise = isClockwise;

        if (startingRadius == 0f)
        {
            startingRadius = GameParameters.PlayerRadius;
        }

        this.startingRadius = startingRadius;

        CalculateIsMovementPossible();
    }

    // Used to sort movement options by the required delta angle 
    public int CompareTo(MovementOption other)  {
        float angleToConsider = angleDelta;
        if (!isMovementPossible) { // We want impossible movements to be treated as higher angles.
            angleToConsider = angleToConsider + 360f;
        }

        if (angleToConsider == other.angleDelta) {
            return 0;
        }
        else if (angleToConsider < other.angleDelta) {
            return -1;
        }
        return 1;
    }

    private int SlicesToTraverse() {
        int slices = Mathf.RoundToInt(angleDelta / 60f); // Hardcoded value of 60 degrees based on number of slices.
        return slices;
    }

    // Use MovementPath calculations to estimate whether an obstacle is hit.
    public bool CalculateIsMovementPossible() {
        float playerAngle = SHSBehavior.playerPivot.GetComponent<SHControls>().GetAngle();            

        MovementPath lastMovement = null;
        int slices = SlicesToTraverse();

        for (int i = 0; i < slices; i++) { 

            MovementPath nextMovement = null;

            if (i == 0) { // First movement starts from the player's location. 
                nextMovement = new MovementPath(new Vector2(startingRadius - 0.04f, playerAngle), isClockwise);
            } else { // Subsequent movements start at the boundary of two slices.
                nextMovement = new MovementPath(lastMovement.endPointPolar, isClockwise);
            }
            
            if (GameParameters.SolverDebuggingOn) { // Draw a line if debugging is enabled.
                nextMovement.DebugMovement(false, true);
            }

            List<RaycastHit2D> hits = nextMovement.PerformRaycast();

            if (hits.Count > 0) { // We found an obstacle in the way!
                isMovementPossible = false;
                return false;
            }

            lastMovement = nextMovement;
        }

        // Nothing can stop us
        isMovementPossible = true;
        return true;
    }
}

// Radial movement is approximated by straight lines. One line per slice/lane.
public class MovementPath {        
    public Vector2 startPointPolar;
    public Vector2 endPointPolar;

    public MovementPath(Vector2 startPointPolar, bool isClockwise = true) {
        CalcEndPointPolar(startPointPolar, isClockwise);
    }

    public static Vector2 ToCartesian(Vector2 polar) {
        return new Vector2(
            -polar.x * Mathf.Sin(Mathf.Deg2Rad * polar.y),
                polar.x * Mathf.Cos(Mathf.Deg2Rad * polar.y)
            ); 
    }
    
    // Using polar coordinates, work out a raycast clockwise (decreasing angle) or counter-clockwise (increasing angle)
    public Vector2 CalcEndPointPolar(Vector2 startPointPolar, bool isClockwise = true)
    {
        this.startPointPolar = startPointPolar;

        // Distance represented as an angle.
        float angleDelta = 0;

        if (isClockwise) {
            angleDelta = -1 * (startPointPolar.y - 30) % 60; // TODO: 30 and 60 are hardcoded based on lanes=6

            if (angleDelta == 0) { // If the start point is exactly on the intersection of two slices
                angleDelta = -60;
            }
            else if (angleDelta > 0) { // Required for 0-30 degree scenario
                angleDelta = angleDelta - 60;
            }
        }
        else {
            angleDelta = 60 - ((startPointPolar.y - 30) % 60); // TODO: 30 and 60 are hardcoded based on lanes=6

            if (angleDelta == 0) { // If the start point is exactly on the intersection of two slices
                angleDelta = 60;
            }
            else if (angleDelta > 60) { // Required for 0-30 degree scenario
                angleDelta = angleDelta - 60;
            }
        }

        // Seconds the player needs to rotate to the next lane
        float rotationSeconds = Mathf.Abs(angleDelta / (GameParameters.PlayerRotationRate / Time.fixedDeltaTime)); 

        // The ray needs to be drawn toward this radius value
        float radialDelta = GameParameters.ThreatRadialRate * rotationSeconds;

        endPointPolar = new Vector2(startPointPolar.x + radialDelta, startPointPolar.y + angleDelta);
        return endPointPolar;
    }

    public List<RaycastHit2D> PerformRaycast() {
        var start = ToCartesian(startPointPolar);
        var end = ToCartesian(endPointPolar);
        float distance = Vector2.Distance(start, end);

        List<RaycastHit2D> results = new List<RaycastHit2D>();

        ContactFilter2D filterObstaclesOnly = new ContactFilter2D();
        filterObstaclesOnly.layerMask = LayerMask.GetMask("Threat");
        filterObstaclesOnly.useLayerMask = true;

        Physics2D.CircleCast(start, 0.01f, end - start, filterObstaclesOnly, results, distance);

        return results;
    }

    public void DebugMovement(bool message, bool line) {
        if (message) {
            Debug.Log(string.Format("RADII  -> Begin: {0}, Target: {1}", startPointPolar.x, endPointPolar.x));
            Debug.Log(string.Format("ANGLES -> Begin: {0}, Target: {1}", startPointPolar.y, endPointPolar.y));
        }

        if (line) {
            Vector2 startPoint = ToCartesian(startPointPolar);
            Vector2 endPoint = ToCartesian(endPointPolar);
            Debug.DrawLine(new Vector3(startPoint.x, startPoint.y, 1f), new Vector3(endPoint.x, endPoint.y, 1f));
        }
    }
}