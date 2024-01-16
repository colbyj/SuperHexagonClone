using System;
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

        private class SessionList
        { 
            // Workaround for Unity's built-in JSON Serialization tools.
            public List<SessionParameters> Sessions;
        }
#pragma warning restore 0649


#if UNITY_STANDALONE
        private string _experimentLaunchGuid;
#endif

        public string MovementsCsv()
        {
            string csv = "";

            if (_movements.Count == 0)
            {
                return csv;
            }

            for (int i = 0; i < _movements.Count; i++)
            {
                csv += _movements[i].Csv();
            }

            return csv;
        }

        public void SaveTrial(bool interrupted = false)
        {
            float framerate = _trialFrames / TimerTrial.Value;

            if (float.IsNaN(framerate))
            {
                framerate = 0f;
            }

            string durationStr = TimerTrial.Value.ToString("#.00");
            string fpsStr = framerate.ToString("#.00");
            string difficultyRotation = DifficultyManager.Instance.RotationSpeed.ToString("#.00000");
            string difficultySpawning = DifficultyManager.Instance.ThreatSpeed.ToString("#.00000");
            string interruptedStr = interrupted ? "true" : "false";
            string movements = StringCompressor.CompressString(MovementsCsv());

#if UNITY_WEBGL
            WWWForm frm = new WWWForm();
            if (ParticipantId != 0)
            {
                frm.AddField("participantID", ParticipantId);
            }

            frm.AddField("duration", durationStr);
            frm.AddField("avgFps", fpsStr);
            frm.AddField("trialNumber", TrialNumber);
            frm.AddField("sessionNumber", SessionNumber);
            frm.AddField("difficultyRotation", difficultyRotation);
            frm.AddField("difficultySpawning", difficultySpawning);
            frm.AddField("interrupted", interruptedStr);
            frm.AddField("movements", movements);

            try
            {
                var request = UnityWebRequest.Post(ServerUrl + "/sh_post_trial", frm);
                request.SendWebRequest();
            }
            catch (Exception ex)
            {
                Debug.Log("Error in SaveTrial(): " + ex.Message);
            }
#elif UNITY_STANDALONE
            string logFile = Path.Combine(Application.streamingAssetsPath, "trials.csv");

            if (!File.Exists(logFile))
            {
                using (File.Create(logFile))
                {
                }

                File.AppendAllText(logFile,
                    "InstanceID,Duration,AvgFPS,TrialNumber,SessionNumber,DifficultyRotation,DifficultySpawning,Interrupted\n");
            }

            if (string.IsNullOrEmpty(_experimentLaunchGuid))
            {
                _experimentLaunchGuid = Guid.NewGuid().ToString();
            }

            File.AppendAllText(logFile, 
                $"{_experimentLaunchGuid},{durationStr},{fpsStr},{TrialNumber},{SessionNumber},{difficultyRotation},{difficultySpawning},{interruptedStr}\n");
#endif
        }

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

            try
            {
                using var request = UnityWebRequest.Post(ServerUrl + "/sh_post_state", frm);
                request.SendWebRequest();
            }
            catch (Exception ex)
            {
                Debug.Log("Error in SaveState(): " + ex.Message);
            }
#endif
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
                        InterSessionTimeRemaining = state.InterSessionTimeRemaining;
                        SessionTimeRemaining = state.SessionTimeRemaining;
                        SessionNumber = state.SessionNumber;
                        TrialNumber = state.TrialNumber;
                        MaxDuration = state.MaxDuration;
                    }
                }

                doAfter();
            }
#endif
            doAfter();
            yield return null;
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
                        Sessions = JsonUtility.FromJson<SessionList>(response).Sessions;
                    }
                }

                doAfter();
            }
#elif UNITY_STANDALONE
            string sessionJson = File.ReadAllText($"{Path.Combine(Application.streamingAssetsPath, "settings.json")}");
            Sessions = JsonUtility.FromJson<SessionList>(sessionJson).Sessions;
            doAfter();
#endif
            yield return null;
        }
    }
}

