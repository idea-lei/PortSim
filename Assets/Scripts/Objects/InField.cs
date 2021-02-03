﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public sealed class InField : IoField {
    private MeshRenderer[] meshRenderersInChildren;
    private BoxCollider[] collidersInChildren;

    private bool inited {
        get {
            return meshRenderersInChildren != null && collidersInChildren != null;
        }
    }
    private void Awake() {
        initField();
    }
    private void OnEnable() {
        updateState(true);
    }

    private void OnDisable() {
        updateState(false);
    }

    protected override void updateState(bool state) {
        base.updateState(state);
        if (meshRenderersInChildren != null) {
            foreach (var m in meshRenderersInChildren) {
                m.enabled = state;
            }
        }
        if (collidersInChildren != null) {
            foreach (var c in collidersInChildren) {
                c.enabled = state;
            }
        }
    }

    protected override void initField() {
        base.initField();
        name = "InField_" + DateTime.Now.ToString("T");
        if (!inited) initContainers();
        transform.position = Port.transform.position;
    }

    protected override void initContainers() {
        base.initContainers();
        foreach (var s in Ground) {
            foreach (var c in s) {
                c.InField = this;
            }
        }
        meshRenderersInChildren = GetComponentsInChildren<MeshRenderer>();
        collidersInChildren = GetComponentsInChildren<BoxCollider>();
    }
}
