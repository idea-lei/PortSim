using Ilumisoft.VisualStateMachine;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Container : MonoBehaviour {
    #region fields
    public Guid Id;
    [SerializeField] private OutField _outField;
    public int Weight;
    public OutField OutField { 
        get => _outField;
        set {
            _outField = value;
            stateMachine = value.Port.GetComponentInParent<ObjectCollection>().StateMachine;
        }
    }
    public InField InField;
    [SerializeField] private Field _currentField;
    public Field CurrentField {
        get => _currentField;
        set => _currentField = value;
    }
    public IndexInStack IndexInCurrentField;

    private StateMachine stateMachine;

    public DateTime? StartMoveTime; // to make sure no error when directly spawn containers in stackfield, use nullable
    private TimeSpan _totalMoveTime = new TimeSpan();
    public TimeSpan TotalMoveTime {
        get => _totalMoveTime;
        set {
            _totalMoveTime = value;
            totalTimeDisplay = _totalMoveTime.ToString("g");
        }
    }
    [SerializeField] private string totalTimeDisplay;

    public int RearrangeCount;

    public bool IsPeak => CurrentField.Ground[IndexInCurrentField.x, IndexInCurrentField.z].Peek() == this;
    #endregion

    private void Awake() {
        RearrangeCount = -1; // cuz each container needs at least one moving-in, so the base value is set to -1
    }

    #region unity methods
    private void OnTriggerEnter(Collider other) {
        if (!stateMachine) return;
        // these touches are because of initialization
        if (tag.Contains("_stack") && other.tag.Contains("_stack")) return;
        if (tag.Contains("_in") && other.tag.Contains("_in")) return;

        if (other.tag.Contains("container") || other.CompareTag("field_out") || other.CompareTag("field_stack"))
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
