using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class StackField : Field {
    private void OnEnable() {
        DimX = Parameters.DimX;
        DimZ = Parameters.DimZ;
        _ground = new Stack<Container>[DimX, DimZ];
    }
    private void Start() {
        DimX = Parameters.DimX;
        DimZ = Parameters.DimZ;
        initField();
        initContainers();
    }
    protected override void initField() {
        base.initField();
        transform.position = new Vector3();
    }

    protected override void initContainers() {
        for (int x = 0; x < DimX; x++) {
            for (int z = 0; z < DimZ; z++) {
                Ground[x, z] = new Stack<Container>();
                for (int k = 0; k <= UnityEngine.Random.Range(-1, Parameters.MaxLayer); k++) {
                    var pos = IndexToLocalPosition(x, z, Ground[x, z].Count());
                    var container = generateContainer(pos);
                    Ground[x, z].Push(container);
                }
            }
        }
    }


}
