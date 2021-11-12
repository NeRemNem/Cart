using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    private Collider _collider;
    public string NextCollider;
    public string test;
    public bool advantage;
    [System.Serializable]
    public struct NextCheckpoint
    {
        public Collider collider;
    }
    public NextCheckpoint[] next_checkpoints;
    [SerializeField]
    private List<int> _next_nodes = new List<int>();
    private void Awake()
    {
        _collider = GetComponent<BoxCollider>();
        SetNextNodes();
        test = gameObject.transform.position.ToString();
    }

    private void SetNextNodes()
    {
        if (_next_nodes == null)
            _next_nodes = new List<int>();
        for (int i = 0; i < next_checkpoints.Length; i++)
        {
            _next_nodes.Add(next_checkpoints[i].collider.GetInstanceID());
            NextCollider = next_checkpoints[i].collider.gameObject.name;
        }
    }

    public Collider Collider => _collider;
    public List<int> GetNextNode() => _next_nodes;
    
}
