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
    public TrackPerceptionSensor track_sensor;

    public LayerMask CheckpointMask;
    public float timer = 0f;

    public UnityEvent Goal = new UnityEvent();
    public TrajectoryRecorder tr_record;


    void Awake()
    {
        tr_record = GetComponent<TrajectoryRecorder>();
        m_Kart = GetComponent<ArcadeKart>();
    }

    public void Init(Course course, int id, Collider collider)
    {
        this.course = course;
        dead_sensor.Init();
        checkpoint_sensor.Init(course);
        _cur_checkpointID = id;
        transform.rotation = collider.transform.rotation;
        transform.position = collider.transform.position;
    }

    private void FixedUpdate()
    {
        timer += Time.deltaTime;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        InterpretDiscreteActions(actions);
        if (dead_sensor.IsHitWall())
        {
#if UNITY_EDITOR
            print("hit");
#endif
            AddReward(-0.1f);
        }
        AddReward(-4.1f / MaxStep);
        AddReward( (Mathf.Max(0f,m_Kart.LocalSpeed()))/ MaxStep);
    }

   

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(CalcurateVector());
        sensor.AddObservation(checkpoint_sensor.GetObservation(_cur_checkpointID));
        sensor.AddObservation(track_sensor.GetObservation());
        sensor.AddObservation(dead_sensor.GetObservation());
        sensor.AddObservation(m_Kart.LocalSpeed());
        sensor.AddObservation(m_Acceleration);
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
                var score = ZFilter.GetScore(timer * -1);
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
                if(course.IsShortCut(other.GetInstanceID()))
                    AddReward((4f / course.map.Count));
                else                
                    AddReward((2f / course.map.Count));
                checkpoint_count++;
               
            }
        }
        else if (triggered > 0 && IsCurrentCheckpoint(other) == false)
        {
            AddReward(-1.0f);
            EndEpisode();
        }
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
        tr_record.Init();
        var collider = course.StartPoint;
        transform.localRotation = collider.transform.rotation;
        transform.position = collider.transform.position;

        m_Kart.Rigidbody.velocity = default;
        m_Acceleration = false;
        m_Brake = false;
        timer = 0f;
        m_Steering = 0f;
    }

    private List<float> CalcurateVector()
    {
        List<float> dot_obs = new List<float>();
        int count = 0;
        foreach (var i in course.pos_map[_cur_checkpointID])
        {            
            var vector = new Vector3(i.x, 0f, i.z);
            var cart_position = new Vector3(this.transform.position.x, 0f, this.transform.position.z);
            count += 1;
            var toward = vector - cart_position;
            var item = Vector3.Dot(this.gameObject.transform.forward.normalized, toward.normalized);
            dot_obs.Add(item);
            print(item);
        }
        while(dot_obs.Count != 3)
            dot_obs.Add(-1f);
        
        return dot_obs;

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