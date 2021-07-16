using Ilumisoft.VisualStateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.MLAgents;
using UnityEngine;

public sealed class StackField : Field {
    public IndexInStack TrainingResult;

    EnvironmentParameters envParams;

    private void Awake() {
        DimX = Parameters.DimX;
        DimZ = Parameters.DimZ;
        MaxLayer = Parameters.MaxLayer;
        envParams = Academy.Instance.EnvironmentParameters;
    }
    private void Start() {
        initField(GetComponentInParent<ObjectCollection>().IoFieldsGenerator);
        initContainers();
    }

    public override void AddToGround(Container container) {
        base.AddToGround(container);
        container.tag = "container_stacked";

        int bd = 0;
        foreach (var s in Ground) {
            bd += objs.CRPAgent.blockingDegree(s);
        }
        objs.CRPAgent.AddReward((bd - objs.CRPAgent.bDegree) * 0.3f);
    }

    /// <summary>
    /// to generate containers for field
    /// </summary>
    public void initContainers() {
        int i = 0;
        int amount = (int)(envParams?.GetWithDefault("amount", Parameters.DimZ * (Parameters.SpawnMaxLayer - 1) + 1) ?? 6);
        amount += UnityEngine.Random.Range(-3, 3);
        if (amount > Parameters.DimZ * Parameters.SpawnMaxLayer - 1) amount = Parameters.DimZ * Parameters.SpawnMaxLayer - 1;
        for (int x = 0; x < DimX; x++) { // x for crp is always 1

            while (i < amount) {
                int z = UnityEngine.Random.Range(0, Parameters.DimZ);

                var idx = new IndexInStack(x, z);
                if (Ground[idx.x, idx.z].Count >= Parameters.SpawnMaxLayer) continue;

                i++;
                var pos = IndexToLocalPositionInWorldScale(idx);
                var container = generateContainer(pos);
                container.IndexInCurrentField = idx;
                AddToGround(container, idx);
                assignOutField(container, DateTime.Now);
                container.tag = "container_stacked";
            }
        }
        //var cs = GetComponentsInChildren<Container>();
        //var timeSet = new HashSet<DateTime>();
        //foreach(var c in cs) {
        //    timeSet.Add(c.OutField.TimePlaned);
        //}
        //if (cs.Length > timeSet.Count) Destroy(objs.gameObject);
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

    public bool IsStackNeedRearrange(IndexInStack idx) {
        return IsStackNeedRearrange(Ground[idx.x, idx.x]);
    }
}
