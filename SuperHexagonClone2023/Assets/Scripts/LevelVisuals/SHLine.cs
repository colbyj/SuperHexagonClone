using System;
using System.Collections;
using System.Linq;
using Assets.Scripts.LevelBehavior;
using CustomExtensions;
using UnityEngine;

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
        private MeshRenderer _meshRenderer;

        [SerializeField] private bool _needsUpdate = true;
        [SerializeField] private Vector3[] _points;
        private int[] _indices;

        // Materials
        public Material StandardMaterial;
        public Material EditMaterial;

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
                if (value)
                {
                    _meshRenderer.material = EditMaterial;
                    SelectedLine = this;
                }
                else
                {
                    _meshRenderer.material = StandardMaterial;
                    SelectedLine = null;
                }
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

        public static SHLine SelectedLine
        {
            get => s_selectedLine;
            set
            {
                s_selectedLine = value;
                if (SelectedThreatChanged != null)
                {
                    SelectedThreatChanged.Invoke();
                }
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
            else
            {
                _meshRenderer.material = StandardMaterial;
            }

            if (tag == "Threat" && resetPosition)
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

            float arcLengthOuter = ArcLength(RadiusOuter());
            float arcLengthInner = ArcLength(displayRadius);
            float radiusOuter = RadiusOuter();

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

        public float GetAngle()
        {
            return gameObject.transform.eulerAngles.z;
        }

        public bool HasJustPassedRadius(float otherRadius)
        {
            return _lastRadius > otherRadius && _radius < otherRadius;
        }

        public void OnMouseDown()
        {
            if (!ThreatManager.IsEditScene)
                return;
            
            IsSelected = !IsSelected;
        }

        public float RadiusOuter()
        {
            return Radius + Thickness;
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

        # region Fading
        private const float FadeAmount = 0.05f;
        private const float FadeWait = 0.05f;

        public void StartFadeIn()
        {
            StartCoroutine(FadeIn());
        }

        private void StartFadeOut()
        {
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