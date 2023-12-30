using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshFromPoints : MonoBehaviour 
{
    public Vector2[] points;
    public bool invert;
    [SerializeField] private PolygonCollider2D polygonCollider;

    void Start() 
    {
        Triangulator tri = new Triangulator(points);
        GetComponent<MeshFilter>().mesh = tri.CreateMesh(invert);

        if (polygonCollider != null)
        {
            polygonCollider.SetPath(0, points);
        }
    }
}
