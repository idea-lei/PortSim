using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class StackField : Field {
    
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
            }
        }
    }

    
}
