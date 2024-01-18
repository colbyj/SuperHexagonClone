using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Xml;
using Assets.Scripts.LevelVisuals;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.LevelBehavior 
{
    [Serializable]
    public enum LevelCommandType
    {
        RotationDifficulty,
        RotationDifficultyRate,
        RotationDifficultyMax,
        ThreatSpeed,
        ThreatSpeedRate,
        ThreatSpeedMax,
        PlayerSpeed,
        PatternRadiusOffset,
        SpawnOne,
        SpawnGroup,
        RepeatFromHere,
        CWCamera,
        CCWCamera,
    }

    [Serializable]
    public class LevelCommand
    {
        public static LevelCommandType[] FloatCommandEnums = new LevelCommandType[] {
            LevelCommandType.RotationDifficulty,
            LevelCommandType.RotationDifficultyRate,
            LevelCommandType.RotationDifficultyMax,
            LevelCommandType.ThreatSpeed,
            LevelCommandType.ThreatSpeedRate,
            LevelCommandType.ThreatSpeedMax,
            LevelCommandType.PlayerSpeed,
            LevelCommandType.PatternRadiusOffset,
        };

        public LevelCommandType CommandType;

        public LevelCommand() { }

        public LevelCommand(LevelCommandType commandType)
        {
            CommandType = commandType;
        }
    }

    public class LevelFloatCommand : LevelCommand
    {
        public float Argument;

        public LevelFloatCommand(LevelCommandType type, string toParse)
        {
            this.CommandType = type;
            this.Argument = (float)Convert.ToDouble(toParse);
        }
    }

    public class LevelSpawnOneCommand : LevelCommand
    {
        public List<PatternInstance> ToSpawn;
        public PatternInstance GetRandomToSpawn
        {
            get
            {
                int randomIdx = UnityEngine.Random.Range(0, ToSpawn.Count());
                return ToSpawn[randomIdx];
            }
        }

        public LevelSpawnOneCommand(XmlNodeList patternNodes)
        {
            CommandType = LevelCommandType.SpawnOne;
            ToSpawn = new List<PatternInstance>();

            foreach (XmlNode patternNode in patternNodes)
            {
                ToSpawn.Add(new PatternInstance(patternNode));
            }
        }
    }

    public class LevelSpawnGroupCommand : LevelCommand
    {
        public List<List<PatternInstance>> GroupsToSpawn;
        public List<PatternInstance> GetRandomGroupToSpawn
        {
            get
            {
                int randomIdx = UnityEngine.Random.Range(0, GroupsToSpawn.Count());
                Debug.Log(randomIdx);
                return GroupsToSpawn[randomIdx];
            }
        }

        public LevelSpawnGroupCommand(XmlNodeList patternGroupNodes)
        {
            CommandType = LevelCommandType.SpawnGroup;
            GroupsToSpawn = new List<List<PatternInstance>>();

            foreach (XmlNode patternGroupNode in patternGroupNodes)
            {
                List<PatternInstance> group = new List<PatternInstance>();

                foreach (XmlNode patternNode in patternGroupNode.ChildNodes)
                {
                    group.Add(new PatternInstance(patternNode));
                }
                GroupsToSpawn.Add(group);
            }
        }
    }

    [Serializable]
    public class ParsedLevel
    {
        public List<LevelCommand> LevelCommands;
        [SerializeField] private int _currentIndex;
        [SerializeField] private int _repeatFromIndex = 0;

        public void ParseLevelXml(string xmlString)
        {
            LevelCommands = new List<LevelCommand>();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlString);

            foreach (XmlNode commandNode in doc.DocumentElement.SelectSingleNode("/Commands").ChildNodes)
            {
                AddLevelCommand(commandNode);
            }
        }

        private void AddLevelCommand(XmlNode commandNode)
        {
            LevelCommandType commandType = Enum.Parse<LevelCommandType>(commandNode.Name);

            if (LevelCommand.FloatCommandEnums.Contains(commandType))
            {
                LevelCommands.Add(new LevelFloatCommand(commandType, commandNode.InnerText));
            }
            else if (commandType == LevelCommandType.SpawnOne)
            {
                LevelCommands.Add(new LevelSpawnOneCommand(commandNode.ChildNodes));
            }
            else if (commandType == LevelCommandType.SpawnGroup)
            {
                LevelCommands.Add(new LevelSpawnGroupCommand(commandNode.ChildNodes));
            }
            else if (commandType == LevelCommandType.RepeatFromHere)
            {
                _repeatFromIndex = LevelCommands.Count;
                LevelCommands.Add(new LevelCommand(commandType));
            }
            else
            {
                LevelCommands.Add(new LevelCommand(commandType));
            }
        }

        public LevelCommand NextCommand => LevelCommands[_currentIndex];

        public void CommandHandled()
        {
            _currentIndex++;

            if (_currentIndex >= LevelCommands.Count)
            {
                _currentIndex = _repeatFromIndex;
            }
        }

        public void ResetLevel()
        {
            _currentIndex = 0;
        }
    }

    [Serializable]
    public class PatternInstance
    {        
        public Pattern Pattern;
        public float DistanceOffset;
        public int RotationOffset;
        public bool Mirrored;
        public bool LastBeforeRestart;

        public string Name => Pattern.Name;

        // These are used for viewing the level pattern
        public List<SHLine> Threats = new List<SHLine>();
        public SHLine ClosestThreat = null;
        public SHLine FurthestThreat = null;

        public float TotalLength => FurthestThreat.RadiusOuter() - ClosestThreat.Radius;

        public PatternInstance(Pattern pattern)
        {
            this.Pattern = pattern;
            DistanceOffset = 0;
            RotationOffset = 0;
            Mirrored = false;
        }

        public PatternInstance(XmlNode node)
        {
            Pattern = new Pattern(node.InnerText);
            DistanceOffset = (float)Convert.ToDouble(node.Attributes["offset"].Value);
            RotationOffset = Convert.ToInt32(node.Attributes["rotate"].Value);
            Mirrored = node.Attributes["mirrored"].Value == "1";
        }

        public void UpdateClosestAndFurthestThreats()
        {
            SHLine closest = null;
            SHLine furthest = null;

            foreach (SHLine threat in Threats)
            {
                if (closest == null)
                {
                    closest = threat;
                }
                else if (threat.Radius < closest.Radius)
                {
                    closest = threat;
                }

                if (furthest == null)
                {
                    furthest = threat;
                }
                else if (threat.RadiusOuter() > furthest.RadiusOuter())
                {
                    furthest = threat;
                }
            }

            ClosestThreat = closest;
            FurthestThreat = furthest;
        }
    }

    [Serializable]
    public class Pattern
    {
        [Serializable]
        public class Wall
        {
            public int Side;
            /// <summary>
            /// Radius. Name comes from Super Haxagon
            /// </summary>
            public float Distance;
            /// <summary>
            /// Thickness. Name comes from Super Haxagon
            /// </summary>
            public float Height;

            public Wall(int side, float distance, float height)
            {
                Side = side;
                Distance = distance;
                Height = height;
            }

            public string ToXmlNodeString()
            {
                return $"<Wall><Distance>{Distance}</Distance><Height>{Height}</Height><Side>{Side}</Side></Wall>";

            }
        }

        public string Name;
        public List<Wall> Walls;

        private static Dictionary<string, XmlDocument> s_loadedPatterns = new();

        public Pattern(string name)
        {
            Name = name;
            XmlDocument doc;

            if (s_loadedPatterns.ContainsKey(name))
            {
                doc = s_loadedPatterns[name];
            }
            else
            {
                try
                {
                    string patternText;

                    if (Application.isEditor)
                    {
                        patternText = File.ReadAllText($"{Application.streamingAssetsPath}/../Resources/Patterns/{name}.xml");
                    }
                    else
                    {
                        patternText = Resources.Load<TextAsset>($"Patterns/{name}").text;
                    }

                    doc = new XmlDocument();
                    doc.LoadXml(patternText);

                    s_loadedPatterns.Add(name, doc);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Could not load pattern {name}, exception was: {e.Message}");
                    return;
                }
            }
            
            var walls = new List<Wall>();

            foreach (XmlNode node in doc.DocumentElement.SelectNodes("/Pattern/Wall"))
            {
                int side = Convert.ToInt32(node.SelectSingleNode("Side").InnerText);
                float distance = (float)Convert.ToDouble(node.SelectSingleNode("Distance").InnerText);
                float height = (float)Convert.ToDouble(node.SelectSingleNode("Height").InnerText);
                walls.Add(new Wall(side, distance, height));
            }

            Walls = walls;
        }

        public string XmlDocumentText()
        {
            string xmlStart = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no""?>" +
                "<Pattern><SidesRequired>6</SidesRequired>\n";

            string xmlEnd = "</Pattern>";

            string xmlMid = "";

            foreach (Wall wall in Walls)
            {
                xmlMid += wall.ToXmlNodeString() + "\n";
            }
            return xmlStart + xmlMid + xmlEnd;
        }
    }
}
