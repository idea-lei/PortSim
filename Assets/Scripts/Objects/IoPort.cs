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
    #region fields get from ioFieldGenerator
    private StackField stackField;
    private TempField[] tempFields;
    private IoPort[] ioPorts;
    private Crane crane;
    #endregion
    // the max dim of the ioField

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
        stackField = generator.StackField;
        tempFields = generator.TempFields;
        ioPorts = generator.IoPorts;
        crane = generator.Crane;
        transform.position = GetComponentInParent<ObjectCollection>().transform.position + new Vector3(0, 0,
            Mathf.Sign(transform.position.z - GetComponentInParent<ObjectCollection>().transform.position.z) * ((Parameters.DimZ + 2) * (Parameters.ContainerWidth + Parameters.Gap_Container) + Parameters.Gap_Field));
        InvokeRepeating(nameof(delayField), Parameters.SetDelayInterval, Parameters.SetDelayInterval);
        InvokeRepeating(nameof(UpdateCurrentField), 2f, 2f);
    }

    // do u really need to write it in Coroutine form? decision making when event is triggered is much more unstable than polling
    public void UpdateCurrentField() {
        if (fieldsBuffer.Count == 0) return;
        if (CurrentField) return;

        fieldsBuffer.Sort((a, b) => a.TimePlaned < b.TimePlaned ? -1 : 1);
        // calculate sum count of containers
        int sumCount = stackField.Count;
        if (crane.ContainerCarrying) sumCount++;
        foreach (var t in tempFields) sumCount += t.Count;
        foreach (var i in ioPorts) if (i.CurrentField is InField) sumCount += i.CurrentField.Count;

        IoField next = sumCount >= stackField.MaxCount ? fieldsBuffer.Find(f => f is OutField) : fieldsBuffer[0];
        if (nextField != next) nextField = next;

        if (nextField != null && nextField.TimePlaned < DateTime.Now) {
            if (nextField is OutField) {
                foreach (var c in ((OutField)nextField).IncomingContainers) {
                    // means the inField is still not enabled
                    if (c.CurrentField && c.CurrentField is InField && !c.CurrentField.isActiveAndEnabled) {
                        delayField(nextField);
                        return;
                    }
                }
            }
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
        Debug.Log($"delay field {oldName} to {field.TimePlaned.ToString("G")}");
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
