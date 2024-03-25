using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Assets.Scripts.LevelBehavior;
using Assets.Scripts.Logging;
using Assets.Scripts.SHPlayer;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Music : MonoBehaviour
{
    public static Music Instance;
    private bool paused = false;
    private AudioSource audioSource;

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();

        Experiment.OnSessionEnd += () =>
        {
            if (paused && Application.platform == RuntimePlatform.WebGLPlayer)
            {
                audioSource.volume = 1;
            }

            audioSource.Stop();
            paused = false;
        };

        LevelManager.OnFirstBegin += () =>
        {
            if (!paused)
            {
                audioSource.Play();
            }
        };
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPlayerDied()
    {
        paused = true;
        //Debug.Log($"Paused and time is {audioSource.time}");
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            audioSource.volume = 0;
        }
        else
        {
            audioSource.Pause();
        }
    }

    public void OnPlayerRespawn()
    {
        if (audioSource == null)
            return;

        if (paused)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                audioSource.volume = 1;
            }
            else
            {
                audioSource.UnPause();
            }

            paused = false;
            //Debug.Log($"Unpaused and time is {audioSource.time}");
        }
        else
        {
            Debug.Log("Play music OnPlayerRespawn");
            audioSource.Play();
        }
    }
}
