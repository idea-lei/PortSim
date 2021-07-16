using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class ArrayAgent : Agent {

    private int[,] bay;
    private int initTier = 3;
    public override void Initialize() {
        
    }

    public override void OnEpisodeBegin() {
        bay = new int[6, 7];
    }

    public override void CollectObservations(VectorSensor sensor) {
        base.CollectObservations(sensor);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        base.OnActionReceived(actions);
    }

    #region tool methods

    #endregion
}
