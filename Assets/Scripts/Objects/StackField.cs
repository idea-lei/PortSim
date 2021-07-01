using Ilumisoft.VisualStateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public sealed class StackField : Field {
    public IndexInStack TrainingResult;

    private void Awake() {
        DimX = Parameters.DimX;
        DimZ = Parameters.DimZ;
        MaxLayer = Parameters.MaxLayer;
    }
    private void Start() {
        initField(GetComponentInParent<ObjectCollection>().IoFieldsGenerator);
        //initContainers();
    }

    public override void AddToGround(Container container) {
        base.AddToGround(container);
        container.tag = "container_stacked";
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
                    container.IndexInCurrentField = new IndexInStack(x, z);
                    AddToGround(container, new IndexInStack(x, z));
                    assignOutField(container, DateTime.Now);
                    container.tag = "container_stacked";
                }
            }
        }
    }

    public bool IsIndexFull(IndexInStack idx) {
        return Ground[idx.x, idx.z].Count >= MaxLayer;
    }

    public bool IsIndexFull(int x, int z) {
        return Ground[x, z].Count >= MaxLayer;
    }

    public bool IsStackNeedRearrange(Stack<Container> stack) {
        if (stack.Count == 0) return false;
        var list = stack.ToArray();
        var min = list.First(x => x.OutField.TimePlaned == list.Min(y => y.OutField.TimePlaned));
        if (min != stack.Peek()) return true;
        return false;
    }
}
