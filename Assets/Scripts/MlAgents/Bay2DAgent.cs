using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class Bay2DAgent : Agent {
    Bay bay;
    int maxLabel = 6;

    public override void Initialize() {
    }

    public override void OnEpisodeBegin() {
        bay = new Bay(Parameters.DimZ, Parameters.MaxLayer, Parameters.SpawnMaxLayer, maxLabel);
        Debug.Log(bay);
        nextOperation();
    }

    public override void CollectObservations(VectorSensor sensor) {
        sensor.AddObservation(bay.Observation);
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        var aout = actionsOut.DiscreteActions;
        aout[0] = Random.Range(0, Parameters.DimZ);
        aout[1] = Random.Range(0, Parameters.DimZ);
    }

    // actions can only be relocation
    public override void OnActionReceived(ActionBuffers actions) {
        int z0 = actions.DiscreteActions[0];
        int z1 = actions.DiscreteActions[1];

        // relocation failed
        if (!bay.relocate(z0, z1)) {
            AddReward(-1);
            Debug.LogWarning($"failed to relocate from {z0} to {z1}");
            nextOperation();
            return;
        }

        // rewarding system




        Debug.Log(bay);
        // relocation success
        nextOperation();
    }

    private void nextOperation() {
        if (bay.empty) EndEpisode();
        if (bay.canRetrieve) {
            bay.retrieve();
            AddReward(1);
        }
        RequestDecision();
    }

}












public struct Container2D {

}

public class Bay {
    private Stack<int>[] bay; //[z,t]
    private int maxTier;
    private int dimZ;
    private int initTier;
    private int maxLabel;

    public int[] BlockingDegrees => bay.Select(s => BlockingDegree(s)).ToArray();

    public bool canRetrieve {
        get {
            var m = min;
            return bay[m.Item2].Peek() == m.Item1;
        }
    }

    public bool empty {
        get {
            foreach (var s in bay) {
                if (s.Count > 0) return false;
            }
            return true;
        }
    }

    public float[,] Observation2D {
        get {
            float[,] res = new float[dimZ, maxTier];
            for (int z = 0; z < dimZ; z++) {
                var list = bay[z].ToList();
                if (list.Count == 0) continue;
                list.Reverse();
                for (int t = 0; t < maxTier; t++) {
                    if (t < list.Count) res[z, t] = list[t] / (float)maxLabel;
                    else continue;
                }
            }
            return res;
        }
    }

    public float[] Observation {
        get {
            var list = new List<float>();
            foreach (var i in Observation2D) {
                list.Add(i);
            }
            return list.ToArray();
        }
    }

    /// <summary>
    /// Item1: value, 
    /// Item2: z-index
    /// </summary>
    public (int, int) min {
        get {
            int m = int.MaxValue;
            int index = 0;
            for (int i = 0; i < bay.Length; i++) {
                int tempMin = bay[i].Count > 0 ? bay[i].Min() : int.MaxValue;
                if (m > tempMin) {
                    m = tempMin;
                    index = i;
                }
            }
            return (m, index);
        }
    }

    public Bay(int z, int t, int _initTier, int _maxLabel) {
        bay = new Stack<int>[z];
        for (int i = 0; i < z; i++) {
            bay[i] = new Stack<int>();
        }
        maxTier = t;
        dimZ = z;
        initTier = _initTier;
        maxLabel = _maxLabel;
        assignValues(generateSequence(_maxLabel));
    }

    // if not relocateable, return false
    public bool relocate(int z0, int z1) {
        if (z0 == z1) return false;
        if (bay[z1].Count == maxTier) return false;
        if (bay[z0].Count == 0) return false;

        bay[z1].Push(bay[z0].Pop());
        return true;
    }

    public bool stack(int z, int v) {
        if (bay[z].Count == maxTier) return false;
        bay[z].Push(v);
        return true;
    }

    public bool retrieve() {
        var m = min;
        if (bay[m.Item1].Peek() != m.Item1) return false;
        bay[m.Item1].Pop();
        return true;
    }

    private int[] generateSequence(int i) {
        System.Random random = new System.Random();
        return Enumerable.Range(1, i).OrderBy(x => random.Next()).ToArray();
    }

    private void assignValues(int[] arr) {
        int i = 0;
        while (i < arr.Length) {
            int z = UnityEngine.Random.Range(0, dimZ);
            if (bay[z].Count >= initTier) continue;
            if (stack(z, arr[i])) i++;
        }
    }

    // peak value of z-index
    public int Peek(int z) {
        return bay[z].Peek();
    }


    // from https://iopscience.iop.org/article/10.1088/1742-6596/1873/1/012050/pdf
    public int BlockingDegree(Stack<int> s) {
        int degree = 0;
        var list = s.ToList();
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
        foreach (var s in bay) {
            var list = s.ToList();
            list.Reverse();
            sb.Append(string.Join(", ", list.ToArray()) + "\n");
        }
        return sb.ToString();
    }
}
