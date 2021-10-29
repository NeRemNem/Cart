using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DeadPerceptionSensor : MonoBehaviour
{
    public List<DeadSensor> dead_sensors;
    public LayerMask mask;
    private List<float> _sensor_obs_list = new List<float>();
    private int _mask_value;
    public float[] hit_vaildations;
    [System.Serializable]
    public struct DeadSensor
    {
        public Transform Transform;
        public float HitValidationDistance;
        public float RayDistance;
    }

    private void Awake()
    {
        if (_sensor_obs_list == null)
            _sensor_obs_list = new List<float>();
        if (dead_sensors == null)
            dead_sensors = new List<DeadSensor>();
        _mask_value = 1 << mask;
        for (int i = 0; i < transform.childCount; i++)
        {
            var sensor = new DeadSensor();
            sensor.Transform = transform.GetChild(i);
            sensor.HitValidationDistance = hit_vaildations[i];
            sensor.RayDistance = 10f;
            dead_sensors.Add(sensor);

        }
    }

    public void Init()
    {
    }

    public bool IsHitWall()
    {
        for (int i = 0; i < dead_sensors.Count; i++)
        {
            var cur_sensor = dead_sensors[i];
            if (Physics.Raycast(cur_sensor.Transform.position, cur_sensor.Transform.forward
                , out var hit, cur_sensor.RayDistance, _mask_value))
                if (hit.distance <= cur_sensor.HitValidationDistance)
                    return true;
        }

        return false;
    }

    public List<float> GetObservation()
    {
        PrepCheckpointObservation();
        return _sensor_obs_list;
    }

    private void PrepCheckpointObservation()
    {
        _sensor_obs_list.Clear();
        for (int i = 0; i < dead_sensors.Count; i++)
        {
            var obs = ShotRay(dead_sensors[i]);
            _sensor_obs_list.Add(obs.Item1);
            _sensor_obs_list.Add(obs.Item2);
        }
    }

    private (float,float) ShotRay(DeadSensor sensor)
    {
        var xform = sensor.Transform;
        var hit = Physics.Raycast(xform.position, xform.forward, out var hitInfo
            , sensor.RayDistance, _mask_value);
        return hit ? (1.0f, hitInfo.distance / sensor.RayDistance) : (0f, 1f);
        
    }
    // public void OnDrawGizmosSelected()
    // {
    //     Gizmos.color = Color.magenta;
    //     for (int i = 0; i < dead_sensors.Count; i++)
    //     {
    //         Gizmos.DrawRay(this.transform.position
    //             , dead_sensors[i].Transform.forward * dead_sensors[i].HitValidationDistance);
    //         if (Physics.Raycast(dead_sensors[i].Transform.position, dead_sensors[i].Transform.forward
    //             , dead_sensors[i].HitValidationDistance, _mask_value))
    //         {
    //             Debug.Log(i);
    //         }
    //     }
    //
    // }
}

   
