using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class LaneThreats {
    public List<ThreatPosition> threatPositions;
    public float nearestInnerRadius;
    public SHLane shLane;
    public GameObject goLane;

    [Serializable]
    public class ThreatPosition {
        public float innerRadius;
        public float outerRadius;

        public override string ToString() {
            return string.Format("innerRadius = {0}, outerRadius = {1}", innerRadius, outerRadius);
        }
    }
}