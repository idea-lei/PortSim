using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Evaluation : MonoBehaviour
{
    public int OutContainerCount = 0;
    public int RearrangeCount = 0;
    public TimeSpan TotalTimeSpan = new TimeSpan();

    [SerializeField] private float avgRearrangeCount;
    [SerializeField] private string avgTimeSpan;

    /// <summary>
    /// thsi method should be called when removing a outfield
    /// </summary>
    public void UpdateEvaluation(Container[] containers) {
        OutContainerCount += containers.Length;
        foreach(var c in containers) {
            RearrangeCount += c.RearrangeCount;
            TotalTimeSpan += c.TotalMoveTime;
        }
        avgRearrangeCount = (float)RearrangeCount / OutContainerCount;
        avgTimeSpan = new TimeSpan(Convert.ToInt64(TotalTimeSpan.Ticks / OutContainerCount)).ToString("g");
    }
}
