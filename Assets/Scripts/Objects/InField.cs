using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class InField : IoField {

    public override DateTime TimePlaned {
        get => base.TimePlaned;
        set {
            base.TimePlaned = value;
            name = "InField_" + value.ToString("G");
        }
    }

    #region unity life circle
    private void Awake() {
        initField();
    }
    #endregion

    protected override void initField() {
        TimePlaned = DateTime.Now + GenerateRandomTimeSpan();
        base.initField();
        initContainers();
        assignPort();
    }

    private void initContainers() {
        for (int x = 0; x < DimX; x++) {
            for (int z = 0; z < DimZ; z++) {
                for (int k = 0; k <= UnityEngine.Random.Range(0, MaxLayer); k++) {
                    var pos = IndexToLocalPositionInWorldScale(new IndexInStack(x, z));
                    var container = generateContainer(pos);
                    container.indexInCurrentField = new IndexInStack(x, z);
                    AddToGround(container, new IndexInStack(x, z));
                    container.InField = this;
                    assignOutField(container, TimePlaned);
                    container.tag = "container_in";
                }
            }
        }
    }
}
