using System;
using System.Collections.Generic;
using UnityEngine;

public class OutField : IoField {
    [SerializeField] private List<Container> incomingContainers = new List<Container>();

    public int IncomingContainersCount => incomingContainers.Count;

    public override bool IsGroundEmpty => base.IsGroundEmpty && (incomingContainers.Count == 0);

    public bool GroundFullPlaned => IncomingContainersCount >= MaxCount;

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

    public void AddContainerToList(Container c) {
        incomingContainers.Add(c);
        if(!c.InField && TimePlaned < DateTime.Now) {
            TimePlaned = DateTime.Now + GenerateRandomTimeSpan();
        }

        if (c.InField && c.InField.TimePlaned > TimePlaned) {
            TimePlaned = c.InField.TimePlaned + GenerateRandomTimeSpan();
        }
        name = "OutField_" + TimePlaned.ToString("G");
    }

    protected override void initField() {
        base.initField();
        transform.position = Port.transform.position;
    }
}
