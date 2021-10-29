using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PerceptionSensor : MonoBehaviour
{
    public Transform sensors_transform;
    public string mask_name;
    protected List<float> _sensor_obs_list = new List<float>();
    protected int _mask;
    // Start is called before the first frame update
    protected virtual void BaseInit()
    {
        if (_sensor_obs_list == null)
            _sensor_obs_list = new List<float>();
        _mask = 1 << LayerMask.NameToLayer(mask_name);
    }
    public abstract List<float> GetObservation(params object[] param);

    protected abstract void PerpObservation(params object[] param);

    protected abstract void ShotRay(params object[] param);
}
