using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class OutField : IoField
{
    private void Awake() {
        initField();
    }

    protected override void initField() {
        base.initField();
        name = "Out" + name;
        transform.position = Port.transform.position;
    }
    private void OnEnable() {
        updateState(true);
    }

    private void OnDisable() {
        updateState(false);
    }
}
