using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class OutField : IoField
{
    private void OnEnable() {
        updateState(true);
        transform.position = Port.transform.position;
    }

    private void OnDisable() {
        updateState(false);
    }
}
