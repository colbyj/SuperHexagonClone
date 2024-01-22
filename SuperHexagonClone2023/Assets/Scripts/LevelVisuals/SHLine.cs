using System;
using System.Collections;
using System.Linq;
using Assets.Scripts.LevelBehavior;
using Assets.Scripts.SHPlayer;
using CustomExtensions;
using UnityEngine;
using UnityEngine.EventSystems;
using Assets.Scripts.Solver;

namespace Assets.Scripts.LevelVisuals
{
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
        private static SHLine s_selectedLine;
        public static Action SelectedThreatChanged;

        [SerializeField] private bool _isSelected;
        private float _lastRadius = ThreatManager.SpawnPatternsUntilRadius;
        [SerializeField] private float _radius = ThreatManager.SpawnPatternsUntilRadius;
        [SerializeField] private float _thickness;

        private EdgeCollider2D _edgeCol;
        private PolygonCollider2D _polyCol;
        private Mesh _mesh;
        private MeshFilter _meshFilter;
        public MeshRenderer _meshRenderer;

        [SerializeField] private bool _needsUpdate = true;
        [SerializeField] private Vector3[] _points;
        private int[] _indices;

        // Materials
        public Material TriggerMaterial;
        public Material TriggerPreviewMaterial;
        public Material StandardMaterial;
        public Material EditMaterial;

        private float _spawnRadiusOffset;


        /// <summary>
        /// Are we repurposing the SHLine to simply trigger logs?
        /// </summary>
        public bool IsTriggerOnly
        {
            get { return _isTriggerOnly; }
            set
            {
                _isTriggerOnly = value;
                tag = _isTriggerOnly ? "Trigger" : _defaultTag;
                _polyCol.isTrigger = _isTriggerOnly;
                SetCorrectMaterial();
            }
        }
        [SerializeField] private bool _isTriggerOnly;
        [SerializeField] private string _defaultTag = "Threat";


        public PatternInstance AssociatedPatternInstance
        {
            get;
            private set;
        }

        public Pattern.Wall AssociatedWall
        {
            get;
            private set;
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value && SelectedLine != null && SelectedLine != this)
                {
                    // Ensure if we are selecting something that any other line that was selected is now not selected.
                    SelectedLine.IsSelected = false;
                    SelectedLine = null;
                }

                _isSelected = value;
                SelectedLine = _isSelected ? this : null;
                SetCorrectMaterial();
            }
        }

        /// <summary>
        /// Distance from the centre of the game. (Inner radius)
        /// </summary>
        public float Radius
        {
            get => _radius;
            set
            {
                _lastRadius = _radius;
                _radius = value;
                _needsUpdate = true;
            }
        }

        public float RadiusOuter => Radius + Thickness;

        /// <summary>
        /// Is this a line that player could move into?
        /// </summary>
        public bool IsBesidePlayer
        {
            get
            {
                return _radius < GameParameters.PlayerRadius && RadiusOuter > GameParameters.PlayerRadius;
            }
        }

        public static SHLine SelectedLine
        {
            get => s_selectedLine;
            private set
            {
                s_selectedLine = value;
                SelectedThreatChanged?.Invoke();
            }
        }

        public float Thickness
        {
            get => _thickness;
            set
            {
                _thickness = value;
                _needsUpdate = true;
            }
        }

        public float Angle => gameObject.transform.eulerAngles.z;

        public float ArcLengthDegrees = 360f / LaneManager.LanesRequired;

        /// <summary>
        /// The "right" side of the line (given that Unity's angles go in a CCW direction)
        /// </summary>
        public float AngleStart => (gameObject.transform.eulerAngles.z - (180f / LaneManager.LanesRequired)) % 360;
        /// <summary>
        /// The "left" side of the line (given that Unity's angles go in a CCW direction)
        /// </summary>
        public float AngleEnd => (gameObject.transform.eulerAngles.z + (180f / LaneManager.LanesRequired)) % 360;


        private void Awake()
        {
            _polyCol = GetComponent<PolygonCollider2D>();
            _edgeCol = GetComponent<EdgeCollider2D>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();

            _mesh = new Mesh();
            _points = new Vector3[4];

            Init();
        }

        private void Init(bool resetPosition = false)
        {
            transform.position = new Vector3(0f, 0f, -0.2f);

            if (StandardMaterial == null)
            {
                StandardMaterial = _meshRenderer.material;
            }

            _polyCol.isTrigger = _isTriggerOnly;

            // This is just done to call the property's get function.
            IsTriggerOnly = _isTriggerOnly;

            if ((tag == "Threat" || tag == "Trigger") && resetPosition)
            {
                Radius = ThreatManager.SpawnPatternsUntilRadius; // what is set in the prefab is ignored
            }

            UpdatePolygon();
        }

        private void OnEnable()
        {
            Init(true);
        }

        private void Update()
        {
            if (_needsUpdate)
            {
                UpdatePolygon();
            }
        }

        private void SetCorrectMaterial()
        {
            if (_isTriggerOnly && !_isSelected)
            {
                _meshRenderer.material = ThreatManager.AreTriggersVisible ? TriggerPreviewMaterial : TriggerMaterial;
            }
            else
            {
                _meshRenderer.material = _isSelected ? EditMaterial : StandardMaterial;
            }
        }

        /// <summary>
        /// Length of a side of the hexagon (in cartesian coordinates) for a given distance from the centre of the screen (radius).
        /// </summary>
        /// <param name="radius"></param>
        /// <returns></returns>
        private static float ArcLength(float radius)
        {
            // Careful: radius uses same name as class variable.
            if (radius < 0f)
            {
                return 0;
            }

            // Math from http://mathworld.wolfram.com/RegularPolygon.html
            return 2 * radius * Mathf.Tan(Mathf.PI / (float)LaneManager.LanesRequired);
        }

        private void UpdatePoints()
        {
            float displayRadius = Radius;
            if (Radius < 0f)
            {
                displayRadius = 0f;
            }

            float arcLengthOuter = ArcLength(RadiusOuter);
            float arcLengthInner = ArcLength(displayRadius);
            float radiusOuter = RadiusOuter;

            if (radiusOuter < 0f)
            {
                radiusOuter = 0f;
            }

            _points[0] = new Vector3(-arcLengthOuter / 2.0f, radiusOuter, 0);
            _points[1] = new Vector3(arcLengthOuter / 2.0f, radiusOuter, 0);
            _points[2] = new Vector3(arcLengthInner / 2.0f, displayRadius, 0);
            _points[3] = new Vector3(-arcLengthInner / 2.0f, displayRadius, 0);

            _mesh.vertices = _points;
            _meshFilter.mesh = _mesh;

            // The indices need to be done once, since there will always be the same number of points in the same order.
            if (_mesh.triangles == null || _mesh.triangles.Length == 0)
            {
                var tri = new Triangulator(_points);
                _indices = tri.Triangulate();

                _mesh.triangles = _indices;
            }
        }

        public bool HasJustPassedRadius(float otherRadius)
        {
            return _lastRadius > otherRadius && _radius < otherRadius;
        }

        public bool AngleIsWithin(float testAngle)
        {
            float angleDelta = Mathf.Abs(Mathf.DeltaAngle(testAngle, Angle));
            return angleDelta <= ArcLengthDegrees / 2;
        }

        public Solver.MovementOption ToMoveToThis(float testAngle, float acceptableAngleDelta = 5f)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(testAngle, Angle)) <= 5)
            {
                return Solver.MovementOption.None;
            }
            else if (testAngle > Angle)
            {
                return Solver.MovementOption.Clockwise;
            }
            else if (testAngle < Angle)
            {
                return Solver.MovementOption.CounterClockwise;
            }

            return Solver.MovementOption.None;
        }

        /// <summary>
        /// Work out how far away (from "left" side) a given angle is.
        /// If the specified angle is within the angles bounded by the line, then returns 0.
        /// </summary>
        /// <param name="fromAngle"></param>
        /// <returns></returns>
        public float ClockwiseDistance(float fromAngle)
        {
            float target = AngleEnd;
            if (AngleEnd > fromAngle)
                fromAngle += 360;

            return fromAngle - target;
        }

        /// <summary>
        /// Work out how far away (from "right" side) a given angle is.
        /// If the specified angle is within the angles bounded by the line, then returns 0.
        /// </summary>
        /// <param name="fromAngle"></param>
        /// <returns></returns>
        public float CounterclockwiseDistance(float fromAngle)
        {
            float target = AngleStart;
            if (target < fromAngle)
                target += 360;

            return target - fromAngle;
        }

        public void OnMouseDown()
        {
            if (!ThreatManager.IsEditScene || !PlayerBehavior.IsDead)
                return;

            if (EventSystem.current.IsPointerOverGameObject()) 
                return;
            
            IsSelected = !IsSelected;
        }

        public void ResetLine()
        {
            Radius = ThreatManager.SpawnPatternsUntilRadius;
            Thickness = 0;
            UpdatePolygon();
        }

        public void UpdatePolygon()
        {
            UpdatePoints();
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            Vector2[] points2D = _points.ToVector2Array();
            _polyCol.SetPath(0, points2D);
            _edgeCol.SetPoints(points2D.Skip(2).Take(2).ToList());

            _needsUpdate = false;
        }

        public void SetAssociations(PatternInstance patternInstance, Pattern.Wall wall, float? spawnRadiusOffset = null)
        {
            AssociatedPatternInstance = patternInstance;
            AssociatedWall = wall;
            
            RebuildFromAssociations(spawnRadiusOffset);
        }

        public void RebuildFromAssociations(float? newSpawnRadiusOffset = null)
        {
            if (newSpawnRadiusOffset.HasValue)
            {
                _spawnRadiusOffset = newSpawnRadiusOffset.Value;
            }

            int sideIndex = (AssociatedWall.Side + AssociatedPatternInstance.RotationOffset) % ThreatManager.LanesRequired;
            if (AssociatedPatternInstance.Mirrored)
            {
                sideIndex = ThreatManager.LanesRequired - 1 - sideIndex;
            }

            float wallDistance = _spawnRadiusOffset + AssociatedWall.Distance + AssociatedPatternInstance.DistanceOffset;
            float rotation = sideIndex * (360f / ThreatManager.LanesRequired);

            Radius = wallDistance;
            Thickness = AssociatedWall.Height;
            IsTriggerOnly = AssociatedWall.IsTrigger;

            // Reset the position and rotation of the line/threat.
            transform.position = new Vector3(0f, 0f, -0.2f);
            transform.rotation = Quaternion.Euler(0, 0, rotation);

            UpdatePolygon();
        }

        # region Fading
        private const float FadeAmount = 0.05f;
        private const float FadeWait = 0.05f;

        public void StartFadeIn()
        {
            if (tag != "Threat")
                return;

            StartCoroutine(FadeIn());
        }

        private void StartFadeOut()
        {
            if (tag != "Threat")
                return;

            StartCoroutine(FadeOut());
        }

        private IEnumerator FadeIn()
        {
            float alpha = 0f;

            while (alpha < 1f)
            {
                _meshRenderer.material.color = new Color(_meshRenderer.material.color.r, _meshRenderer.material.color.g, _meshRenderer.material.color.b, alpha);
                alpha += FadeAmount;
                yield return new WaitForSecondsRealtime(FadeWait);
            }
        }

        private IEnumerator FadeOut()
        {
            float alpha = 1f;

            while (alpha > 0)
            {
                _meshRenderer.material.color = new Color(_meshRenderer.material.color.r, _meshRenderer.material.color.g, _meshRenderer.material.color.b, alpha);
                alpha += FadeAmount;
                yield return new WaitForSecondsRealtime(FadeWait);
            }
        }
        #endregion
    }
}