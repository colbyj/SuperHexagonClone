using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts.LevelBehavior;
using Assets.Scripts.LevelVisuals;
using Assets.Scripts.SHPlayer;
using UnityEngine;

namespace Assets.Scripts.Solver
{
    public enum SolverState
    {
        WaitingForThreats, Solving, WaitingToMove, Moving, Solved
    }

    public enum MovementOption
    {
        None, Clockwise, CounterClockwise
    }

    public class HexagonSolver : MonoBehaviour
    {
        public static HexagonSolver Instance;

        private const float IgnoreThreatsPastRadius = 60f;
        public List<SHLine> NextTriggers;
        public List<SHLine> ThreatsAlignedWithPlayer;
        public SHLine ClosestCwTrigger;
        public SHLine ClosestCcwTrigger;
        public SHLine TriggerAlignedWithPlayer;
        public float ClosestCwTriggerAngle;
        public float ClosestCcwTriggerAngle;
        public bool CanMoveCw;
        public bool CanMoveCcw;

        public bool NeedToMakeDecision;

        public SolverState State = SolverState.WaitingForThreats;
        public MovementOption BestMovementOption = MovementOption.None;
        public List<MovementOption> ValidMovementOptions = new List<MovementOption>();

        public static Action<PatternInstance> OnNextTriggersChanged;

        private void Awake()
        {
            Instance = this;
            //Time.timeScale = 0.5f; // Slow down the simulation for testing
        }

        private void Update()
        {
            //if (State == SolverState.WaitingForThreats)
            UpdateMovementOptions();

            /*if (BestMovementOption == MovementOption.CounterClockwise)
            {
                PlayerBehavior.Instance.Input = -1;
            }
            else if (BestMovementOption == MovementOption.Clockwise)
            {
                PlayerBehavior.Instance.Input = 1;
            }
            else
            {
                PlayerBehavior.Instance.Input = 0;
            }*/
        }

        private void OnGUI()
        {

            /*if (BestMovementOption == MovementOption.CounterClockwise)
            {
                GUI.Box(new Rect(0, 0, 100, Screen.height), $"LEFT!");
            }
            else if (BestMovementOption == MovementOption.Clockwise)
            {
                GUI.Box(new Rect(Screen.width-100, 0, 100, Screen.height), $"RIGHT!");
            }*/
            
            
        }

        private bool AreTriggerListsEqual(List<SHLine> l1, List<SHLine> l2)
        {
            if (l1.Count != l2.Count) return false;

            
            for (int i = 0; i < l1.Count; i++)
            {
                if (l1[i] != l2[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void UpdateMovementOptions()
        {
            if (ThreatManager.Instance.PatternsOnScreen.Count == 0)
            {
                BestMovementOption = MovementOption.None;
                return;  // No threat; nothing to solve!
            }

            PatternInstance pi = ThreatManager.Instance.PatternsOnScreen[0];

            if (pi.ClosestThreat.Radius > IgnoreThreatsPastRadius)
            {
                BestMovementOption = MovementOption.None;
                return;  // Too far away to see right now.
            }

            // TODO: Don't calculate this every frame; it is wasteful.
            // Work out which safe lane(s) are next.
            List<SHLine> nextTriggers = pi.NextTriggers();

            bool triggersChanged = false;

            if (!AreTriggerListsEqual(NextTriggers, nextTriggers))
            {
                NeedToMakeDecision = true;
                NextTriggers = nextTriggers;

                triggersChanged = true;
            }

            if (!NeedToMakeDecision) // Already made a decision about what to do, just need to carry out that decision.
                return;

            UpdateClosestTriggers();
            UpdateTriggerAlignedWithPlayer();

            if (TriggerAlignedWithPlayer != null)
            {
                // This is intended to get players to move toward the centre of the trigger
                //BestMovementOption = TriggerAlignedWithPlayer.ToMoveToThis(PlayerBehavior.Instance.CurrentAngle);
                BestMovementOption = MovementOption.None;
                
                if (triggersChanged)
                {
                    Debug.Log("Triggers changed.");
                    OnNextTriggersChanged?.Invoke(pi);
                }

                return; // The player is already where they need to be, stop checking anything else.
            }
            else
            {
                UpdateThreatsAlignedWithPlayer();
                UpdateCanMove();
            }

            if (ClosestCcwTriggerAngle <= ClosestCwTriggerAngle && CanMoveCcw) // A CCW rotation would be faster, and we can do it.
            {
                BestMovementOption = MovementOption.CounterClockwise;
            }
            else if (ClosestCwTriggerAngle <= ClosestCcwTriggerAngle && CanMoveCw)
            {
                BestMovementOption = MovementOption.Clockwise;
            }

            if (triggersChanged)
            {
                Debug.Log("Triggers changed.");
                OnNextTriggersChanged?.Invoke(pi);
            }
            //NeedToMakeDecision = false;
        }

        /// <summary>
        /// Work out which of the next trigger(s) are the closest to the player. 
        /// </summary>
        private void UpdateClosestTriggers()
        {
            float smallestCwAngle = 360;
            float smallestCcwAngle = 360;

            foreach (SHLine trigger in NextTriggers)
            {
                if (trigger.AngleIsWithin(PlayerBehavior.Instance.CurrentAngle))
                {
                    smallestCwAngle = 0;
                    smallestCcwAngle = 0;

                    ClosestCwTrigger = trigger;
                    ClosestCcwTrigger = trigger;
                    break;
                }

                float cwDist = trigger.ClockwiseDistance(PlayerBehavior.Instance.CurrentAngle);
                if (cwDist < smallestCwAngle)
                {
                    smallestCwAngle = cwDist;
                    ClosestCwTrigger = trigger;
                }

                float ccwDist = trigger.CounterclockwiseDistance(PlayerBehavior.Instance.CurrentAngle);
                if (ccwDist < smallestCcwAngle)
                {
                    smallestCcwAngle = ccwDist;
                    ClosestCcwTrigger = trigger;
                }
            }

            if (ThreatManager.AreTriggersVisible)
            {
                foreach (SHLine trigger in NextTriggers)
                {
                    if (trigger == ClosestCcwTrigger || trigger == ClosestCwTrigger)
                    {
                        trigger._meshRenderer.material = trigger.EditMaterial;
                    }
                    else
                    {
                        trigger._meshRenderer.material = trigger.TriggerPreviewMaterial;
                    }
                }
            }

            ClosestCwTriggerAngle = smallestCwAngle;
            ClosestCcwTriggerAngle = smallestCcwAngle;
        }

        /// <summary>
        /// Work out if one the triggers is aligned with the player and update "TriggerAlignedWithPlayer"
        /// </summary>
        private void UpdateTriggerAlignedWithPlayer()
        {
            if (ClosestCwTrigger != ClosestCcwTrigger)
            {
                TriggerAlignedWithPlayer = null;  // The two triggers aren't the same.
                return;
            }

            if (ClosestCwTrigger.AngleIsWithin(PlayerBehavior.Instance.CurrentAngle))
            {
                TriggerAlignedWithPlayer = ClosestCwTrigger;
            }
            else
            {
                TriggerAlignedWithPlayer = null;
            }
        }

        /// <summary>
        /// Any threats in line with the player could prevent the player from moving particular directions.
        /// </summary>
        private void UpdateThreatsAlignedWithPlayer()
        {
            ThreatsAlignedWithPlayer = new List<SHLine>();

            foreach (SHLine threat in ThreatManager.Instance.PatternsOnScreen[0].Threats)
            {
                if (threat.IsTriggerOnly)
                    continue;

                if (threat.Radius > GameParameters.PlayerRadius)
                    break; // Because of the sorted list, the next threats aren't possibly aligned with the player.

                if (threat.RadiusOuter < GameParameters.PlayerRadius)
                    continue;

                if (threat.Radius <= GameParameters.PlayerRadius && threat.RadiusOuter >= GameParameters.PlayerRadius)
                    ThreatsAlignedWithPlayer.Add(threat);
            }
        }

        private void UpdateCanMove()
        {
            if (ThreatsAlignedWithPlayer.Count == 0)
            {
                CanMoveCw = true;
                CanMoveCcw = true;
                return;
            }
            
            bool cwBlocked = false;
            bool ccwBlocked = false;

            foreach (SHLine threat in ThreatsAlignedWithPlayer)
            {
                if (threat.ClockwiseDistance(PlayerBehavior.Instance.CurrentAngle) < ClosestCwTriggerAngle)
                    cwBlocked = true;
                if (threat.CounterclockwiseDistance(PlayerBehavior.Instance.CurrentAngle) < ClosestCcwTriggerAngle)
                    ccwBlocked = true;
            }

            CanMoveCcw = !ccwBlocked;
            CanMoveCw = !cwBlocked;
        }

        /// <summary>
        /// If this is true, then the player needs to be moving in some direction to avoid catastrophe!
        /// If it's false, then simply take the shortest path to the trigger.
        /// </summary>
        /// <param name="trigger"></param>
        /// <returns></returns>
        private bool AreThreatsBeforeTrigger(SHLine trigger)
        {
            float triggerRadius = trigger.Radius;
            
            if (ThreatManager.Instance.PatternsOnScreen.Count == 0)
                return false;

            PatternInstance currentPattern = ThreatManager.Instance.PatternsOnScreen[0];
            foreach (SHLine threat in currentPattern.Threats)
            {
                if (threat.Radius < GameParameters.PlayerRadius) 
                    continue;  // Threat is past player, don't consider it.
                
                if (threat.Radius < trigger.Radius)
                {
                    return true;
                }

                return false;  // Because of the sorting, we can exit the loop earlier
            }

            return false;
        }
    }
}
