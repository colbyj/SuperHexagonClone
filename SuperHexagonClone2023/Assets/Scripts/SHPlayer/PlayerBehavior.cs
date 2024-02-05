using System;
using System.Collections.Generic;
using Assets.Scripts.LevelBehavior;
using Assets.Scripts.LevelVisuals;
using Assets.Scripts.Logging;
using UnityEngine;

namespace Assets.Scripts.SHPlayer
{
    public class PlayerBehavior : MonoBehaviour
    {
        public static PlayerBehavior Instance;

        public static Action<SHLine> OnPlayerDied;
        public static Action<SHLine> OnCheckpointTrigger;
        public static Action OnPlayerRespawn;
        public static Action OnInputStart;
        public static Action OnInputEnd;

        /// <summary>
        /// greater than 0 is clockwise, less than 0 is counter-clockwise.
        /// </summary>
        public float Input;

        private static bool s_isDead;
        public static bool IsDead
        {
            set
            {
                if (value == s_isDead)
                    return;

                s_isDead = value;

                if (!s_isDead)
                {
                    OnPlayerRespawn?.Invoke();
                    //Debug.Log("Player respawned!");
                }
            }
            get => s_isDead;
        }

        private bool _isMoving;
        private GameObject _lastTriggerTouched = null;

        private AudioSource _audioSource;
        private Rigidbody2D _rb;
        private PolygonCollider2D _polygonCollider;
        [SerializeField] private CircleCollider2D _leftCircleCollider;
        [SerializeField] private CircleCollider2D _rightCircleCollider;

        public Vector2 BottomPolarCoords => new Vector2(GameParameters.PlayerRadius, CurrentAngle);
        public Vector2 TopPolarCoords => new Vector2(GameParameters.PlayerRadius + GameParameters.PlayerHeight, CurrentAngle);

        private void Awake()
        {
            Instance = this;

            _audioSource = GetComponent<AudioSource>();
            _rb = GetComponent<Rigidbody2D>();
            _polygonCollider = GetComponent<PolygonCollider2D>();
        }

        private void Update()
        {
            if (IsDead)
            {
                return;
            }

            if (!GameParameters.OverrideInput)
            {
                Input = UnityEngine.Input.GetAxis("Horizontal");
            }

            if (_isMoving && Input == 0)
            {
                _isMoving = false;
                OnInputEnd?.Invoke();
            }

            if (Input == 0)
            {
                return;
            }

            if (!_isMoving)
            {
                _isMoving = true;
                OnInputStart?.Invoke();
            }

            float rotation = 0;
            bool allowRotation = false;

            var contactFilter = new ContactFilter2D
            {
                layerMask = 8 // Check for threats only
            };
            var leftResults = new List<Collider2D>();
            var rightResults = new List<Collider2D>();

            bool leftCollision = _leftCircleCollider.OverlapCollider(contactFilter, leftResults) > 0;
            bool rightCollision = _rightCircleCollider.OverlapCollider(contactFilter, rightResults) > 0;

            if (leftCollision && rightCollision && leftResults[0] is PolygonCollider2D)
            {
                SHLine lineTouched = leftResults[0].gameObject.GetComponent<SHLine>();
                Die(lineTouched);
                return;
            }

            // Horizontal input greater than 1 is the rotate right input which implies rotating clockwise
            if (Input > 0 && !rightCollision)
            {
                rotation = -DifficultyManager.Instance.PlayerRotationRate;
                allowRotation = true;
            }
            else if (Input < 0 && !leftCollision)
            {
                rotation = DifficultyManager.Instance.PlayerRotationRate;
                allowRotation = true;
            }

            if (allowRotation)
            {
                rotation *= Time.unscaledDeltaTime;
                //Debug.Log($"Rotation amount: {rotation} in {Time.unscaledDeltaTime} with input {Input}. Old rotation angle was {_rb.rotation}");
                //_rb.MoveRotation(_rb.rotation + rotation);
                transform.Rotate(Vector3.forward, rotation);
            }
        }

        public float CurrentAngle => gameObject.transform.rotation.eulerAngles.z;

        // Figures out what lanes the player is currently in. Usually one lane, unless player is on the boundary of two lanes.
        public List<GameObject> GetTouchingLanes()
        {
            GameObject[] lanes = GameObject.FindGameObjectsWithTag("Lane");
            var playerCollider = GetComponent<PolygonCollider2D>();
            var currentLanes = new List<GameObject>();

            for (int i = 0; i < lanes.Length; i++)
            {
                var laneCollider = lanes[i].GetComponent<PolygonCollider2D>();
                if (playerCollider.IsTouching(laneCollider))
                {
                    currentLanes.Add(lanes[i]);
                }
            }

            return currentLanes;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!GameParameters.EnableCollisions)
                return;

            if (collision.gameObject.tag == "Threat" && collision is EdgeCollider2D)
            {
                //Debug.Log($"You are not a super hexagon because you touched {col.gameObject.name} with parent {col.gameObject.transform.parent.name}");
                SHLine lineTouched = collision.gameObject.GetComponent<SHLine>();
                Die(lineTouched);
            }
        }



        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!GameParameters.EnableCollisions)
                return;

            // Player went past a checkpoint trigger, and that the trigger wasn't the same as what they just touched.
            if (collision.gameObject.tag == "Trigger" && collision is PolygonCollider2D && _lastTriggerTouched != collision.gameObject)
            {
                _lastTriggerTouched = collision.gameObject;
                SHLine lineTouched = collision.gameObject.GetComponent<SHLine>();
                OnCheckpointTrigger?.Invoke(lineTouched);
            }
        }

        public void Die(SHLine lineTouched)
        {
            OnPlayerDied?.Invoke(lineTouched);
            _audioSource.Play();

            var exp = FindObjectOfType<Experiment>();

            if (exp)
            {
                exp.EndTrial();
            }
            DifficultyManager.Instance.ResetDifficulty();

            IsDead = true;
        }
    }
}