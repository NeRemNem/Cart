using System.Collections.Generic;
using KartGame.KartSystems;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class CartAgent : Agent, IInput
{
    [SerializeField] private bool _is_recording = false;
 
    private int checkpoint_count = 0;
    ArcadeKart m_Kart;
    bool m_Acceleration;
    bool m_Brake;
    float m_Steering;
    int _cur_checkpointID;

    public Course course;
    private int trackMask;
    private int checkpointMask;

    public trackSensor[] DeadSensors;
    public CheckpointSensor[] CheckpointSensors;
    public CheckpointSensor[] SubCheckpointSensors;

    public Transform checkpoint_sensors_tr;
    public Transform sub_checkpoint_sensors_tr;
    public Transform dead_sensros_tr;

    public LayerMask CheckpointMask;
    private float _timer = 0f;
    private List<float> _checkpoint_sensor_obs_list = new List<float>();
    private List<float> _sub_checkpoint_sensor_obs_list = new List<float>();
    private List<float> _dead_sensor_obs_list = new List<float>();
    public UnityEvent Goal = new UnityEvent();

    
    #region Sensor Structs

    [System.Serializable]
    public struct trackSensor
    {
        public Transform Transform;
        public float HitValidationDistance;

        [FormerlySerializedAs("HitValidationDistance")]
        public float RayDistance;
    }

    [System.Serializable]
    public struct CheckpointSensor
    {
        public Transform Transform;
        public float RayDistance;
    }

    #endregion

    void Awake()
    {
        m_Kart = GetComponent<ArcadeKart>();
        trackMask = 1 << LayerMask.NameToLayer("Track");
        checkpointMask = 1 << LayerMask.NameToLayer("TrainingCheckpoints");
    }

    public void Init(Course course, int id, Collider collider)
    {
        this.course = course;
        _cur_checkpointID = id;
        transform.localRotation = collider.transform.rotation;
        transform.position = collider.transform.position;
    }

    private void FixedUpdate()
    {
        _timer += Time.deltaTime;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        InterpretDiscreteActions(actions);
        if (isHitWall())
        {
            AddReward(-0.1f);
        }

        AddReward(-4.1f / MaxStep);
        AddReward(m_Kart.LocalSpeed() / (MaxStep));
    }

    private void PrepCheckpointObservation()
    {
        _checkpoint_sensor_obs_list.Clear();
        for (int i = 0; i < CheckpointSensors.Length; i++)
        {
            var obs = ShotCheckpointRay(CheckpointSensors[i],checkpoint_sensors_tr);
            _checkpoint_sensor_obs_list.Add(obs.Item1);
            _checkpoint_sensor_obs_list.Add(obs.Item2);
            _checkpoint_sensor_obs_list.Add(obs.Item3);
        }

        _sub_checkpoint_sensor_obs_list.Clear();
        for (int i = 0; i < SubCheckpointSensors.Length; i++)
        {
            var obs = ShotCheckpointRay(SubCheckpointSensors[i],sub_checkpoint_sensors_tr);
            _sub_checkpoint_sensor_obs_list.Add(obs.Item1);
            _sub_checkpoint_sensor_obs_list.Add(obs.Item2);
            _sub_checkpoint_sensor_obs_list.Add(obs.Item3);
        }
        
    }

    bool IsWrongWay() //잘못된길 가는것 방지, 잘못된길이면 True반환
    {
        var sensor = true;
        var sub_sensor = true;
        for (int i = 0; i < CheckpointSensors.Length; i++)
        {
            var xform = CheckpointSensors[i].Transform;
            var hit = Physics.Raycast(checkpoint_sensors_tr.position, xform.forward, out var hitInfo
                , CheckpointSensors[i].RayDistance, checkpointMask);
            if (hit)
            {
                //다음 체크포인트 이거나, 현재 체크포인트일때 
                if (IsNextCheckpoint(hitInfo.collider) || IsCurrentCheckpoint(hitInfo.collider))
                    sensor = false;
            }
        }
        for (int i = 0; i < SubCheckpointSensors.Length; i++)
        {
            var xform = SubCheckpointSensors[i].Transform;
            var hit = Physics.Raycast(sub_checkpoint_sensors_tr.position, xform.forward, out var hitInfo
                , SubCheckpointSensors[i].RayDistance, checkpointMask);
            if (hit)
            {
                //다음 체크포인트 이거나, 현재 체크포인트일때 
                if (IsNextCheckpoint(hitInfo.collider) || IsCurrentCheckpoint(hitInfo.collider))
                    sub_sensor = false;
            }
        }
        
        return sensor && sub_sensor;
    }

    private void PrepDeadObservation()
    {
        _dead_sensor_obs_list.Clear();
        for (int i = 0; i < DeadSensors.Length; i++)
        {
            var obs = ShotDeadRay(DeadSensors[i]);
            _dead_sensor_obs_list.Add(obs.Item1);
            _dead_sensor_obs_list.Add(obs.Item2);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(IsWrongWay());
        //3*9
        PrepCheckpointObservation();
        sensor.AddObservation(_checkpoint_sensor_obs_list);
        //3*9
        sensor.AddObservation(_sub_checkpoint_sensor_obs_list);
        
        //2*6
        PrepDeadObservation();
        sensor.AddObservation(_dead_sensor_obs_list);
        // 2+4
        sensor.AddObservation(m_Kart.LocalSpeed());
        sensor.AddObservation(m_Acceleration);
        sensor.AddObservation(transform.localRotation);
    }

    public bool isHitWall()
    {
        for (int i = 0; i < DeadSensors.Length; i++)
        {
            var cur_sensor = DeadSensors[i];
            if (Physics.Raycast(cur_sensor.Transform.position, cur_sensor.Transform.forward
                , out var hit, cur_sensor.RayDistance, trackMask))
                if (hit.distance <= cur_sensor.HitValidationDistance)
                    return true;
        }

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        var maskedValue = 1 << other.gameObject.layer;
        var triggered = maskedValue & CheckpointMask;
        if (triggered > 0 && IsNextCheckpoint(other))
        {
            _cur_checkpointID = other.GetInstanceID();
            //도착
            if (course.isTerminal(_cur_checkpointID))
            {
                checkpoint_count = 0;
                var score = ZFilter.GetScore(_timer * -1);
                SetReward((float) score);
                Goal.Invoke();
                EndEpisode();
            }
            //체크포인트
            else
            {
                AddReward(2f / course.map.Count);
#if UNITY_EDITOR
                checkpoint_count++;
                //20
                if (checkpoint_count == 20 && _is_recording)
                {
                    UnityEditor.EditorApplication.isPlaying = false;
                }
#endif
            }
        }
        else if (triggered > 0 && IsCurrentCheckpoint(other) == false)
        {
            AddReward(-1.0f);
            EndEpisode();
        }
    }

    // return (HitSomething, distance)
    private (float, float) ShotDeadRay(trackSensor sensor)
    {
        var xform = sensor.Transform;
        var hit = Physics.Raycast(dead_sensros_tr.position, xform.forward, out var hitInfo
            , sensor.RayDistance, trackMask);
        return hit ? (1.0f, hitInfo.distance / sensor.RayDistance) : (0f, 1f);
    }

    // return (IsNextCheckpoint, HitSomething, distance)
    private (float, float, float) ShotCheckpointRay(CheckpointSensor sensor, Transform sensor_tr)
    {
        var xform = sensor.Transform;
        var hit = Physics.Raycast(sensor_tr.position, xform.forward, out var hitInfo
            , sensor.RayDistance, checkpointMask);
        if (hit)
        {
            
            if (IsNextCheckpoint(hitInfo.collider))
                return (1f, 1f, hitInfo.distance / sensor.RayDistance);
            else if (hitInfo.collider.GetInstanceID() != _cur_checkpointID)
            {
                return (0f, 1f, hitInfo.distance / sensor.RayDistance);
            }
        }

        return (0f, 0f, 1f);
    }

    private bool IsNextCheckpoint(Collider checkPoint)
    {
        if (course.map[_cur_checkpointID].Contains(checkPoint.GetInstanceID()))
            return true;

        return false;
    }

    private bool IsCurrentCheckpoint(Collider checkpoint) => _cur_checkpointID == checkpoint.GetInstanceID();

    public override void OnEpisodeBegin()
    {
        _cur_checkpointID = course.StartPoint.GetInstanceID();

        var collider = course.StartPoint;
        transform.localRotation = collider.transform.rotation;
        transform.position = collider.transform.position;

        m_Kart.Rigidbody.velocity = default;
        m_Acceleration = false;
        m_Brake = false;
        _timer = 0f;
        m_Steering = 0f;
    }


    #region InputThings

    void InterpretDiscreteActions(ActionBuffers actions)
    {
        m_Steering = actions.DiscreteActions[0] - 1f;
        m_Acceleration = actions.DiscreteActions[1] == 1;
        m_Brake = actions.DiscreteActions[1] == 2;
    }

    public InputData GenerateInput()
    {
        return new InputData
        {
            Accelerate = m_Acceleration,
            Brake = m_Brake,
            TurnInput = m_Steering
        };
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var action_out = actionsOut.DiscreteActions;
        if (Input.GetButton("Accelerate"))
        {
            action_out[1] = 1;
        }

        if (Input.GetButton("Brake"))
        {
            action_out[1] = 2;
        }

        action_out[0] = (int) Input.GetAxisRaw("Horizontal") + 1;
    }

    #endregion


    #region DebugThings

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        trackMask = 1 << LayerMask.NameToLayer("Track");

        for (int i = 0; i < DeadSensors.Length; i++)
        {
            Gizmos.DrawRay(dead_sensros_tr.position
                , DeadSensors[i].Transform.forward * DeadSensors[i].HitValidationDistance);
            if (Physics.Raycast(DeadSensors[i].Transform.position, DeadSensors[i].Transform.forward
                , DeadSensors[i].HitValidationDistance, trackMask))
            {
                Debug.Log(i);
            }
        }

        checkpointMask = 1 << LayerMask.NameToLayer("TrainingCheckpoints");

        for (int i = 0; i < CheckpointSensors.Length; i++)
        {
            if (Physics.Raycast(checkpoint_sensors_tr.position, CheckpointSensors[i].Transform.forward
                , out var hit, CheckpointSensors[i].RayDistance, checkpointMask))
            {
                if (IsNextCheckpoint(hit.collider))
                    Debug.DrawRay(checkpoint_sensors_tr.position,
                        CheckpointSensors[i].Transform.forward * hit.distance, Color.green);
            }
            else
            {
                Debug.DrawRay(checkpoint_sensors_tr.position, CheckpointSensors[i].Transform.forward
                                                              * CheckpointSensors[i].RayDistance, Color.cyan);
            }
        }
    }

    #endregion
}