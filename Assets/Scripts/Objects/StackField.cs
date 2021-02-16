using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class StackField : Field {
    public (bool, Container) NeedRearrange {
        get {
            foreach(var s in Ground) {
                var peek = s.Peek();
                foreach(var c in s) {
                    if(c.OutField.TimePlaned < peek.OutField.TimePlaned) {
                        return (true, c);
                    }
                }
            }
            return (false, null);
        }
    }
    private void Awake() {
        DimX = Parameters.DimX;
        DimZ = Parameters.DimZ;
        MaxLayer = Parameters.MaxLayer;
    }
    private void Start() {
        initField();
        transform.position = new Vector3();
        initContainers();
    }

    protected override void initContainers() {
        base.initContainers();
        // assign outFields
        foreach (var s in Ground) {
            foreach (var c in s) {
                assignOutPort(c);
                c.tag = "container_stacked";
            }
        }
    }
}
