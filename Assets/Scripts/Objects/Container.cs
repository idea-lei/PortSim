using Ilumisoft.VisualStateMachine;
using System;
using UnityEngine;

public class Container : MonoBehaviour {
    #region fields
    public Guid Id;
    public OutField OutField;
    public InField InField;
    private StackField stackField;
    private StateMachine stateMachine;
    private Crane crane;
    public Field CurrentField;
    public IndexInStack indexInCurrentField;
    #endregion


    #region unity methods
    private void Awake() {
        stateMachine = FindObjectOfType<StateMachine>();
        stackField = FindObjectOfType<StackField>();
        crane = FindObjectOfType<Crane>();
    }
    private void OnTriggerEnter(Collider other) {
        // these touches are because of initialization
        if (tag.Contains("_stack") && other.tag.Contains("_stack")) return;
        if (tag.Contains("_in") && other.tag.Contains("_in")) return;

        if (other.tag.Contains("container")) { // container touches container, which means add to ground, finished moving
            var oC = other.GetComponent<Container>();
            if (oC.CurrentField && oC.CurrentField.Ground[oC.indexInCurrentField.x, oC.indexInCurrentField.z].Count == oC.CurrentField.MaxLayer) {
                stateMachine.TriggerByState("Wait");
                Debug.LogError("put onto full stack!");
                return;
            }

            if (stateMachine.CurrentState != "PickUp") {
                if (crane.CanPickUp) stateMachine.TriggerByState("PickUp");
                else if (stateMachine.CurrentState != "Wait") stateMachine.TriggerByState("Wait");
            }
        }

        if (other.CompareTag("field_out") || other.CompareTag("field_stack")) { // container touches field, which means add to ground, finished moving
            if (stateMachine.CurrentState != "PickUp") {
                if (crane.CanPickUp) stateMachine.TriggerByState("PickUp");
                else if (stateMachine.CurrentState != "Wait") stateMachine.TriggerByState("Wait");
            }
        }

    }
    #endregion

    public void RemoveFromGround() {
        CurrentField.RemoveFromGround(this);
        CurrentField = null;
    }

    public override string ToString() {
        return $"{name}\n" +
            $"OutField: " + (OutField == null ? "" : OutField.name) + "\n" +
            $"InField: " + (InField == null ? "" : InField.name);
    }
}
