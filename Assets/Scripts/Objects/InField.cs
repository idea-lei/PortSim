using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public sealed class InField : IoField {

    public override DateTime TimePlaned {
        get => base.TimePlaned;
        set {
            base.TimePlaned = value;
            name = "InField_" + value.ToString("G");
        }
    }

    public override bool Finished => GetComponentsInChildren<Container>().Length == 0;

    #region unity life circle
    #endregion

    public void Init(IoPort[] ports, IoFieldsGenerator generator) {
        initField(generator);
        assignPort(ports);
    }

    protected override void initField(IoFieldsGenerator generator) {
        TimePlaned = DateTime.Now + GenerateRandomTimeSpan(true);
        base.initField(generator);
        initContainers();
    }

    private void initContainers() {
        for (int x = 0; x < DimX; x++) {
            for (int z = 0; z < DimZ; z++) {
                for (int k = 0; k <= UnityEngine.Random.Range(0, MaxLayer); k++) {
                    var pos = IndexToLocalPositionInWorldScale(new IndexInStack(x, z));
                    var container = generateContainer(pos);
                    container.IndexInCurrentField = new IndexInStack(x, z);
                    AddToGround(container, new IndexInStack(x, z));
                    container.InField = this;
                    container.tag = "container_in";
                    assignOutField(container, TimePlaned);
                }
            }
        }
    }
}
