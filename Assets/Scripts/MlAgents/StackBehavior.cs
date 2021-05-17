using Ilumisoft.VisualStateMachine;
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

    [SerializeField] private Crane crane;
    private StackField stackField;
    private StateMachine stateMachine;

    private float minSqrDistance;
    private float maxSqrDistance;
    private void Start() {
        stackField = GetComponent<StackField>();
        stateMachine = crane.GetComponent<StateMachine>();
        Initialize();
    }

    /// <remark>
    /// 1. OutTimes: a ContainerCarrying.OutTime, b StackField.PeekOutTime(time.now + 1 day if peek is null) --dimX*dimZ+1
    /// 2. Distance between containerCarrying and all the peeks --dimX*dimZ
    /// 3. layers of all the the stacks --dimX*dimZ  (currently comment out!)
    /// 4. isStackFull --dimX*dimZ (redundent with 3)
    /// 5. stacks need rearrange --dimX*dimZ
    /// 6. ContainerCarrying.currentIndex (only for rearrange, to avoid stack onto same index) --2
    /// 7. IsRearrange Process (represented by (-1,-1) of 6.) --0
    /// 8. stackedIndex --dimX*dimZ
    /// total: dimX * dimZ * 5 + 3
    /// </remark>
    public override void CollectObservations(VectorSensor sensor) {
        if (crane.ContainerCarrying == null) {
            SimDebug.LogError(this, "container carrying is null!");
            EndEpisode();
            return;
        }

        var times = new List<DateTime>();
        var distances = new List<float>();
        var layers = new List<float>();
        var indexFullList = new List<bool>();
        var needRearrangeList = new List<bool>();
        var isStackedList = new List<bool>();

        // 1. 
        times.Add(crane.ContainerCarrying.OutField.TimePlaned);

        for (int x = 0; x < stackField.DimX; x++) {
            for (int z = 0; z < stackField.DimZ; z++) {
                layers.Add(stackField.Ground[x, z].Count / (float)stackField.MaxLayer); // 3
                needRearrangeList.Add(stackField.IsStackNeedRearrange(stackField.Ground[x, z]));
                indexFullList.Add(stackField.Ground[x, z].Count == stackField.MaxLayer);
                isStackedList.Add(crane.ContainerCarrying.StackedIndices.Contains(new IndexInStack(x, z)));

                if (stackField.Ground[x, z].Count > 0) {
                    times.Add(stackField.Ground[x, z].Peek().OutField.TimePlaned);
                    distances.Add(Vector3.SqrMagnitude(crane.ContainerCarrying.transform.position - stackField.Ground[x, z].Peek().transform.position));
                } else {
                    times.Add(DateTime.Now + TimeSpan.FromDays(1));
                    distances.Add(Vector3.SqrMagnitude(crane.ContainerCarrying.transform.position - stackField.IndexToGlobalPosition(x, z)));
                }
            }
        }

        var maxTime = times.Max();
        var minTime = times.Min();
        float diffTime = (float)(maxTime - minTime).TotalSeconds;

        minSqrDistance = distances.Min();
        maxSqrDistance = distances.Max();

        foreach (var t in times) sensor.AddObservation(Mathf.InverseLerp(0, diffTime, (float)(t - minTime).TotalSeconds)); // 1
        foreach (var d in distances) sensor.AddObservation(Mathf.InverseLerp(minSqrDistance, maxSqrDistance, d)); // 2
        //foreach (var l in layers) sensor.AddObservation(l); // 3
        foreach (var i in indexFullList) sensor.AddObservation(i); //4
        foreach (var n in needRearrangeList) sensor.AddObservation(n); //5
        foreach (var i in isStackedList) sensor.AddObservation(i); // 8
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
        float sqrDistanceInvLerp = Mathf.InverseLerp(minSqrDistance, maxSqrDistance, Vector3.SqrMagnitude(crane.ContainerCarrying.transform.position - stackField.IndexToGlobalPosition(idx)));
        if (sqrDistanceInvLerp > 1) Debug.LogError($"invLerp > 1, {sqrDistanceInvLerp}");
        reward += 1 - sqrDistanceInvLerp;

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
