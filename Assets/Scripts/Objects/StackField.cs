using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class StackField : Field {
    private void Awake() {
        DimX = Parameters.DimX;
        DimZ = Parameters.DimZ;
    }
    private void Start() {
        DimX = Parameters.DimX;
        DimZ = Parameters.DimZ;
        MaxLayer = Parameters.MaxLayer;
        initField();
        transform.position = new Vector3();
        initContainers();
    }
}
