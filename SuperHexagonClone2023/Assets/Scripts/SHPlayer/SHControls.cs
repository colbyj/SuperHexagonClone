using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SHControls : MonoBehaviour
{
    public float input;
    private SHPlayer player;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = FindObjectOfType<SHPlayer>();
    }

    void FixedUpdate()
    {
        if (!GameParameters.OverrideInput)
        {
            input = Input.GetAxisRaw("Horizontal");
        }

        if (input == 0)
            return;

        float rotation = 0;

        //Horizontal input greater than 1 is the rotate right input which implies rotating clockwise
        if (input > 0)
        {
             rotation = -DifficultyManager.Instance.PlayerRotationRate;
        }
        else if (input < 0)
        {
            rotation = DifficultyManager.Instance.PlayerRotationRate;
        }

        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.layerMask = 8;  // Check for threats only
        List<Collider2D> results = new List<Collider2D>();

        rb.MoveRotation(rb.rotation + rotation);
        int count = rb.OverlapCollider(contactFilter, results);

        if (count > 0)  // Prevent player from going through walls.
            rb.MoveRotation(rb.rotation - rotation);
    }

    public float GetAngle()
    {
        return gameObject.transform.rotation.eulerAngles.z;
    }

    // Figures out what lanes the player is currently in. Usually one lane, unless player is on the boundary of two lanes.
    public List<GameObject> GetTouchingLanes()
    {
        GameObject[] lanes = GameObject.FindGameObjectsWithTag("Lane");
        CircleCollider2D playerCollider = player.GetComponent<CircleCollider2D>();
        List<GameObject> currentLanes = new List<GameObject>();

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
}
