using System;
using System.Collections.Generic;
using Assets.Scripts.LevelVisuals;
using UnityEngine;

[Serializable]
public class LaneThreats
{
    public List<ThreatPosition> ThreatPositions;
    public float NearestInnerRadius;
    public SHLane ShLane;
    public GameObject GoLane;

    [Serializable]
    public class ThreatPosition
    {
        public float InnerRadius;
        public float OuterRadius;

        public override string ToString()
        {
            return string.Format("innerRadius = {0}, outerRadius = {1}", InnerRadius, OuterRadius);
        }
    }
}