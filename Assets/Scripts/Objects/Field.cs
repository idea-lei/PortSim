using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// this is the base class of the fields (ioField, stackField)
/// this class should have no unity life circle methods
/// </summary>
public abstract class Field : MonoBehaviour {
    #region public properties
    public Guid Id;

    // the fllowing 3 elements should be set on Awake
    public int DimX, DimZ, MaxLayer;

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
    [SerializeField]
    private GameObject containerPrefab;
    private IoFieldsGenerator ioFieldsGenerator;
    #endregion

    #region logic methods

    /// <summary>
    /// 
    /// </summary>
    /// <param name="crane"> need crane position to avoid same index</param>
    /// <returns></returns>
    public virtual IndexInStack FindAvailableIndexToStack(Crane crane) {
        var index = new IndexInStack();
        for (int x = 0; x < DimX; x++) {
            for (int z = 0; z < DimZ; z++) {
                var indexOfCrane = GlobalPositionToIndex(crane.transform.position);
                bool resultEqualToCraneIndex = indexOfCrane.x == x && indexOfCrane.z == z;
                if (Ground[x, z].Count < MaxLayer && !resultEqualToCraneIndex) {
                    index.x = x;
                    index.z = z;
                    index.IsValid = true;
                    return index;
                }
            }
        }
        return index;
    }

    public virtual void AddToGround(Container container, IndexInStack index) {
        if (!IsAbleToAddContainerToIndex(index)) {
            throw new Exception("can not add container to index!");
        }
        //container.tag = "container_stacked";
        container.transform.SetParent(transform);
        Ground[index.x, index.z].Push(container);
        container.indexInCurrentField = new IndexInStack(index.x, index.z);
        container.transform.position = IndexToGlobalPosition(container.indexInCurrentField);
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
        throw new NotImplementedException();
    }

    public virtual Container RemoveFromGround(Container c) {
        if (Ground[c.indexInCurrentField.x, c.indexInCurrentField.z].Peek() == c) {
            return Ground[c.indexInCurrentField.x, c.indexInCurrentField.z].Pop();
        }

        throw new Exception("can not remove from ground");
    }

    /// <summary>
    /// this function is to destory the field and containers belongs to it
    /// </summary>
    public virtual void DestroyField() {
        Destroy(gameObject, Parameters.EventDelay);
    }

    public bool IsAbleToAddContainerToIndex(int x, int z) {
        if (x >= DimX || z >= DimZ) {
            Debug.LogError("dimension exceeds");
            return false;
        }
        if (Ground[x, z].Count + 1 > MaxLayer) {
            Debug.LogError("layer exceeds");
            return false;
        }
        return true;
    }
    public bool IsAbleToAddContainerToIndex(IndexInStack index) {
        if (!index.IsValid) {
            Debug.LogError("not valid");
            return false;
        }
        return IsAbleToAddContainerToIndex(index.x, index.z);
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
        var index = new IndexInStack(
            Mathf.RoundToInt((vec.x - (Parameters.Gap_Container + (Parameters.ContainerLength_Long - transform.localScale.x * 10) / 2f)) / (Parameters.ContainerLength_Long + Parameters.Gap_Container)),
            Mathf.RoundToInt((vec.z - (Parameters.Gap_Container + (Parameters.ContainerWidth - transform.localScale.z * 10) / 2f)) / (Parameters.ContainerWidth + Parameters.Gap_Container))
            );

        return index;
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
    protected virtual void initField() {
        // because the plane scale 1 means 10m
        transform.localScale = new Vector3(
            (DimX * (Parameters.ContainerLength_Long + Parameters.Gap_Container) + Parameters.Gap_Container) / 10f,
            0.00001f,
            (DimZ * (Parameters.ContainerWidth + Parameters.Gap_Container) + Parameters.Gap_Container) / 10f);

        ioFieldsGenerator = FindObjectOfType<IoFieldsGenerator>();
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
        model.name = "Container-" + DateTime.Now.ToString("T");
        model.transform.parent = transform;

        var container = model.GetComponent<Container>();
        container.Id = Guid.NewGuid();

        return container;
    }

    /// <summary>
    /// this method assign the outFields of the containers,
    /// will generate outField if not exist
    /// </summary>
    protected virtual void assignOutPort(Container container, DateTime initTime) {
        if (UnityEngine.Random.Range(0, 1f) > Parameters.PossibilityOfNewOutField) {
            var outFields = FindObjectsOfType<OutField>();
            if (outFields.Length > 0) {
                var index = UnityEngine.Random.Range(0, outFields.Length);
                if (!outFields[index].GroundFullPlaned && outFields[index].TimePlaned > initTime) {
                    outFields[index].incomingContainers.Add(container);
                    container.OutField = outFields[index];
                    return;
                }
            }
        }
        var field = ioFieldsGenerator.GenerateOutField();
        field.incomingContainers.Add(container);
        container.OutField = field;
    }

    private (float, float, float) genRGB() {
        float r = UnityEngine.Random.Range(0f, 1f);
        float g = UnityEngine.Random.Range(0f, 1f);
        float b = UnityEngine.Random.Range(0f, 1f);
        return (r, g, b);
    }

    // todo: this function need to be trained for stackField
    /// <summary>
    /// this function could vary between stackField and outField
    /// </summary>
    /// <returns></returns>
    protected virtual IndexInStack findIndexToStack() {
        for (int x = 0; x < DimX; x++) {
            for (int z = 0; z < DimZ; z++) {
                if (IsAbleToAddContainerToIndex(x, z)) {
                    return new IndexInStack {
                        IsValid = true,
                        x = x,
                        z = z
                    };
                }
            }
        }
        return new IndexInStack();
    }
    #endregion
}
