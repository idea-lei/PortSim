﻿using Ilumisoft.VisualStateMachine;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Container : MonoBehaviour {
    #region fields
    public Guid Id;
    private OutField _outField;
    public int Weight;
    public OutField OutField { 
        get => _outField;
        set {
            _outField = value;
            stateMachine = value.Port.GetComponentInParent<ObjectCollection>().StateMachine;
        }
    }
    public InField InField;
    private Field _currentField;
    public Field CurrentField {
        get => _currentField;
        set {
            _currentField = value;
            if (value && !(value is StackField)) {
                StackedIndices.Clear();
            }
        }
    }
    public IndexInStack IndexInCurrentField;
    public HashSet<IndexInStack> StackedIndices = new HashSet<IndexInStack>();

    private StateMachine stateMachine;
    #endregion


    #region unity methods
    private void OnTriggerEnter(Collider other) {
        if (!stateMachine) return;
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
            $"Weight: {Weight}\n"+
            $"OutField: " + (OutField == null ? "" : OutField.name) + "\n" +
            $"InField: " + (InField == null ? "" : InField.name);
    }
}
