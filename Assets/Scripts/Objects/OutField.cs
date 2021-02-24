using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class OutField : IoField
{
    public List<Container> incomingContainers = new List<Container>();

    public override bool IsGroundEmpty { get {
            return base.IsGroundEmpty && incomingContainers.Count ==0;
        } 
    }

    public bool GroundFullPlaned=> incomingContainers.Count + Count >= MaxCount;

    public bool IsStackable => IsGroundFull;

    private void Awake() {
        initField();
    }

    private void OnEnable() {
        updateState(true);
    }

    private void OnDisable() {
        updateState(false);
    }

    protected override void initField() {
        base.initField();
        name = "Out" + name;
        transform.position = Port.transform.position;
    }
}
