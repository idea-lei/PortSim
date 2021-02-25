using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Text;
using Ilumisoft.VisualStateMachine;

/// <summary>
/// this class stricts the ioField, means which size of the field is suitable for the port
/// </summary>
public class IoPort : MonoBehaviour {
    // the max dim of the ioField
    [NonSerialized] public int DimX, DimZ;

    [SerializeField] private IoField _currentField;

    public List<IoField> FieldsBuffer = new List<IoField>();
    public IoField CurrentField {
        get { return _currentField; }
    }

    private void Start() {
        UpdateCurrentField();
    }

    public void UpdateCurrentField() {
        if (FieldsBuffer.Count == 0) {
            Debug.LogWarning("no available field!");
            return;
        }
        FieldsBuffer.Sort((a, b) => a.TimePlaned < b.TimePlaned ? -1 : 1);
        _currentField = FieldsBuffer[0];
        FieldsBuffer.RemoveAt(0);
        if (CurrentField.TimePlaned < DateTime.Now) {
            Invoke(nameof(setFieldEnabled), Parameters.EventDelay);
        } else {
            StartCoroutine(WaitUntilEnable());
        }
    }

    private void setFieldEnabled() {
        CurrentField.enabled = true;
    }

    public IEnumerator WaitUntilEnable() {
        Debug.Log((CurrentField is InField ? "In " : "Out ") + $"Field enable unitil {CurrentField.TimePlaned - DateTime.Now}");
        while (CurrentField.TimePlaned > DateTime.Now) {
            yield return null;
        }
        CurrentField.enabled = true;
        FindObjectOfType<Crane>().ContainerToPick = null;
    }

    public override string ToString() {
        var str = new StringBuilder();
        str.Append(name + "\n");
        foreach (var f in FieldsBuffer) {
            str.Append($"{f.name} -- time planed: {f.TimePlaned:T}\n");
        }
        return str.ToString();
    }
}
