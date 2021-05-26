using Ilumisoft.VisualStateMachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class StackBehavior : Agent {

    // this private class is for buffersensor
    private class ObservationObject {
        public IndexInStack index; //this one is not a observation variable
        public int indexInList;
        public DateTime outTime;
        public int weight;
        public float energy;
        public int layer;
        public bool isIndexNeedRearrange;
        public bool isStacked;

        // n means normalized
        public float n_outTime;
        public float n_weight;
        public float n_energy;
        public float n_layer;
        public float n_isIndexNeedRearrange;
        public float n_isStacked;
    }

    private Crane crane;
    private StackField stackField;
    private StateMachine stateMachine;
    private BufferSensorComponent bufferSensor;

    private List<ObservationObject> obList = new List<ObservationObject>();

    private readonly int listLength = Parameters.DimX * Parameters.DimZ;

    // reward coefficients
    private const float c_outOfRange = 1f;
    private const float c_full = 1f;
    private const float c_stacked = 1f;

    private const float c_time = 0.4f;
    private const float c_rearrange = 0.4f;
    private const float c_layer = 0.2f;
    private const float c_energy = 0.1f;
    private const float c_weight = 0.05f;

    private static List<float> heuristicRewards = new List<float>();
    private int invalidTimes = 0;


    private void Start() {
        crane = GetComponentInParent<ObjectCollection>().Crane;
        stackField = GetComponent<StackField>();
        stateMachine = crane.GetComponent<StateMachine>();
        bufferSensor = GetComponent<BufferSensorComponent>();
        Initialize();
    }

    /// <summary>
    /// for vector sensor:
    /// 1. ContainerCarrying: outTime and weight -> 2
    /// 2. amount of available indices -> 1
    /// 
    /// for buffer sensor: (each available index)
    /// 0. whether this index is selected -> dimX * dimZ
    /// 1. peek container outTime -> 1
    /// 2. peek container weight -> 1
    /// 3. Energy Cost (kind of distance) (containerCarrying to index) -> 1
    /// 4. index need rearrange -> 1
    /// 5. current layer of the index -> 1
    /// </summary>
    public override void CollectObservations(VectorSensor sensor) {
        obList.Clear();
        if (crane.ContainerCarrying == null) {
            SimDebug.LogError(this, "container carrying is null!");
            EndEpisode();
            return;
        }

        // add observation to list
        int idx = 0;
        for (int x = 0; x < stackField.DimX; x++) {
            for (int z = 0; z < stackField.DimZ; z++) {
                //// index is available if it is not full and not stacked
                if (stackField.IsIndexFull(x, z)) continue;
                if (crane.ContainerCarrying.StackedIndices.Any(i => i.x == x && i.z == z)) continue;

                var ob = new ObservationObject {
                    index = new IndexInStack(x, z),
                    indexInList = idx++,
                    isStacked = crane.ContainerCarrying.StackedIndices.Any(i => i.x == x && i.z == z)
                };

                if (stackField.Ground[x, z].Count > 0) {
                    ob.layer = stackField.Ground[x, z].Count;
                    ob.isIndexNeedRearrange = stackField.IsStackNeedRearrange(stackField.Ground[x, z]);
                    ob.weight = stackField.Ground[x, z].Peek().Weight;
                    ob.outTime = stackField.Ground[x, z].Peek().OutField.TimePlaned;

                    var vec = crane.ContainerCarrying.transform.position - stackField.Ground[x, z].Peek().transform.position;
                    ob.energy = Mathf.Abs(vec.x) * Parameters.Ex + Mathf.Abs(vec.z) * Parameters.Ez;
                } else {
                    ob.layer = 0;
                    ob.isIndexNeedRearrange = false;
                    ob.weight = Parameters.MaxContainerWeight;
                    ob.outTime = DateTime.Now + TimeSpan.FromDays(1);

                    var vec = crane.ContainerCarrying.transform.position - stackField.IndexToGlobalPosition(x, z);
                    ob.energy = Mathf.Abs(vec.x) * Parameters.Ex + Mathf.Abs(vec.z) * Parameters.Ez;
                }
                obList.Add(ob);
            }
        }

        if (obList.Count == 0) {
            SimDebug.LogError(this, "obList is empty");
        }

        //normalize
        var times = obList.Select(o => o.outTime).ToList();
        times.Add(crane.ContainerCarrying.OutField.TimePlaned);
        var maxTime = times.Max();
        var minTime = times.Min();

        var distances = obList.Select(o => o.energy).ToList();
        float minSqrDistance = distances.Min();
        float maxSqrDistance = distances.Max();
        float diffDistance = maxSqrDistance - minSqrDistance;

        sensor.AddObservation((float)obList.Count / (Parameters.DimX * Parameters.DimZ));
        sensor.AddObservation(Mathf.InverseLerp(minTime.Second, maxTime.Second, crane.ContainerCarrying.OutField.TimePlaned.Second));
        sensor.AddObservation((float)crane.ContainerCarrying.Weight / Parameters.MaxContainerWeight);

        foreach (var o in obList) {
            o.n_outTime = Mathf.InverseLerp(minTime.Second, maxTime.Second, o.outTime.Second);
            o.n_weight = (float)o.weight / Parameters.MaxContainerWeight;
            o.n_energy = Mathf.InverseLerp(minSqrDistance, maxSqrDistance, o.energy);
            o.n_layer = (float)o.layer / Parameters.MaxLayer;
            o.n_isIndexNeedRearrange = o.isIndexNeedRearrange ? 1 : -1;

            if (o.n_outTime < 0 || o.n_outTime > 1) SimDebug.LogError(this, "outTime Lerp out of range");
            if (o.n_energy < 0 || o.n_energy > 1) SimDebug.LogError(this, "n_energy Lerp out of range");

            var indicies = new float[listLength];
            indicies[o.indexInList] = 1;
            float[] buffer = new float[] { o.n_outTime, o.n_weight, o.n_energy, o.n_layer, o.n_layer >= 1 ? 0 : 1, o.n_isIndexNeedRearrange };

            bufferSensor.AppendObservation(indicies.Concat(buffer).ToArray());
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        var continuousActionsOut = actionsOut.DiscreteActions;

        var rewardList = new List<float>();
        for (int i = 0; i < obList.Count; i++) {
            float reward = 0;

            // 1. energy reward
            reward = (1 - obList[i].n_energy) * c_energy;

            // 2. needRearrange reward
            if (obList[i].isIndexNeedRearrange) {
                // if not all indices need rearrange
                if (!obList.All(o => o.isIndexNeedRearrange)) reward -= c_rearrange;
            } else reward += c_rearrange;

            // 3. time reward
            var times = obList.Select(o => o.outTime).ToList();
            times.Add(crane.ContainerCarrying.OutField.TimePlaned);
            var maxTime = times.Max();
            var minTime = times.Min();

            if (crane.ContainerCarrying.OutField.TimePlaned < obList[i].outTime) {
                reward += (1 - Mathf.InverseLerp(minTime.Second, maxTime.Second, crane.ContainerCarrying.OutField.TimePlaned.Second)) * c_time;
            } else {
                if (!obList.All(o => crane.ContainerCarrying.OutField.TimePlaned > o.outTime))
                    reward -= c_time;
            }

            // 4. layer reward
            reward += (1 - obList[i].n_layer) * c_layer;

            // 5. weight reward
            if (crane.ContainerCarrying.Weight <= obList[i].weight) {
                reward += crane.ContainerCarrying.Weight / obList[i].weight * c_weight;
            } else {
                if (!obList.All(o => crane.ContainerCarrying.Weight >= o.weight)) reward -= c_weight;
            }

            rewardList.Add(reward);
        }

        var strBuilder = new StringBuilder();
        int ii = 0;
        foreach (var r in rewardList) {
            strBuilder.Append($"{ii++} reward: {r}\n");
        }

        float max = rewardList.Max();
        heuristicRewards.Add(max);
        strBuilder.Append($"mean reward = {heuristicRewards.Average()}");
        Debug.Log(strBuilder.ToString());
        continuousActionsOut[0] = rewardList.IndexOf(max);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        int result = actions.DiscreteActions[0];

        if (result >= obList.Count) {
            if (obList.Count == 0) {
                // this means no available index, which should be determined before request decision, so error
                SimDebug.LogError(this, "no stackable index");
                return;
            }
            AddReward(c_outOfRange * (++invalidTimes));
            Debug.LogWarning("out of range, request new decision");
            RequestDecision();
            //SimDebug.LogError(this, "outOfRange");
            return;
        }

        invalidTimes = 0;

        if (obList[result].n_layer >= 1) {
            AddReward(c_full);
            SimDebug.LogError(this, "full");
            return;
        }

        if (obList[result].isStacked) {
            AddReward(c_stacked);
            SimDebug.LogError(this, "stacked");
            return;
        }

        var strBuilder = new StringBuilder($"available indices amount: {obList.Count}\n" +
            $"result is: {result} -- {obList[result].index}\n");

        float reward = 0;

        // 1. energy reward
        reward = (1 - obList[result].n_energy) * c_energy;

        // 2. needRearrange reward
        if (obList[result].isIndexNeedRearrange) {
            // if not all indices need rearrange
            if (!obList.All(o => o.isIndexNeedRearrange)) {
                strBuilder.Append("index need rearrange");
                reward -= c_rearrange;
            }
        } else reward += c_rearrange;

        // 3. time reward
        var times = obList.Select(o => o.outTime).ToList();
        times.Add(crane.ContainerCarrying.OutField.TimePlaned);
        var maxTime = times.Max();
        var minTime = times.Min();

        if (crane.ContainerCarrying.OutField.TimePlaned < obList[result].outTime) {
            reward += (1 - Mathf.InverseLerp(minTime.Second, maxTime.Second, crane.ContainerCarrying.OutField.TimePlaned.Second)) * c_time;
        } else {
            if (!obList.All(o => crane.ContainerCarrying.OutField.TimePlaned > o.outTime)) {
                reward -= c_time;
                strBuilder.Append($"carrying is later than peek\n");
            }

        }

        // 4. layer reward
        reward += (1 - obList[result].n_layer) * c_layer;

        // 5. weight reward
        if (crane.ContainerCarrying.Weight <= obList[result].weight) {
            reward += crane.ContainerCarrying.Weight / obList[result].weight * c_weight;
        } else {
            if (!obList.All(o => crane.ContainerCarrying.Weight >= o.weight)) {
                reward -= c_weight;
                strBuilder.Append($"carrying is heavier than peek\n");
            }
        }

        Debug.Log(strBuilder.ToString());

        AddReward(reward);
        EndEpisode();
        stackField.TrainingResult = obList[result].index;
        stateMachine.TriggerByState(crane.ContainerCarrying.CompareTag("container_in") || crane.ContainerCarrying.CompareTag("container_temp") ? "MoveIn" : "Rearrange");
    }
}


// obsolete collectObservation method
///// <remark>
///// 1. OutTimes: a ContainerCarrying.OutTime, b StackField.PeekOutTime(time.now + 1 day if peek is null) --dimX*dimZ+1
///// 2. Distance between containerCarrying and all the peeks --dimX*dimZ
///// 3. layers of all the the stacks --dimX*dimZ  (currently comment out!)
///// 4. isStackFull --dimX*dimZ (redundent with 3)
///// 5. stacks need rearrange --dimX*dimZ
///// 6. ContainerCarrying.currentIndex (only for rearrange, to avoid stack onto same index) --2
///// 7. IsRearrange Process (represented by (-1,-1) of 6.) --0
///// 8. stackedIndex --dimX*dimZ
///// total: dimX * dimZ * 5 + 3
///// </remark>
//public override void CollectObservations(VectorSensor sensor) {
//    if (crane.ContainerCarrying == null) {
//        SimDebug.LogError(this, "container carrying is null!");
//        EndEpisode();
//        return;
//    }

//    var times = new List<DateTime>();
//    var distances = new List<float>();
//    var layers = new List<float>();
//    var indexFullList = new List<bool>();
//    var needRearrangeList = new List<bool>();
//    var isStackedList = new List<bool>();

//    // 1. 
//    times.Add(crane.ContainerCarrying.OutField.TimePlaned);

//    for (int x = 0; x < stackField.DimX; x++) {
//        for (int z = 0; z < stackField.DimZ; z++) {
//            layers.Add(stackField.Ground[x, z].Count / (float)stackField.MaxLayer); // 3
//            needRearrangeList.Add(stackField.IsStackNeedRearrange(stackField.Ground[x, z]));
//            indexFullList.Add(stackField.Ground[x, z].Count == stackField.MaxLayer);
//            isStackedList.Add(crane.ContainerCarrying.StackedIndices.Contains(new IndexInStack(x, z)));

//            if (stackField.Ground[x, z].Count > 0) {
//                times.Add(stackField.Ground[x, z].Peek().OutField.TimePlaned);
//                distances.Add(Vector3.SqrMagnitude(crane.ContainerCarrying.transform.position - stackField.Ground[x, z].Peek().transform.position));
//            } else {
//                times.Add(DateTime.Now + TimeSpan.FromDays(1));
//                distances.Add(Vector3.SqrMagnitude(crane.ContainerCarrying.transform.position - stackField.IndexToGlobalPosition(x, z)));
//            }
//        }
//    }

//    var maxTime = times.Max();
//    var minTime = times.Min();
//    float diffTime = (float)(maxTime - minTime).TotalSeconds;

//    minSqrDistance = distances.Min();
//    maxSqrDistance = distances.Max();

//    foreach (var t in times) sensor.AddObservation(Mathf.InverseLerp(0, diffTime, (float)(t - minTime).TotalSeconds)); // 1
//    foreach (var d in distances) sensor.AddObservation(Mathf.InverseLerp(minSqrDistance, maxSqrDistance, d)); // 2
//                                                                                                              //foreach (var l in layers) sensor.AddObservation(l); // 3
//    foreach (var i in indexFullList) sensor.AddObservation(i); //4
//    foreach (var n in needRearrangeList) sensor.AddObservation(n); //5
//    foreach (var i in isStackedList) sensor.AddObservation(i); // 8
//    IndexInStack index = crane.ContainerCarrying.IndexInCurrentField;
//    if (!(crane.ContainerCarrying.CurrentField is StackField)) { //rearrange
//        index = new IndexInStack(-1, -1);
//    }
//    sensor.AddObservation(index.x);
//    sensor.AddObservation(index.z);
//}


//public override void Heuristic(in ActionBuffers actionsOut) {
//    var result = stackField.FindIndexToStack();
//    var continuousActionsOut = actionsOut.DiscreteActions;
//    continuousActionsOut[0] = result.IsValid ? 1 : 0;
//    continuousActionsOut[1] = result.x;
//    continuousActionsOut[2] = result.z;
//}

///// <param name="idx">the training result</param>
//private bool handleResult(IndexInStack idx) {
//    var resOldMethod = stackField.FindIndexToStack();

//    // 1. if the training result isValid is false
//    if (!idx.IsValid) {
//        bool same = resOldMethod.IsValid == idx.IsValid;
//        AddReward(same ? 1 : -1);
//        return same;
//    }

//    float reward = 0;

//    // from here, this isValid is true

//    //time difference reward
//    if (stackField.Ground[idx.x, idx.z].Count > 0) {
//        float d = (float)(stackField.Ground[idx.x, idx.z].Peek().OutField.TimePlaned
//    - crane.ContainerCarrying.OutField.TimePlaned).TotalMinutes;
//        reward += 1 / (Mathf.Exp(-d) + 1) - 1; // scaled sigmoid funciton (-0.5,0.5)
//    } else reward += 1;

//    //distance reward (0,1)


//    // layer reward
//    reward += 1 - stackField.Ground[idx.x, idx.z].Count / (float)stackField.MaxLayer;

//    if (stackField.IsStackNeedRearrange(stackField.Ground[idx.x, idx.z])) {
//        reward -= 0.1f;
//    }

//    // if the target is already full
//    if (stackField.IsIndexFull(idx)) {
//        Debug.LogWarning("already full!");
//        AddReward(-2);
//        return false; // need to redecide
//    }

//    // the result is already stacked
//    if (crane.ContainerCarrying.StackedIndices.Contains(idx)) {
//        Debug.LogWarning("already stacked!");
//        reward -= 1f;
//    }
//    AddReward(reward);

//    return true;
//}

//public override void OnActionReceived(ActionBuffers actions) {
//    IndexInStack idx = new IndexInStack();
//    idx.IsValid = actions.DiscreteActions[0] > 0;
//    idx.x = actions.DiscreteActions[1];
//    idx.z = actions.DiscreteActions[2];
//    if (handleResult(idx)) {
//        stackField.TrainingResult = idx;
//        stateMachine.TriggerByState(crane.ContainerCarrying.CompareTag("container_in") || crane.ContainerCarrying.CompareTag("container_temp") ? "MoveIn" : "Rearrange");
//    } else RequestDecision();
//}