using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// base class of the inField and outField
/// </summary>
public abstract class IoField : Field, IComparable<IoField> {
    [NonSerialized] public DateTime TimePlaned;
    [NonSerialized] public DateTime TimeReal;   // do we need this?
    [NonSerialized] public TimeSpan EstimatedDuration;  // this estimated duration for loading / unloading process
    [NonSerialized] public IoPort Port;

    private void OnDestroy() {
        if (Port.isActiveAndEnabled) Port.UpdateCurrentField();
    }

    #region logic methods
    protected override void initField() {
        DimX = UnityEngine.Random.Range(1, Parameters.DimX - Parameters.MinDim);
        DimZ = UnityEngine.Random.Range(1, Parameters.DimZ - Parameters.MinDim);
        MaxLayer = UnityEngine.Random.Range(1, Parameters.MaxLayer - Parameters.MinDim);
        assignPort();
        base.initField();
    }
    private void assignPort() {
        var ports = FindObjectsOfType<IoPort>();
        Port = ports[UnityEngine.Random.Range(0, ports.Length)];
        Port.FieldsBuffer.Add(this);
    }

    protected virtual void updateState(bool state) {
        var collider = GetComponent<MeshCollider>();
        var renderer = GetComponent<MeshRenderer>();
        if (collider && renderer) {
            renderer.enabled = state;
            collider.enabled = state;
        }
    }
    #endregion

    #region minor methods
    public override string ToString() {
        var str = new StringBuilder();
        str.Append($"time planed: {TimePlaned:T}\n");
        str.Append($"Ground:\n{base.ToString()}");
        return str.ToString();
    }

    public TimeSpan GenerateRandomTimeSpan() {
        return new TimeSpan(
            UnityEngine.Random.Range(0, 0),
            UnityEngine.Random.Range(0, 0),
            UnityEngine.Random.Range(0, 3),
            UnityEngine.Random.Range(0, 30));
    }

    public int CompareTo(IoField other) {
        return DateTime.Compare(TimePlaned, other.TimePlaned);
    }
    #endregion
}
