﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public sealed class InField : IoField {
    private MeshRenderer[] meshRenderersInChildren;
    private BoxCollider[] collidersInChildren;

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
        name = "In" + name;
        initContainers();
        transform.position = Port.transform.position;
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
                    container.InField = this;
                    assignOutPort(container, TimePlaned);
                    container.tag = "container_in";
                }
            }
        }
        meshRenderersInChildren = GetComponentsInChildren<MeshRenderer>();
        collidersInChildren = GetComponentsInChildren<BoxCollider>();
    }

    public override void DestroyField() {
        // don't edit this, i have no idea why directly destroy gameobject will cause deleting all containers generated from this field
        transform.DetachChildren();
        Destroy(GetComponent<MeshCollider>(), 0.2f);
        Destroy(GetComponent<MeshRenderer>(), 0.6f);
        Destroy(GetComponent<MeshFilter>(), 0.6f);
        Destroy(this, 0.8f);
        base.DestroyField();
    }
}
