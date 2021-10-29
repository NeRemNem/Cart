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
    public DeadPerceptionSensor dead_sensor;
    public CheckPointPerceptionSensor checkpoint_sensor;

    public LayerMask CheckpointMask;
    private float _timer = 0f;

    public UnityEvent Goal = new UnityEvent();


    void Awake()
    {
        m_Kart = GetComponent<ArcadeKart>();

    }

    public void Init(Course course, int id, Collider collider)
    {
        this.course = course;
        dead_sensor.Init();
        checkpoint_sensor.Init(course);
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
        if (dead_sensor.IsHitWall())
        {
            AddReward(-0.1f);
        }
        
        AddReward(-4.1f / MaxStep);
        AddReward(m_Kart.LocalSpeed() / (MaxStep));
    }

    // private void PrepCheckpointObservation()
    // {
    //     _checkpoint_sensor_obs_list.Clear();
    //     for (int i = 0; i < CheckpointSensors.Length; i++)
    //     {
    //         
    //         var obs = ShotCheckpointRay(CheckpointSensors[i],checkpoint_sensors_tr);
    //         _checkpoint_sensor_obs_list.Add(obs.Item1);
    //         _checkpoint_sensor_obs_list.Add(obs.Item2);
    //         _checkpoint_sensor_obs_list.Add(obs.Item3);
    //     }
    // }
    //
    // private void PrepDeadObservation()
    // {
    //     _dead_sensor_obs_list.Clear();
    //     for (int i = 0; i < DeadSensors.Length; i++)
    //     {
    //         var obs = ShotDeadRay(DeadSensors[i]);
    //         _dead_sensor_obs_list.Add(obs.Item1);
    //         _dead_sensor_obs_list.Add(obs.Item2);
    //     }
    // }

    public override void CollectObservations(VectorSensor sensor)
    {
        //sensor.AddObservation(IsWrongWay());
        //3*9
        // var checkpoint_obs = checkpiont_sensor.GetObservation(_cur_checkpointID);
        // sensor.AddObservation(checkpoint_obs);
        // //3*9
        // //sensor.AddObservation(_sub_checkpoint_sensor_obs_list);
        //
        // //2*6
        // PrepDeadObservation();
        // sensor.AddObservation(_dead_sensor_obs_list);
        // 2+4
        sensor.AddObservation(checkpoint_sensor.GetObservation(_cur_checkpointID));
        sensor.AddObservation(dead_sensor.GetObservation());
        sensor.AddObservation(m_Kart.LocalSpeed());
        sensor.AddObservation(m_Acceleration);
        sensor.AddObservation(transform.localRotation);
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
                var score = ZFilter.GetScore(_timer * -1);
                SetReward((float) score);
                Goal.Invoke();                
                EndEpisode();
#if UNITY_EDITOR

                if ( _is_recording)
                {
                    UnityEditor.EditorApplication.isPlaying = false;
                }
#endif

            }
            //체크포인트
            else
            {
                AddReward(2f / course.map.Count);
                checkpoint_count++;
               
            }
        }
        else if (triggered > 0 && IsCurrentCheckpoint(other) == false)
        {
            AddReward(-1.0f);
            EndEpisode();
        }
    }

    // // return (HitSomething, distance)
    // private (float, float) ShotDeadRay(DeadSensor sensor)
    // {
    //     var xform = sensor.Transform;
    //     var hit = Physics.Raycast(dead_sensros_tr.position, xform.forward, out var hitInfo
    //         , sensor.RayDistance, trackMask);
    //     return hit ? (1.0f, hitInfo.distance / sensor.RayDistance) : (0f, 1f);
    // }
    //
    // // return (IsNextCheckpoint, HitSomething, distance)
    // private (float, float, float) ShotCheckpointRay(CheckpointSensor sensor, Transform sensor_tr)
    // {
    //     var xform = sensor.Transform;
    //     var hit = Physics.Raycast(sensor_tr.position, xform.forward, out var hitInfo
    //         , sensor.RayDistance, checkpointMask);
    //     if (hit)
    //     {
    //         if (IsNextCheckpoint(hitInfo.collider))
    //         {
    //             return (1f, 1f, hitInfo.distance / sensor.RayDistance);
    //         }
    //
    //         if (hitInfo.collider.GetInstanceID() == _cur_checkpointID)
    //         {
    //             return (0f, 1f, hitInfo.distance / sensor.RayDistance);
    //         }
    //         if (hitInfo.collider.GetInstanceID() != _cur_checkpointID)
    //         {
    //             return (-1f, 1f, hitInfo.distance / sensor.RayDistance);
    //         }
    //     }
    //
    //     return (0f, 0f, 1f);
    // }

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



}