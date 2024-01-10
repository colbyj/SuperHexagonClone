using CustomExtensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// This draws the lines which act as obstacles as well as the central hexagon.
/// If velocity is set, these lines will also move.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(EdgeCollider2D))]
public class SHLine : MonoBehaviour
{
    [Header("Display Parameters")]
    /// <summary>
    /// Distance from the centre of the game. (Inner radius)
    /// </summary>
    public float radius = ThreatParameters.StartingRadius;
    public float thickness = ThreatParameters.DefaultThickness;
    public int laneCount = 6;  // This gets set by LaneThreat

    [Header("Position Parameters")]
    /// <summary>
    /// Radial velocity in radii per second.
    /// </summary>
    public float velocity = 0f;
    public Vector3[] points;

    private PolygonCollider2D polyCol;
    private EdgeCollider2D edgeCol;
    private MeshRenderer meshRenderer;

    // To avoid wasting memory, only create an use one mesh object
    private Mesh mesh;
    private int[] indicies;

    public static Action SelectedThreatChanged;
    public static SHLine selectedLine 
    {
        get { return _selectedLine; }
        set { 
            _selectedLine = value;
            if (SelectedThreatChanged != null)
                SelectedThreatChanged.Invoke();
        }
    }
    private static SHLine _selectedLine = null;
    private bool _isSelected = false;
    public bool isSelected 
    {
        get
        {
            return _isSelected;
        }
        set
        {
            if (value == true && selectedLine != null && selectedLine != this)
            { 
                // Ensure if we are selecting something that any other line that was selected is now not selected.
                selectedLine.isSelected = false;
                selectedLine = null;
            }

            _isSelected = value;
            if (value)
            {
                meshRenderer.material = editMaterial;
                selectedLine = this;
            }
            else
            {
                meshRenderer.material = standardMaterial;
                selectedLine = null;
            }
        }
    }
    
    // Materials
    public Material standardMaterial;
    public Material editMaterial;


    // Use this for initialization
    void Awake()
    {
        Init();
    }

    void OnEnable()
    {
        Init(true);
    }

    public void Init(bool resetPosition = false)
    {
        transform.position = new Vector3(0f, 0f, -0.2f);
        polyCol = GetComponent<PolygonCollider2D>();
        edgeCol = GetComponent<EdgeCollider2D>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (standardMaterial == null)
        {
            standardMaterial = meshRenderer.material;
        }
        else
        {
            meshRenderer.material = standardMaterial;
        }

        if (tag == "Threat" && resetPosition)
        {
            radius = ThreatParameters.StartingRadius;
            velocity = DifficultyManager.Instance.ThreatSpeed;
        } // what is set in the prefab is ignored

        laneCount = LaneManager.lanesRequired;

        mesh = new Mesh();
        points = new Vector3[4];
        UpdatePoints();

        // The indicies need to be done once, since there will always be the same number of points in the same order.
        Triangulator tri = new Triangulator(points);
        indicies = tri.Triangulate();

        UpdatePolygon();

        GetComponent<MeshFilter>().mesh = mesh;
    }

    // Update is called once per frame
    void Update()
    {
        if (LaneManager.Instance.Paused)
            return;

        radius -= velocity * Time.deltaTime;

        // Threats are removed by SHLane.
        UpdatePolygon();
    }

    public void UpdatePolygon()
    {
        UpdatePoints();
        UpdateMesh();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var points2D = points.ToVector2Array();
        polyCol.SetPath(0, points2D);
        edgeCol.SetPoints(points2D.Skip(2).Take(2).ToList());
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

    public void ResetLine()
    {
        radius = ThreatParameters.StartingRadius;
        thickness = ThreatParameters.DefaultThickness;
        velocity = 0;
        UpdatePolygon();
    }

    public void OnMouseDown()
    {
        isSelected = !isSelected;
    }
}
