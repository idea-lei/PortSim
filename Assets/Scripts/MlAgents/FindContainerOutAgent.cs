using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class FindContainerOutAgent : AgentBase {
    private class FindContainerOutObservationObject {
        public Container container; //this one is not a observation variable

        public bool isPeek;
        public float energy;

        // n means normalized
        public float n_isPeek;
        public float n_energy;
    }

    private List<FindContainerOutObservationObject> obList = new List<FindContainerOutObservationObject>();


    void Start() {

    }

    public override void CollectObservations(VectorSensor sensor) {
        obList.Clear();
        if (objs.Crane.ContainerToPick != null) {
            SimDebug.LogError(this, "ContainerToPick is not null!");
            EndEpisode();
            return;
        }

        var outFields = objs.IoPorts
            .Where(i => i.CurrentField 
                && i.CurrentField is OutField
                && ((OutField)i.CurrentField).IncomingContainers.Count > i.CurrentField.GetComponentsInChildren<Container>().Length)
            .Select(i => i.CurrentField);
        foreach (var s in objs.StackField.Ground) {
            foreach (var c in s.ToArray()) {
                if (outFields.Contains(c.OutField)) {
                    obList.Add(new FindContainerOutObservationObject() {
                        container = c,
                        isPeek = c == s.Peek(),
                        energy = CalculateEnergy(c)
                    });
                    // this break ensures the each stack has max. 1 Container out
                    // which means the obList won't larger than dimX * dimZ
                    break;
                }
            }
        }

        // normalize
        float maxE = obList.Max(o => o.energy);
        float minE = obList.Min(o => o.energy);
        foreach (var o in obList) {
            o.n_energy = Mathf.InverseLerp(minE, maxE, o.energy);
            o.n_isPeek = o.isPeek ? 1 : 0;
        }

        int dimHotEncoding = Parameters.DimX * Parameters.DimZ;
        foreach (var ob in obList) {
            float[] arr = new float[dimHotEncoding + 2];
            arr[obList.IndexOf(ob)] = 1;
            arr[dimHotEncoding] = ob.n_energy;
            arr[dimHotEncoding + 1] = ob.n_isPeek;
            bufferSensor.AppendObservation(arr);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        var rewardList = new List<float>();
        foreach (var ob in obList) {
            rewardList.Add(CalculateReward(ob));
        }
        var actOut = actionsOut.DiscreteActions;
        actOut[0] = rewardList.IndexOf(rewardList.Max());
    }

    public override void OnActionReceived(ActionBuffers actions) {
        if (obList.Count == 0) {
            // this means no available index, which should be determined before request decision, so error
            SimDebug.LogError(this, "no suitable container");
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
        AddReward(CalculateReward(result));

        objs.Crane.ContainerToPick = obList[actions.DiscreteActions[0]].container;
        objs.StateMachine.TriggerByState("PickUp");
    }

    // backup
    //public void FindContainerToPick() {
    //    var c = findContainerToMoveOut();
    //    if (c != null) {
    //        objs.Crane.ContainerToPick = c;
    //        objs.StateMachine.TriggerByState("PickUp");
    //        return;
    //    }
    //    c = findContainerToMoveIn();
    //    if (c != null) {
    //        objs.Crane.ContainerToPick = c;
    //        objs.StateMachine.TriggerByState("PickUp");
    //        return;
    //    }
    //    objs.StateMachine.TriggerByState("Wait");
    //    //c = findContainerToRearrange();
    //}

    /// <returns>
    /// container,
    /// state (the corresponding movement state of the container)
    /// </returns>
    private Container findContainerToMoveOut() {
        foreach (var outP in objs.IoPorts) {
            if (outP.CurrentField is OutField && outP.CurrentField.enabled) {
                foreach (var s in objs.StackField.Ground) {
                    //var peek = s.Peek();
                    foreach (var c in s.ToArray()) {
                        if (c.OutField == outP.CurrentField)
                            //return (peek, c == peek ? "MoveOut" : "Rearrange");
                            return c;
                    }
                }
            }
        }
        return null;
    }

    private Container findContainerToRearrange() {
        if (objs.StackField.IsGroundFull) return null;
        foreach (var s in objs.StackField.Ground) {
            if (s.Count == 0) continue;
            var list = s.ToArray();
            var min = list.First(x => x.OutField.TimePlaned == list.Min(y => y.OutField.TimePlaned));
            if (min != s.Peek()) return min;
        }
        return null;
    }



    private float CalculateReward(FindContainerOutObservationObject ob) {
        float reward = 0;

        // 1) energy reward
        reward += (1 - ob.n_energy) * c_energy;

        // 2) is Peek reward
        if (!ob.isPeek && obList.Any(o => o.isPeek)) reward -= c_isPeek;
        else reward += c_isPeek;

        return reward;
    }

}
