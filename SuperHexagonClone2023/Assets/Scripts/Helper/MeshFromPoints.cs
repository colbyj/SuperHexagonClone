using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshFromPoints : MonoBehaviour 
{
    public Vector2[] Points;
    public bool Invert;
    [SerializeField] private PolygonCollider2D _polygonCollider;

    internal virtual void Awake() 
    {
        Triangulator tri = new Triangulator(Points);
        GetComponent<MeshFilter>().mesh = tri.CreateMesh(Invert);

        if (_polygonCollider != null)
        {
            _polygonCollider.SetPath(0, Points);
        }
    }
}
