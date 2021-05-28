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
