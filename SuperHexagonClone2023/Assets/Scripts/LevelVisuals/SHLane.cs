using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Assets.Scripts.LevelVisuals
{
    /// <summary>
    /// Spawn threats, and removes those threats when needed.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class SHLane : MonoBehaviour
    {
        [Header("Object Pooling Config")] public ObjectPool<SHLine> ThreatPool;
        public SHLine ThreatPrefab;
        public List<SHLine> ActiveThreats = new();
        public int AmountToPool;

        public float GetFurthestThreat()
        {
            var furthestThreat = 0f;

            for (var i = 0; i < ActiveThreats.Count; i++)
            {
                if (ActiveThreats[i].Radius > furthestThreat) furthestThreat = ActiveThreats[i].Radius;
            }

            return furthestThreat;
        }

        private void Update()
        {
            if (ActiveThreats.Count == 0) return;

            // Use ToArray to avoid having the collection modifed by the loop errors.
            foreach (SHLine threat in ActiveThreats.ToArray())
            {
                if (threat.RadiusOuter <= 0) ThreatPool.Release(threat);
            }

            //GameObject nextThreat = threats[0];

            /*if (nextThreat.GetComponent<SHLine>().RadiusOuter() <= 0f)
        {
            threats.RemoveAt(0);
            ObjectPooler.SharedInstance.RecycleObject(nextThreat);
        }*/
        }

        public void ClearThreats()
        {
            // Use ToArray to avoid having the collection modifed by the loop errors.
            foreach (SHLine threat in ActiveThreats.ToArray())
            {
                threat.ResetLine();
                ThreatPool.Release(threat);
            }
            //threatPool.Clear();
            /*GameObject[] threats = GameObject.FindGameObjectsWithTag("Threat");

        foreach (GameObject threat in threats)
        {
            Destroy(threat);
        }
        this.threats = new List<GameObject>();*/
        }

        /*public float GetAngle()
    {
        return gameObject.transform.rotation.eulerAngles.z;
    }*/
    }
}