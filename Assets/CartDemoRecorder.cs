using System.Collections.Generic;
using KartGame.KartSystems;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Demonstrations;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;

public class CartDemoRecorder : Agent, IInput
{
    ArcadeKart m_Kart;
    bool m_Acceleration;
    bool m_Brake;
    float m_Steering;
    int m_CheckpointID;

    public Course course;
    private int trackMask;
    private int checkpointMask;
    public trackSensor[] DeadSensors;
    public CheckpointSensor[] CheckpointSensors;
    public Transform CheckpointSensorsTransform;
    public Transform DeadSensorsTransform;

    public LayerMask CheckpointMask;
    public float timer = 0f;
    private List<float> _checkpoint_sensor_obs_list = new List<float>();
    private List<float> _dead_sensor_obs_list = new List<float>();
    
    private DemonstrationRecorder _recorder;
    private bool _is_recording = false;
    
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

    void Awake()
    {
        _recorder = GetComponent<DemonstrationRecorder>();
        if (_recorder.Record == true)
        {
            _is_recording = true;
        }
        
        m_Kart = GetComponent<ArcadeKart>();
        trackMask = 1 << LayerMask.NameToLayer("Track");
        checkpointMask = 1 << LayerMask.NameToLayer("TrainingCheckpoints");
        
    }

    public void Init(Course course, int id)
    {
        this.course = course;
        m_CheckpointID = id;

    }

    private void Update()
    {
        timer += Time.deltaTime;
        //Debug.Log(string.Format("{0}, {1}, {2}",gameObject.name, transform.localPosition, transform.localRotation));

    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        InterpretDiscreteActions(actions);
        if (isHitWall())
        {
            AddReward(-0.1f);
            if(_is_recording)
                EndEpisode();
        }
        
        AddReward(-4.0f/ MaxStep);
        AddReward(m_Kart.LocalSpeed()/ (MaxStep*4));
        AddReward((m_Acceleration && !m_Brake ? 1.0f:0.0f) / (MaxStep*4));
        
    }

    private void PrepCheckpointObservation()
    {
        _checkpoint_sensor_obs_list.Clear();
        for(int i = 0; i<CheckpointSensors.Length; i++)
        {
            var obs = ShotCheckpointRay(CheckpointSensors[i]);
            _checkpoint_sensor_obs_list.Add(obs.Item1);
            _checkpoint_sensor_obs_list.Add(obs.Item2);
            _checkpoint_sensor_obs_list.Add(obs.Item3);
        }
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
        //3*11
        PrepCheckpointObservation();
        sensor.AddObservation(_checkpoint_sensor_obs_list);
        //2*6
        PrepDeadObservation();
        sensor.AddObservation(_dead_sensor_obs_list);
        // 9
        sensor.AddObservation(m_Kart.LocalSpeed());
        sensor.AddObservation(m_Acceleration);
        sensor.AddObservation(transform.localPosition);
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
            m_CheckpointID = other.GetInstanceID();
            //도착
            if (course.isTerminal(m_CheckpointID))
            {
                if (_is_recording)
                {
                    _recorder.Record = false;
                    _is_recording = false;
                }
                var score = ZFilter.GetScore(timer*-1);
                SetReward((float)score);
                EndEpisode();
            }
            //체크포인트
            else
            {
                AddReward(2f/course.map.Count);
            }
        }
        else if (triggered > 0 && other.GetInstanceID() != m_CheckpointID)
        {
            AddReward(-1.0f);
            EndEpisode();
        }
    }

    private (float,float) ShotDeadRay(trackSensor sensor)
    {
        var xform = sensor.Transform;
        var hit = Physics.Raycast(DeadSensorsTransform.position, xform.forward, out var hitInfo
            , sensor.RayDistance, trackMask);
        return hit ? (1.0f,hitInfo.distance/sensor.RayDistance) : (0f,1f);
    }

    private (float,float,float) ShotCheckpointRay(CheckpointSensor sensor)
    {
        var xform = sensor.Transform;
        var hit = Physics.Raycast(CheckpointSensorsTransform.position, xform.forward, out var hitInfo
            , sensor.RayDistance, checkpointMask);
        if (hit)
        {
            if (IsNextCheckpoint(hitInfo.collider))
                return (1f,1f,hitInfo.distance/sensor.RayDistance);
            else if (hitInfo.collider.GetInstanceID() != m_CheckpointID)
            {
                return (0f,1f,hitInfo.distance/sensor.RayDistance);
            }
        }

        return (0f,0f,1f);
    }

    private bool IsNextCheckpoint(Collider checkPoint)
    {
        if (course.map[m_CheckpointID].Contains(checkPoint.GetInstanceID()))
            return true;

        return false;
    }

    public override void OnEpisodeBegin()
    {
        m_CheckpointID = course.StartPoint.GetInstanceID();

        var collider = course.StartPoint;
        transform.localRotation = collider.transform.rotation;
        transform.position = collider.transform.position;

        m_Kart.Rigidbody.velocity = default;
        m_Acceleration = false;
        m_Brake = false;
        timer = 0f;
        m_Steering = 0f;
    }


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

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        trackMask = 1 << LayerMask.NameToLayer("Track");

        for (int i = 0; i < DeadSensors.Length; i++)
        {
            Gizmos.DrawRay(DeadSensorsTransform.position
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
            if (Physics.Raycast(CheckpointSensorsTransform.position, CheckpointSensors[i].Transform.forward
                , out var hit, CheckpointSensors[i].RayDistance, checkpointMask))
            {
                if (IsNextCheckpoint(hit.collider))
                    Debug.DrawRay(CheckpointSensorsTransform.position,
                        CheckpointSensors[i].Transform.forward * hit.distance, Color.green);
            }
            else
            {
                Debug.DrawRay(CheckpointSensorsTransform.position, CheckpointSensors[i].Transform.forward
                                                                   * CheckpointSensors[i].RayDistance, Color.cyan);
            }
        }
    }
}