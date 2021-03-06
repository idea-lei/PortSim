﻿using UnityEngine;

/// <summary>
/// this class is to generate and store the input/output fields
/// it's not the ioField!
/// </summary>
public class IoFieldsGenerator : MonoBehaviour {

    [SerializeField]
    private GameObject inFieldPrefab;
    [SerializeField]
    private GameObject outFieldPrefab;

    private void Start() {
        initFields();
        InvokeRepeating(nameof(GenerateInField), Parameters.InFieldGenerationInterval, Parameters.InFieldGenerationInterval);
    }

    private void initFields() {
        for (int i = 0; i < Parameters.InFieldNumber; i++) {
            GenerateInField();
        }
    }

    public InField GenerateInField() {
        var obj = Instantiate(inFieldPrefab);
        var inField = obj.GetComponent<InField>();
        inField.transform.SetParent(inField.Port.transform);
        inField.enabled = false;
        return inField;
    }

    public OutField GenerateOutField() {
        var obj = Instantiate(outFieldPrefab);
        var outField = obj.GetComponent<OutField>();
        outField.transform.SetParent(outField.Port.transform);
        outField.enabled = false;
        return outField;
    }
}
