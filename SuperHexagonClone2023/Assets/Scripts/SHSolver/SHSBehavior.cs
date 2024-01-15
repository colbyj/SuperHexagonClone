using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.LevelVisuals;
using Assets.Scripts.SHPlayer;
using UnityEngine;

public class SHSBehavior : MonoBehaviour
{
    /*class CompareInnerRadiuses : IComparer <ThreatPosition> {
        public ThreatPosition Compare (ThreatPosition x, ThreatPosition y) {
        }
    }*/

    // These static variables are used by LaneThreats and MovementOptions.
    public static GameObject PlayerPivot;
    public static GameObject Player;

    [Header("Preferences")] public float LookAheadRadius = 50f;

    [Header("Player State")] private float _playerAngle;
    private List<GameObject> _playerLanes;


    [Header("Stimuli Identification")]
    public float PlayerDeathIn; // radius at which player will die. is public simply so it shows up in the editor...

    public float NextNearestRadius; // public for debugging purposes
    public List<int> SafestLanes;
    public List<LaneThreats> LaneThreats = new List<LaneThreats>();

    [Header("Response Selection")] public bool ExecutingDecision = false;
    public GameObject DecidedLane;
    public int DecidedInput;
    public List<MovementOption> MovementOptions = new List<MovementOption>();


    // Start is called before the first frame update
    private void Start()
    {
        Invoke("BeginSolving", 1.0f); // TODO: More robust solution... just wanna get this going asap.
        PlayerPivot = GameObject.Find("PlayerPivot");
        //playerPivot.GetComponent<PlayerBehavior>().overrideInput = overrideInput;

        Player = GameObject.Find("Player");
    }

    private void BeginSolving()
    {
        var lanes = FindObjectsOfType<SHLane>();
        foreach (var lane in lanes)
        {
            var lt = new LaneThreats();
            lt.ShLane = lane;
            lt.GoLane = lane.gameObject;

            LaneThreats.Add(lt);
        }
    }

    private bool ParseStimuli()
    {
        // Rebuild the metadata regarding the threat positions.
        for (var idxLane = 0; idxLane < LaneThreats.Count; idxLane++)
        {
            LaneThreats[idxLane].ThreatPositions = new List<LaneThreats.ThreatPosition>();
            var currentLane = LaneThreats[idxLane].ShLane;

            // Closer threats should be lower indexes, so break the look when we exceed our threshold lookAheadRadius
            for (var idxThreat = 0; idxThreat < currentLane.ActiveThreats.Count; idxThreat++)
            {
                var threatLine = currentLane.ActiveThreats[idxThreat].GetComponent<SHLine>();

                if (threatLine.Radius <= LookAheadRadius)
                {
                    LaneThreats[idxLane].ThreatPositions.Add(
                        new LaneThreats.ThreatPosition()
                        {
                            InnerRadius = threatLine.Radius,
                            OuterRadius = threatLine.RadiusOuter()
                        }
                    );
                }
                else
                {
                    break;
                }
            }

            // Sort threats by inner radius and remove those that are past the player.
            LaneThreats[idxLane].ThreatPositions = LaneThreats[idxLane].ThreatPositions.OrderBy(t => t.InnerRadius)
                .Where(t => t.OuterRadius > GameParameters.PlayerRadius - 0.5f).ToList();

            // Now that the list is sorted, the first index should have the closest obstacle
            if (LaneThreats[idxLane].ThreatPositions.Count > 0)
            {
                LaneThreats[idxLane].NearestInnerRadius = LaneThreats[idxLane].ThreatPositions[0].InnerRadius;
            }
            else
            {
                LaneThreats[idxLane].NearestInnerRadius = LookAheadRadius;
            }
        }

        // How soon will the player die if they don't move? (in Radii)
        PlayerDeathIn = LookAheadRadius;

        for (var idxLane = 0; idxLane < LaneThreats.Count; idxLane++)
        {
            if (_playerLanes.IndexOf(LaneThreats[idxLane].GoLane) == -1)
            {
                continue;
            }

            // Player is not in this lane.
            if (LaneThreats[idxLane].NearestInnerRadius < PlayerDeathIn)
            {
                PlayerDeathIn = LaneThreats[idxLane].NearestInnerRadius;
            }
        }


        // Are any of the lanes safer than the one we are in?
        NextNearestRadius = LookAheadRadius;

        // this could be improved..
        // a safer lane will have a radius > playerDeathIn and < any other.

        for (var idxLane = 0; idxLane < LaneThreats.Count; idxLane++)
            if (LaneThreats[idxLane].NearestInnerRadius < NextNearestRadius &&
                LaneThreats[idxLane].NearestInnerRadius > PlayerDeathIn)
            {
                NextNearestRadius = LaneThreats[idxLane].NearestInnerRadius;
            }

        if (NextNearestRadius == PlayerDeathIn) // No safer lanes were found. Don't do anything.
        {
            return false;
        }

        // Now see if there are multiple lanes which are equally safe        
        SafestLanes = new List<int>();

        for (var idxLane = 0; idxLane < LaneThreats.Count; idxLane++)
            if (LaneThreats[idxLane].NearestInnerRadius == NextNearestRadius)
            {
                SafestLanes.Add(idxLane);
            }

        if (SafestLanes.Count == 0) // No safe lanes! The first if statement should prevent this from occurring.
        {
            return false;
        }

        return true;
    }

    public bool ConsiderResponses()
    {
        // Don't try moving until the outer radius of the nearest inner radius obstacle is past the player!
        MovementOptions = new List<MovementOption>();

        foreach (var idxLane in SafestLanes)
        {
            var angle = LaneThreats[idxLane].GoLane.transform.eulerAngles.z;
            var deltaAngleCcw = angle - _playerAngle;

            if (deltaAngleCcw < 0)
            {
                deltaAngleCcw += 360;
            }

            var deltaAngleCw = 360 - deltaAngleCcw;

            MovementOptions.Add(new MovementOption(LaneThreats[idxLane].GoLane, deltaAngleCw, true));
            MovementOptions.Add(new MovementOption(LaneThreats[idxLane].GoLane, deltaAngleCcw, false));
        }

        MovementOptions.Sort();
        //Debug.Log("movementOptions.Count = " + movementOptions.Count);

        if (MovementOptions.Count == 0) // No options! Can't do anything.
        {
            return false;
        }

        if (!GameParameters.OverrideInput)
        {
            return false;
        }

        if (!MovementOptions[0]
                .IsMovementPossible) // Don't try to move as it will fail. Maybe waiting will make that movement possible.
        {
            return false;
        }

        // This section: Work out how long we can wait before needing to move.
        var furthestRadius = PlayerDeathIn;
        const float stepSize = 0.02f; // TODO: scope

        while (true)
        {
            // Evaluate movement option
            var mo = new MovementOption(
                MovementOptions[0].TargetLane,
                MovementOptions[0].AngleDelta,
                MovementOptions[0].IsClockwise,
                furthestRadius);

            if (mo.IsMovementPossible)
            {
                break;
            }

            furthestRadius -= stepSize;
        }

        Debug.Log("The furthest radius is: " + furthestRadius);

        return true;
    }

    private void FixedUpdate()
    {
        // Where is the player?
        _playerAngle = PlayerPivot.GetComponent<PlayerBehavior>().GetAngle();
        _playerLanes = PlayerPivot.GetComponent<PlayerBehavior>().GetTouchingLanes();

        // If a response is being carried out, then don't try parsing stimuli or choosing a new response.
        if (GameParameters.OverrideInput)
        {
            if (ExecutingDecision)
            {
                if (!(_playerLanes.Count == 1 && _playerLanes[0] == DecidedLane))
                {
                    return;
                }

                ExecutingDecision = false;
            }

            PlayerPivot.GetComponent<PlayerBehavior>().Input = 0; // Reset any movement.
        }

        if (LaneThreats == null || LaneThreats.Count == 0)
        {
            return;
        }
        // The Solver probably hasn't been started yet.

        if (!ParseStimuli()) // Identify safe slices
        {
            return;
        }
        // No response is needed

        if (!ConsiderResponses()) // Choose the best slice based on raytraces finding no collisions and smallest rotation
        {
            return;
        }
        // A safe response could not be found!

        // Act on the best movement options (index 0)
        DecidedLane = MovementOptions[0].TargetLane;
        DecidedInput = MovementOptions[0].IsClockwise ? 1 : -1;
        ExecutingDecision = true;

        PlayerPivot.GetComponent<PlayerBehavior>().Input = DecidedInput;
    }
}