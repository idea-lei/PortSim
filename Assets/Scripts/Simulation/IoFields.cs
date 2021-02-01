using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// this class is to generate and store the input/output fields
/// it's not the ioField!
/// </summary>
public class IoFields : MonoBehaviour {
    public List<InField> InFields;
    public List<OutField> OutFields;

    [SerializeField]
    private GameObject inFieldPrefab;
    [SerializeField]
    private GameObject outFieldPrefab;

    //private IoPort[] ioPorts;

    private void Awake() {
        InFields = new List<InField>();
        OutFields = new List<OutField>();
        //ioPorts = FindObjectsOfType<IoPort>();
    }

    private void Start() {
        initFields();
    }

    private void initFields() {
        // init inFields, add 3 fields for test
        for (int i = 0; i < 3; i++) {
            var (obj, field) = GenerateInField();
            InFields.Add(field);
            obj.SetActive(false);
        }
        // init outFields, add 20 fields for test
        for (int i = 0; i < 5; i++) {
            var (obj, field) = GenerateOutField();
            OutFields.Add(field);
            obj.SetActive(false);
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
        if (field is InField) InFields.Remove((InField)field);
        if (field is OutField) OutFields.Remove((OutField)field);
        field.gameObject.SetActive(true);
    }

    private bool isOkToActivate(IoField field) {
        if (DateTime.Now <= field.TimePlaned) return false;
        if (field.Port.CurrentField != null) return false;
        return true;
    }
}
