using System;
using System.Collections.Generic;
using System.Text;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Linq;

public class ObservationFindOperation {
    public List<Container> Containers;
    public Field ContainerField;
    public Field TargetField;
    public List<IndexInStack> AvailableIndices;
    public string State;
}

public class FindNextOperation : Agent {

    private ObjectCollection objs;
    [SerializeField] private BufferSensorComponent containerBuffer;
    [SerializeField] private BufferSensorComponent indexBuffer;

    //[SerializeField] private float c_t = 0.5f;
    //[SerializeField] private float c_e = 0.5f;

    private float maxObLength;
    private float distanceScaleZ;
    private float distanceScaleX;

    public ObservationFindOperation Ob = new ObservationFindOperation();

    private void Awake() {
        objs = GetComponentInParent<ObjectCollection>();
        maxObLength = Parameters.DimX * Parameters.DimZ;
        distanceScaleZ = objs.StackField.transform.localScale.z * 10;
        distanceScaleX = objs.StackField.transform.localScale.x * 10;
    }

    /// <summary>
    /// 1. target field is outfield? (will affect the reward)
    /// 
    /// </summary>
    /// <param name="sensor"></param>
    public override void CollectObservations(VectorSensor sensor) {
        if (Ob == null) {
            SimDebug.LogError(this, "Observation is null");
            return;
        }

        // is out field? for out field, dont consider the time difference
        sensor.AddObservation(Ob.TargetField is OutField);

        // crane pos
        var cranePos = objs.Crane.transform.position - objs.transform.position;
        sensor.AddObservation(cranePos.x / distanceScaleX);
        sensor.AddObservation(cranePos.z / distanceScaleZ);

        // list length
        sensor.AddObservation(Ob.Containers.Count / maxObLength);
        sensor.AddObservation(Ob.AvailableIndices.Count / maxObLength);

        // container buffer
        foreach (var c in Ob.Containers) {
            var list = new float[Parameters.DimX * Parameters.DimZ + 4];
            var vec = c.transform.position - objs.transform.position;
            list[Ob.Containers.IndexOf(c)] = 1;
            list[list.Length - 1] = vec.z / distanceScaleZ; // plane init size is 10
            list[list.Length - 2] = vec.x / distanceScaleX;
            list[list.Length - 3] = (float)(c.OutField.TimePlaned - DateTime.Now).TotalSeconds / 600; // normalization
            if (list[list.Length - 3] > 1) Debug.LogWarning("time not fully normalized");
            list[list.Length - 4] = 1; // if this buffer is manually added, to train the out-of-range problem
        }

        // index buffer
        foreach (var i in Ob.AvailableIndices) {
            bool needRelocation = Ob.TargetField is StackField stackField && stackField.IsStackNeedRearrange(i);

            var list = new float[Parameters.DimX * Parameters.DimZ + 5];
            list[Ob.AvailableIndices.IndexOf(i)] = 1;
            list[list.Length - 1] = i.z / (float)Parameters.DimZ;
            list[list.Length - 2] = i.x / (float)Parameters.DimX;
            list[list.Length - 3] = needRelocation ? 1 : 0;

            // stack on stackfield and corresponding index is not empty
            if (Ob.TargetField is StackField field && field.Ground[i.x, i.z].Count > 0) {
                list[list.Length - 4] = (float)(field.Ground[i.x, i.z].Peek().OutField.TimePlaned - DateTime.Now).TotalSeconds / 600;
                if (list[list.Length - 4] > 1) Debug.LogWarning("time not fully normalized");
            } else {
                list[list.Length - 4] = 1;
            }

            list[list.Length - 5] = 1; // if this buffer is manually added, to train the out-of-range problem
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        var actOut = actionsOut.DiscreteActions;

        int cIndex = 0;
        int iIndex = 0;
        float reward = Mathf.NegativeInfinity;

        foreach (var c in Ob.Containers) {
            foreach (var i in Ob.AvailableIndices) {
                float r = CalculateReward(c, i);
                if (r > reward) {
                    reward = r;
                    cIndex = Ob.Containers.IndexOf(c);
                    iIndex = Ob.AvailableIndices.IndexOf(i);
                }
            }
        }

        actOut[0] = cIndex;
        actOut[1] = iIndex;
    }

    public override void OnActionReceived(ActionBuffers actions) {
        if (actions.DiscreteActions[0] >= Ob.Containers.Count || actions.DiscreteActions[1] >= Ob.AvailableIndices.Count) {
            AddReward(-1f);
            RequestDecision();
            return;
        }

        var index = Ob.AvailableIndices[actions.DiscreteActions[1]];
        var container = Ob.Containers[actions.DiscreteActions[0]];

        AddReward(CalculateReward(container, index));

        objs.Crane.OpObj = new OpObject();
        objs.Crane.OpObj.State = Ob.State;
        objs.Crane.OpObj.Container = container;
        objs.Crane.OpObj.PickUpPos = container.transform.position;
        objs.Crane.OpObj.StackPos = Ob.TargetField.IndexToGlobalPosition(index);
        objs.Crane.OpObj.TargetField = Ob.TargetField;
        objs.StateMachine.TriggerByState("PickUp");

        objs.FindNextOperationAgent.EndEpisode();
    }

    float CalculateReward(Container container, IndexInStack index) {
        float reward = 0;

        // relocation reward
        if (Ob.TargetField is StackField field) {
            if (field.IsStackNeedRearrange(index)) reward -= 0.5f;
            var peak = field.Ground[index.x, index.z].Count > 0 ? field.Ground[index.x, index.z].Peek() : null;
            if (peak != null && container.OutField.TimePlaned > peak.OutField.TimePlaned) {
                reward -= 0.5f;
            } else reward += 0.5f;
        }

        // time reward
        var crane = objs.Crane;
        var f = Ob.TargetField;
        var vec1 = crane.transform.position - container.transform.position;
        var vec2 = container.transform.position - f.IndexToGlobalPosition(index);

        float t = 0;
        if (Mathf.Abs(vec1.x) > Parameters.DistanceError) t += Mathf.Abs(vec1.x) / Parameters.Vx_Unloaded + Parameters.T_adjust / Time.timeScale;
        if (Mathf.Abs(vec1.z) > Parameters.DistanceError) t += Mathf.Abs(vec1.z) / Parameters.Vz_Unloaded + Parameters.T_adjust / Time.timeScale;
        if (Mathf.Abs(vec2.x) > Parameters.DistanceError) t += Mathf.Abs(vec2.x) / Parameters.Vx_Unloaded + Parameters.T_adjust / Time.timeScale;
        if (Mathf.Abs(vec2.z) > Parameters.DistanceError) t += Mathf.Abs(vec2.z) / Parameters.Vz_Unloaded + Parameters.T_adjust / Time.timeScale;

        Debug.Log(t);

        reward += 0.5f / (float)(t * Time.timeScale);

        return reward;
    }
}
