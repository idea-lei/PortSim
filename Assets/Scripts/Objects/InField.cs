using System;
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
        name = "In"+name;
        initContainers();
        transform.position = Port.transform.position;
    }

    /// <summary>
    /// assign properties for generated containers
    /// </summary>
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
