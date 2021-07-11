using System.Collections.Generic;
using System.Text;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class ObservationFindOperation {
    public List<Container> Containers;
    public Field TargetField;
    public List<IndexInStack> AvailableIndices;
    public string State; 
}

public class FindNextOperation : Agent {
    private ObjectCollection objs;

    [SerializeField] private float c_t = 0.5f;
    [SerializeField] private float c_e = 0.5f;

    public ObservationFindOperation Ob = new ObservationFindOperation();

    private void Awake() {
        objs = GetComponentInParent<ObjectCollection>();
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


    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        var actOut = actionsOut.DiscreteActions;
        actOut[0] = Random.Range(0, Ob.Containers.Count);
        actOut[1] = Random.Range(0, Ob.AvailableIndices.Count);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        if(actions.DiscreteActions[0] >= Ob.Containers.Count) {
            AddReward(-1);
            RequestDecision();
            return;
        }
        if (actions.DiscreteActions[1] >= Ob.AvailableIndices.Count) {
            AddReward(-1);
            RequestDecision();
            return;
        }
        var index = Ob.AvailableIndices[actions.DiscreteActions[1]];
        var container = Ob.Containers[actions.DiscreteActions[0]];

        objs.Crane.OpObj = new OpObject();
        objs.Crane.OpObj.State = Ob.State;
        objs.Crane.OpObj.Container = container;
        objs.Crane.OpObj.PickUpPos = container.transform.position;
        objs.Crane.OpObj.StackPos = Ob.TargetField.IndexToGlobalPosition(index);

        objs.StateMachine.TriggerByState("PickUp");

        // check script "Field->AddToGround", for AddReward if nex operation is moving out
    }
}
