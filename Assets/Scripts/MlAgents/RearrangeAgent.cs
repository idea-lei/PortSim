using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class RearrangeAgent : AgentBase {
    private List<RearrangeObservationObject> obList = new List<RearrangeObservationObject>();

    /// <summary>
    /// for buffer sensor: (each available index)
    /// 0. whether this index is selected -> dimX * dimZ
    /// 1. peek container outTime comparsion -> 1
    /// 2. peek container weight comparsion -> 1
    /// 3. Energy Cost (kind of distance) (containerCarrying to index) -> 1
    /// 4. index need rearrange -> 1
    /// 5. current layer of the index -> 1
    /// </summary>
    public override void CollectObservations(VectorSensor sensor) {
        obList.Clear();
        if (objs.Crane.ContainerCarrying == null) {
            SimDebug.LogError(this, "container carrying is null!");
            EndEpisode();
            return;
        }

        // add observation to list
        for (int x = 0; x < objs.StackField.DimX; x++) {
            for (int z = 0; z < objs.StackField.DimZ; z++) {
                //// index is available if it is not full and not stacked
                if (objs.StackField.IsIndexFull(x, z)) continue;
                if (objs.Crane.ContainerCarrying.StackedIndices.Any(i => i == new IndexInStack(x, z))) continue;

                var ob = new RearrangeObservationObject {
                    index = new IndexInStack(x, z),
                    notStacked = objs.Crane.ContainerCarrying.StackedIndices.All(i => i.x != x || i.z != z),
                    layer = objs.StackField.Ground[x, z].Count,
                    noRearrange = !objs.StackField.IsStackNeedRearrange(objs.StackField.Ground[x, z])
                };

                Vector3 vec;
                if (objs.StackField.Ground[x, z].Count > 0) {
                    ob.weightDiff = objs.StackField.Ground[x, z].Peek().Weight - objs.Crane.ContainerCarrying.Weight;
                    ob.timeDiff = objs.StackField.Ground[x, z].Peek().OutField.TimePlaned - objs.Crane.ContainerCarrying.OutField.TimePlaned;
                    vec = objs.Crane.ContainerCarrying.transform.position - objs.StackField.Ground[x, z].Peek().transform.position;
                } else {
                    ob.weightDiff = Parameters.MaxContainerWeight - objs.Crane.ContainerCarrying.Weight;
                    ob.timeDiff = DateTime.Now + TimeSpan.FromMinutes(20) - objs.Crane.ContainerCarrying.OutField.TimePlaned;
                    vec = objs.Crane.ContainerCarrying.transform.position - objs.StackField.IndexToGlobalPosition(x, z);
                }
                ob.energy = Mathf.Abs(vec.x) * Parameters.Ex + Mathf.Abs(vec.z) * Parameters.Ez;
                obList.Add(ob);
            }
        }

        if (obList.Count == 0) {
            SimDebug.LogError(this, "obList is empty");
        }

        //normalize
        var times = obList.Select(o => o.timeDiff).ToList();
        var maxTime = times.Max();
        var minTime = times.Min();

        var distances = obList.Select(o => o.energy).ToList();
        float minSqrDistance = distances.Min();
        float maxSqrDistance = distances.Max();

        foreach (var o in obList) {
            o.n_timeDiff = Mathf.InverseLerp((float)minTime.TotalSeconds, (float)maxTime.TotalSeconds, (float)o.timeDiff.TotalSeconds);
            o.n_weightDiff = (float)o.weightDiff / Parameters.MaxContainerWeight;
            o.n_energy = Mathf.InverseLerp(minSqrDistance, maxSqrDistance, o.energy);

            if (o.n_timeDiff < 0 || o.n_timeDiff > 1) SimDebug.LogError(this, "outTime Lerp out of range");
            if (o.n_energy < 0 || o.n_energy > 1) SimDebug.LogError(this, "n_energy Lerp out of range");

            float[] hotEncoding = new float[25];
            hotEncoding[obList.IndexOf(o)] = 1;

            float[] buffer = new float[] {
                o.n_timeDiff,
                o.n_isTimeOk,
                o.n_weightDiff,
                o.n_isWeightOk,
                o.n_energy,
                o.n_layer,
                o.n_isLayerOk,
                o.n_notStacked,
                o.n_noRearrange
            };

            bufferSensor.AppendObservation(hotEncoding.Concat(buffer).ToArray());
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        var continuousActionsOut = actionsOut.DiscreteActions;
        var rewardList = new List<float>();

        foreach (var ob in obList) rewardList.Add(RearrangeReward(ob, true));

        float max = rewardList.Max();
        continuousActionsOut[0] = rewardList.IndexOf(max);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        if (obList.Count == 0) {
            // this means no available index, which should be determined before request decision, so error
            SimDebug.LogError(this, "no stackable index");
            return;
        }

        if (actions.DiscreteActions[0] >= obList.Count) {
            AddReward(-c_outOfRange);
            Debug.LogWarning("out of range, request new decision");
            RequestDecision();
            //SimDebug.LogError(this, "result is null");
            return;
        }

        var result = obList[actions.DiscreteActions[0]];
        float reward = RearrangeReward(result);

        AddReward(reward);
        if (reward <= -1) {
            RequestDecision();
            return;
        }

        objs.StackField.TrainingResult = result.index;
        objs.StateMachine.TriggerByState("Rearrange");
    }


    /// <summary>
    /// this function calculates the reward of stack
    /// it works only on objs.StackField (no training for outField)</summary>
    /// <param name="ob"></param>
    /// <param name="isHeuristic"></param>
    /// <returns></returns>
    private float RearrangeReward(RearrangeObservationObject ob, bool isHeuristic = false) {

        // 0.1) is Full?
        if (!ob.isLayerOk) {
            if (!isHeuristic) Debug.LogWarning("full");
            return -c_full;
        }

        // 0.2) is stacked?
        if (!ob.notStacked) {
            if (!isHeuristic) Debug.LogWarning("stacked");
            return -c_stacked;
        }

        float reward = 0;

        // 1) energy reward
        reward += (1 - ob.n_energy) * c_energy;

        // 2) noRearrange reward
        if (!ob.noRearrange) {
            // if not all indices need rearrange
            if (obList.Any(o => o.noRearrange)) reward -= c_rearrange;
        } else reward += c_rearrange;

        // 3) time reward
        if (!ob.isTimeOk) {
            if (obList.Any(o => o.isTimeOk)) reward -= c_time;
        } else reward += c_time;

        // 4) layer reward
        reward += (1 - ob.n_layer) * c_layer;

        // 5) weight reward
        if (!ob.isWeightOk) {
            if (obList.Any(o => o.isWeightOk)) reward -= c_weight;
        } else reward += (1 - ob.n_weightDiff) * c_weight;

        return reward;
    }
}

/// <summary>
/// this class is unit of buffersensor
/// it contains all the variables that needs to be observed to calculate the stack reward
/// make sure the positive value represents that this field is good
/// eg: n_isNotFull true -> 1, false -> 0
/// </summary>
class RearrangeObservationObject {
    public IndexInStack index; //this one is not a observation variable

    public TimeSpan timeDiff; // diff is always peek - carrying
    public bool isTimeOk => timeDiff >= new TimeSpan(0);

    public int weightDiff;
    public bool isWeightOk => weightDiff >= 0;

    public float energy;

    public int layer;
    public bool isLayerOk => layer < Parameters.MaxLayer;
    public bool noRearrange;
    public bool notStacked;


    // n means normalized
    public float n_timeDiff;
    public float n_isTimeOk => isTimeOk ? 1f : 0f;

    public float n_weightDiff;
    public float n_isWeightOk => isWeightOk ? 1f : 0f;

    public float n_energy;
    public float n_layer => layer / (float)Parameters.MaxLayer;
    public float n_isLayerOk => isLayerOk ? 1f : 0f;
    public float n_noRearrange => noRearrange ? 1f : 0f;
    public float n_notStacked => notStacked ? 1f : 0f;
}
