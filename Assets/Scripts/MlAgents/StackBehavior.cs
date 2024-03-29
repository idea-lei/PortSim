﻿using Ilumisoft.VisualStateMachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class StackBehavior : Agent {

    // this private class is for buffersensor
    private class ObservationObject {
        public IndexInStack index; //this one is not a observation variable
        public DateTime outTime;
        public int weight;
        public float distance;
        public int layer;
        public bool isIndexNeedRearrange;

        // n means normalized
        public float n_outTime;
        public float n_weight;
        public float n_distance;
        public float n_layer;
    }

    private Crane crane;
    private StackField stackField;
    private StateMachine stateMachine;
    private BufferSensorComponent bufferSensor;

    private void Start() {
        crane = GetComponentInParent<ObjectCollection>().Crane;
        stackField = GetComponent<StackField>();
        stateMachine = crane.GetComponent<StateMachine>();
        bufferSensor = GetComponent<BufferSensorComponent>();
        Initialize();
    }

    /// <summary>
    /// for vector sensor:
    /// 1. ContainerCarrying: outTime and weight -> 2
    /// 
    /// for buffer sensor: (each available index)
    /// 1. peek container outTime -> 1
    /// 2. peek container weight -> 1
    /// 3. distance (containerCarrying to index) -> 1
    /// 4. index need rearrange -> 1
    /// 5. current layer of the index -> 1
    /// </summary>
    public override void CollectObservations(VectorSensor sensor) {
        if (crane.ContainerCarrying == null) {
            SimDebug.LogError(this, "container carrying is null!");
            EndEpisode();
            return;
        }

        var obList = new List<ObservationObject>();

        for (int x = 0; x < stackField.DimX; x++) {
            for (int z = 0; z < stackField.DimZ; z++) {
                // index is available if it is not full and not stacked
                if (stackField.IsIndexFull(x, z) && crane.ContainerCarrying.StackedIndices.All(i => i.x != x || i.z != z)) {
                    var ob = new ObservationObject {
                        index = new IndexInStack(x, z)
                    };

                    if (stackField.Ground[x, z].Count > 0) {
                        ob.layer = stackField.Ground[x, z].Count;
                        ob.isIndexNeedRearrange = stackField.IsStackNeedRearrange(stackField.Ground[x, z]);
                        ob.weight = stackField.Ground[x, z].Peek().Weight;
                        ob.outTime = stackField.Ground[x, z].Peek().OutField.TimePlaned;
                        ob.distance = Vector3.SqrMagnitude(crane.ContainerCarrying.transform.position - stackField.Ground[x, z].Peek().transform.position);
                    } else {
                        ob.layer = 0;
                        ob.isIndexNeedRearrange = false;
                        ob.weight = Parameters.MaxContainerWeight;
                        ob.outTime = DateTime.Now + TimeSpan.FromDays(1);
                        ob.distance = Vector3.SqrMagnitude(crane.ContainerCarrying.transform.position - stackField.IndexToGlobalPosition(x, z));
                    }
                    obList.Add(ob);
                }
            }
        }

        var times = obList.Select(o => o.outTime).ToList();
        times.Add(crane.ContainerCarrying.OutField.TimePlaned);
        var maxTime = times.Max();
        var minTime = times.Min();
        float diffTime = (float)(maxTime - minTime).TotalSeconds;

        var distances = obList.Select(o => o.distance).ToList();
        float minSqrDistance = distances.Min();
        float maxSqrDistance = distances.Max();
        float diffDistance = maxSqrDistance - minSqrDistance;

        foreach(var o in obList) {
            o.n_outTime = Mathf.InverseLerp(0, diffTime, (float)(o.outTime - minTime).TotalSeconds);
            o.n_weight = o.weight / Parameters.MaxContainerWeight;
            o.n_distance = Mathf.InverseLerp(minSqrDistance, maxSqrDistance, o.distance);
            o.n_layer = o.layer / Parameters.MaxLayer;
        }

        
        IndexInStack index = crane.ContainerCarrying.IndexInCurrentField;
        if (!(crane.ContainerCarrying.CurrentField is StackField)) { //rearrange
            index = new IndexInStack(-1, -1);
        }
        sensor.AddObservation(index.x);
        sensor.AddObservation(index.z);
    }



    public override void Heuristic(in ActionBuffers actionsOut) {
        var result = stackField.FindIndexToStack();
        var continuousActionsOut = actionsOut.DiscreteActions;
        continuousActionsOut[0] = result.IsValid ? 1 : 0;
        continuousActionsOut[1] = result.x;
        continuousActionsOut[2] = result.z;
    }

    public override void OnActionReceived(ActionBuffers actions) {
        IndexInStack idx = new IndexInStack();
        idx.IsValid = actions.DiscreteActions[0] > 0;
        idx.x = actions.DiscreteActions[1];
        idx.z = actions.DiscreteActions[2];
        if (handleResult(idx)) {
            stackField.TrainingResult = idx;
            stateMachine.TriggerByState(crane.ContainerCarrying.CompareTag("container_in") || crane.ContainerCarrying.CompareTag("container_temp") ? "MoveIn" : "Rearrange");
        } else RequestDecision();
    }

    /// <param name="idx">the training result</param>
    private bool handleResult(IndexInStack idx) {
        var resOldMethod = stackField.FindIndexToStack();

        // 1. if the training result isValid is false
        if (!idx.IsValid) {
            bool same = resOldMethod.IsValid == idx.IsValid;
            AddReward(same ? 1 : -1);
            return same;
        }

        float reward = 0;

        // from here, this isValid is true

        //time difference reward
        if (stackField.Ground[idx.x, idx.z].Count > 0) {
            float d = (float)(stackField.Ground[idx.x, idx.z].Peek().OutField.TimePlaned
        - crane.ContainerCarrying.OutField.TimePlaned).TotalMinutes;
            reward += 1 / (Mathf.Exp(-d) + 1) - 1; // scaled sigmoid funciton (-0.5,0.5)
        } else reward += 1;

        //distance reward (0,1)
        

        // layer reward
        reward += 1 - stackField.Ground[idx.x, idx.z].Count / (float)stackField.MaxLayer;

        if (stackField.IsStackNeedRearrange(stackField.Ground[idx.x, idx.z])) {
            reward -= 0.1f;
        }

        // if the target is already full
        if (stackField.IsIndexFull(idx)) {
            Debug.LogWarning("already full!");
            AddReward(-2);
            return false; // need to redecide
        }

        // the result is already stacked
        if (crane.ContainerCarrying.StackedIndices.Contains(idx)) {
            Debug.LogWarning("already stacked!");
            reward -= 1f;
        }
        AddReward(reward);

        return true;
    }
}


// obsolete collectObservation method
///// <remark>
///// 1. OutTimes: a ContainerCarrying.OutTime, b StackField.PeekOutTime(time.now + 1 day if peek is null) --dimX*dimZ+1
///// 2. Distance between containerCarrying and all the peeks --dimX*dimZ
///// 3. layers of all the the stacks --dimX*dimZ  (currently comment out!)
///// 4. isStackFull --dimX*dimZ (redundent with 3)
///// 5. stacks need rearrange --dimX*dimZ
///// 6. ContainerCarrying.currentIndex (only for rearrange, to avoid stack onto same index) --2
///// 7. IsRearrange Process (represented by (-1,-1) of 6.) --0
///// 8. stackedIndex --dimX*dimZ
///// total: dimX * dimZ * 5 + 3
///// </remark>
//public override void CollectObservations(VectorSensor sensor) {
//    if (crane.ContainerCarrying == null) {
//        SimDebug.LogError(this, "container carrying is null!");
//        EndEpisode();
//        return;
//    }

//    var times = new List<DateTime>();
//    var distances = new List<float>();
//    var layers = new List<float>();
//    var indexFullList = new List<bool>();
//    var needRearrangeList = new List<bool>();
//    var isStackedList = new List<bool>();

//    // 1. 
//    times.Add(crane.ContainerCarrying.OutField.TimePlaned);

//    for (int x = 0; x < stackField.DimX; x++) {
//        for (int z = 0; z < stackField.DimZ; z++) {
//            layers.Add(stackField.Ground[x, z].Count / (float)stackField.MaxLayer); // 3
//            needRearrangeList.Add(stackField.IsStackNeedRearrange(stackField.Ground[x, z]));
//            indexFullList.Add(stackField.Ground[x, z].Count == stackField.MaxLayer);
//            isStackedList.Add(crane.ContainerCarrying.StackedIndices.Contains(new IndexInStack(x, z)));

//            if (stackField.Ground[x, z].Count > 0) {
//                times.Add(stackField.Ground[x, z].Peek().OutField.TimePlaned);
//                distances.Add(Vector3.SqrMagnitude(crane.ContainerCarrying.transform.position - stackField.Ground[x, z].Peek().transform.position));
//            } else {
//                times.Add(DateTime.Now + TimeSpan.FromDays(1));
//                distances.Add(Vector3.SqrMagnitude(crane.ContainerCarrying.transform.position - stackField.IndexToGlobalPosition(x, z)));
//            }
//        }
//    }

//    var maxTime = times.Max();
//    var minTime = times.Min();
//    float diffTime = (float)(maxTime - minTime).TotalSeconds;

//    minSqrDistance = distances.Min();
//    maxSqrDistance = distances.Max();

//    foreach (var t in times) sensor.AddObservation(Mathf.InverseLerp(0, diffTime, (float)(t - minTime).TotalSeconds)); // 1
//    foreach (var d in distances) sensor.AddObservation(Mathf.InverseLerp(minSqrDistance, maxSqrDistance, d)); // 2
//                                                                                                              //foreach (var l in layers) sensor.AddObservation(l); // 3
//    foreach (var i in indexFullList) sensor.AddObservation(i); //4
//    foreach (var n in needRearrangeList) sensor.AddObservation(n); //5
//    foreach (var i in isStackedList) sensor.AddObservation(i); // 8
//    IndexInStack index = crane.ContainerCarrying.IndexInCurrentField;
//    if (!(crane.ContainerCarrying.CurrentField is StackField)) { //rearrange
//        index = new IndexInStack(-1, -1);
//    }
//    sensor.AddObservation(index.x);
//    sensor.AddObservation(index.z);
//}
