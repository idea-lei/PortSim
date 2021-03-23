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
    }

    public override void CollectObservations(VectorSensor sensor) {
        Debug.Assert(crane.ContainerCarrying != null, "container carrying is null!");
        var times = new List<DateTime>() { crane.ContainerCarrying.OutField.TimePlaned };
        var distances = new List<float>();

        for (int x = 0; x < stackField.DimX; x++) {
            for (int z = 0; z < stackField.DimZ; z++) {
                if (stackField.Ground[x, z].Count > 0) {
                    times.Add(stackField.Ground[x, z].Peek().OutField.TimePlaned);
                    distances.Add(Vector3.SqrMagnitude(crane.ContainerCarrying.transform.position - stackField.Ground[x, z].Peek().transform.position));
                } else {
                    times.Add(DateTime.MinValue);
                    distances.Add(Vector3.SqrMagnitude(crane.ContainerCarrying.transform.position - stackField.IndexToGlobalPosition(x, z)));
                }
            }
        }

        var maxTime = times.Max();
        for (int i = 0; i < times.Count; i++) if (times[i] == DateTime.MinValue) times[i] = maxTime + new TimeSpan(1, 0, 0); // empty stack get 1 hour bonus
        maxTime = times.Max();
        var minTime = times.Min();
        float diffTime = (float)(maxTime - minTime).TotalSeconds;

        float minDistance = distances.Min();
        float maxDistance = distances.Max();

        // norm timeplaned
        foreach (var t in times) sensor.AddObservation(Mathf.Lerp(0, diffTime, (float)(t - minTime).TotalSeconds));
        // norm distance
        foreach (var d in distances) sensor.AddObservation(Mathf.Lerp(minDistance, maxDistance, d));
        foreach (var s in stackField.Ground) {
            // norm layer of index
            sensor.AddObservation(s.Count / (float)stackField.MaxLayer);
            // whether the stack need to rearrange
            sensor.AddObservation(stackField.IsStackNeedRearrange(s));
        }
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

        Debug.Assert(crane.ContainerCarrying, "ContainerCarrying null");
        handleResult();
        stateMachine.TriggerByState(crane.ContainerCarrying.CompareTag("container_in") ? "MoveIn" : "Rearrange");
    }

    /// <summary>
    /// Reward system:
    /// 1. if the result is unavailable or already known as rearrange-needed, reward -1
    /// 2. if the result is available, reward + corresponding time diff (can also be negative)
    /// 3. 
    /// </summary>
    private void handleResult() {
        // 1. if ground not full but the result is not valid
        if (stackField.TrainingResult.IsValid == false) {
            if (stackField.IsGroundFull) {
                AddReward(1f);
                return;
            }
            if (crane.ContainerCarrying.CompareTag("container_in")) {
                AddReward(-1f);
                stackField.TrainingResult = stackField.FindIndexToStack();
                return;
            } else { //rearrange
                for (int x = 0; x < stackField.DimX; x++) {
                    for (int z = 0; z < stackField.DimZ; z++) {
                        if (stackField.Ground[x, z].Count == 0 ||
                            crane.ContainerCarrying.OutField.TimePlaned <
                            stackField.Ground[x, z].Peek().OutField.TimePlaned) {
                            AddReward(-1f);
                            stackField.TrainingResult = new IndexInStack(x, z);
                            return;
                        }
                    }
                }
                // this means the container carrying got max outTime, index.IsValid should be false
                AddReward(1f);
                return;
            }
        }

        // 2.
        if (stackField.IsIndexFull(stackField.TrainingResult)) {
            AddReward(-1f);
            stackField.TrainingResult = stackField.FindIndexToStack();
            return;
        }

        // 3.
        if (stackField.IsStackNeedRearrange(stackField.Ground[stackField.TrainingResult.x, stackField.TrainingResult.z])) {
            AddReward(-0.1f);
            return;
        }

        // 4. till here, the result is available.
        if (stackField.Ground[stackField.TrainingResult.x, stackField.TrainingResult.z].Count == 0) {
            AddReward(0.5f);
            return;
        }

        // 5. the result is already stacked
        if (crane.ContainerCarrying.StackedIndices.Contains(stackField.TrainingResult)) {
            AddReward(-0.5f);
            return;
        }

        AddReward((float)(
                stackField.Ground[stackField.TrainingResult.x, stackField.TrainingResult.z].Peek().OutField.TimePlaned
                - crane.ContainerCarrying.OutField.TimePlaned).TotalSeconds
                / 300f); // this value should not smaller than the total sec in IoField GenerateRandomTimeSpan()
    }
}
