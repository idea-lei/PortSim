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
    public List<IndexInStack> StackedIndices = new List<IndexInStack>();

    private StackField stackField;
    private StateMachine stateMachine;
    private Crane crane;
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

        if (other.tag.Contains("container") || other.CompareTag("field_out") || other.CompareTag("field_stack") || other.CompareTag("field_temp"))
            if (stateMachine.CurrentState != "Wait") stateMachine.TriggerByState("Wait");

        //if (other.tag.Contains("container")) { // container touches container, which means add to ground, finished moving
        //    //var oC = other.GetComponent<Container>();
        //    //if (oC.CurrentField && oC.CurrentField.Ground[oC.IndexInCurrentField.x, oC.IndexInCurrentField.z].Count == oC.CurrentField.MaxLayer) {
        //    //    stateMachine.TriggerByState("Wait");
        //    //    Debug.LogError("put onto full stack!");
        //    //    return;
        //    //}

        //    //if (stateMachine.CurrentState != "PickUp") {
        //    //    if (crane.CanPickUp) stateMachine.TriggerByState("PickUp");
        //    //    else if (stateMachine.CurrentState != "Wait") stateMachine.TriggerByState("Wait");
        //    //}

        //    stateMachine.TriggerByState("Wait");
        //}

        //if (other.CompareTag("field_out") || other.CompareTag("field_stack") || other.CompareTag("field_temp")) { // container touches field, which means add to ground, finished moving
        //    //if (stateMachine.CurrentState != "PickUp") {
        //    //    if (crane.CanPickUp) stateMachine.TriggerByState("PickUp");
        //    //    else if (stateMachine.CurrentState != "Wait") stateMachine.TriggerByState("Wait");
        //    //}
        //    stateMachine.TriggerByState("Wait");
        //}

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
