using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class OutField : Field
{
    public bool IsReadyToLeave;
    public DateTime TimePlaned; // planed time to be active

    private void Awake() {
        DimX = UnityEngine.Random.Range(1, Parameters.DimX - Parameters.MinDim);
        DimZ = UnityEngine.Random.Range(1, Parameters.DimZ - Parameters.MinDim);
        MaxLayer = UnityEngine.Random.Range(1, Parameters.MaxLayer - Parameters.MinDim);

        TimePlaned = DateTime.Now;
        TimePlaned.AddDays(UnityEngine.Random.Range(0, 7));
        TimePlaned.AddHours(UnityEngine.Random.Range(0, 24));
        TimePlaned.AddMinutes(UnityEngine.Random.Range(0, 60));
    }
    private void Start() {
        initField();
    }

    public override string ToString() {
        var str = new StringBuilder();
        str.Append($"out time planed: {TimePlaned.ToString("T")}\n");
        str.Append($"Ground:\n{base.ToString()}");
        return str.ToString();
    }
}
