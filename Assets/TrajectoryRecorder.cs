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
    private string _model_name = "";
    private int record_count = 0;

    private void Awake()
    {
        _behavior_parameters = GetComponent<BehaviorParameters>();
        if (_behavior_parameters.Model != null)
            _model_name = _behavior_parameters.Model.name;
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
            if(record_full_track == false)
                _is_saved = true;
            try
            {
                string json = JsonUtility.ToJson(_data, true);

                if (json.Equals("{}"))
                {
                    Debug.Log("json null");
                    return;
                }

                string path = "";
                if(_model_name == "")
                    path = save_path + "/" +name+".json";
                else
                    path = save_path + "/" +name+"_"+_model_name+".json";
                var i = 0;
                while (File.Exists(path))
                {
                    ++i;
                    if(_model_name == "")
                        path = save_path + "/" +name+"_"+i+".json";
                    else
                        path = save_path + "/" +name+"_"+_model_name+"_"+i+".json";
                }
                File.WriteAllText(path, json);

                Debug.Log(json);
                record_count++;
#if UNITY_EDITOR

                if (record_count >= 5)
                    _is_saved = true;
#endif

                _data = new data();
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
