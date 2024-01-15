using UnityEngine;

//
// Summary:
//     Simply creates a mesh for the "slices" of the hexagon stage.
namespace Assets.Scripts.LevelVisuals
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class SHSlice : MonoBehaviour
    {

        public float Radius = 2.5f;
        public int LaneCount = 6; // TODO: Add support for levels other than hexagons
        private PolygonCollider2D _polyCol;


        // Use this for initialization
        void Start()
        {
            LaneCount = LaneManager.LanesRequired;

            Vector2[] points = new Vector2[3];
            float arcLengthOuter = ArcLengthOuter();

            points[0] = new Vector2(0f, 0f);
            points[1] = new Vector2(-arcLengthOuter / 2.0f, Radius);
            points[2] = new Vector2(arcLengthOuter / 2.0f, Radius);

            Triangulator tri = new Triangulator(points);
            GetComponent<MeshFilter>().mesh = tri.CreateMesh();

            _polyCol = GetComponent<PolygonCollider2D>();

            if (_polyCol != null)
            {
                _polyCol.SetPath(0, points);
            }

        }

        // Update is called once per frame
        void Update()
        {

        }

        float ArcLengthOuter()
        {
            return 2 * (Radius) * Mathf.Tan(Mathf.PI / (float)LaneCount);
        }
    }
}
