using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// this class is to generate and store the input/output fields
/// it's not the ioField!
/// </summary>
public class IoFields : MonoBehaviour {
    public HashSet<InField> InFields = new HashSet<InField>();
    public HashSet<OutField> OutFields = new HashSet<OutField>();

    [SerializeField]
    private GameObject inFieldPrefab;
    [SerializeField]
    private GameObject outFieldPrefab;

    private HashSet<Field> activeFields = new HashSet<Field>();

    private void Start() {
        initFields();
    }

    private void initFields() {
        // init inFields, add 3 fields for test
        for (int i = 0; i < 3; i++) {
            var (obj, field) = GenerateInField();
            InFields.Add(field);
        }
        // init outFields, add 20 fields for test
        for (int i = 0; i < 20; i++) {
            var (obj, field) = GenerateOutField();
            OutFields.Add(field);
        }
    }

    public (GameObject, InField) GenerateInField() {
        var obj = Instantiate(inFieldPrefab);
        var inField = obj.GetComponent<InField>();
        obj.SetActive(false);
        return (obj, inField);
    }

    public (GameObject, OutField) GenerateOutField() {
        var obj = Instantiate(outFieldPrefab);
        var outField = obj.GetComponent<OutField>();
        obj.SetActive(false);
        return (obj, outField);
    }

    private void setFieldActive(Field field) {
        if (field is InField) InFields.Remove((InField)field);
        if (field is OutField) OutFields.Remove((OutField)field);
        activeFields.Add(field);
        field.gameObject.SetActive(true);
    }
}
