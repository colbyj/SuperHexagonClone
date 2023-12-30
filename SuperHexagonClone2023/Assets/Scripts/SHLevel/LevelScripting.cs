using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.Linq;

namespace SH.Level 
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
        Spawn,
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

    public class LevelSpawnCommand : LevelCommand
    {
        public LevelPattern ToSpawn;

        public LevelSpawnCommand(string toParse)
        {
            CommandType = LevelCommandType.Spawn;
            ToSpawn = new LevelPattern(toParse);
        }
    }

    [Serializable]
    public class ParsedLevel
    {
        public List<LevelCommand> LevelCommands;
        [SerializeField] private int currentIndex;
        [SerializeField] private int repeatFromIndex = 0;

        public void ParseLevelDefintion(string text)
        {
            LevelCommands = new List<LevelCommand>();
            string[] rows = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            for (int i = 0; i < rows.Length; i++)
            {
                AddLevelCommand(rows[i]);
            }
        }

        private void AddLevelCommand(string commandString)
        {
            string[] parts = commandString.Split(',', 2);
            LevelCommandType type = Enum.Parse<LevelCommandType>(parts[0]);

            if (type == LevelCommandType.Spawn)
            {
                LevelCommands.Add(new LevelSpawnCommand(parts[1]));
            }
            else if (type == LevelCommandType.RepeatFromHere)
            {
                repeatFromIndex = LevelCommands.Count;
                LevelCommands.Add(new LevelCommand(type));
            }
            else if (LevelCommand.FloatCommandEnums.Contains(type))
            {
                LevelCommands.Add(new LevelFloatCommand(type, parts[1]));
            }
            else
            {
                LevelCommands.Add(new LevelCommand(type));
            }
        }

        public LevelCommand NextCommand 
        { 
            get
            {
                return LevelCommands[currentIndex];
            } 
        }

        public void CommandHandled()
        {
            currentIndex++;

            if (currentIndex >= LevelCommands.Count)
                currentIndex = repeatFromIndex;
        }

        public void ResetLevel()
        {
            currentIndex = 0;
        }
    }

    [Serializable]
    public struct LevelPattern
    {
        public readonly Pattern Pattern;
        public readonly float DistanceOffset;
        public readonly int RotationOffset;
        public readonly bool Mirrored;

        public LevelPattern(string row)
        {
            var parts = row.Split(',');
            Pattern = new Pattern(parts[0]);
            DistanceOffset = (float)Convert.ToDouble(parts[1]);
            RotationOffset = Convert.ToInt32(parts[2]);
            Mirrored = parts[3] == "1";
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
