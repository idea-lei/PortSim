using Ilumisoft.VisualStateMachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Container : MonoBehaviour {
    #region fields
    public Guid Id;
    public OutField OutField;
    public InField InField;
    private StackField stackField;
    private StateMachine stateMachine;
    public Field CurrentField {
        get {
            if (CompareTag("container_in")) return InField;
            if (CompareTag("container_out")) return OutField;
            return stackField;
        }
    }
    public IndexInStack indexInCurrentField;
    #endregion


    #region unity methods
    private void Awake() {
        stateMachine = FindObjectOfType<StateMachine>();
        stackField = FindObjectOfType<StackField>();
    }
    private void OnTriggerEnter(Collider other) {
        if (other.tag.Contains("container")) { // container touches container, which means add to ground, finished moving
            if (stateMachine.CurrentState != "PickUp") stateMachine.TriggerByState("PickUp");
        }

        if (other.CompareTag("field_out") || other.CompareTag("field_stack")) { // container touches container, which means add to ground, finished moving
            if (stateMachine.CurrentState != "PickUp") stateMachine.TriggerByState("PickUp");
        }

    }
    #endregion

    public void RemoveFromGround() {
        CurrentField.RemoveFromGround(this);
    }

    public override string ToString() {
        return $"{name}\n" +
            $"OutField: " + (OutField == null ? "" : OutField.name) + "\n" +
            $"InField: " + (InField == null ? "" : InField.name);
    }
}
