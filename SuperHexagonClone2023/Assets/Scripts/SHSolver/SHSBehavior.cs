using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SHSBehavior : MonoBehaviour {

    /*class CompareInnerRadiuses : IComparer <ThreatPosition> {
        public ThreatPosition Compare (ThreatPosition x, ThreatPosition y) {   
        }
    }*/

    // These static variables are used by LaneThreats and MovementOptions.
    public static GameObject playerPivot;
    public static GameObject player;

    [Header("Preferences")]
    public float lookAheadRadius = 50f;

    [Header("Player State")]
    float playerAngle;
    List<GameObject> playerLanes;


    [Header("Stimuli Identification")]
    public float playerDeathIn; // radius at which player will die. is public simply so it shows up in the editor...
    public float nextNearestRadius; // public for debugging purposes
    public List<int> safestLanes; 
    public List<LaneThreats> laneThreats = new List<LaneThreats>(); 

    [Header("Response Selection")]
    public bool executingDecision = false;
    public GameObject decidedLane;
    public int decidedInput;
    public List<MovementOption> movementOptions = new List<MovementOption>(); 



    // Start is called before the first frame update
    void Start() {
        Invoke("BeginSolving", 1.0f); // TODO: More robust solution... just wanna get this going asap.
        playerPivot = GameObject.Find("PlayerPivot");
        //playerPivot.GetComponent<SHControls>().overrideInput = overrideInput;

        player = GameObject.Find("Player");
    }

    void BeginSolving() {
        SHLane[] lanes = FindObjectsOfType<SHLane>();
        foreach (var lane in lanes) {
            LaneThreats lt = new LaneThreats();
            lt.shLane = lane;
            lt.goLane = lane.gameObject;

            laneThreats.Add(lt);
        }
    }

    private bool ParseStimuli() {
        // Rebuild the metadata regarding the threat positions.
        for (int idxLane = 0; idxLane < laneThreats.Count; idxLane++) { 
            laneThreats[idxLane].threatPositions = new List<LaneThreats.ThreatPosition>();
            var currentLane = laneThreats[idxLane].shLane;

            // Closer threats should be lower indexes, so break the look when we exceed our threshold lookAheadRadius
            for (int idxThreat = 0; idxThreat < currentLane.activeThreats.Count; idxThreat++) {
                var threatLine = currentLane.activeThreats[idxThreat].GetComponent<SHLine>();
                
                if (threatLine.radius <= lookAheadRadius) {
                    laneThreats[idxLane].threatPositions.Add(
                        new LaneThreats.ThreatPosition() { 
                            innerRadius = threatLine.radius,
                            outerRadius = threatLine.RadiusOuter(),
                        }
                    );
                }
                else {
                    break;
                }
            }

            // Sort threats by inner radius and remove those that are past the player.
            laneThreats[idxLane].threatPositions = laneThreats[idxLane].threatPositions.
                OrderBy(t=>t.innerRadius).
                Where(t=>t.outerRadius > GameParameters.PlayerRadius - 0.5f).
                ToList();

            // Now that the list is sorted, the first index should have the closest obstacle
            if (laneThreats[idxLane].threatPositions.Count > 0) {
                laneThreats[idxLane].nearestInnerRadius = laneThreats[idxLane].threatPositions[0].innerRadius;
            }
            else {
                laneThreats[idxLane].nearestInnerRadius = lookAheadRadius;
            }
        }

        // How soon will the player die if they don't move? (in Radii)
        playerDeathIn = lookAheadRadius;

        for (int idxLane = 0; idxLane < laneThreats.Count; idxLane++) { 
            if (playerLanes.IndexOf(laneThreats[idxLane].goLane) == -1) {
                continue; // Player is not in this lane.
            }
            if (laneThreats[idxLane].nearestInnerRadius < playerDeathIn) {
                playerDeathIn = laneThreats[idxLane].nearestInnerRadius;
            }
        }


        // Are any of the lanes safer than the one we are in?
        nextNearestRadius = lookAheadRadius;

        // this could be improved..
        // a safer lane will have a radius > playerDeathIn and < any other.

        for (int idxLane = 0; idxLane < laneThreats.Count; idxLane++) {
            if (laneThreats[idxLane].nearestInnerRadius < nextNearestRadius && laneThreats[idxLane].nearestInnerRadius > playerDeathIn) {
                nextNearestRadius = laneThreats[idxLane].nearestInnerRadius;
            }
        }

        if (nextNearestRadius == playerDeathIn) { // No safer lanes were found. Don't do anything.
            return false;
        }

        // Now see if there are multiple lanes which are equally safe        
        safestLanes = new List<int>(); 

        for (int idxLane = 0; idxLane < laneThreats.Count; idxLane++) {
            if (laneThreats[idxLane].nearestInnerRadius == nextNearestRadius) {
                safestLanes.Add(idxLane);
            }
        }

        if (safestLanes.Count == 0) { // No safe lanes! The first if statement should prevent this from occurring.
            return false;
        }

        return true;
    }

    public bool ConsiderResponses() {
        // Don't try moving until the outer radius of the nearest inner radius obstacle is past the player!
        movementOptions = new List<MovementOption>();

        foreach (int idxLane in safestLanes)
        {
            float angle = laneThreats[idxLane].goLane.transform.eulerAngles.z;
            float deltaAngleCCW = angle - playerAngle;
     
            if (deltaAngleCCW < 0) {
                deltaAngleCCW += 360;
            }

            float deltaAngleCW = 360 - deltaAngleCCW;

            movementOptions.Add(new MovementOption(laneThreats[idxLane].goLane, deltaAngleCW, true));
            movementOptions.Add(new MovementOption(laneThreats[idxLane].goLane, deltaAngleCCW, false));
        }

        movementOptions.Sort();
        //Debug.Log("movementOptions.Count = " + movementOptions.Count);

        if (movementOptions.Count == 0) { // No options! Can't do anything.
            return false;
        }

        if (!GameParameters.OverrideInput) {
            return false;
        }

        if (!movementOptions[0].isMovementPossible) { // Don't try to move as it will fail. Maybe waiting will make that movement possible.
            return false;
        }

        // This section: Work out how long we can wait before needing to move.
        float furthestRadius = playerDeathIn;
        const float stepSize = 0.02f; // TODO: scope

        while (true) {
            // Evaluate movement option
            MovementOption mo = new MovementOption(
                movementOptions[0].targetLane, 
                movementOptions[0].angleDelta, 
                movementOptions[0].isClockwise,
                furthestRadius);

            if (mo.isMovementPossible) {
                break;
            }
            furthestRadius -= stepSize;
        }
        
        Debug.Log("The furthest radius is: " + furthestRadius);

        return true;
    }

    void FixedUpdate() {

        // Where is the player?
        playerAngle = playerPivot.GetComponent<SHControls>().GetAngle();
        playerLanes = playerPivot.GetComponent<SHControls>().GetTouchingLanes();

        // If a response is being carried out, then don't try parsing stimuli or choosing a new response.
        if (GameParameters.OverrideInput) {
            if (executingDecision) {
                if (!(playerLanes.Count == 1 && playerLanes[0] == decidedLane)) {
                    return;
                }
                executingDecision = false; 
            }
            playerPivot.GetComponent<SHControls>().input = 0; // Reset any movement.
        }

        if (laneThreats == null || laneThreats.Count == 0) { 
            return; // The Solver probably hasn't been started yet.
        }

        if (!ParseStimuli()) { // Identify safe slices
            return; // No response is needed
        }

        if (!ConsiderResponses()) { // Choose the best slice based on raytraces finding no collisions and smallest rotation
            return; // A safe response could not be found!
        }

        // Act on the best movement options (index 0)
        decidedLane = movementOptions[0].targetLane;
        decidedInput = movementOptions[0].isClockwise ? 1 : -1;
        executingDecision = true;

        playerPivot.GetComponent<SHControls>().input = decidedInput;
    }
}
