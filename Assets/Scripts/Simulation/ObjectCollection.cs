using Ilumisoft.VisualStateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// this class collects all the objects that needed for other objects
/// </summary>
public class ObjectCollection : MonoBehaviour
{
    public StackField StackField;
    public Crane Crane;
    public StateMachine StateMachine;
    public IoFieldsGenerator IoFieldsGenerator;
    public IoPort[] IoPorts;
    public TempField[] TempFields;

    private void OnDestroy() {
        var areaContainer = GetComponentInParent<AreaContainer>();
        Invoke(nameof(areaContainer.GenerateArea),3);
    }
}
