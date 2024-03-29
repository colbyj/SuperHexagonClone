﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts.LevelBehavior;
using UnityEngine;
using UnityEngine.Networking;


// The HTTP server communication code.
namespace Assets.Scripts.Logging
{
    public partial class Experiment
    {
        // The Unity documentation recommends this to unserialize json. This class is not used for any other reason.
#pragma warning disable 0649
        private class ExperimentState
        {
            public int ParticipantId;
            public float InterSessionTimeRemaining; // If this is anything other than 0, the participant is assumed to be in a break period
            public float SessionTimeRemaining;
            public int SessionNumber;
            public int TrialNumber;
            public float MaxDuration;
        }

        private class ExperimentSettings
        { 
            // Workaround for Unity's built-in JSON Serialization tools.
            public List<SessionParameters> Sessions;
        }
#pragma warning restore 0649

        public void SaveState()
        {
#if UNITY_WEBGL
            WWWForm frm = new WWWForm();
            if (ParticipantId != 0)
            {
                frm.AddField("participantID", ParticipantId);
            }

            frm.AddField("interSessionTimeRemaining", InterSessionTimeRemaining.ToString());
            frm.AddField("sessionTimeRemaining", SessionTimeRemaining.ToString());
            frm.AddField("sessionNumber", SessionNumber);
            frm.AddField("trialNumber", TrialNumber);
            frm.AddField("maxDuration", MaxDuration.ToString("#.00"));

            StartCoroutine(SaveStateWebRequest(frm));
#endif
        }

        private IEnumerator SaveStateWebRequest(WWWForm form)
        {
            using var request = UnityWebRequest.Post(ServerUrl + "/sh_post_state", form);
            yield return request.SendWebRequest();
        }

        // TODO: This doesn't handle interSession breaks.
        IEnumerator LoadState(Action doAfter)
        {
#if UNITY_WEBGL
            string url = "";

            if (ParticipantId != 0)
            {
                url = ServerUrl + "/sh_get_state/" + ParticipantId;
            }
            else
            {
                url = ServerUrl + "/sh_get_state";
            }

            using UnityWebRequest request = UnityWebRequest.Get(url);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
            }
            else
            {
                string response = request.downloadHandler.text;

                if (response.Length == 0)
                {
                    // This will happen for new participants too.
                    //Debug.Log("Unable to load state! Does the participant exist? Do they have a valid condition?");
                }
                else
                {
                    ExperimentState state = JsonUtility.FromJson<ExperimentState>(response);
                    InterSessionTimeRemaining = state.InterSessionTimeRemaining;
                    SessionTimeRemaining = state.SessionTimeRemaining;
                    SessionNumber = state.SessionNumber;
                    TrialNumber = state.TrialNumber;
                    MaxDuration = state.MaxDuration;
                }
            }

            request.Dispose();
            doAfter();
#else
            doAfter();
            yield return null;
#endif
        }

        IEnumerator LoadSettings(Action doAfter)
        {
#if UNITY_WEBGL
            string url = "";

            if (ParticipantId != 0)
            {
                url = ServerUrl + "/sh_get_settings/" + ParticipantId;
            }
            else
            {
                url = ServerUrl + "/sh_get_settings";
            }

            using UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);

                // If error, attempt to do what desktop does
                string sessionJson = File.ReadAllText($"{Path.Combine(Application.streamingAssetsPath, "settings.json")}");

                var settings = JsonUtility.FromJson<ExperimentSettings>(sessionJson);
                Sessions = settings.Sessions;
            }
            else
            {
                string response = request.downloadHandler.text;

                if (response.Length == 0)
                {
                    Debug.Log("Unable to load settings! Does the participant exist? Do they have a valid condition?");
                }
                else
                {
                    Debug.Log(response);

                    var settings = JsonUtility.FromJson<ExperimentSettings>(response);
                    Sessions = settings.Sessions;
                }
            }
            request.Dispose();

            doAfter();
#elif UNITY_STANDALONE
            string sessionJson = File.ReadAllText($"{Path.Combine(Application.streamingAssetsPath, "settings.json")}");
            
            var settings = JsonUtility.FromJson<ExperimentSettings>(sessionJson);
            CurrentFeedbackMode = Enum.Parse<FeedbackMode>(settings.FeedbackType);
            Sessions = settings.Sessions;

            doAfter();
            yield return null;
#endif
        }
    }
}

