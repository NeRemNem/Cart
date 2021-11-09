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
    public class Data
    {
        public List<Vector3> traj = new List<Vector3>();
    }

    public class LapTime
    {
        public List<float> lap_time = new List<float>();
    }
    private BehaviorParameters _behavior_parameters;
    private BinaryFormatter _formatter = new BinaryFormatter();
    public string save_path;
    private Data _data = new Data();
    private LapTime _lap_time_data = new LapTime();
    public string name = "";
    private bool _is_saved = false;
    private string _model_name = "";
    private int record_count = 0;
    public bool record_one;
    private CartAgent _cart_agent;

    private void Awake()
    {
        _behavior_parameters = GetComponent<BehaviorParameters>();
        if (_behavior_parameters.Model != null)
            _model_name = _behavior_parameters.Model.name;
        _cart_agent = GetComponent<CartAgent>();

    }

    public void FixedUpdate()
    {
        _data.traj.Add(transform.localPosition);
    }

    public void Init()
    {
        _data = new Data();
    }

    public void SaveTime()
    {
        _lap_time_data.lap_time.Add(_cart_agent.timer);
    }
    public void SaveTrajectory()
    {
        if (GetComponent<TrajectoryRecorder>().isActiveAndEnabled 
                && _is_saved == false)
        {
            if(record_one == true)
                _is_saved = true;
            try
            {
                SaveTime();
                string json = JsonUtility.ToJson(_data, true);

                if (json.Equals("{}"))
                {
                    Debug.Log("json null");
                    return;
                }
                var i = 0;

                string path = "";
                if(_model_name == "")
                    path = save_path + "/" +name+"_"+i+".json";
                else
                    path = save_path + "/" +name+"_"+_model_name+"_"+i+".json";
                while (File.Exists(path))
                {
                    ++i;
                    if(_model_name == "")
                        path = save_path + "/" +name+"_"+i+".json";
                    else
                        path = save_path + "/" +name+"_"+_model_name+"_"+i+".json";
                }
                File.WriteAllText(path, json);

                Debug.Log(path);
                record_count++;
#if UNITY_EDITOR

                if (record_count >= 5)
                {
                    _is_saved = true;   

                }
#endif
                _data = new Data();
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
