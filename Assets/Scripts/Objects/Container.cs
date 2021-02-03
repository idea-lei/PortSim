using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Container : MonoBehaviour
{
    #region public fields
    public Guid Id;
    public OutField OutField;
    public InField InField;
    #endregion

    #region private fields
    private StackField stackField;
    private DecisionMaker decisionMaker;
    #endregion

    #region unity methods
    private void Awake() {
        stackField = FindObjectOfType<StackField>();
        decisionMaker = FindObjectOfType<DecisionMaker>();
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("container")) {

        }
        if (other.CompareTag("field")) {

        }
    }
    #endregion

    public override string ToString() {
        return $"{name}\n" +
            $"OutField: " + (OutField == null ? "" :OutField.name) + "\n" +
            $"InField: " + (InField == null ? "" : InField.name);
    }
}
