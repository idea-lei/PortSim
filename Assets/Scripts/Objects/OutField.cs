using System;
using System.Collections.Generic;
using UnityEngine;

public class OutField : IoField {
    public List<Container> IncomingContainers = new List<Container>();

    public int IncomingContainersCount => IncomingContainers.Count;

    public override bool IsGroundEmpty => base.IsGroundEmpty && (IncomingContainers.Count == 0);

    public bool GroundFullPlaned => IncomingContainersCount >= MaxCount;

    public bool IsStackable => IsGroundFull;

    public override DateTime TimePlaned {
        get => base.TimePlaned;
        set {
            base.TimePlaned = value;
            name = "OutField_" + value.ToString("G");
        }
    }

    public override bool Finished => IncomingContainers.Count == GetComponentsInChildren<Container>().Length;

    public void Init(IoPort[] ports, IoFieldsGenerator generator) {
        initField(generator);
        assignPort(ports);
    }

    public void AddContainerToList(Container c) {
        IncomingContainers.Add(c);
        if (!c.InField && TimePlaned < DateTime.Now) {
            TimePlaned = DateTime.Now + GenerateRandomTimeSpan();
        }

        if (c.InField && c.InField.TimePlaned > TimePlaned) {
            TimePlaned = c.InField.TimePlaned + GenerateRandomTimeSpan();
        }
    }

    public override void AddToGround(Container container) {
        base.AddToGround(container);
        if (Finished) {
            DestroyField();
        }
    }

    public override void DestroyField() {
        Evaluation.Instance.UpdateEvaluation(GetComponentsInChildren<Container>());
        base.DestroyField();
    }
}
