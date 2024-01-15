using System.IO;
using Assets.Scripts.LevelBehavior;
using Assets.Scripts.SHPlayer;
using UnityEngine;

namespace Assets.Scripts.Edit
{
    public class PatternPreview : MonoBehaviour
    {
        public static PatternPreview Instance;
        public string PatternFileName;
        public Pattern CurrentPattern;
        public PatternInstance CurrentPatternInstance;

        private void Awake()
        {
            Instance = this;
            PlayerBehavior.IsDead = true;
            ThreatManager.Instance.PatternIsOffScreen += PatternIsOffScreen;
            PlayerBehavior.OnPlayerDied += PlayerDied;

            LoadPatternFromFileName();
        }

        private void PatternIsOffScreen(PatternInstance obj)
        {
            //Debug.Log("PatternPreview.PatternIsOffScreen()");
            PlayerBehavior.IsDead = true;
            //ThreatManager.Instance.SpawnLevelPattern(new PatternInstance(CurrentPattern));
        }

        private void PlayerDied()
        {
            //Debug.Log("PatternPreview.PlayerDied()");
            ThreatManager.Instance.Clear();
            ThreatManager.Instance.SpawnLevelPattern(CurrentPatternInstance);
        }

        public void LoadPatternFromFileName()
        {
            if (string.IsNullOrEmpty(PatternFileName))
            {
                return;
            }

            ThreatManager.Instance.Clear();

            CurrentPattern = new Pattern(PatternFileName);
            //LaneManager.Instance.ResetLanes();
            //LaneManager.Instance.SpawnThreats(CurrentPattern, 20);
            CurrentPatternInstance = new PatternInstance(CurrentPattern);
            ThreatManager.Instance.SpawnLevelPattern(CurrentPatternInstance);
        }

        public void SavePattern(string fileName)
        {
            File.WriteAllText(
                $"{Application.streamingAssetsPath}/Patterns/{fileName}.xml", 
                CurrentPattern.XmlDocumentText()
            );
        }
    }
}
