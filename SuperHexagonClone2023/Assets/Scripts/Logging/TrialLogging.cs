using Assets.Scripts.LevelBehavior;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Logging
{
    public class Movement
    {
        public float DeltaTime;
        public int Rotation;

        public Movement(float deltaTime, int rotation)
        {
            this.DeltaTime = deltaTime;
            this.Rotation = rotation;
        }

        public Movement(string csv)
        {
            if (csv.EndsWith(";"))
            {
                csv = csv.Remove(csv.Length, 1); // Remove trailing delimiter.
            }
            string[] movementParts = csv.Split(',');
            this.DeltaTime = float.Parse(movementParts[0]);
            this.Rotation = int.Parse(movementParts[1]);
        }

        public string Csv()
        {
            return $"{DeltaTime},{Rotation};";
        }
    }

    internal class TrialLogging
    {
        public List<Movement> Movements = new List<Movement>();
#if UNITY_STANDALONE
        private string _experimentLaunchGuid;
#endif

        public string MovementsCsv()
        {
            string csv = "";

            if (Movements.Count == 0)
            {
                return csv;
            }

            for (int i = 0; i < Movements.Count; i++)
            {
                csv += Movements[i].Csv();
            }

            return csv;
        }

        public void SaveTrial(int trialFrames, Experiment experiment, bool interrupted = false)
        {
            float framerate = trialFrames / experiment.TimerTrial.Value;

            if (float.IsNaN(framerate))
            {
                framerate = 0f;
            }

            string durationStr = experiment.TimerTrial.Value.ToString("#.00");
            string fpsStr = framerate.ToString("#.00");
            string difficultyRotation = DifficultyManager.Instance.CameraRotationSpeed.ToString("#.00000");
            string difficultySpawning = DifficultyManager.Instance.ThreatSpeed.ToString("#.00000");
            string interruptedStr = interrupted ? "true" : "false";
            string movements = StringCompressor.CompressString(MovementsCsv());

#if UNITY_WEBGL
            WWWForm frm = new WWWForm();
            /*if (ParticipantId != 0)
            {
                frm.AddField("participantID", ParticipantId);
            }*/

            frm.AddField("duration", durationStr);
            frm.AddField("avgFps", fpsStr);
            frm.AddField("trialNumber", experiment.TrialNumber);
            frm.AddField("sessionNumber", experiment.SessionNumber);
            frm.AddField("difficultyRotation", difficultyRotation);
            frm.AddField("difficultySpawning", difficultySpawning);
            frm.AddField("interrupted", interruptedStr);
            frm.AddField("movements", movements);

            try
            {
                // TODO: All UnityWebRequests should be in coroutines
                var request = UnityWebRequest.Post(experiment.ServerUrl + "/sh_post_trial", frm);
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
                $"{_experimentLaunchGuid},{durationStr},{fpsStr},{experiment.TrialNumber},{experiment.SessionNumber},{difficultyRotation},{difficultySpawning},{interruptedStr}\n");
#endif
        }
    }
}
