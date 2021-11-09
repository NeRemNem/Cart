using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoldPos : MonoBehaviour
{
    private float y_pos = 0.3f;
    void Start()
    {
        y_pos = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position = new Vector3(this.transform.position.x,y_pos,this.transform.position.z);
    }
}
