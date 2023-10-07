using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SHControls : MonoBehaviour
{

    public float input;
    private SHPlayer player;

    void Start()
    {
        player = FindObjectOfType<SHPlayer>();
    }

    void FixedUpdate()
    {
        if (!GameParameters.OverrideInput)
        {
            input = Input.GetAxisRaw("Horizontal");
        }

        //Horizontal input greater than 1 is the rotate right input which implies rotating clockwise
        if (input > 0)
        {
            transform.Rotate(Vector3.forward * -GameParameters.PlayerRotationRate);
        }
        else if (input < 0)
        {
            transform.Rotate(Vector3.forward * GameParameters.PlayerRotationRate);
        }
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
