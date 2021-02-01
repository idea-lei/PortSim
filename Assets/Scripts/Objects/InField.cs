using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public sealed class InField : IoField {
    private void Start() {
        initContainers();
    }

    protected override void initContainers() {
        base.initContainers();
        foreach(var s in Ground) {
            foreach(var c in s) {
                c.InField = this;
            }
        }
    }
}
