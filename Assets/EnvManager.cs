using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvManager : MonoBehaviour
{

    public Course[] courses;
    public int course_id;
    public CartAgent[] agents;
    private void Awake()
    {
        if (course_id >= courses.Length)
            course_id = courses.Length - 1;
        foreach (var course in courses)
        {
            course.BuildMap();
        }
        foreach (var agent in agents)
        {
            agent.Init(courses[course_id]
                , courses[course_id].StartPoint.GetInstanceID()
                ,courses[course_id].StartPoint
                );
        }
    }
}
