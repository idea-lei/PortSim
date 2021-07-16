﻿using System;
using System.Collections.Generic;
using System.Text;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Linq;

public class CRPAgent : Agent {
    private ObjectCollection objs;
    [SerializeField] private BufferSensorComponent bufferSensor;
    public ObservationFindOperation Ob;

    //private float distanceScaleZ;
    //private float distanceScaleX;

    private int rTimes = 0;
    private float normTime = 10;
    private DateTime start;
    public int initContainerAmout;

    public int bDegree;

    private void Awake() {
        objs = GetComponentInParent<ObjectCollection>();
    }

    public override void OnEpisodeBegin() {
        rTimes = 0;
        start = DateTime.Now;
        initContainerAmout = 0;
    }

    public override void CollectObservations(VectorSensor sensor) {
        if (initContainerAmout == 0) initContainerAmout = objs.StackField.Count;
        bDegree = 0;
        var outContainer = objs.OutContainers[0];

        for (int z = 0; z < Parameters.DimZ; z++) {
            float[,] list = new float[Parameters.MaxLayer, Parameters.MaxLayer + 2];
            var cArr = objs.StackField.Ground[0, z].ToList();
            cArr.Reverse();
            for (int c = 0; c < cArr.Count; c++) {

                // one hot
                list[c, c] = 1;

                // time out
                list[c, Parameters.MaxLayer] = (float)(cArr[c].OutField.TimePlaned - start).TotalSeconds / normTime;

                // tier
                list[c, Parameters.MaxLayer + 1] = c / (float)Parameters.MaxLayer;
            }

            var buffer = new List<float>();

            // max tier, to avoid stack on full index and find container
            buffer.Add(z / (float)Parameters.DimZ);

            // already need rearrange?
            buffer.Add(objs.StackField.IsStackNeedRearrange(new IndexInStack(0, z)) ? 1 : 0);

            // min out time, if empty, then 1
            buffer.Add(cArr.Count > 0 ? (float)(cArr.Min(c => c.OutField.TimePlaned) - start).TotalSeconds / normTime : 1);

            // contains container out
            buffer.Add(objs.StackField.Ground[0, z].Contains(outContainer) ? 1 : 0);

            // blocking degree
            int bd = blockingDegree(objs.StackField.Ground[0, z]);
            bDegree += bd;
            buffer.Add(bd / (float)Parameters.MaxLayer);

            var oh = new float[Parameters.DimZ];
            oh[z] = 1;
            buffer.AddRange(oh);

            foreach (var l in list) {
                buffer.Add(l);
            }

            if (!buffer.All(b => b <= 1 && b >= -1)) {
                StringBuilder sb = new StringBuilder();
                foreach (var b in buffer) {
                    sb.Append($"{b}, ");
                }
                SimDebug.LogError(this, sb.ToString());
            }
            Debug.Assert(buffer.Count == 59);
            bufferSensor.AppendObservation(buffer.ToArray());
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        var aOut = actionsOut.DiscreteActions;
        aOut[0] = UnityEngine.Random.Range(0, Parameters.DimZ);
        aOut[1] = UnityEngine.Random.Range(0, Parameters.DimZ);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        if (objs.Crane.OpObj == null) SimDebug.LogError(this, "opObj ist null");

        var containerIndex = new IndexInStack(0, actions.DiscreteActions[0]);
        var targetIndex = new IndexInStack(0, actions.DiscreteActions[1]);

        //container index is empty
        if (objs.StackField.Ground[containerIndex.x, containerIndex.z].Count == 0) {
            AddReward(-1);
            RequestDecision();
            return;
        }

        // target index full
        if (objs.StackField.IsIndexFull(targetIndex)) {
            AddReward(-1);
            RequestDecision();
            return;
        }

        // from here the decision can be used
        objs.Crane.OpObj.Container = objs.StackField.Ground[containerIndex.x, containerIndex.z].Peek();
        objs.Crane.OpObj.PickUpPos = objs.Crane.OpObj.Container.transform.position;
        objs.Crane.OpObj.StackPos = objs.Crane.OpObj.TargetField.IndexToGlobalPosition(targetIndex);

        // relocation times
        AddReward(normRelocationReward(rTimes++));


        AddReward((targetIndex.z - objs.Crane.OpObj.Container.IndexInCurrentField.z) * 0.01f);
        objs.StateMachine.TriggerByState("PickUp");
    }

    float normRelocationReward(int t) {
        return -0.1f * (Mathf.Exp(-t) - 1) / initContainerAmout;
    }

    // represented by time
    public int blockingDegree(Stack<Container> s) {
        int degree = 0;
        var list = s.ToList();
        list.Reverse();

        List<Container> hList = new List<Container>();

        while (list.Count > 1) {
            degree += hList.Count > 0 ? hList.Count - 1 : 0;

            int idx = MinByTime(list);
            hList = list.GetRange(idx, list.Count - idx);
            list = list.GetRange(0, idx);
        }
        return 0;
    }

    int MinByTime(List<Container> list) {
        Container min = list.First();
        int idx = 0;
        for (int i = 0; i < list.Count; i++) {
            if (min.OutField.TimePlaned > list[i].OutField.TimePlaned) {
                min = list[i];
                idx = i;
            }
        }
        return idx;
    }
}
