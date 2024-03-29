using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Xml;
using Assets.Scripts.LevelVisuals;
using UnityEngine;

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
        private readonly List<XmlNode> _toSpawn;

        public PatternInstance GetRandomToSpawn
        {
            get
            {
                int randomIdx = UnityEngine.Random.Range(0, _toSpawn.Count());
                return new PatternInstance(_toSpawn[randomIdx]);
            }
        }

        public LevelSpawnOneCommand(XmlNodeList patternNodes)
        {
            CommandType = LevelCommandType.SpawnOne;
            _toSpawn = new List<XmlNode>();

            foreach (XmlNode patternNode in patternNodes)
            {
                _toSpawn.Add(patternNode);
            }
        }
    }

    public class LevelSpawnGroupCommand : LevelCommand
    {
        private List<List<XmlNode>> _groupsToSpawn;

        public List<PatternInstance> GetRandomGroupToSpawn
        {
            get
            {
                int randomIdx = UnityEngine.Random.Range(0, _groupsToSpawn.Count());
                Debug.Log(randomIdx);

                List<PatternInstance> group = new List<PatternInstance>();

                foreach (XmlNode node in _groupsToSpawn[randomIdx])
                {
                    group.Add(new PatternInstance(node));
                }

                return group;
            }
        }

        public LevelSpawnGroupCommand(XmlNodeList patternGroupNodes)
        {
            CommandType = LevelCommandType.SpawnGroup;
            _groupsToSpawn = new List<List<XmlNode>>();

            foreach (XmlNode patternGroupNode in patternGroupNodes)
            {
                List<XmlNode> group = new List<XmlNode>();

                foreach (XmlNode patternNode in patternGroupNode.ChildNodes)
                {
                    group.Add(patternNode);
                }

                _groupsToSpawn.Add(group);
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
        public List<SHLine> Triggers = new List<SHLine>();

        public float TotalLength => FurthestThreat.RadiusOuter - ClosestThreat.Radius;

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

        /// <summary>
        /// Only needs to be called after the PatternInstance is initialized.
        /// Assumes that the Wall list in the Pattern object is sorted.
        /// </summary>
        public void UpdateClosestAndFurthestThreats()
        {
            // Since writing this code I realized it's more useful to sort the list by radius.
            /*SHLine closest = null;
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
            }*/

            if (Threats.Count == 0)
                return;

            ClosestThreat = Threats[0];
            FurthestThreat = Threats.Last();

            Triggers = new List<SHLine>();

            foreach (SHLine line in Threats)
            {
                if (line.IsTriggerOnly)
                {
                    Triggers.Add(line);
                }
            }
        }

        /// <summary>
        /// Work out which triggers come next, after a particular radius.
        /// </summary>
        /// <param name="pastThisRadius">If left blank will be the player's radius.</param>
        /// <returns>If there's a tie, this list contains multiple. Could also be an empty list if the pattern is past the player.</returns>
        public List<SHLine> NextTriggers(float? pastThisRadius = null)
        {
            pastThisRadius ??= GameParameters.PlayerRadius;  // If null, set to player's radius

            var qualifyingTriggers = new List<SHLine>();
            float closestRadiusFound = float.MaxValue;

            foreach (SHLine line in Triggers)
            {
                if (line.Radius >= pastThisRadius && line.Radius < closestRadiusFound)
                {
                    // We should only hit this on the first instance in the Triggers list that meets this requirement.
                    closestRadiusFound = line.Radius; 
                }

                if (line.Radius == closestRadiusFound)
                {
                    qualifyingTriggers.Add(line);
                }
            }

            return qualifyingTriggers;
        }
    }

    [Serializable]
    public class Pattern
    {
        [Serializable]
        public class Wall : IComparable<Wall>
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
            public bool IsTrigger;

            public Wall(int side, float distance, float height, bool isTrigger) 
            {
                Side = side;
                Distance = distance;
                Height = height;
                IsTrigger = isTrigger;
            }

            public int CompareTo(Wall otherWall)
            {
                return Distance.CompareTo(otherWall.Distance);
            }

            public string ToXmlNodeString()
            {
                return $"<Wall><Distance>{Distance}</Distance><Height>{Height}</Height><Side>{Side}</Side><IsTrigger>{IsTrigger.ToString().ToLowerInvariant()}</IsTrigger></Wall>";
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

                    if (!Application.isEditor)
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

                var isTriggerNode = node.SelectSingleNode("IsTrigger");
                bool isTrigger = isTriggerNode != null ? Convert.ToBoolean(isTriggerNode.InnerText) : false; 

                walls.Add(new Wall(side, distance, height, isTrigger));
            }

            walls.Sort();
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
