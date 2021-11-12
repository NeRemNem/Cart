using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackPerceptionSensor : MonoBehaviour
{
    public List<TracktSensor> track_sensor;
    public string mask_name;
    private List<float> _sensor_obs_list = new List<float>();
    private int _mask_value;
    private Course _course;
    public bool draw_gizmo;
    private void Awake()
    {
        if (_sensor_obs_list == null)
            _sensor_obs_list = new List<float>();
        if (track_sensor == null)
            track_sensor = new List<TracktSensor>();
        _mask_value = 1<<LayerMask.NameToLayer(mask_name);
        for (int i = 0; i < transform.childCount; i++)
        {
            var sensor = new TracktSensor();
            sensor.Transform = transform.GetChild(i);
            sensor.RayDistance = 30f;
            track_sensor.Add(sensor);
        }
    }
    [System.Serializable]
    public struct TracktSensor
    {
        public Transform Transform;
        public float RayDistance;
    }

    public List<float> GetObservation()
    {
        PrepCheckpointObservation();
        return _sensor_obs_list;
    }

    private void PrepCheckpointObservation()
    {
        _sensor_obs_list.Clear();
        for (int i = 0; i < track_sensor.Count; i++)
        {
            var obs = ShotRay(track_sensor[i]);
            _sensor_obs_list.Add(obs.Item1);
            _sensor_obs_list.Add(obs.Item2);
        }
    }

    private (float, float) ShotRay(TracktSensor sensor)
    {
        var xform = sensor.Transform;
        var position = xform.position;
        position = new Vector3(position.x, 0.8f, position.z);
        xform.position = position;
        var y = xform.rotation.eulerAngles.y;
        var rotation = new Vector3(0f, y, 0f);
        xform.position = position;
        xform.rotation = Quaternion.Euler(rotation); 
        var hit = Physics.Raycast(position, xform.forward, out var hitInfo
            , sensor.RayDistance, _mask_value);
        return hit ? (1.0f, hitInfo.distance / sensor.RayDistance) : (0f, 1f);
    }

    public void OnDrawGizmosSelected()
    {
        if (draw_gizmo)
        {
            for (int i = 0; i < track_sensor.Count; i++)
            {
                var xform = track_sensor[i].Transform;
                var position = xform.position;
                position = new Vector3(position.x, 0.8f, position.z);
                var y = xform.rotation.eulerAngles.y;
                var rotation = new Vector3(0f, y, 0f);
                xform.position = position;
                xform.rotation = Quaternion.Euler(rotation); 
                if (Physics.Raycast(xform.position, xform.forward
                    , out var hit, track_sensor[i].RayDistance, _mask_value))
                {
                    Debug.DrawRay(xform.position,
                        xform.forward * hit.distance, Color.red);
                }
                else
                {
                    Debug.DrawRay(xform.position, xform.forward
                                                  * track_sensor[i].RayDistance,
                        Color.white);
                }
            }
        }
    }
}
