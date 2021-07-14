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
    public FindNextOperation FindNextOperationAgent;
    [HideInInspector] public Evaluation Evaluation;

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

    #region properties
    public bool HasOutField => OutFields.Length > 0;

    public bool HasInField => Infields.Length > 0;

    public Container[] ContainersInStackField => StackField.GetComponentsInChildren<Container>();

    //public Container[] ContainersInTempFields {
    //    get {
    //        List<Container> cs = new List<Container>();
    //        foreach (var field in TempFields) {
    //            cs.AddRange(field.GetComponentsInChildren<Container>());
    //        }
    //        return cs.ToArray();
    //    }
    //}

    public Container[] InContainers {
        get {
            if (!HasInField) return new Container[0];
            return Infields[0].GetComponentsInChildren<Container>();
        }
    }

    public Container[] InContainersOnPeak {
        get {
            if (!HasInField) return new Container[0];
            var list = new List<Container>();
            foreach (var s in Infields[0].Ground) {
                if (s.Count > 0) list.Add(s.Peek());
            }
            return list.ToArray();
        }
    }

    public Container[] OutContainers {
        get {
            if (!HasOutField) return new Container[0];
            var ofs = OutFields;
            return ContainersInStackField.Where(c => ofs.Contains(c.OutField)).ToArray();
        }
    }

    public IndexInStack[] OutContainersIndices {
        get {
            var set = new HashSet<IndexInStack>();
            foreach (var c in OutContainers) {
                set.Add(c.IndexInCurrentField);
            }
            return set.ToArray();
        }
    }

    public Container[] PeakContainersToRelocate {
        get {
            var set = new HashSet<IndexInStack>();
            var containers = new List<Container>();
            foreach (var c in OutContainers) {
                set.Add(c.IndexInCurrentField);
            }
            foreach (var i in set) {
                containers.Add(StackField.Ground[i.x, i.z].Peek());
            }
            return containers.ToArray();
        }
    }

    public Container[] RelocateContainers {
        get {
            if (!HasOutField) return new Container[0];
            var ofs = OutFields;
            return ContainersInStackField.Where(c => ofs.Contains(c.OutField)).ToArray();
        }
    }

    public Container[] OutContainersOnPeak {
        get {
            if (!HasOutField) return new Container[0];
            var ofs = OutFields;
            return ContainersInStackField.Where(c => ofs.Contains(c.OutField) && c.IsPeak).ToArray();
        }
    }
    #endregion

    private void Awake() {
        Time.timeScale = Parameters.TimeScale;
        Evaluation = Evaluation.Instance;
    }

    private void Start() {
        InvokeRepeating(nameof(CheckStackFull), 10 + Random.Range(-5, 5), 10);
        InvokeRepeating(nameof(DestoryGameObject), 60, 60); // for training, dont make too many empty indices
    }

    private void DestoryGameObject() {
        if (StackField.Count < 15)
            Destroy(gameObject);
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
