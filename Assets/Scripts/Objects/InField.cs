using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class InField : IoField {
    private List<MeshRenderer> meshRenderersInChildren = new List<MeshRenderer>();
    private List<BoxCollider> collidersInChildren = new List<BoxCollider>();

    #region unity life circle
    private void Awake() {
        initField();
    }
    private void OnEnable() {
        updateState(true);
    }

    private void OnDisable() {
        updateState(false);
    }
    #endregion

    protected override void updateState(bool state) {
        base.updateState(state);
        if (meshRenderersInChildren != null) {
            foreach (var m in meshRenderersInChildren) {
                if (m) m.enabled = state;
            }
        }
        if (collidersInChildren != null) {
            foreach (var c in collidersInChildren) {
                if (c) c.enabled = state;
            }
        }
    }

    protected override void initField() {
        base.initField();
        TimePlaned = DateTime.Now + GenerateRandomTimeSpan();
        name = "InField_" + TimePlaned.ToString("G"); ;
        initContainers();
        transform.position = Port.transform.position;
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
                    assignOutPort(container, TimePlaned);
                    container.tag = "container_in";
                    meshRenderersInChildren.Add(container.GetComponent<MeshRenderer>());
                    collidersInChildren.Add(container.GetComponent<BoxCollider>());
                }
            }
        }
    }

    public override Container RemoveFromGround(Container c) {
        // because of the updateState, must update the list when remove from ground
        collidersInChildren.Remove(c.GetComponent<BoxCollider>());
        meshRenderersInChildren.Remove(c.GetComponent<MeshRenderer>());
        return base.RemoveFromGround(c);
    }


}
