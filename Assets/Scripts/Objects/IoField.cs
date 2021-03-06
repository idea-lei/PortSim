using System;
using System.Text;
using UnityEngine;

/// <summary>
/// base class of the inField and outField
/// </summary>
public abstract class IoField : Field, IComparable<IoField> {
    private DateTime _timePlaned;
    public virtual DateTime TimePlaned {
        get => _timePlaned;
        set {
            _timePlaned = value;
        }
    }
    [NonSerialized] public DateTime TimeReal;   // do we need this?
    [NonSerialized] public TimeSpan EstimatedDuration;  // this estimated duration for loading / unloading process
    [NonSerialized] public IoPort Port;

    private void OnDestroy() {
        if (Port.isActiveAndEnabled) Port.CurrentField = null;
    }

    #region logic methods
    protected override void initField() {
        DimX = UnityEngine.Random.Range(1, Parameters.DimX - Parameters.MinDim);
        DimZ = UnityEngine.Random.Range(1, Parameters.DimZ - Parameters.MinDim);
        MaxLayer = UnityEngine.Random.Range(1, Parameters.MaxLayer - Parameters.MinDim);
        TimePlaned = DateTime.Now + GenerateRandomTimeSpan();
        base.initField();
    }
    protected void assignPort() {
        var ports = FindObjectsOfType<IoPort>();
        Port = ports[UnityEngine.Random.Range(0, ports.Length)];
        Port.AddToBuffer(this);
        transform.position = Port.transform.position;
    }

    protected virtual void updateState(bool state) {
        var collider = GetComponent<MeshCollider>();
        var renderer = GetComponent<MeshRenderer>();
        if (collider && renderer) {
            renderer.enabled = state;
            collider.enabled = state;
        }
    }

    /// <summary>
    /// this function is to destory the field and containers belongs to it
    /// </summary>
    public void DestroyField() {
        Invoke(nameof(disableField), Parameters.EventDelay);
        Destroy(gameObject, Parameters.EventDelay * 2);
    }
    #endregion

    #region minor methods
    private void disableField() { enabled = false; }
    public override string ToString() {
        var str = new StringBuilder();
        str.Append($"time planed: {TimePlaned:T}\n");
        str.Append($"Ground:\n{base.ToString()}");
        return str.ToString();
    }

    public static TimeSpan GenerateRandomTimeSpan() {
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
