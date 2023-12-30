using CustomExtensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Code Reviewed Tuesday, May 15th, 2018 by Parker Neufeld, pdn844   */
public class SHPlayer : MonoBehaviour
{
    void OnCollisionEnter2D(Collision2D col)
    {
        /*if (GameParameters.EnableCollisions && col.gameObject.tag == "Threat" && col.collider is EdgeCollider2D)
        {
            Debug.Log($"You are not a super hexagon because you touched {col.gameObject.name} with parent {col.gameObject.transform.parent.name}");
            Die();
        }*/

    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (GameParameters.EnableCollisions && col.gameObject.tag == "Threat" && col is EdgeCollider2D)
        {
            Debug.Log($"You are not a super hexagon because you touched {col.gameObject.name} with parent {col.gameObject.transform.parent.name}");
            Die();
        }
    }

    void Die()
    {
        Experiment exp = FindObjectOfType<Experiment>();

        if (exp)
        {
            exp.EndTrial();
        }
    }
}
