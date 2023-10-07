using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class handles the definition of the level.
/// </summary>
[Serializable]
public class SHLevel
{
    /// <summary>
    /// Each obstacle is made up of threats (individual lines in a lane). Obstacles are the 
    /// combination of threats at a particular moment in time.
    /// </summary>
    [Serializable]
    public class Obstacle
    {
        public float Delta;
        public bool[] Lane; // TODO: Support arbitrary number of lanes
        public bool Rot;
        public float Thickness;

        public Obstacle(string[] csvRow)
        {
            Lane = new bool[6];

            Delta = float.Parse(csvRow[0].ToString());
            Lane[0] = csvRow[1].ToString() == "1" ? true : false;
            Lane[1] = csvRow[2].ToString() == "1" ? true : false;
            Lane[2] = csvRow[3].ToString() == "1" ? true : false;
            Lane[3] = csvRow[4].ToString() == "1" ? true : false;
            Lane[4] = csvRow[5].ToString() == "1" ? true : false;
            Lane[5] = csvRow[6].ToString() == "1" ? true : false;
            Rot = csvRow[7].ToString() == "1" ? true : false;
            Thickness = float.Parse(csvRow[8].ToString());
        }
    }

    public List<Obstacle> obstacles = new List<Obstacle>();
    public bool isLoaded = false;

    public void Load(string csvString)
    {
        obstacles.Clear();

        string[] rows = csvString.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

        Debug.Log(string.Format("Total obstacles: {0}", rows.Length));

        for (int i = 1; i < rows.Length; i++)
        {
            obstacles.Add(new Obstacle(rows[i].Split(',')));
        }
        isLoaded = true;
    }

    public int NumObstacles()
    {
        return obstacles.Count;
    }

    public Obstacle GetObstacleAt(int index)
    {
        if (obstacles.Count <= index) // TODO: Duplicate obstacles to make the level longer if needed.
            return null;
        return obstacles[index];
    }
}