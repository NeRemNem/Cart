using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.MLAgents.Policies;
using UnityEngine;

public class TrajectoryRecorder : MonoBehaviour
{
    [System.Serializable]
    public class data
    {
        public List<Vector3> traj = new List<Vector3>();
    }

    private BehaviorParameters _behavior_parameters;
    private BinaryFormatter _formatter = new BinaryFormatter();
    public string save_path;
    private data _data = new data();
    public Collider destination;
    public bool record_full_track = false;
    public string name = "";
    private bool _is_saved = false;

    private void Awake()
    {
        _behavior_parameters = GetComponent<BehaviorParameters>();
        if (destination == null)
        {
            record_full_track = true;
        }
    }

    public void FixedUpdate()
    {
        _data.traj.Add(transform.localPosition);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (record_full_track == false)
        {
            if (other.GetInstanceID() == destination.GetInstanceID())
                SaveTrajectory();
        }
    }

    public void SaveTrajectory()
    {
        if (GetComponent<TrajectoryRecorder>().isActiveAndEnabled 
                && _is_saved == false)
        {

            _is_saved = true;
            try
            {
                string json = JsonUtility.ToJson(_data, true);

                if (json.Equals("{}"))
                {
                    Debug.Log("json null");
                    return;
                }
                string path = save_path + "/" +name+"_"+_behavior_parameters.Model.name+".json";
                var i = 0;
                while (File.Exists(path))
                {
                    path = save_path + "/"+name+"_"+ _behavior_parameters.Model.name+"_"+i+".json";
                }
                File.WriteAllText(path, json);

                Debug.Log(json);
            }
            catch (FileNotFoundException e)
            {
                Debug.Log("The file was not found:" + e.Message);
            }
            catch (DirectoryNotFoundException e)
            {
                Debug.Log("The directory was not found: " + e.Message);
            }
            catch (IOException e)
            {
                Debug.Log("The file could not be opened:" + e.Message);
            }
        }
    }
}
