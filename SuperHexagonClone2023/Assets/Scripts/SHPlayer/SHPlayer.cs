using CustomExtensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Code Reviewed Tuesday, May 15th, 2018 by Parker Neufeld, pdn844   */
public class SHPlayer : MonoBehaviour
{

    void OnCollisionEnter2D(Collision2D col)
    {
        if (GameParameters.EnableCollisions && col.gameObject.tag == "Threat")
        {
            Die();
        }
    }

    void Die()
    {
        //Debug.Log("You are not a super hexagon.");

        Experiment exp = FindObjectOfType<Experiment>();

        if (exp)
        {
            exp.EndTrial();
        }
    }
}
