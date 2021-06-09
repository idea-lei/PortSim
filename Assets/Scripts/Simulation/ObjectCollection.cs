using Ilumisoft.VisualStateMachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// this class collects all the objects that needed for other objects
/// </summary>
public class ObjectCollection : MonoBehaviour {
    public StackField StackField;
    public Crane Crane;
    public StateMachine StateMachine;
    public IoFieldsGenerator IoFieldsGenerator;
    public IoPort[] IoPorts;
    public TempField[] TempFields;
    public FindContainerInAgent FindContainerInAgent;
    public FindContainerOutAgent FindContainerOutAgent;
    public FindIndexAgent FindIndexAgent;

    public OutField[] OutFields {
        get {
            List<OutField> fields = new List<OutField>();
            foreach (var port in IoPorts) {
                if (port.CurrentField &&
                    port.CurrentField.isActiveAndEnabled &&
                    port.CurrentField is OutField field &&
                    !field.Finished)
                    fields.Add(field);
            }
            return fields.ToArray();
        }
    }

    public InField[] Infields {
        get {
            List<InField> fields = new List<InField>();
            foreach (var port in IoPorts) {
                if (port.CurrentField &&
                    port.CurrentField.isActiveAndEnabled &&
                    port.CurrentField is InField field &&
                    !field.Finished)
                    fields.Add(field);
            }
            return fields.ToArray();
        }
    }

    public bool HasOutField => OutFields.Length > 0;

    public bool HasInField => Infields.Length > 0;

    public Container[] ContainersInStackField => StackField.GetComponentsInChildren<Container>();

    public Container[] ContainersInTempFields {
        get {
            List<Container> cs = new List<Container>();
            foreach (var field in TempFields) {
                cs.AddRange(field.GetComponentsInChildren<Container>());
            }
            return cs.ToArray();
        }
    }

    public Container[] OutContainersInTempFields {
        get {
            if (!HasOutField) return new Container[0];
            var ofs = OutFields;
            return ContainersInTempFields.Where(c => ofs.Contains(c.OutField)).ToArray();
        }
    }

    public Container[] OutContainersInStackField {
        get {
            if (!HasOutField) return new Container[0];
            var ofs = OutFields;
            return ContainersInStackField.Where(c => ofs.Contains(c.OutField)).ToArray();
        }
    }

    private void Start() {
        InvokeRepeating(nameof(CheckStackFull), 10 + Random.Range(-5, 5), 10);
    }

    // if stack field full and still has inField, destroy the object
    private void CheckStackFull() {
        if (IoPorts.Any(i => i.CurrentField is OutField)) return;
        if (StackField.IsGroundFull && IoPorts.Any(i => i.CurrentField is InField)) Destroy(gameObject);
    }

    private void OnDestroy() {
        var areaContainer = GetComponentInParent<AreaContainer>();
        Invoke(nameof(areaContainer.GenerateArea), 3);
    }
}
