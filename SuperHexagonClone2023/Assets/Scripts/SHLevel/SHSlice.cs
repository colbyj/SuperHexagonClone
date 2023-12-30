using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
// Summary:
//     Simply creates a mesh for the "slices" of the hexagon stage.
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SHSlice : MonoBehaviour
{

    public float radius = 2.5f;
    public int laneCount = 6; // TODO: Add support for levels other than hexagons
    private PolygonCollider2D polyCol;


    // Use this for initialization
    void Start()
    {
        laneCount = LaneManager.lanesRequired;

        Vector2[] points = new Vector2[3];
        float arcLengthOuter = ArcLengthOuter();

        points[0] = new Vector2(0f, 0f);
        points[1] = new Vector2(-arcLengthOuter / 2.0f, radius);
        points[2] = new Vector2(arcLengthOuter / 2.0f, radius);

        Triangulator tri = new Triangulator(points);
        GetComponent<MeshFilter>().mesh = tri.CreateMesh();

        polyCol = GetComponent<PolygonCollider2D>();

        if (polyCol != null)
        {
            polyCol.SetPath(0, points);
        }

    }

    // Update is called once per frame
    void Update()
    {

    }

    float ArcLengthOuter()
    {
        return 2 * (radius) * Mathf.Tan(Mathf.PI / (float)laneCount);
    }
}
