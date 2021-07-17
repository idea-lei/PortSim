using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public static class ToolFunctions {
    public static void RemoveAtIndices<T>(ref List<T> list, int[] a) {
        if (a is null || a.Length == 0) return;
        var sorted = a.ToList();
        sorted.Sort();
        var stack = new Stack<int>();
        foreach (var i in sorted) {
            stack.Push(i);
        }
        while (stack.Count > 0) {
            list.RemoveAt(stack.Pop());
        }
    }
}

public class Bay2DAgent : Agent {
    Bay bay;
    int maxLabel = 6;
    readonly float blockingDegreeCoefficient = 100;
    int blockingDegreeOfState;
    int[] indicesToAvoid = null;
    int relocationTimes = 0;

    EnvironmentParameters envParams;

    public override void Initialize() {
        envParams = Academy.Instance.EnvironmentParameters;
    }

    public override void OnEpisodeBegin() {
        relocationTimes = 0;
        maxLabel = (int)envParams.GetWithDefault("amount", 16);
        //maxLabel = 16;
        bay = new Bay(Parameters.DimZ, Parameters.MaxLayer, Parameters.SpawnMaxLayer, maxLabel);
        //Debug.Log(bay);
        nextOperation(indicesToAvoid);
    }

    public override void CollectObservations(VectorSensor sensor) {
        var bd = bay.BlockingDegrees;
        //blockingDegreeOfState = bd.Sum();

        var layout = bay.LayoutAs2DArray;

        var ob = new List<List<float>>();
        for (int z = 0; z < bay.DimZ; z++) {
            var list = new List<float>();

            // avoid this index? -- 1
            list.Add((indicesToAvoid !=null && indicesToAvoid.Contains(z)) ? 0 : 1);

            // one hot -- dimZ
            var oh = new float[bay.DimZ];
            oh[z] = 1;
            list.AddRange(oh);

            // z index -- 1
            list.Add(z / (float)bay.DimZ);

            // blockingDegree of stack -- 1
            list.Add(bd[z] / blockingDegreeCoefficient);

            // layout -- maxTier
            for (int t = 0; t < bay.MaxTier; t++) {
                list.Add(layout[z, t] is null ? 0 : layout[z, t].priority / (float)bay.MaxLabel);
            }

            Debug.Assert(list.Count == bay.DimZ + bay.MaxTier + 3);
            Debug.Assert(list.All(l => l <= 1 && l >= -1));
            ob.Add(list);
        }

        // remove the avoided index
        //ToolFunctions.RemoveAtIndices(ref ob, indicesToAvoid);

        var rnd = new System.Random();
        ob.OrderBy(n => rnd.Next());

        foreach(var l in ob) {
            sensor.AddObservation(l);
        }

        //var observation = new List<float>();
        //foreach (var o in ob) {
        //    observation.AddRange(o);
        //}
        //observation.AddRange(new float[bay.DimZ * (bay.DimZ + bay.MaxTier + 3) - observation.Count]);

        //sensor.AddObservation(observation);
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        var aout = actionsOut.DiscreteActions;


        aout[0] = UnityEngine.Random.Range(0, Parameters.DimZ);
        aout[1] = UnityEngine.Random.Range(0, Parameters.DimZ);
    }

    // actions can only be relocation
    public override void OnActionReceived(ActionBuffers actions) {
        indicesToAvoid = null;

        int z0 = actions.DiscreteActions[0];
        int z1 = actions.DiscreteActions[1];

        // relocation failed
        var canRelocate = bay.canRelocate(z0, z1);
        if (!canRelocate.Item1) {
            AddReward(-1);
            Debug.LogWarning($"failed to relocate from {z0} to {z1}");
            int[] indicesToAvoid = null;
            switch (canRelocate.Item2) {
                case 0: // z0
                case 3:
                    indicesToAvoid = new int[] { z0 };
                    break;
                case 1: // z1
                    indicesToAvoid = new int[] { z1 };
                    break;
                case 2: // z0 and z1
                    indicesToAvoid = new int[] { z0, z1 };
                    break;
            }
            nextOperation(indicesToAvoid);
            return;
        }
        bay.relocate(z0, z1);

        // rewarding system

        // state blocking degree 
        float bd = bay.BlockingDegrees.Sum();
        AddReward((blockingDegreeOfState - bd) / blockingDegreeCoefficient);

        // step reward
        AddReward(0.01f / bay.MaxLabel);

        // relocation success
        nextOperation(null);
    }

    private void nextOperation(int[] _indicesToAvoid) {
        if (bay.empty) {
            Evaluation2D.Instance.UpdateValue(maxLabel, relocationTimes);
            EndEpisode();
        }
        if (bay.canRetrieve) {
            var min = bay.min;
            relocationTimes += bay.retrieve();
            AddReward(1 - 0.001f * min.Item2);
        }
        indicesToAvoid = _indicesToAvoid;

        RequestDecision();
    }
}












public class Container2D : IComparable {
    public readonly int priority;
    public int relocationTimes;

    public Container2D(int p) {
        relocationTimes = 0;
        priority = p;
    }

    public static bool operator >(Container2D a, Container2D b) {
        return a.priority > b.priority;
    }

    public static bool operator <(Container2D a, Container2D b) {
        return a.priority < b.priority;
    }

    public static bool operator ==(Container2D a, Container2D b) {
        if (a is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(Container2D a, Container2D b) {
        return !a.Equals(b);
    }

    public int CompareTo(object obj) {
        if (obj is Container2D c) {
            if (priority > c.priority) return 1;
            if (priority == c.priority) return 0;
            else return -1;
        }
        throw new Exception("other is not Container2D");
    }

    public override bool Equals(object obj) {
        if (obj is Container2D c) {
            return GetHashCode() == c.GetHashCode();
        }
        return false;
    }

    public override int GetHashCode() {
        return base.GetHashCode();
    }

    public override string ToString() {
        return priority.ToString();
    }
}

public class Bay {
    private Stack<Container2D>[] layout; //[z,t]
    public readonly int MaxTier;
    public readonly int DimZ;

    private int initTier;
    private int maxLabel;
    public int MaxLabel => maxLabel;

    /// <summary>
    /// this property is for observation, the stack will automatically from top to bottom
    /// </summary>
    public List<Container2D>[] Layout {
        get {
            var list = new List<Container2D>[DimZ];
            for (int i = 0; i < DimZ; i++) {
                list[i] = layout[i].ToList();
            }
            return list;
        }
    }

    public Container2D[,] LayoutAs2DArray {
        get {
            var res = new Container2D[DimZ, MaxTier];
            var layout = Layout;
            for (int z = 0; z < DimZ; z++) {
                for (int t = 0; t < DimZ; t++) {
                    res[z, t] = t < layout[z].Count ? layout[z][t] : null;
                }
            }
            return res;
        }
    }

    public int[] BlockingDegrees => layout.Select(s => BlockingDegree(s)).ToArray();

    public bool empty {
        get {
            foreach (var s in layout) {
                if (s.Count > 0) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Item1: Item, 
    /// Item2: z-index
    /// </summary>
    public (Container2D, int) min {
        get {
            Container2D m = new Container2D(int.MaxValue);
            int index = 0;
            for (int i = 0; i < layout.Length; i++) {
                if (layout[i].Count > 0) {
                    var _m = layout[i].Min();
                    if (m > _m) {
                        m = _m;
                        index = i;
                    }
                }
            }
            return (m, index);
        }
    }

    public Bay(int z, int t, int _initTier, int _maxLabel) {
        layout = new Stack<Container2D>[z];
        for (int i = 0; i < z; i++) {
            layout[i] = new Stack<Container2D>();
        }
        MaxTier = t;
        DimZ = z;
        initTier = _initTier;
        maxLabel = _maxLabel;
        assignValues(generateSequence(_maxLabel));
    }

    public bool CheckDim() {
        return Layout.All(s => s.Count <= MaxTier);
    }

    /// <param name="z0">pick up pos</param>
    /// <param name="z1">stack pos</param>
    /// <returns>true if can relocate, the Item2 is reason--> 0: z0 empty, 1: z1 full, 2: both, 3: same index, 4: success</returns>

    public (bool, int) canRelocate(int z0, int z1) {
        (bool, int) res = (true, 4);
        if (layout[z0].Count <= 0) res = (false, 0);
        if (layout[z1].Count >= DimZ) res = (false, 1);
        if (layout[z0].Count <= 0 && layout[z1].Count >= DimZ) res = (false, 2);
        if (z0 == z1) res = (false, 3);
        return res;
    }

    // check canRelocate first
    public void relocate(int z0, int z1) {
        var c = layout[z0].Pop();
        c.relocationTimes++;
        layout[z1].Push(c);
    }

    public bool stack(int z, Container2D v) {
        if (layout[z].Count == MaxTier) return false;
        layout[z].Push(v);
        return true;
    }

    public bool canRetrieve {
        get {
            var m = min;
            return layout[m.Item2].Peek() == m.Item1;
        }
    }

    /// <summary>
    /// check canRetrieve first!
    /// </summary>
    /// <returns> relocation times of this container</returns>
    public int retrieve() {
        var c = layout[min.Item2].Pop();
        return c.relocationTimes;
    }

    private int[] generateSequence(int i) {
        System.Random random = new System.Random();
        return Enumerable.Range(1, i).OrderBy(x => random.Next()).ToArray();
    }

    private void assignValues(int[] arr) {
        int i = 0;
        while (i < arr.Length) {
            int z = UnityEngine.Random.Range(0, DimZ);
            if (layout[z].Count >= initTier) continue;
            if (stack(z, new Container2D(arr[i]))) i++;
        }
    }

    // peak value of z-index
    public Container2D Peek(int z) {
        return layout[z].Peek();
    }


    // from https://iopscience.iop.org/article/10.1088/1742-6596/1873/1/012050/pdf
    public int BlockingDegree(Stack<Container2D> s) {
        int degree = 0;
        var list = s.Select(c => c.priority).ToList();
        list.Reverse();

        List<int> hList;
        while (list.Count > 1) {
            int truncate = list.IndexOf(list.Min());
            hList = list.GetRange(truncate, list.Count - truncate);
            if (hList.Count > 1) foreach (int x in hList) degree += hList[0] - x;
            list = list.GetRange(0, truncate);
        }

        return degree;
    }

    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        foreach (var s in layout) {
            var list = s.ToList();
            list.Reverse();
            sb.Append(string.Join(", ", list) + "\n");
        }
        return sb.ToString();
    }
}
