using System.Collections.Generic;
using CustomExtensions;
using UnityEngine;

/* Written by Parker Neufeld under the supervision of Regan Mandryk
 * Property of The University of Saskatchewan Interaction Lab */

namespace Assets.Scripts.LevelVisuals
{
    /// <summary>
    /// This is used to dynamically build the lane game objects.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class LaneManager : MonoBehaviour
    {
        public static LaneManager Instance { get; private set; }

        public enum ColorMode
        {
            Greyscale,
            Fruity,
            Dark,
            Cosmic
        }

        [Range(3, 20)] public static int LanesRequired = 6;

        // TODO: Make use of delegates so that the number of lanes can be 
        // changed on the fly without sending messages to individual objects.
        // For now this is not necessary.

        [SerializeField] private GameObject _basicLane;
        [SerializeField] private Material[] _mats = new Material[2];

        //Graphical
        private readonly Color[] _laneColors = new Color[]
        {
            new(0.6117647f, 0.6117647f, 0.6117647f),
            new(0.48235297f, 0.48235297f, 0.48235297f)
        };

        public List<GameObject> Lanes = new();

        private void Awake()
        {
            Instance = this;

            if (_basicLane == null)
            {
                throw new UnityException("There is no basic lane prefab!!! Can't construct the board.");
            }

            if (!HasValidColors())
            {
                throw new UnityException("The current stage configuration will have two lanes of the same color touching!");
            }

            CreateLanes();
        }

        #region Set-up methods
        private void CreateLanes()
        {
            int lanesNeeded = 0;
            if (Lanes.Count != LanesRequired)
            {
                lanesNeeded = LanesRequired - Lanes.Count;
            }
            //Debug.Log("need" + lanesNeeded);


            //levelManager.Lanes = new List<SHLane>();
            Lanes = new List<GameObject>();

            for (int i = LanesRequired - lanesNeeded; i < LanesRequired; i++)
            {
                //Instantiate
                GameObject newLane = Instantiate(_basicLane, transform.position,
                    Quaternion.Euler(0, 0, i * 360f / LanesRequired), transform);
                Lanes.Add(newLane);

                //levelManager.Lanes.Add(newLane.GetComponent<SHLane>());
            }

            ConfigLanes();
        }

        /// <summary>
        /// Adjust rotation and colour of lanes.
        /// </summary>
        private void ConfigLanes()
        {
            // This seems to be called more often than necessary?
            for (int i = 0; i < Lanes.Count; i++)
            {
                GameObject currentLane = Lanes[i];

                if (i >= LanesRequired)
                {
                    currentLane.SetActive(false);
                    continue;
                }
                else if (i < LanesRequired && currentLane.activeInHierarchy == false)
                {
                    currentLane.SetActive(true);
                }

                //Instantiate
                //GameObject newLane = Instantiate(basicLane, transform.position, Quaternion.Euler(0, 0, i * 360f / zonesCount), transform);
                currentLane.transform.rotation = Quaternion.Euler(0, 0, i * 360f / LanesRequired);
                currentLane.name = "Lane" + i;

                Material matToApply = _mats[i % _mats.Length];
                //matToApply.SetColor("_TintColor", laneColors[i % laneColors.Length]);
                //matToApply.SetColor(i%laneColors.Length,laneColors[i % laneColors.Length]);
                currentLane.GetRequiredComponent<MeshRenderer>().material = matToApply;
                currentLane.GetRequiredComponent<MeshRenderer>().material.color = _laneColors[i % _laneColors.Length];
            }
        }

        private bool HasValidColors()
        {
            return LanesRequired % _laneColors.Length != 1;
        }

        public void ResetLanes()
        {
            ConfigLanes();
        }
        #endregion
    }
}