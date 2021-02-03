using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// this is the base class of the fields (ioField, stackField)
/// this class should have no unity life circle methods
/// </summary>
public abstract class Field : MonoBehaviour {
    #region public properties
    public Guid Id;

    // the fllowing 3 elements should be set on Awake
    public int DimX;
    public int DimZ;
    public int MaxLayer;

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
    public virtual bool IsGroundFull {
        get {
            bool isFull = true;
            foreach (var stack in Ground) {
                if (stack.Count < Parameters.MaxLayer) {
                    isFull = false;
                    break;
                }
            }
            return isFull;
        }
    }
    public int MaxCount => DimX * DimZ * MaxLayer;

    public int Count {
        get {
            int sum = 0;
            foreach(var s in Ground) {
                if (s == null) break;
                sum += s.Count;
            }
            return sum;
        }
    }
    #endregion

    #region private / protected properties
    private Stack<Container>[,] _ground;
    [SerializeField]
    protected GameObject containerPrefab;
    #endregion

    #region logic methods
    public (Container, IndexInStack) FindContainerWithIndex(Guid id) {
        throw new NotImplementedException();
    }

    public virtual void AddToGround(Container container, IndexInStack index) {
        if (!IsAbleToAddContainerToIndex(index)) {
            throw new Exception("can not add container to index!");
        }
        //container.tag = "container_stacked";
        container.transform.SetParent(transform);
        Ground[index.x, index.z].Push(container);
    }

    /// <summary>
    /// this method will automatically find a index to stack the container
    /// </summary>
    /// <param name="container"></param>
    public virtual void AddToGround(Container container) {
        var index = findIndexToStack();
        AddToGround(container, index);
    }

    public virtual Container RemoveFromGround(Guid id) {
        throw new NotImplementedException();
    }

    public virtual Container RemoveFromGround(Container c_wanted) {
        var index = CoordinateToIndex(c_wanted.transform.localPosition);
        if (Ground[index.x, index.z].Peek() == c_wanted) {
            Ground[index.x, index.z].Pop();
        }

        c_wanted.tag = "container_out";
        return c_wanted;
    }

    public virtual void RearrangeContainer(Container container) {
        throw new NotImplementedException();
    }

    /// <summary>
    /// this function is to destory the field and containers belongs to it
    /// </summary>
    public void DestroyField() {
        Destroy(gameObject);
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

    /// <summary>
    /// 
    /// </summary>
    /// <returns>local position of the index</returns>
    public Vector3 IndexToLocalPosition(int x, int z, int layer) {
        float coord_x = Parameters.Gap_Container + (Parameters.ContainerLength_Long - transform.localScale.x * 10) / 2f // x=0
            + x * (Parameters.ContainerLength_Long + Parameters.Gap_Container);   // x_th container
        float coord_y = Parameters.ContainerHeight / 2f + layer * Parameters.ContainerHeight;
        float coord_z = Parameters.Gap_Container + (Parameters.ContainerWidth - transform.localScale.z * 10) / 2f // z=0
            + z * (Parameters.ContainerWidth + Parameters.Gap_Container); // z_th container
        return new Vector3(coord_x, coord_y, coord_z);
    }
    #endregion

    #region public minor methods
    public Vector3 IndexToCoordinate(IndexInStack index) {
        throw new NotImplementedException();
    }

    public IndexInStack CoordinateToIndex(Vector3 vec) {
        throw new NotImplementedException();
    }

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
        Id = Guid.NewGuid();
        _ground = new Stack<Container>[DimX, DimZ];
        // because the plane scale 1 means 10m
        transform.localScale = new Vector3(
            (DimX * (Parameters.ContainerLength_Long + Parameters.Gap_Container) + Parameters.Gap_Container) / 10f,
            0.00001f,
            (DimZ * (Parameters.ContainerWidth + Parameters.Gap_Container) + Parameters.Gap_Container) / 10f);
    }

    /// <summary>
    /// to generate containers for field
    /// </summary>
    protected virtual void initContainers() {
        for (int x = 0; x < DimX; x++) {
            for (int z = 0; z < DimZ; z++) {
                Ground[x, z] = new Stack<Container>();
                for (int k = 0; k <= UnityEngine.Random.Range(0, MaxLayer); k++) {
                    var pos = IndexToLocalPosition(x, z, Ground[x, z].Count());
                    var container = generateContainer(pos);
                    AddToGround(container, new IndexInStack(x, z));
                }
            }
        }
    }

    protected virtual Container generateContainer(Vector3 initPos) {
        var model = Instantiate(containerPrefab);
        model.transform.localPosition = initPos;
        var (r, g, b) = genRGB();
        model.GetComponent<MeshRenderer>().material.color = new Color(r, g, b);
        model.name = "Container-" + DateTime.Now.ToString("T");
        model.transform.parent = transform;

        var container = model.GetComponent<Container>();
        container.Id = Guid.NewGuid();

        return container;
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
