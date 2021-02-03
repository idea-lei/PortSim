using Ilumisoft.VisualStateMachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Container : MonoBehaviour {
    #region public fields
    public Guid Id;
    public OutField OutField;
    public InField InField;
    #endregion

    #region private fields
    private StateMachine stateMachine;
    #endregion

    #region unity methods
    private void Awake() {
        stateMachine = FindObjectOfType<StateMachine>();
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

    public override string ToString() {
        return $"{name}\n" +
            $"OutField: " + (OutField == null ? "" : OutField.name) + "\n" +
            $"InField: " + (InField == null ? "" : InField.name);
    }
}
