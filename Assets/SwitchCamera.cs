using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class SwitchCamera : MonoBehaviour
{
    [System.Serializable]
    struct CameraModel
    {
        public string name;
        public Transform model;
        public Transform body;
    }
    [SerializeField] private CinemachineVirtualCamera _cine_camera;
    [SerializeField] private List<CameraModel> _cam_list;
    [SerializeField] private List<CameraModel> _model_list;
    [SerializeField] private Text _model_pannel;

    private int target_index =0 ; 
    // Start is called before the first frame update
    void ControlCamera()
    {
        if (Input.GetKeyUp(KeyCode.Comma))
        {
            MoveTargetIndex(-1);
            SetCameraTarget();
        }
        else if (Input.GetKeyUp(KeyCode.Period))
        {
            MoveTargetIndex(1);
            SetCameraTarget();
        }
    }

    void SetCameraTarget()
    {
        _model_pannel.text = _model_list[target_index].name;
        _cine_camera.Follow = _model_list[target_index].model;
        _cine_camera.LookAt = _model_list[target_index].body;
    }

    void MoveTargetIndex(int i)
    {
        if (i < 0 && target_index + i < 0)
        {
            target_index = _model_list.Count - 1;
        }

        else if (i > 0 &&target_index + i > _model_list.Count - 1)
        {
            target_index = 0;
        }
        else
        {
            target_index += i;
        }
    }

    private void Start()
    {
        target_index = 0;
        SetCameraTarget();
    }

    // Update is called once per frame
    void Update()
    {
        ControlCamera();
    }
}
