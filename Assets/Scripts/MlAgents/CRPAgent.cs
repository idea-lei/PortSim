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

    public override void CollectObservations(VectorSensor sensor) {
        var indices = objs.StackField.AvailableIndices.Except(objs.OutContainersIndices);
        if (indices.Count() == 0) {
            indices = objs.StackField.AvailableIndices;
        }
        Ob = new ObservationFindOperation() {
            ContainerField = objs.StackField,
            Containers = objs.StackField.GetComponentsInChildren<Container>().ToList(),
            AvailableIndices = indices.ToList()
        };

        var time = DateTime.Now;

        int i = 0;
        foreach (var c in Ob.Containers) {
            bufferSensor.AppendObservation(addBuffer(
                time, 
                i++, 
                c.OutField.TimePlaned, 
                c.IndexInCurrentField.z, 
                c.transform.position.y));
        }

        // if there are empty stack
        for(int z =0;z<Parameters.DimZ;z++) {
            if(objs.StackField.Ground[0,z].Count == 0) {
                bufferSensor.AppendObservation(addBuffer(
                    time, 
                    i++, 
                    time + TimeSpan.FromSeconds(100), 
                    z, 
                    objs.StackField.IndexToGlobalPosition(0,z).y));
            }
        }
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

        // cause relocation
        //if (objs.StackField.IsStackNeedRearrange(index)) {
        //    AddReward(-0.5f);
        //}

        objs.Crane.OpObj.StackPos = objs.Crane.OpObj.TargetField.IndexToGlobalPosition(index);
        objs.StateMachine.TriggerByState("PickUp");
    }
}
