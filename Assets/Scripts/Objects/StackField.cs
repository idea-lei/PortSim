using System;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

public sealed class StackField : Field {
    private IndexInStack trainingResult = new IndexInStack();
    private Crane crane;

    private void Awake() {
        DimX = Parameters.DimX;
        DimZ = Parameters.DimZ;
        MaxLayer = Parameters.MaxLayer;

        crane = FindObjectOfType<Crane>();
    }
    private void Start() {
        initField();
        transform.position = new Vector3();
        initContainers();
        Debug.Assert(crane != null, "crane null!");
    }

    public override void AddToGround(Container container) {
        base.AddToGround(container);
        container.tag = "container_stacked";
    }

    /// <summary>
    /// to generate containers for field
    /// </summary>
    private void initContainers() {
        for (int x = 0; x < DimX; x++) {
            for (int z = 0; z < DimZ; z++) {
                for (int k = 0; k <= UnityEngine.Random.Range(0, MaxLayer); k++) {
                    var pos = IndexToLocalPositionInWorldScale(new IndexInStack(x, z));
                    var container = generateContainer(pos);
                    container.IndexInCurrentField = new IndexInStack(x, z);
                    AddToGround(container, new IndexInStack(x, z));
                    assignOutField(container, DateTime.Now);
                    container.tag = "container_stacked";
                }
            }
        }
    }

    private bool isIndexFull(IndexInStack idx) {
        return Ground[idx.x, idx.z].Count >= MaxLayer;
    }

    private bool isStackNeedRearrange(Stack<Container> stack) {
        if (stack.Count == 0) return false;
        var list = stack.ToArray();
        var min = list.First(x => x.OutField.TimePlaned == list.Min(y => y.OutField.TimePlaned));
        if (min != stack.Peek()) return true;
        return false;
    }

    public override IndexInStack FindIndexToStack() {
        RequestDecision();
        return trainingResult;
    }

    public override void CollectObservations(VectorSensor sensor) {
        Debug.Assert(crane.ContainerCarrying != null, "container carrying is null!");
        var times = new List<DateTime>() { crane.ContainerCarrying.OutField.TimePlaned };
        var distances = new List<float>();

        for (int x = 0; x < DimX; x++) {
            for (int z = 0; z < DimZ; z++) {
                if (Ground[x, z].Count > 0) {
                    times.Add(Ground[x, z].Peek().OutField.TimePlaned);
                    distances.Add(Vector3.SqrMagnitude(crane.ContainerCarrying.transform.position - Ground[x, z].Peek().transform.position));
                } else {
                    times.Add(DateTime.MinValue);
                    distances.Add(Vector3.SqrMagnitude(crane.ContainerCarrying.transform.position - IndexToGlobalPosition(x, z)));
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
        foreach (var s in Ground) {
            // norm layer of index
            sensor.AddObservation(s.Count / (float)MaxLayer);
            // whether the stack need to rearrange
            sensor.AddObservation(isStackNeedRearrange(s));
        }
    }

    public override void OnActionReceived(ActionBuffers actions) {
        trainingResult.IsValid = actions.DiscreteActions[0] > 0;
        trainingResult.x = actions.DiscreteActions[1];
        trainingResult.z = actions.DiscreteActions[2];
    }
}
