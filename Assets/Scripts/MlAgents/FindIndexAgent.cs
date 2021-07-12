using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// this class is for moveIn and rearrange
/// </summary>
public class FindIndexAgent : AgentBase {
    // this private class is for buffersensor
    // make sure the positive value represents that this field is good
    //eg: n_isNotFull true -> 1
    //private class FindIndexObservationObject {
    //    public IndexInStack index; //this one is not a observation variable


    //    public TimeSpan timeDiff; // diff is always peek - carrying
    //    public bool isTimeOk => timeDiff >= new TimeSpan(0);

    //    public int weightDiff;
    //    public bool isWeightOk => weightDiff >= 0;

    //    public float energy;

    //    public int layer;
    //    public bool isLayerOk => layer < Parameters.MaxLayer;
    //    public bool noRearrange;
    //    public bool notStacked;


    //    // n means normalized
    //    public float n_timeDiff;
    //    public float n_isTimeOk => isTimeOk ? 1f : 0f;
    //    public float n_weightDiff;
    //    public float n_isWeightOk => isWeightOk ? 1f : 0f;
    //    public float n_energy;
    //    public float n_layer => layer / (float)Parameters.MaxLayer;
    //    public float n_isLayerOk => isLayerOk ? 1f : 0f;
    //    public float n_noRearrange => noRearrange ? 1f : 0f;
    //    public float n_notStacked => notStacked ? 1f : 0f;
    //    public float reward;
    //}

    //[SerializeField] private float c_e = 0.2f;
    //[SerializeField] private float c_t = 0.2f;
    //[SerializeField] private float c_l = 0.2f;
    //[SerializeField] private float c_w = 0.2f;
    //[SerializeField] private float c_r = 0.2f;
    ////private float lastReward;
    ////private int train_times;

    //private List<FindIndexObservationObject> obList = new List<FindIndexObservationObject>();

    //public override void Initialize() {
    //}

    ///// <summary>
    ///// for buffer sensor: (each available index)
    ///// 0. whether this index is selected -> dimX * dimZ
    ///// 1. peek container outTime comparsion -> 1
    ///// 2. peek container weight comparsion -> 1
    ///// 3. Energy Cost (kind of distance) (containerCarrying to index) -> 1
    ///// 4. index need rearrange -> 1
    ///// 5. current layer of the index -> 1
    ///// </summary>
    //public override void CollectObservations(VectorSensor sensor) {
    //    obList.Clear();
    //    if (objs.Crane.ContainerCarrying == null) {
    //        SimDebug.LogError(this, "container carrying is null!");
    //        EndEpisode();
    //        return;
    //    }

    //    // add observation to list
    //    for (int x = 0; x < objs.StackField.DimX; x++) {
    //        for (int z = 0; z < objs.StackField.DimZ; z++) {
    //            //// index is available if it is not full and not stacked
    //            if (objs.StackField.IsIndexFull(x, z)) continue;
    //            if (objs.Crane.ContainerCarrying.StackedIndices.Any(i => i == new IndexInStack(x, z))) continue;

    //            var ob = new FindIndexObservationObject {
    //                index = new IndexInStack(x, z),
    //                notStacked = objs.Crane.ContainerCarrying.StackedIndices.All(i => i.x != x || i.z != z),
    //                layer = objs.StackField.Ground[x, z].Count,
    //                noRearrange = !objs.StackField.IsStackNeedRearrange(objs.StackField.Ground[x, z])
    //            };

    //            Vector3 vec;
    //            if (objs.StackField.Ground[x, z].Count > 0) {
    //                ob.weightDiff = objs.StackField.Ground[x, z].Peek().Weight - objs.Crane.ContainerCarrying.Weight;
    //                ob.timeDiff = objs.StackField.Ground[x, z].Peek().OutField.TimePlaned - objs.Crane.ContainerCarrying.OutField.TimePlaned;
    //                vec = objs.Crane.ContainerCarrying.transform.position - objs.StackField.Ground[x, z].Peek().transform.position;
    //            } else {
    //                ob.weightDiff = Parameters.MaxContainerWeight - objs.Crane.ContainerCarrying.Weight;
    //                ob.timeDiff = DateTime.Now + TimeSpan.FromMinutes(20) - objs.Crane.ContainerCarrying.OutField.TimePlaned;
    //                vec = objs.Crane.ContainerCarrying.transform.position - objs.StackField.IndexToGlobalPosition(x, z);
    //            }
    //            ob.energy = Mathf.Abs(vec.x) * Parameters.Ex + Mathf.Abs(vec.z) * Parameters.Ez;
    //            ob.reward = CalculateReward(ob);
    //            obList.Add(ob);
    //        }
    //    }

    //    if (obList.Count == 0) {
    //        SimDebug.LogError(this, "obList is empty");
    //    }

    //    //normalize
    //    var times = obList.Select(o => o.timeDiff).ToList();
    //    var maxTime = times.Max();
    //    var minTime = times.Min();

    //    var distances = obList.Select(o => o.energy).ToList();
    //    float minSqrDistance = distances.Min();
    //    float maxSqrDistance = distances.Max();

    //    foreach (var o in obList) {
    //        o.n_timeDiff = Mathf.InverseLerp((float)minTime.TotalSeconds, (float)maxTime.TotalSeconds, (float)o.timeDiff.TotalSeconds);
    //        o.n_weightDiff = (float)o.weightDiff / Parameters.MaxContainerWeight;
    //        o.n_energy = Mathf.InverseLerp(minSqrDistance, maxSqrDistance, o.energy);

    //        if (o.n_timeDiff < 0 || o.n_timeDiff > 1) SimDebug.LogError(this, "outTime Lerp out of range");
    //        if (o.n_energy < 0 || o.n_energy > 1) SimDebug.LogError(this, "n_energy Lerp out of range");

    //        var buffer = new float[Parameters.DimX * Parameters.DimZ + 9];
    //        buffer[obList.IndexOf(o)] = 1;
    //        buffer[buffer.Length - 1] = o.n_timeDiff;
    //        buffer[buffer.Length - 2] = o.n_isTimeOk;
    //        buffer[buffer.Length - 3] = o.n_weightDiff;
    //        buffer[buffer.Length - 4] = o.n_isWeightOk;
    //        buffer[buffer.Length - 5] = o.n_energy;
    //        buffer[buffer.Length - 6] = o.n_layer;
    //        buffer[buffer.Length - 7] = o.n_isLayerOk;
    //        buffer[buffer.Length - 8] = o.n_notStacked;
    //        buffer[buffer.Length - 9] = o.n_noRearrange;
    //        bufferSensor.AppendObservation(buffer);//ob.reward
    //    }
    //}

    //public override void Heuristic(in ActionBuffers actionsOut) {
    //    //var continuousActionsOut = actionsOut.DiscreteActions;
    //    //var rewardList = new List<float>();

    //    //foreach (var ob in obList) rewardList.Add(CalculateReward(ob, true));

    //    //float max = rewardList.Max();
    //    //continuousActionsOut[0] = rewardList.IndexOf(max);

    //    var continuousActionsOut = actionsOut.ContinuousActions;
    //    continuousActionsOut[0] = 0.2f;
    //    continuousActionsOut[1] = 0.2f;
    //    continuousActionsOut[2] = 0.2f;
    //    continuousActionsOut[3] = 0.2f;
    //    continuousActionsOut[4] = 0.2f;
    //}

    //public override void OnActionReceived(ActionBuffers actions) {
    //    //lastReward = obList.Select(o => o.reward).Max();

    //    var t = actions.ContinuousActions[0] / 2f + 0.5f;
    //    var e = actions.ContinuousActions[1] / 2f + 0.5f;
    //    var l = actions.ContinuousActions[2] / 2f + 0.5f;
    //    var w = actions.ContinuousActions[3] / 2f + 0.5f;
    //    var r = actions.ContinuousActions[4] / 2f + 0.5f;

    //    if (t * e * l * w * r == 0 &&
    //        t + e + l + w + r == 0) {
    //        AddReward(-1);
    //        RequestDecision();
    //        return;
    //    }

    //    c_t = t / (t + e + l + w + r);
    //    c_e = e / (t + e + l + w + r);
    //    c_l = l / (t + e + l + w + r);
    //    c_w = w / (t + e + l + w + r);
    //    c_r = r / (t + e + l + w + r);

    //    foreach (var o in obList) {
    //        o.reward = CalculateReward(o);
    //    }

    //    AddReward(obList.Select(o => o.reward).Max());

    //    EndEpisode();
    //    var rewardList = obList.Select(o => o.reward).ToList();
    //    objs.StackField.TrainingResult = obList[rewardList.IndexOf(rewardList.Max())].index;
    //    objs.StateMachine.TriggerByState(
    //    objs.Crane.ContainerCarrying.CompareTag("container_in")
    //    || objs.Crane.ContainerCarrying.CompareTag("container_temp")
    //    ? "MoveIn" : "Rearrange");

    //    //if (train_times++ >= 10) {
    //    //    train_times = 0;
    //    //    EndEpisode();
    //    //    var rewardList = obList.Select(o => o.reward).ToList();
    //    //    objs.StackField.TrainingResult = obList[rewardList.IndexOf(rewardList.Max())].index;
    //    //    objs.StateMachine.TriggerByState(
    //    //    objs.Crane.ContainerCarrying.CompareTag("container_in")
    //    //    || objs.Crane.ContainerCarrying.CompareTag("container_temp")
    //    //    ? "MoveIn" : "Rearrange");
    //    //} else {
    //    //    RequestDecision();
    //    //}
    //}

    //private float CalculateReward(FindIndexObservationObject ob, bool isHeuristic = false) {
    //    float reward = 0;
    //    // 0.1) is Full?
    //    if (!ob.isLayerOk) {
    //        if (!isHeuristic) Debug.LogWarning("full");
    //        return -c_full;
    //    }

    //    // 0.2) is stacked?
    //    if (!ob.notStacked) {
    //        if (!isHeuristic) Debug.LogWarning("stacked");
    //        return -c_stacked;
    //    }

    //    // 1) energy reward
    //    reward += (1 - ob.n_energy) * c_e;

    //    // 2) noRearrange reward
    //    if (!ob.noRearrange) {
    //        // if not all indices need rearrange
    //        if (obList.Any(o => o.noRearrange)) reward -= c_r;
    //    } else reward += c_r;

    //    // 3) time reward
    //    if (!ob.isTimeOk) {
    //        if (obList.Any(o => o.isTimeOk)) reward -= c_t;
    //    } else reward += c_t;

    //    // 4) layer reward
    //    reward += (1 - ob.n_layer) * c_l;

    //    // 5) weight reward
    //    if (!ob.isWeightOk) {
    //        if (obList.Any(o => o.isWeightOk)) reward -= c_w;
    //    } else reward += (1 - ob.n_weightDiff) * c_w;

    //    return reward;
    //}

}
