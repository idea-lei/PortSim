using System;
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

    private void Awake() {
        objs = GetComponentInParent<ObjectCollection>();
        //distanceScaleZ = objs.StackField.transform.localScale.z * 10;
        //distanceScaleX = objs.StackField.transform.localScale.x * 10;
    }

    /// <param name="start"> start time stampel</param>
    /// <param name="i">hotencoding index</param>
    /// <param name="t">time out</param>
    /// <param name="z"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private float[] addBuffer(DateTime start, int i, DateTime t, int z, float y) {
        int length = Parameters.DimX * Parameters.DimZ * Parameters.MaxLayer + 3;//
        var buffer = new float[length];
        buffer[i] = 1;
        buffer[length - 1] = (float)(t - start).TotalSeconds / 100;
        buffer[length - 2] = z / (float)Parameters.DimZ;
        buffer[length - 3] = y / objs.Crane.TranslationHeight;
        return buffer;
    }

    public override void OnEpisodeBegin() {
        rTimes = 0;
    }

    public override void CollectObservations(VectorSensor sensor) {
        var start = DateTime.Now;
        Debug.Assert(objs.Crane.OpObj.Container != null);
        sensor.AddObservation((float)(objs.Crane.OpObj.Container.OutField.TimePlaned - start).TotalSeconds / 100);
        sensor.AddObservation(objs.Crane.OpObj.Container.IndexInCurrentField.z / (float)Parameters.DimZ);


        //objs.Crane.OpObj.Container

        for (int z = 0; z < Parameters.DimZ; z++) {
            float[,] list = new float[Parameters.MaxLayer, Parameters.MaxLayer + 2];
            var cArr = objs.StackField.Ground[0, z].ToArray();
            for (int c = 0; c < cArr.Length; c++) {
                if (cArr[c] == objs.Crane.OpObj.Container) break;
                list[c, c] = 1;
                list[c, Parameters.MaxLayer] = (float)(cArr[c].OutField.TimePlaned - start).TotalSeconds / 100;
                list[c, Parameters.MaxLayer + 1] = c / (float)Parameters.MaxLayer;
            }


            var buffer = new List<float>();
            buffer.Add(z / (float)Parameters.DimZ);
            buffer.Add(cArr.Length == 0 ? 1 : 0); // is index empty
            buffer.Add(objs.StackField.IsStackNeedRearrange(new IndexInStack(0, z)) ? 0 : 1);
            buffer.Add(cArr.Length > 0 ? (float)(cArr.Min(c => c.OutField.TimePlaned) - start).TotalSeconds / 100 : 1); // min out time, if empty, then 1
            buffer.Add(objs.StackField.Ground[0, z].Contains(objs.Crane.OpObj.Container) ? 0 : 1);

            var oh = new float[Parameters.DimZ];
            oh[z] = 1;
            buffer.AddRange(oh);

            foreach (var l in list) {
                buffer.Add(l);
            }

            Debug.Assert(buffer.Count == 60);
            bufferSensor.AppendObservation(buffer.ToArray());
        }








        //var indices = objs.StackField.AvailableIndices.Except(objs.OutContainersIndices);
        //if (indices.Count() == 0) {
        //    indices = objs.StackField.AvailableIndices;
        //}
        //Ob = new ObservationFindOperation() {
        //    ContainerField = objs.StackField,
        //    Containers = objs.StackField.GetComponentsInChildren<Container>().ToList(),
        //    AvailableIndices = indices.ToList()
        //};



        //int i = 0;
        //foreach (var c in Ob.Containers) {
        //    bufferSensor.AppendObservation(addBuffer(
        //        start,
        //        i++,
        //        c.OutField.TimePlaned,
        //        c.IndexInCurrentField.z,
        //        c.transform.position.y));
        //}

        //// if there are empty stack
        //for (int z = 0; z < Parameters.DimZ; z++) {
        //    if (objs.StackField.Ground[0, z].Count == 0) {
        //        bufferSensor.AppendObservation(addBuffer(
        //            start,
        //            i++,
        //            start + TimeSpan.FromSeconds(100),
        //            z,
        //            objs.StackField.IndexToGlobalPosition(0, z).y));
        //    }
        //}
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        var aOut = actionsOut.DiscreteActions;
        aOut[0] = UnityEngine.Random.Range(0, Parameters.DimZ);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        if (objs.Crane.OpObj == null) SimDebug.LogError(this, "opObj ist null");

        var index = new IndexInStack(0, actions.DiscreteActions[0]);

        // stack full
        if (objs.StackField.IsIndexFull(index)) {
            AddReward(-1);
            RequestDecision();
            return;
        }

        // same stack
        if (index == objs.Crane.OpObj.Container.IndexInCurrentField) {
            AddReward(-1);
            RequestDecision();
            return;
        }

        rTimes++;

        if (objs.StackField.IsStackNeedRearrange(index)) { // already need relocation
            AddReward(normRelocationReward(rTimes));
        } else {
            if (objs.StackField.Ground[index.x, index.z].Count > 0) {
                // cause new relocation
                if (objs.Crane.OpObj.Container.OutField.TimePlaned > objs.StackField.Ground[index.x, index.z].Min(c => c.OutField.TimePlaned)) {
                    AddReward(normRelocationReward(rTimes));
                } else {
                    AddReward(0.5f);
                }
            } else { // index empty
                var start = DateTime.Now;
                var cs = objs.StackField.GetComponentsInChildren<Container>();
                float maxOutTime = (float)(cs.Max(c => c.OutField.TimePlaned) - start).TotalSeconds;
                float minOutTime = (float)(cs.Min(c => c.OutField.TimePlaned) - start).TotalSeconds;
                float cTime = (float)(objs.Crane.OpObj.Container.OutField.TimePlaned - start).TotalSeconds;

                AddReward(Mathf.InverseLerp(minOutTime, maxOutTime, cTime) - 0.5f);
            }
        }

        AddReward((index.z - objs.Crane.OpObj.Container.IndexInCurrentField.z) * 0.1f);

        objs.Crane.OpObj.StackPos = objs.Crane.OpObj.TargetField.IndexToGlobalPosition(index);
        objs.StateMachine.TriggerByState("PickUp");
    }

    float normRelocationReward(int t) {
        return Mathf.Exp(-t) - 1;
    }
}
