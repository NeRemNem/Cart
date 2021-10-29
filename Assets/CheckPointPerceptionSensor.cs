using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CheckPointPerceptionSensor : MonoBehaviour
{
    public List<CheckpointSensor> checkpoint_sensor;
    public LayerMask mask;
    private List<float> _sensor_obs_list = new List<float>();
    private int _mask_value;
    private Course _course;
    private void Awake()
    {
        if (_sensor_obs_list == null)
            _sensor_obs_list = new List<float>();
        if (checkpoint_sensor == null)
            checkpoint_sensor = new List<CheckpointSensor>();
        _mask_value = 1 << mask;
        for (int i = 0; i < transform.childCount; i++)
        {
            var sensor = new CheckpointSensor();
            sensor.Transform = transform.GetChild(i);
            sensor.RayDistance = 30f;
            checkpoint_sensor.Add(sensor);
        }
    }

    public void Init(Course course)
    {
        _course = course;
    }
    [System.Serializable]
    public struct CheckpointSensor
    {
        public Transform Transform;
        public float RayDistance;
    }

    public List<float> GetObservation(int cur_checkpointID)
    {
        PrepCheckpointObservation(cur_checkpointID);
        return _sensor_obs_list;

    }
    private void PrepCheckpointObservation(int cur_checkpointID)
    {
        _sensor_obs_list.Clear();
        for (int i = 0; i < checkpoint_sensor.Count; i++)
        {
            var obs = ShotRay(checkpoint_sensor[i], cur_checkpointID);
            _sensor_obs_list.Add(obs.Item1);
            _sensor_obs_list.Add(obs.Item2);
            _sensor_obs_list.Add(obs.Item3);
        }
    }

    private (float, float, float) ShotRay(CheckpointSensor sensor, int cur_checkpointID)
    {
        var xform = sensor.Transform;
        var hit = Physics.Raycast(xform.position, xform.forward, out var hitInfo
            , sensor.RayDistance, _mask_value);
        if (hit)
        {
            if (IsNextCheckpoint(hitInfo.collider, cur_checkpointID))
            {
                return (1f, 1f, hitInfo.distance / sensor.RayDistance);
            }

            if (hitInfo.collider.GetInstanceID() == cur_checkpointID)
            {
                return (0f, 1f, hitInfo.distance / sensor.RayDistance);
            }
            if (hitInfo.collider.GetInstanceID() != cur_checkpointID)
            {
                return (-1f, 1f, hitInfo.distance / sensor.RayDistance);
            }
        }

        return (0f, 0f, 1f);
    }
    private bool IsNextCheckpoint(Collider checkPoint,int cur_checkpointID)
    {
        if (_course.map[cur_checkpointID].Contains(checkPoint.GetInstanceID()))
            return true;

        return false;
    }
    // public void OnDrawGizmosSelected()
    // {
    //     
    //     for (int i = 0; i < checkpoint_sensor.Count; i++)
    //     {
    //         if (Physics.Raycast(transform.position, checkpoint_sensor[i].Transform.forward
    //             , out var hit, checkpoint_sensor[i].RayDistance, _mask_value))
    //         {
    //             Debug.DrawRay(checkpoint_sensor[i].Transform.position,
    //                 checkpoint_sensor[i].Transform.forward * hit.distance, Color.green);
    //         }
    //         else
    //         {
    //             Debug.DrawRay(checkpoint_sensor[i].Transform.position, checkpoint_sensor[i].Transform.forward
    //                                                                    * checkpoint_sensor[i].RayDistance, Color.cyan);
    //         }
    //     }
    // }

}
