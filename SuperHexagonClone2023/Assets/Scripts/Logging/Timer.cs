using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    [Header("Time Settings")]
    public float curTime;
    public Text t;

    private void Awake()
    {
        t = GetComponentInChildren<Text>();
        if (t == null)
        {
            throw new UnityException(gameObject.name + " not properly configured : NO TEXT BOX FOUND");
        }
    }
    void FixedUpdate()
    {
        curTime += Time.fixedDeltaTime;
        t.text = curTime.ToString("#.00");

    }

    public void Reset()
    {
        curTime = 0;

    }
}
