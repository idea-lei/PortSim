using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class OutField : IoField
{
    private void Awake() {
        name = "OutField_" + DateTime.Now.ToString("T");
        base.initField();
        transform.position = Port.transform.position;
    }
    private void OnEnable() {
        updateState(true);
    }

    private void OnDisable() {
        updateState(false);
    }
}
