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
    private void Start() {
        stackField = GetComponent<StackField>();
        stateMachine = crane.GetComponent<StateMachine>();
        Initialize();
    }

    /// <remark>
    /// 1. OutTimes: a ContainerCarrying.OutTime, b StackField.PeekOutTime(time.now + 1 day if peek is null) --dimX*dimZ+1
    /// 2. Distance between containerCarrying and all the peeks --dimX*dimZ
    /// 3. layers of all the the stacks --dimX*dimZ
    /// 4. stacks need rearrange --dimX*dimZ
    /// 5. ContainerCarrying.currentIndex (only for rearrange, to avoid stack onto same index) --2
    /// 6. IsRearrange Process (represented by (-1,-1) of 5.) --0
    /// 
    /// total: dimX * dimZ * 4 + 3
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
        var needRearrangeList = new List<bool>();

        // 1. 
        times.Add(crane.ContainerCarrying.OutField.TimePlaned);

        for (int x = 0; x < stackField.DimX; x++) {
            for (int z = 0; z < stackField.DimZ; z++) {
                layers.Add(stackField.Ground[x, z].Count / (float)stackField.MaxLayer); // 3
                needRearrangeList.Add(stackField.IsStackNeedRearrange(stackField.Ground[x, z]));
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

        float minDistance = distances.Min();
        float maxDistance = distances.Max();

        foreach (var t in times) sensor.AddObservation(Mathf.Lerp(0, diffTime, (float)(t - minTime).TotalSeconds)); // 1
        foreach (var d in distances) sensor.AddObservation(Mathf.Lerp(minDistance, maxDistance, d)); // 2
        foreach (var l in layers) sensor.AddObservation(l); // 3
        foreach (var n in needRearrangeList) sensor.AddObservation(n); //4
        IndexInStack index = crane.ContainerCarrying.IndexInCurrentField;
        if (crane.ContainerCarrying.CurrentField is StackField) { //rearrange
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
        stackField.TrainingResult = idx;
        if (!crane.ContainerCarrying) {
            SimDebug.LogError(this, "ContainerCarrying null");
            EndEpisode();
            return;
        }
        handleResult();
        stateMachine.TriggerByState(crane.ContainerCarrying.CompareTag("container_in") || crane.ContainerCarrying.CompareTag("container_temp") ? "MoveIn" : "Rearrange");
    }

    /// <param name="idx">the training result</param>
    private void handleResult() {
        var resOldMethod = stackField.FindIndexToStack();

        // 1. if the training result isValid is false
        if (!stackField.TrainingResult.IsValid) {
            if (resOldMethod.IsValid == stackField.TrainingResult.IsValid) {
                AddReward(1f);
            } else {
                AddReward(-0.1f);
                stackField.TrainingResult = resOldMethod;
            }
            return;
        }

        // from here, this isValid is true
        //time difference reward

        if (stackField.Ground[stackField.TrainingResult.x, stackField.TrainingResult.z].Count > 0) {
            float d = (float)(stackField.Ground[stackField.TrainingResult.x, stackField.TrainingResult.z].Peek().OutField.TimePlaned
        - crane.ContainerCarrying.OutField.TimePlaned).TotalMinutes;
            d = 1 - 2 * (Mathf.Exp(-d) + 1); // scaled sigmoid funciton
            AddReward(d);
        } else AddReward(1);


        // this situation could not happen because of the algorithms control
        if (stackField.IsIndexFull(stackField.TrainingResult)) {
            AddReward(-0.1f);
            stackField.TrainingResult = resOldMethod;
            return;
        }

        if (stackField.IsStackNeedRearrange(stackField.Ground[stackField.TrainingResult.x, stackField.TrainingResult.z])) {
            AddReward(-0.01f);
        }

        // the result is already stacked
        if (crane.ContainerCarrying.StackedIndices.Contains(stackField.TrainingResult)) {
            AddReward(-0.1f);
            stackField.TrainingResult = resOldMethod;
            return;
        }

        AddReward(1 - stackField.Ground[stackField.TrainingResult.x, stackField.TrainingResult.z].Count / (float)stackField.MaxLayer);

        if (StepCount > 1000) {
            Debug.LogWarning("reset episode");
            EndEpisode();
        }
    }
}
