using Ilumisoft.VisualStateMachine;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Container : MonoBehaviour {
    #region fields
    public Guid Id;
    public OutField OutField;
    public InField InField;
    private Field _currentField;
    public Field CurrentField {
        get => _currentField;
        set {
            if (value && value != _currentField) {
                _currentField = value;
                StackedIndices.Clear();
            }
        }
    }
    public IndexInStack IndexInCurrentField;
    public HashSet<IndexInStack> StackedIndices = new HashSet<IndexInStack>();

    private StateMachine stateMachine;
    #endregion


    #region unity methods
    private void Awake() {
        stateMachine = FindObjectOfType<StateMachine>();
    }
    private void OnTriggerEnter(Collider other) {
        // these touches are because of initialization
        if (tag.Contains("_stack") && other.tag.Contains("_stack")) return;
        if (tag.Contains("_in") && other.tag.Contains("_in")) return;

        if (other.tag.Contains("container") || other.CompareTag("field_out") || other.CompareTag("field_stack") || other.CompareTag("field_temp"))
            if (stateMachine.CurrentState != "Wait") stateMachine.TriggerByState("Wait");

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
