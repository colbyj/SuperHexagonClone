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
    private bool paused = false;

    // Start is called before the first frame update
    void Start()
    {
        AudioSource audioSource = GetComponent<AudioSource>();

        Experiment.OnSessionEnd += () =>
        {
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

        PlayerBehavior.OnPlayerRespawn += () =>
        {
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
        };

        PlayerBehavior.OnPlayerDied += (line) =>
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
        };
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
