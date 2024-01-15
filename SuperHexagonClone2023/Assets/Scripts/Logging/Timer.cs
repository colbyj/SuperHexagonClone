using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    [Header("Time Settings")]
    public float CurTime;
    public Text T;

    private void Awake()
    {
        T = GetComponentInChildren<Text>();
        if (T == null)
        {
            throw new UnityException(gameObject.name + " not properly configured : NO TEXT BOX FOUND");
        }
    }
    void FixedUpdate()
    {
        CurTime += Time.fixedDeltaTime;
        T.text = CurTime.ToString("#.00");

    }

    public void Reset()
    {
        CurTime = 0;

    }
}
