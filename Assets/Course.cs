using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Course : MonoBehaviour
{
    [SerializeField]
    public CheckPoint[] checkpoints;
    public Dictionary<int,List<int>> map = new Dictionary<int, List<int>>();
    public int start_point;
    public int terminal_point;

    public void BuildMap()
    {
        if (map == null)
            map = new Dictionary<int, List<int>>();
        start_point = checkpoints[0].Collider.GetInstanceID();

        foreach (var t in checkpoints)
        {
            map[t.Collider.GetInstanceID()] = t.GetNextNode();
        }
        terminal_point = checkpoints[checkpoints.Length - 1].GetNextNode()[0];

    }

    public Collider StartPoint => checkpoints[0].Collider;

    public bool isTerminal(int id) => terminal_point == id;


}
