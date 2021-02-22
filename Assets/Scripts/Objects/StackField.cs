using System;
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

    public override void AddToGround(Container container) {
        base.AddToGround(container);
        container.tag = "container_stacked";
    }

    /// <summary>
    /// to generate containers for field
    /// </summary>
    private void initContainers() {
        for (int x = 0; x < DimX; x++) {
            for (int z = 0; z < DimZ; z++) {
                for (int k = 0; k <= UnityEngine.Random.Range(0, MaxLayer); k++) {
                    var pos = IndexToLocalPositionInWorldScale(new IndexInStack(x, z));
                    var container = generateContainer(pos);
                    container.indexInCurrentField = new IndexInStack(x, z);
                    AddToGround(container, new IndexInStack(x, z));
                    assignOutPort(container, DateTime.Now);
                    container.tag = "container_stacked";
                }
            }
        }
    }
}
