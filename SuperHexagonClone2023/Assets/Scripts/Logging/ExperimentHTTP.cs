using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;


// The HTTP server communication code.
public partial class Experiment
{


    // The Unity documentation recommends this to unserialize json. This class is not used for any other reason.
#pragma warning disable 0649
    private class ExperimentState
    {
        public int participantID;
        public float interSessionTimeRemaining; // If this is anything other than 0, the participant is assumed to be in a break period
        public float sessionTimeRemaining;
        public int sessionNumber;
        public int trialNumber;
        public float maxDuration;
    }

    private class SessionList
    { // Stupid workaround for Unity...
        public List<SessionParameters> sessions;
    }
#pragma warning restore 0649


    public string movementsCSV()
    {
        string csv = "";

        if (movements.Count == 0) return csv;

        for (int i = 0; i < movements.Count; i++)
        {
            csv += movements[i].CSV();
        }

        return csv;
    }

    public void SaveTrial(bool interrupted = false)
    {
        float framerate = trialFrames / timerTrial.value;

        if (float.IsNaN(framerate))
        {
            framerate = 0f;
        }

        WWWForm frm = new WWWForm();
        if (participantID != 0) frm.AddField("participantID", participantID);
        frm.AddField("duration", timerTrial.value.ToString("#.00"));
        frm.AddField("avgFps", framerate.ToString("#.00"));
        frm.AddField("trialNumber", trialNumber);
        frm.AddField("sessionNumber", sessionNumber);
        frm.AddField("difficultyRotation", levelManager.diffSettings.rotationDifficulty.GetValue().ToString("#.00000"));
        frm.AddField("difficultySpawning", levelManager.diffSettings.spawningDifficulty.GetValue().ToString("#.00000"));
        frm.AddField("interrupted", interrupted ? "true": "false");
        frm.AddField("movements", StringCompressor.CompressString(movementsCSV()));

        try
        {
            var request = UnityWebRequest.Post(serverUrl + "/sh_post_trial", frm);
            request.SendWebRequest();
        }
        catch (Exception ex)
        {
            Debug.Log("Error in SaveTrial(): " + ex.Message);
        }
    }

    public void SaveState()
    {
        WWWForm frm = new WWWForm();
        if (participantID != 0) frm.AddField("participantID", participantID);
        frm.AddField("interSessionTimeRemaining", interSessionTimeRemaining.ToString());
        frm.AddField("sessionTimeRemaining", sessionTimeRemaining.ToString());
        frm.AddField("sessionNumber", sessionNumber);
        frm.AddField("trialNumber", trialNumber);
        frm.AddField("maxDuration", maxDuration.ToString("#.00"));

        try
        {
            var request = UnityWebRequest.Post(serverUrl + "/sh_post_state", frm);
            request.SendWebRequest();
        }
        catch (Exception ex)
        {
            Debug.Log("Error in SaveState(): " + ex.Message);
        }
    }

    // TODO: This doesn't handle interSession breaks.
    IEnumerator LoadState(Action doAfter)
    {
        string url = "";

        if (participantID != 0)
        {
            url = serverUrl + "/sh_get_state/" + participantID;
        }
        else
        {
            url = serverUrl + "/sh_get_state";
        }

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {

            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
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
                    interSessionTimeRemaining = state.interSessionTimeRemaining;
                    sessionTimeRemaining = state.sessionTimeRemaining;
                    sessionNumber = state.sessionNumber;
                    trialNumber = state.trialNumber;
                    maxDuration = state.maxDuration;
                }
            }

            doAfter();
        }
    }

    IEnumerator LoadSettings(Action doAfter)
    {
        string url = "";

        if (participantID != 0)
        {
            url = serverUrl + "/sh_get_settings/" + participantID;
        }
        else
        {
            url = serverUrl + "/sh_get_settings";
        }

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {

            yield return request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.Log(request.error);
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
                    //Debug.Log(response);
                    sessions = JsonUtility.FromJson<SessionList>(response).sessions;
                }
            }

            doAfter();
        }
    }
}

