using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.UI;

public class ModelNameController : MonoBehaviour
{
    private BehaviorParameters _bp;
    public Text text;
    private void Start()
    {
        _bp = GetComponent<BehaviorParameters>();
        text.text = _bp.Model.name;
    }
}
