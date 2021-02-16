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
    public Field CurrentField {
        get {
            if (CompareTag("container_in")) return InField;
            if (CompareTag("container_out")) return OutField;
            return stackField;
        }
    }
    #endregion


    #region unity methods
    private void Awake() {
        stackField = FindObjectOfType<StackField>();
    }
    private void OnTriggerEnter(Collider other) {
        if (CompareTag("container_in")) {
            switch (other.tag) {
                case "container_out":
                case "container_stacked":
                    // stack onto container
                    break;
                case "field_out":
                case "field_stack":
                    // stack onto ground
                    break;
                default:
                    throw new Exception("illegal container touch");
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
                default:
                    throw new Exception("illegal container touch");
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
