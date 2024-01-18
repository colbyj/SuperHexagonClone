using System.Collections;
using System.Collections.Generic;
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
                audioSource.UnPause();
                paused = false;
            }
        };

        PlayerBehavior.OnPlayerDied += (line) =>
        {
            paused = true;
            audioSource.Pause();
        };
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
