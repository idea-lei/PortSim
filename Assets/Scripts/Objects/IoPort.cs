using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// this class stricts the ioField, means which size of the field is suitable for the port
/// </summary>
public class IoPort : MonoBehaviour {
    // the max dim of the ioField
    [NonSerialized] public int DimX, DimZ;

    [SerializeField] private IoField _currentField;

    [SerializeField] private List<IoField> fieldsBuffer = new List<IoField>();
    private Coroutine coroutine;
    public IoField CurrentField {
        get { return _currentField; }
        set {
            _currentField = value;
            if (CurrentField) {
                fieldsBuffer.Remove(CurrentField);
                CurrentField.enabled = true;
                if (coroutine != null) {
                    StopCoroutine(coroutine);
                    coroutine = null;
                }
            } else UpdateCurrentField();
        }
    }

    private void Start() {
        transform.position = new Vector3(0, 0,
            Mathf.Sign(transform.position.z) * (Parameters.DimZ * (Parameters.ContainerWidth + Parameters.Gap_Container) + Parameters.Gap_Field));
        InvokeRepeating(nameof(delayField), Parameters.SetDelayInterval, Parameters.SetDelayInterval);
    }

    // do u really need to optimize it with Coroutine?
    public void UpdateCurrentField() {
        if (fieldsBuffer.Count == 0) return;
        if (CurrentField != null) return;

        fieldsBuffer.Sort((a, b) => a.TimePlaned < b.TimePlaned ? -1 : 1);
        var nextField = fieldsBuffer[0];
        if (nextField.TimePlaned < DateTime.Now) {
            Debug.Log(nextField.TimePlaned);
            CurrentField = nextField;
        } else {
            Debug.Log($"next Field {nextField.name} has {nextField.TimePlaned - DateTime.Now} to enable");
            coroutine = StartCoroutine(nameof(waitUntilEnable), nextField);
        }
    }

    public void AddToBuffer(IoField field) {
        fieldsBuffer.Add(field);
        UpdateCurrentField();
    }

    private IEnumerable waitUntilEnable(IoField nextField) {
        while (nextField.TimePlaned > DateTime.Now) {
            yield return null;
        }
        CurrentField = nextField;
    }

    private void delayField() {
        if (UnityEngine.Random.Range(0, 1f) < Parameters.PossibilityOfDelay) {
            if (fieldsBuffer.Count == 0) return;
            var field = fieldsBuffer[UnityEngine.Random.Range(0, fieldsBuffer.Count)];
            string oldName = field.name;
            field.TimePlaned += IoField.GenerateRandomTimeSpan();
            UpdateCurrentField();
            Debug.Log($"delay field {oldName} to {field.TimePlaned.ToString("G")}");
            // this is to delay the outfields correspond to the inField
            if (field is InField) {
                var outFields = new List<OutField>();
                foreach (var s in ((InField)field).Ground) {
                    foreach (var c in s) {
                        if (!outFields.Contains(c.OutField)) {
                            outFields.Add(c.OutField);
                            c.OutField.TimePlaned = field.TimePlaned + IoField.GenerateRandomTimeSpan();
                        }
                    }
                }
            }
        }
    }

    public override string ToString() {
        var str = new StringBuilder();
        str.Append(name + "\n");
        foreach (var f in fieldsBuffer) {
            str.Append($"{f.name} -- time planed: {f.TimePlaned:T}\n");
        }
        return str.ToString();
    }
}
