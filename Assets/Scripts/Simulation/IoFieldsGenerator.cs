using System;
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
    [SerializeField]
    private GameObject inFieldsGroup;
    [SerializeField]
    private GameObject outFieldsGroup;

    private IoPort[] ioPorts;

    private void Awake() {
        ioPorts = FindObjectsOfType<IoPort>();
    }

    private void Start() {
        initFields();
    }

    private void initFields() {
        // init inFields, add 3 fields for test
        for (int i = 0; i < 3; i++) {
            var (obj, field) = GenerateInField();
            field.transform.SetParent(inFieldsGroup.transform);
            field.enabled = false;
        }
        // init outFields, add 20 fields for test
        for (int i = 0; i < 5; i++) {
            var (obj, field) = GenerateOutField();
            field.transform.SetParent(outFieldsGroup.transform);
            field.enabled = false;
        }
    }

    public (GameObject, InField) GenerateInField() {
        var obj = Instantiate(inFieldPrefab);
        var inField = obj.GetComponent<InField>();
        return (obj, inField);
    }

    public (GameObject, OutField) GenerateOutField() {
        var obj = Instantiate(outFieldPrefab);
        var outField = obj.GetComponent<OutField>();
        return (obj, outField);
    }

    private void setFieldActive(IoField field) {
        field.Port.FieldsBuffer.Remove(field);
        field.gameObject.SetActive(true);
    }

    private bool isOkToActivate(IoField field) {
        if (DateTime.Now <= field.TimePlaned) return false;
        if (field.Port.CurrentField != null) return false;
        return true;
    }
}
