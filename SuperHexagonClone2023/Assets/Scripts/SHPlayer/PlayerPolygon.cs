using UnityEngine;

namespace Assets.Scripts.SHPlayer
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(PolygonCollider2D))]
    [RequireComponent(typeof(AudioSource))]
    public class PlayerPolygon : MeshFromPoints
    {
        private PolygonCollider2D _polyCollider;

        internal override void Awake()
        {
            Points = new[]
            {
                new Vector2(0f, GameParameters.PlayerHeight),
                new Vector2(-GameParameters.PlayerWidth/2, 0f),
                new Vector2(GameParameters.PlayerWidth/2, 0f),
            };

            Points[0].y = GameParameters.PlayerHeight + GameParameters.PlayerRadius;
            Points[1].y = GameParameters.PlayerRadius;
            Points[2].y = GameParameters.PlayerRadius;

            _polyCollider = GetComponent<PolygonCollider2D>();
            _polyCollider.points = Points;

            base.Awake();
        }
    }
}
