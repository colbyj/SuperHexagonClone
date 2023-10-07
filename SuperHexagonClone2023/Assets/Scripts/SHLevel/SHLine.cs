using CustomExtensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//
// Summary:
//          This draws the lines which act as obstacles as well as the central hexagon.
//          If velocity is set, these lines will also move.
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class SHLine : MonoBehaviour
{
    /// <summary>
    /// Distance from the centre of the game. (Inner radius)
    /// </summary>
    [HideInInspector] public float radius = GameParameters.ThreatStartingRadius;
    [HideInInspector] public float thickness = 3f;
    public int laneCount = 6;  // This gets set by LaneThreat
    /// <summary>
    /// Radial velocity in radii per second.
    /// </summary>
    public float velocity = 0f;
    public Vector3[] points;
    private PolygonCollider2D polyCol;

    // To avoid wasting memory, only create an use one mesh object
    private Mesh mesh;
    private int[] indicies;

    // Use this for initialization
    void Start()
    {
        transform.position = new Vector3(0f, 0f, -0.2f);
        polyCol = GetComponent<PolygonCollider2D>();

        laneCount = StageConstructor.laneCount;

        mesh = new Mesh();
        points = new Vector3[4];
        UpdatePoints();

        // The indicies need to be done once, since there will always be the same number of points in the same order.
        Triangulator tri = new Triangulator(points);
        indicies = tri.Triangulate();

        UpdateMesh();

        GetComponent<MeshFilter>().mesh = mesh;
    }

    void OnEnable()
    {
        transform.position = new Vector3(0f, 0f, -0.2f);

        polyCol = GetComponent<PolygonCollider2D>();

        if (tag == "Threat")
        {
            radius = GameParameters.ThreatStartingRadius;
            velocity = GameParameters.ThreatRadialRate;
        } // what is set in the prefab is sgnored
        laneCount = StageConstructor.laneCount;

        mesh = new Mesh();
        points = new Vector3[4];
        UpdatePoints();

        // The indicies need to be done once, since there will always be the same number of points in the same order.
        Triangulator tri = new Triangulator(points);
        indicies = tri.Triangulate();

        UpdateMesh();

        GetComponent<MeshFilter>().mesh = mesh;
    }

    // Update is called once per frame
    void Update()
    {
        radius -= velocity * Time.deltaTime;

        // Threats are removed by SHLane.

        UpdatePoints();
        UpdateMesh();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        polyCol.SetPath(0, points.ToVector2Array());
    }

    private void UpdatePoints()
    {
        float displayRadius = radius;
        if (radius < 0f)
        {
            displayRadius = 0f;
        }

        float arcLengthOuter = ArcLength(RadiusOuter());
        float arcLengthInner = ArcLength(displayRadius);
        float radiusOuter = RadiusOuter();

        points[0] = new Vector3(-arcLengthOuter / 2.0f, radiusOuter, 0);
        points[1] = new Vector3(arcLengthOuter / 2.0f, radiusOuter, 0);
        points[2] = new Vector3(arcLengthInner / 2.0f, displayRadius, 0);
        points[3] = new Vector3(-arcLengthInner / 2.0f, displayRadius, 0);
    }

    public void UpdateMesh()
    {
        mesh.vertices = points;
        mesh.triangles = indicies;

    }

    public float GetAngle()
    {
        return gameObject.transform.eulerAngles.z;
    }

    public float RadiusOuter()
    {
        return radius + thickness;
    }

    // Math from http://mathworld.wolfram.com/RegularPolygon.html
    public float ArcLength(float radius)
    { 
        // Careful: radius uses same name as class variable.
        if (radius < 0f)
        {
            return 0;
        }
        return 2 * radius * Mathf.Tan(Mathf.PI / (float)laneCount);
    }
}
