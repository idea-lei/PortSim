using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

/// <summary>
/// this class stricts the ioField, means which size of the field is suitable for the port
/// </summary>
public class IoPort : MonoBehaviour
{
    // the max dim of the ioField
    [NonSerialized] public int DimX;
    [NonSerialized] public int DimZ;

    private IoField _currentField;
    private IoFields ioFields;
    public IoField CurrentField {
        get { return _currentField; }
    }

    private void Awake() {
        ioFields = FindObjectOfType<IoFields>();
    }

    private void Start() {
        UpdateCurrentField();
    }

    public void UpdateCurrentField() {
        _currentField = FindNextField();
        updateFieldProperties();
    }

    /// <summary>
    /// outfield has higher priority than inField
    /// </summary>
    /// <returns>next field that will be set active</returns>
    public IoField FindNextField() {
        var availableFields = new List<IoField>();
        if(ioFields.InFields.Count==0) throw new Exception("infields null");
        availableFields.AddRange(ioFields.InFields.FindAll(x => x.Port == this));
        availableFields.AddRange(ioFields.OutFields.FindAll(x => x.Port == this));
        if (availableFields.Count == 0) throw new Exception("no available field!");
        var next = availableFields.Where(x => x.TimePlaned == availableFields.Min(f => f.TimePlaned)).First();
        if (next is InField) ioFields.InFields.Remove((InField)next);
        if (next is OutField) ioFields.OutFields.Remove((OutField)next);
        return next;
    }

    private void updateFieldProperties() {
        CurrentField.enabled = false;
        CurrentField.StartCoroutine(WaitUntilEnable());
    }

    public IEnumerator WaitUntilEnable() {
        Debug.Log((CurrentField is InField ? "In " : "Out ") + $"Container enable unitil {CurrentField.TimePlaned - DateTime.Now}");
        while (CurrentField.TimePlaned > DateTime.Now) {
            yield return null;

        }
        Debug.Log("time up!");
        CurrentField.enabled = true;
    }
}
