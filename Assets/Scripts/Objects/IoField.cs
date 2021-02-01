using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// base class of the inField and outField
/// </summary>
public abstract class IoField : Field {
    public DateTime TimePlaned;
    public DateTime TimeReal;
    public TimeSpan EstimatedDuration;  // this estimated duration for loading / unloading process
    public IoPort Port;

    #region unity methods
    private void Awake() {
        DimX = UnityEngine.Random.Range(1, Parameters.DimX - Parameters.MinDim);
        DimZ = UnityEngine.Random.Range(1, Parameters.DimZ - Parameters.MinDim);
        MaxLayer = UnityEngine.Random.Range(1, Parameters.MaxLayer - Parameters.MinDim);
        initField();
        TimePlaned = DateTime.Now + new TimeSpan(
            UnityEngine.Random.Range(0, 0),
            UnityEngine.Random.Range(0, 2),
            UnityEngine.Random.Range(0, 3),
            UnityEngine.Random.Range(0, 30));

        assignPort();
    }
    #endregion

    #region logic methods

    private void assignPort() {
        var ports = FindObjectsOfType<IoPort>();
        Port = ports[UnityEngine.Random.Range(0, ports.Length)];
    }
    #endregion

    #region minor methods
    public override string ToString() {
        var str = new StringBuilder();
        str.Append($"time planed: {TimePlaned.ToString("T")}\n");
        str.Append($"Ground:\n{base.ToString()}");
        return str.ToString();
    }
    #endregion
}
