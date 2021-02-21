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
        if (CompareTag("container_in")) {
            switch (other.tag) {
                case "container_out":
                    // todo: check if pickup or wait
                    stateMachine.TriggerByState("PickUp");
                    break;
                case "container_stacked":
                    stateMachine.TriggerByState("PickUp");
                    // stack onto container
                    break;
                case "field_out":
                case "field_stack":
                    // stack onto ground
                    break;
            }
        }

        if (CompareTag("container_out")) {
            switch (other.tag) {
                case "container_out":
                    // stack onto container
                    break;
                case "field_out":
                    // stack onto ground
                    break;
            }
        }
        if (CompareTag("container_rearrange")) {
            if (other.CompareTag("container_stacked")) {
                stackField.AddToGround(this);
                return;
            }
            if (other.CompareTag("field_stack")) {
                return;
            }
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
