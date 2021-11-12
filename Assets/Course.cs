using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Math = UnityEngine.ProBuilder.Math;

public class Course : MonoBehaviour
{
    [SerializeField]
    public CheckPoint[] checkpoints;
    public Dictionary<int,List<int>> map = new Dictionary<int, List<int>>();
    public Dictionary<int,List<Vector3>> pos_map = new Dictionary<int, List<Vector3>>();
    // public Dictionary<int,string> name_map = new Dictionary<int, string>();
    public List<int> short_cut = new List<int>();
    private int short_cut_count = 0;
    public float advantage_score = 0f;
    public int start_point;
    public int terminal_point;

    public void BuildMap()
    {
        if (map == null)
            map = new Dictionary<int, List<int>>();
        if (pos_map == null)
            pos_map = new Dictionary<int, List<Vector3>>();
        start_point = checkpoints[0].Collider.GetInstanceID();

        foreach (var t in checkpoints)
        {
            map[t.Collider.GetInstanceID()] = t.GetNextNode();
            pos_map[t.Collider.GetInstanceID()] = new List<Vector3>();
            // name_map[t.Collider.GetInstanceID()] = t.gameObject.gameObject.name;
            if (t.advantage)
            {
                short_cut.Add(t.Collider.GetInstanceID());
                short_cut_count++;
            }
            foreach (var checkpoint in t.next_checkpoints)
            {
                pos_map[t.Collider.GetInstanceID()].Add(
                    checkpoint.collider.gameObject.transform.position);
            }
        }

        advantage_score = Mathf.Min(1,1 / (short_cut_count+Mathf.Epsilon));
        terminal_point = checkpoints[checkpoints.Length - 1].GetNextNode()[0];

    }

    public Collider StartPoint => checkpoints[0].Collider;

    public bool isTerminal(int id) => terminal_point == id;

    public float GetAdvantageScore(int collider_id)
    {
        if (IsShortCut(collider_id))
            return advantage_score;
        return 0f;
    }

    public bool IsShortCut(int collider_id)
    {
        if (short_cut.Contains(collider_id))
            return true;
        else
        {
            return false;
        }
    }
}
