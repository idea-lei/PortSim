using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;


/// <summary>
/// this is the base class of the fields (ioField, stackField)
/// this class should have no unity life circle methods
/// </summary>
public abstract class Field : MonoBehaviour {
    #region public properties
    public Guid Id; //do we really need a id? if we don't save it to db
    public int DimX, DimZ, MaxLayer; // these 3 elements should be set on Awake
    public Stack<Container>[,] Ground {
        get { return _ground; }
    }
    public virtual bool IsGroundEmpty {
        get {
            bool isEmpty = true;
            foreach (var stack in Ground) {
                if (stack.Count > 0) {
                    isEmpty = false;
                    break;
                }
            }
            return isEmpty;
        }
    }
    public bool IsGroundFull {
        get {
            foreach (var stack in Ground) {
                if (stack.Count < Parameters.MaxLayer) {
                    return false;
                }
            }
            return true;
        }
    }
    public int MaxCount => DimX * DimZ * MaxLayer;
    public int Count {
        get {
            int sum = 0;
            foreach (var s in Ground) {
                sum += s.Count;
            }
            return sum;
        }
    }
    #endregion

    #region private / protected properties
    private Stack<Container>[,] _ground;
    [SerializeField] private GameObject containerPrefab;
    private IoFieldsGenerator ioFieldsGenerator;
    #endregion

    #region logic methods

    /// <summary>
    /// 
    /// </summary>
    /// <param name="indicesToAvoid"> this arg is to avoid rearrange to old pos (repeat rearrange)</param>
    /// <returns></returns>
    public IndexInStack FindIndexToStack(HashSet<IndexInStack> indicesToAvoid) {
        var index = new IndexInStack();
        var rnd = new System.Random();
        foreach (int x in Enumerable.Range(0, DimX).OrderBy(_x => rnd.Next())) {
            foreach (int z in Enumerable.Range(0, DimZ).OrderBy(_z => rnd.Next())) {
                if (indicesToAvoid != null && indicesToAvoid.Any(i => i.x == x && i.z == z)) continue;
                if (Ground[x, z].Count < MaxLayer) {
                    index.x = x;
                    index.z = z;
                    index.IsValid = true;
                    return index;
                }
            }
        }
        index.IsValid = false;
        return index;
    }

    public virtual void AddToGround(Container container, IndexInStack index) {
        if (!IsAbleToAddContainerToIndex(index)) {
            SimDebug.LogError(this, "can not add container to index!");
        }
        container.transform.SetParent(transform);
        Ground[index.x, index.z].Push(container);
        container.CurrentField = this;
        container.IndexInCurrentField = new IndexInStack(index.x, index.z);
        container.transform.position = IndexToGlobalPosition(container.IndexInCurrentField);
        if (this is StackField) container.StackedIndices.Add(index);
    }
    /// <summary>
    /// this method will automatically find a index to stack the container
    /// </summary>
    /// <param name="container"></param>
    public virtual void AddToGround(Container container) {
        var index = GlobalPositionToIndex(container.transform.position);
        AddToGround(container, index);
    }

    public virtual Container RemoveFromGround(Guid id) {
        foreach (var s in Ground) {
            if (s.Peek().Id == id) {
                return s.Pop();
            }
        }
        SimDebug.LogError(this, $"can not find container on peek with id: {id}");
        return null;
    }
    public virtual Container RemoveFromGround(Container c) {
        if (Ground[c.IndexInCurrentField.x, c.IndexInCurrentField.z].Peek() == c) {
            return Ground[c.IndexInCurrentField.x, c.IndexInCurrentField.z].Pop();
        }
        SimDebug.LogError(this, "can not remove from ground, the index does not correspond");
        if (this is StackField) {
            foreach (var otherContainer in transform.GetComponents<Container>()) {
                if (otherContainer != c && otherContainer.StackedIndices.Any(i => i == c.IndexInCurrentField)) {
                    otherContainer.StackedIndices.Remove(otherContainer.StackedIndices.Single(i => i == c.IndexInCurrentField));
                }
            }
        }
        return null;
    }

    public bool IsAbleToAddContainerToIndex(int x, int z) {
        if (x >= DimX || z >= DimZ) {
            SimDebug.LogError(this, "dimension exceeds");
            return false;
        }
        if (Ground[x, z].Count + 1 > MaxLayer) {
            SimDebug.LogError(this, "layer exceeds");
            return false;
        }
        return true;
    }

    public bool IsAbleToAddContainerToIndex(IndexInStack index) {
        if (!index.IsValid) {
            SimDebug.LogError(this, "not valid");
            return false;
        }
        return IsAbleToAddContainerToIndex(index.x, index.z);
    }

    // this method is for outField and tempField stack
    public IndexInStack NearestStackableIndex(Vector3 cranePos) {
        /// <summary>
        /// from s (selected start point) traverse the 0 - dim list
        /// </summary>
        List<int> traverse(int s, int dim) {
            var oList = Enumerable.Range(0, dim).ToList();
            var rList = new List<int>();
            while (oList.Count > 0) {
                rList.Add(oList[s]);
                oList.RemoveAt(s);
                if (oList.Count > s) {
                    rList.Add(oList[s]);
                    oList.RemoveAt(s);
                }
                if (s > 0) s--;
            }
            return rList;
        }

        //findout z index which is shorter 0 or dimZ-1
        var zSelect = 0;
        if (DimZ > 1) {
            var z0 = IndexToGlobalPosition(0, 0);
            var zMax = IndexToGlobalPosition(0, DimZ - 1);

            float distanceZ0 = Vector2.SqrMagnitude(new Vector2(z0.x - cranePos.x, z0.z - cranePos.z));
            float distanceZMax = Vector2.SqrMagnitude(new Vector2(zMax.x - cranePos.x, zMax.z - cranePos.z));
            zSelect = distanceZ0 > distanceZMax ? DimZ - 1 : 0;
        }
        int zDirection = zSelect > 0 ? -1 : 1;

        // findout shortest x
        int xSelect = 0;
        float minDis = float.MaxValue;
        for (int x = 0; x < DimX; x++) {
            var pos = IndexToGlobalPosition(x, zSelect);
            float dis = Vector2.SqrMagnitude(new Vector2(pos.x - cranePos.x, pos.z - cranePos.z));
            if (minDis > dis) {
                xSelect = x;
                minDis = dis;
            } else break;
        }

        var xList = traverse(xSelect, DimX);
        //Debug.LogWarning($"x:{xSelect}, z:{zSelect}, dimX:{DimX}\n" +
        //    $"{string.Join(", ", xList)}");
        foreach (int x in xList) {
            for (int z = zSelect; z >= 0 && z < DimZ; z += zDirection) {
                var idx = new IndexInStack(x, z);
                if (Ground[x, z].Count < MaxLayer)
                    return idx;
            }
        }
        return new IndexInStack(false);
    }

    public IndexInStack StackableIndex(HashSet<IndexInStack> indicesToAvoid) {
        for (int x = 0; x < DimX; x++) {
            for (int z = 0; z < DimZ; z++) {
                var idx = new IndexInStack(x, z);
                if (Ground[x, z].Count < MaxLayer && !indicesToAvoid.Any(i => i == idx))
                    return idx;
            }
        }
        return new IndexInStack(false);
    }
    public IndexInStack StackableIndex(IndexInStack indexToAvoid) {
        return StackableIndex(new HashSet<IndexInStack> { indexToAvoid });
    }
    public IndexInStack StackableIndex() {
        return StackableIndex(null);
    }
    #endregion

    #region Tranformation between Index and Position
    /// <summary>
    /// calculate global position first, do not use localPosition, otherwise will face the scale problem!
    /// </summary>
    /// <param name="index"></param>
    /// <returns> global coordinate</returns>
    public Vector3 IndexToGlobalPosition(IndexInStack index) {
        return transform.position + IndexToLocalPositionInWorldScale(index);
    }
    public Vector3 IndexToGlobalPosition(int x, int z) {
        return IndexToGlobalPosition(new IndexInStack(x, z));
    }
    public IndexInStack GlobalPositionToIndex(Vector3 vec) {
        var localPos = vec - transform.position;
        return LocalPositionInWorldScaleToIndex(localPos);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns>local position of the index of the specific layer</returns>
    public Vector3 IndexToLocalPositionInWorldScale(int x, int z, int count) {
        float coord_x = Parameters.Gap_Container + (Parameters.ContainerLength_Long - transform.localScale.x * 10) / 2f // x=0
            + x * (Parameters.ContainerLength_Long + Parameters.Gap_Container);   // x_th container
        float coord_y = count * Parameters.ContainerHeight - Parameters.ContainerHeight / 2f;
        float coord_z = Parameters.Gap_Container + (Parameters.ContainerWidth - transform.localScale.z * 10) / 2f // z=0
            + z * (Parameters.ContainerWidth + Parameters.Gap_Container); // z_th container
        return new Vector3(coord_x, coord_y, coord_z);
    }
    public Vector3 IndexToLocalPositionInWorldScale(IndexInStack index) {
        return IndexToLocalPositionInWorldScale(index.x, index.z, Ground[index.x, index.z].Count);
    }
    public IndexInStack LocalPositionInWorldScaleToIndex(Vector3 vec) {
        float x = (vec.x - (Parameters.Gap_Container + (Parameters.ContainerLength_Long - transform.localScale.x * 10) / 2f)) / (Parameters.ContainerLength_Long + Parameters.Gap_Container);
        float z = (vec.z - (Parameters.Gap_Container + (Parameters.ContainerWidth - transform.localScale.z * 10) / 2f)) / (Parameters.ContainerWidth + Parameters.Gap_Container);
        int x_round = Mathf.RoundToInt(x);
        int z_round = Mathf.RoundToInt(z);
        return new IndexInStack(x_round, z_round);
    }
    #endregion

    #region public minor methods
    public override string ToString() {
        var str = new StringBuilder();
        for (int x = 0; x < DimX; x++) {
            for (int z = 0; z < DimZ; z++) {
                str.Append(Ground[x, z].Count + " ");
            }
            str.Append("\n");
        }
        return str.ToString();
    }
    #endregion

    #region private methods
    protected virtual void initField(IoFieldsGenerator generator) {
        // because the plane scale 1 means 10m
        transform.localScale = new Vector3(
            (DimX * (Parameters.ContainerLength_Long + Parameters.Gap_Container) + Parameters.Gap_Container) / 10f,
            0.00001f,
            (DimZ * (Parameters.ContainerWidth + Parameters.Gap_Container) + Parameters.Gap_Container) / 10f);
        ioFieldsGenerator = generator;
        Id = Guid.NewGuid();
        _ground = new Stack<Container>[DimX, DimZ];
        for (int x = 0; x < DimX; x++) {
            for (int z = 0; z < DimZ; z++) {
                Ground[x, z] = new Stack<Container>();
            }
        }
    }

    protected virtual Container generateContainer(Vector3 initPos) {
        var model = Instantiate(containerPrefab);
        model.transform.position = initPos + transform.position;
        var (r, g, b) = genRGB();
        model.GetComponent<MeshRenderer>().material.color = new Color(r, g, b);
        model.name = "Container_" + DateTime.Now.ToString("G");
        model.transform.parent = transform;

        var container = model.GetComponent<Container>();
        container.Id = Guid.NewGuid();
        container.Weight = UnityEngine.Random.Range(1, Parameters.MaxContainerWeight + 1);

        return container;
    }

    /// <summary>
    /// this method assign the outFields of the containers,
    /// will generate outField if not exist
    /// </summary>
    protected virtual void assignOutField(Container container, DateTime initTime) {
        if (UnityEngine.Random.Range(0, 1f) > Parameters.PossibilityOfNewOutField) {
            OutField[] outFields = ioFieldsGenerator.GetComponentInParent<ObjectCollection>().GetComponentsInChildren<OutField>();

            if (outFields.Length > 0) {
                var index = UnityEngine.Random.Range(0, outFields.Length);
                if (!outFields[index].GroundFullPlaned && outFields[index].TimePlaned > initTime) {
                    outFields[index].AddContainerToList(container);
                    container.OutField = outFields[index];
                    return;
                }
            }
        }
        var field = ioFieldsGenerator.GenerateOutField();
        field.AddContainerToList(container);
        container.OutField = field;
    }

    private (float, float, float) genRGB() {
        float r = UnityEngine.Random.Range(0f, 1f);
        float g = UnityEngine.Random.Range(0f, 1f);
        float b = UnityEngine.Random.Range(0f, 1f);
        return (r, g, b);
    }
    #endregion
}
