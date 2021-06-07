using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class FindContainerInAgent : AgentBase {
    private class FindContainerInObservationObject {
        public Container container; //this one is not a observation variable

        public DateTime timeOut;
        public float energy;

        // n means normalized
        public float n_timeOut;
        public float n_energy;
    }

    private List<FindContainerInObservationObject> obList = new List<FindContainerInObservationObject>();

    public override void CollectObservations(VectorSensor sensor) {
        obList.Clear();
        var inFields = objs.IoPorts
            .Where(i => i.CurrentField
                && i.CurrentField is InField
                && i.CurrentField.GetComponentsInChildren<Container>().Length > 0)
            .Select(i => i.CurrentField);
        // select the infield with min amount of containers
        var inField = inFields.Aggregate((curMin, x) =>
                       x.GetComponentsInChildren<Container>().Count() < curMin.GetComponentsInChildren<Container>().Count() ? x : curMin);

        foreach (var s in inField.Ground) {
            if (s.Count == 0) continue;
            obList.Add(new FindContainerInObservationObject() {
                container = s.Peek(),
                energy = CalculateEnergy(s.Peek()),
                timeOut = s.Peek().OutField.TimePlaned
            });
        }

        // normalize
        float maxE = obList.Max(o => o.energy);
        float minE = obList.Min(o => o.energy);
        long maxT = obList.Max(o => o.timeOut.Ticks);
        long minT = obList.Max(o => o.timeOut.Ticks);
        foreach (var o in obList) {
            o.n_energy = Mathf.InverseLerp(minE, maxE, o.energy);
            o.n_timeOut = Mathf.InverseLerp(minT, maxT, o.timeOut.Ticks);
        }

        int dimHotEncoding = Parameters.DimX * Parameters.DimZ;
        foreach (var ob in obList) {
            float[] arr = new float[dimHotEncoding + 2];
            arr[obList.IndexOf(ob)] = 1;
            arr[dimHotEncoding] = ob.n_energy;
            arr[dimHotEncoding + 1] = ob.n_timeOut;
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

    private Container findContainerToMoveIn() {
        foreach (var t in objs.TempFields) {
            if (t.IsGroundEmpty) continue;
            foreach (var s in t.Ground) {
                if (s.Count > 0) return s.Peek();
            }
        }
        /*if (stackField.Count + 1 >= stackField.MaxCount) return null;*/ // avoid full stack, otherwise will be no arrange possible
        if (objs.StackField.Count >= objs.StackField.MaxCount) return null;
        foreach (var p in objs.IoPorts) {
            if (p.CurrentField && p.CurrentField is InField && p.CurrentField.isActiveAndEnabled) {
                if (objs.StackField.IsGroundFull) return null;
                foreach (var s in p.CurrentField.Ground) {
                    if (s.Count > 0) return s.Peek();
                }
            }
        }
        return null;
    }

    private float CalculateReward(FindContainerInObservationObject ob) {
        float reward = 0;

        // 1) energy reward
        reward += (1 - ob.n_energy) * c_energy;

        // 2) time reward (the latest out container should goes in first (stack))
        reward += ob.n_timeOut * c_time;

        return reward;
    }
}
