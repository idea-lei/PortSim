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

    private ObjectCollection objs;

    [Header("fields for test")]
    [Space(15)]
    [SerializeField] private IoField _currentField;
    [SerializeField] private IoField _nextField;

    [NonSerialized] public int DimX, DimZ;

    private IoField nextField {
        get => _nextField;
        set {
            _nextField = value;
            if (value) {
                var span = value.TimePlaned - DateTime.Now;
                if (span > TimeSpan.Zero) Debug.Log($"{name} next {value.name} has {span} to enable");
            }
        }
    }

    [SerializeField] private List<IoField> fieldsBuffer = new List<IoField>();
    public IoField CurrentField {
        get { return _currentField; }
        set {
            _currentField = value;
            nextField = null;
            if (CurrentField) {
                fieldsBuffer.Remove(CurrentField);
                CurrentField.enabled = true;
            }
        }
    }

    private void Start() {
        var generator = transform.parent.GetComponent<IoFieldsGenerator>();
        objs = GetComponentInParent<ObjectCollection>();

        transform.position = GetComponentInParent<ObjectCollection>().transform.position + new Vector3(0, 0,
            Mathf.Sign(transform.position.z - GetComponentInParent<ObjectCollection>().transform.position.z) * ((Parameters.DimZ + 2) * (Parameters.ContainerWidth + Parameters.Gap_Container) + Parameters.Gap_Field));
        //InvokeRepeating(nameof(delayField), Parameters.SetDelayInterval, Parameters.SetDelayInterval);
        InvokeRepeating(nameof(UpdateCurrentField), 2f, 2f);
    }

    // do u really need to write it in Coroutine form? decision making when event is triggered is much more unstable than polling
    public void UpdateCurrentField() {
        if (fieldsBuffer.Count == 0) return;
        if (CurrentField) return;

        fieldsBuffer.Sort((a, b) => a.TimePlaned < b.TimePlaned ? -1 : 1);
        // calculate sum count of containers
        int sumCount = objs.StackField.Count;
        if (objs.Crane.ContainerCarrying) sumCount++;
        //foreach (var t in objs.TempFields) sumCount += t.Count;
        foreach (var i in objs.IoPorts) if (i.CurrentField is InField) sumCount += i.CurrentField.Count;

        IoField next = sumCount >= objs.StackField.MaxCount ? fieldsBuffer.Find(f => f is OutField) : fieldsBuffer[0];
        if (nextField != next) nextField = next;

        if (nextField != null && nextField.TimePlaned < DateTime.Now) {
            if(nextField is InField inField) {
                if(inField.Count + objs.StackField.Count >= objs.StackField.MaxCount) {
                    delayField(nextField);
                    return;
                }
            }
            if (nextField is OutField) {
                foreach (var c in ((OutField)nextField).IncomingContainers) {
                    // means the inField is still not enabled
                    if (c.CurrentField && c.CurrentField is InField) { // && !c.CurrentField.isActiveAndEnabled
                        delayField(nextField);
                        return;
                    }
                }
                objs.Evaluation.Data.OutFieldCount++;
            }
            if (nextField is InField) objs.Evaluation.Data.InFieldCount++;
            CurrentField = nextField;
        }
    }

    public void AddToBuffer(IoField field) {
        fieldsBuffer.Add(field);
    }

    // randomly choose a field and delay it, for test
    private void delayField() {
        if (UnityEngine.Random.Range(0, 1f) < Parameters.PossibilityOfDelay) {
            if (fieldsBuffer.Count == 0) return;
            var field = fieldsBuffer[UnityEngine.Random.Range(0, fieldsBuffer.Count)];
            delayField(field);
        }
    }

    private void delayField(IoField field) {
        string oldName = field.name;
        field.TimePlaned += IoField.GenerateRandomTimeSpan();
        UpdateCurrentField();
        Debug.Log($"delay field {oldName} to {field.TimePlaned.ToString("g")}");
        // this is to delay the outfields correspond to the inField
        if (field is InField) {
            var outFields = new List<OutField>(); // avoid repetition
            foreach (var s in ((InField)field).Ground) {
                foreach (var c in s) {
                    if (!outFields.Contains(c.OutField)) {
                        outFields.Add(c.OutField);
                        delayField(c.OutField);
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
