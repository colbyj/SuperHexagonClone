using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.LevelVisuals
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class PolygonMesh : MonoBehaviour
    {
        public float Radius = 2.5f;
        public float Thickness = 1f;
        public int Sides = 6;
        private PolygonCollider2D _polyCol;

        // Update is called once per frame
        void Update()
        {

        }
    }
}
