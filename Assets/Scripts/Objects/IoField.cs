using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// base class of the inField and outField
/// </summary>
public abstract class IoField : Field
{
    public DateTime TimePlaned;
    public DateTime TimeReal;

    private void Awake() {
        DimX = UnityEngine.Random.Range(1, Parameters.DimX - Parameters.MinDim);
        DimZ = UnityEngine.Random.Range(1, Parameters.DimZ - Parameters.MinDim);
        MaxLayer = UnityEngine.Random.Range(1, Parameters.MaxLayer - Parameters.MinDim);
        _ground = new Stack<Container>[DimX, DimZ];

        TimePlaned = DateTime.Now;
        TimePlaned.AddDays(UnityEngine.Random.Range(0, 7));
        TimePlaned.AddHours(UnityEngine.Random.Range(0, 24));
        TimePlaned.AddMinutes(UnityEngine.Random.Range(0, 60));
    }

    private void OnEnable() {
        initField();
    }

    public override string ToString() {
        var str = new StringBuilder();
        str.Append($"time planed: {TimePlaned.ToString("T")}\n");
        str.Append($"Ground:\n{base.ToString()}");
        return str.ToString();
    }
}
