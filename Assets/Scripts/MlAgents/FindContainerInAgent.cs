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
        public float reward;
    }

    [SerializeField] private float c_t = 0.5f;
    [SerializeField] private float c_e = 0.5f;

    private int train_times = 0;
    private float lastReward;

    private List<FindContainerInObservationObject> obList = new List<FindContainerInObservationObject>();

    public override void CollectObservations(VectorSensor sensor) {
        obList.Clear();
        var inFields = objs.Infields;
        Field inField = null;
        if (objs.ContainersInTempFields.Length > 0) {
            foreach (var f in objs.TempFields) {
                if (f.GetComponentInChildren<Container>() != null) {
                    inField = f;
                    break;
                }
            }
        }
        if (inField == null) {
            // select the infield with min amount of containers
            inField = inFields.Aggregate((curMin, x) =>
                           x.GetComponentsInChildren<Container>().Count() < curMin.GetComponentsInChildren<Container>().Count() ? x : curMin);

        }
        foreach (var s in inField.Ground) {
            if (s.Count == 0) continue;
            obList.Add(new FindContainerInObservationObject() {
                container = s.Peek(),
                energy = CalculateEnergy(s.Peek()),
                timeOut = s.Peek().OutField.TimePlaned
            });
        }

        if (obList.Count == 0) SimDebug.LogError(this, "obList is empty");

        // normalize
        float maxE = obList.Max(o => o.energy);
        float minE = obList.Min(o => o.energy);
        long maxT = obList.Max(o => o.timeOut.Ticks);
        long minT = obList.Max(o => o.timeOut.Ticks);
        foreach (var o in obList) {
            o.n_energy = Mathf.InverseLerp(minE, maxE, o.energy);
            o.n_timeOut = Mathf.InverseLerp(minT, maxT, o.timeOut.Ticks);
            o.reward = CalculateReward(o);
        }

        foreach (var ob in obList) {
            bufferSensor.AppendObservation(new float[] { ob.n_energy, ob.n_timeOut, ob.reward });
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        //var rewardList = new List<float>();
        //foreach (var ob in obList) {
        //    rewardList.Add(CalculateReward(ob));
        //}
        //var actOut = actionsOut.DiscreteActions;
        //actOut[0] = rewardList.IndexOf(rewardList.Max());


        var actOut = actionsOut.ContinuousActions;
        actOut[0] = 0f;
        actOut[1] = 0f;
    }

    public override void OnActionReceived(ActionBuffers actions) {
        lastReward = obList.Select(o => o.reward).Max();

        var t = actions.ContinuousActions[0] / 2f + 0.5f;
        var e = actions.ContinuousActions[1] / 2f + 0.5f;

        c_t = t / (t + e);
        c_e = e / (t + e);

        foreach (var o in obList) {
            o.reward = CalculateReward(o);
        }
        AddReward(obList.Select(o => o.reward).Max() - lastReward);

        if (train_times++ >= 100) {
            train_times = 0;
            EndEpisode();
            var rewardList = obList.Select(o => o.reward).ToList();
            objs.Crane.ContainerToPick = obList[rewardList.IndexOf(rewardList.Max())].container;
            objs.StateMachine.TriggerByState("PickUp");
        } else {
            RequestDecision();
        }
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
        reward += (1 - ob.n_energy) * c_e;

        // 2) time reward (the latest out container should goes in first (stack))
        reward += ob.n_timeOut * c_t;

        return reward;
    }
}
