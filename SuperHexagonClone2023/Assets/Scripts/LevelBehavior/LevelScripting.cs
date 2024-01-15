using System;
using System.Collections.Generic;
using System.Linq;
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
        public List<LevelPattern> ToSpawn;
        public LevelPattern GetRandomToSpawn
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
            ToSpawn = new List<LevelPattern>();

            foreach (XmlNode patternNode in patternNodes)
            {
                ToSpawn.Add(new LevelPattern(patternNode));
            }
        }
    }

    public class LevelSpawnGroupCommand : LevelCommand
    {
        public List<List<LevelPattern>> GroupsToSpawn;
        public List<LevelPattern> GetRandomGroupToSpawn
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
            GroupsToSpawn = new List<List<LevelPattern>>();

            foreach (XmlNode patternGroupNode in patternGroupNodes)
            {
                List<LevelPattern> group = new List<LevelPattern>();

                foreach (XmlNode patternNode in patternGroupNode.ChildNodes)
                {
                    group.Add(new LevelPattern(patternNode));
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

        public LevelCommand NextCommand 
        { 
            get
            {
                return LevelCommands[_currentIndex];
            } 
        }

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
    public class LevelPattern
    {        
        public  Pattern Pattern;
        public float DistanceOffset;
        public int RotationOffset;
        public bool Mirrored;
        public string Name;

        // These are used for viewing the level pattern
        public List<SHLine> Threats = new List<SHLine>();
        public SHLine ClosestThreat = null;
        public SHLine FurthestThreat = null;

        public LevelPattern(Pattern pattern)
        {
            this.Pattern = pattern;
            DistanceOffset = 0;
            RotationOffset = 0;
            Mirrored = false;
        }

        public LevelPattern(XmlNode node)
        {
            Name = node.InnerText;
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
    public struct Pattern
    {
        [Serializable]
        public struct Wall
        {
            public readonly int Side;
            public readonly float Distance;
            public readonly float Height;

            public Wall(int side, float distance, float height)
            {
                this.Side = side;
                this.Distance = distance;
                this.Height = height;
            }
        }
        public static Dictionary<string, Pattern> LoadedPatterns = new Dictionary<string, Pattern>();

        public readonly string FileName;
        public readonly Wall[] Walls;

        public Pattern(string fileName)
        {
            FileName = fileName;

            if (LoadedPatterns.ContainsKey(fileName))
            {
                Walls = LoadedPatterns[fileName].Walls;
                return;
            }

            var file = Resources.Load<TextAsset>($"Patterns/{fileName}");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(file.text);

            var walls = new List<Wall>();

            foreach (XmlNode node in doc.DocumentElement.SelectNodes("/Pattern/Wall"))
            {
                int side = Convert.ToInt32(node.SelectSingleNode("Side").InnerText);
                float distance = (float)Convert.ToDouble(node.SelectSingleNode("Distance").InnerText);
                float height = (float)Convert.ToDouble(node.SelectSingleNode("Height").InnerText);
                walls.Add(new Wall(side, distance, height));
            }

            Walls = walls.ToArray();
            LoadedPatterns.Add(fileName, this);
        }
    }
}
