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
        public float reward;
    }

    [SerializeField] private float c_p = 0.5f;
    [SerializeField] private float c_e = 0.5f;

    private int train_times = 0;
    private float lastReward;

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

        var outFields = objs.OutFields;
        var incomingContainers = new List<Container>();
        foreach (var o in outFields) {
            incomingContainers.AddRange(o.IncomingContainers);
        }

        var cOutInTemp = objs.OutContainersInTempFields;
        if (cOutInTemp.Length > 0) {
            foreach (var c in cOutInTemp.Intersect(incomingContainers)) {
                obList.Add(new FindContainerOutObservationObject() {
                    container = c,
                    isPeek = c == objs.StackField.Ground[c.IndexInCurrentField.x, c.IndexInCurrentField.z].Peek(),
                    energy = CalculateEnergy(c)
                });
            }
        } else {
            foreach (var c in objs.OutContainersInStackField.Intersect(incomingContainers)) {
                obList.Add(new FindContainerOutObservationObject() {
                    container = c,
                    isPeek = c == objs.StackField.Ground[c.IndexInCurrentField.x, c.IndexInCurrentField.z].Peek(),
                    energy = CalculateEnergy(c)
                });
            }
        }

        if (obList.Count == 0) SimDebug.LogError(this, "obList is empty");

        // normalize
        float maxE = obList.Max(o => o.energy);
        float minE = obList.Min(o => o.energy);
        foreach (var o in obList) {
            o.n_energy = Mathf.InverseLerp(minE, maxE, o.energy);
            o.n_isPeek = o.isPeek ? 1 : 0;
            o.reward = CalculateReward(o);
        }

        foreach (var ob in obList) {
            bufferSensor.AppendObservation(new float[] { ob.n_energy, ob.n_isPeek, ob.reward });
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

        var p = actions.ContinuousActions[0] / 2f + 0.5f;
        var e = actions.ContinuousActions[1] / 2f + 0.5f;

        c_p = p / (p + e);
        c_e = e / (p + e);

        foreach (var o in obList) {
            o.reward = CalculateReward(o);
        }
        AddReward(obList.Select(o => o.reward).Max() - lastReward);

        if (train_times++ >= 10) {
            train_times = 0;
            EndEpisode();
            var rewardList = obList.Select(o => o.reward).ToList();
            objs.Crane.ContainerToPick = obList[rewardList.IndexOf(rewardList.Max())].container;
            objs.StateMachine.TriggerByState("PickUp");
        } else {
            RequestDecision();
        }
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
        reward += (1 - ob.n_energy) * c_e;

        // 2) is Peek reward
        if (!ob.isPeek && obList.Any(o => o.isPeek)) reward -= c_p;
        else reward += c_p;

        return reward;
    }

}
