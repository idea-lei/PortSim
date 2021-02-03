﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }

    private void initFields() {
        // init inFields, add 3 fields for test
        for (int i = 0; i < 3; i++) {
            GenerateInField();
        }
    }

    public (GameObject, InField) GenerateInField() {
        var obj = Instantiate(inFieldPrefab);
        var inField = obj.GetComponent<InField>();
        inField.transform.SetParent(inField.Port.transform);
        inField.enabled = false;
        return (obj, inField);
    }

    public (GameObject, OutField) GenerateOutField() {
        var obj = Instantiate(outFieldPrefab);
        var outField = obj.GetComponent<OutField>();
        outField.transform.SetParent(outField.Port.transform);
        outField.enabled = false;
        return (obj, outField);
    }
}
