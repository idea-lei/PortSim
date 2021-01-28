using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// this is the base class of the fields (ioField, stackField)
/// </summary>
public abstract class Field : MonoBehaviour {
    #region properties
    public Guid Id;
    public int DimX;
    public int DimZ;
    public int MaxLayer;
    private Stack<Container>[,] _ground;
    public Stack<Container>[,] Ground {
        get { return _ground; }
    }
    public bool IsGroundEmpty {
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
    #endregion

    #region logic methods
    public (Container, IndexInStack) FindContainerWithIndex(Guid id) {
        throw new NotImplementedException();
    }

    public virtual void AddToGround(Container container, IndexInStack index) {
        if (!IsAbleToAddContainerToIndex(index)) {
            throw new Exception("can not add container to index!");
        }
        container.tag = "container_stacked";
        container.transform.SetParent(transform);
        Ground[index.x, index.z].Push(container);
    }

    public virtual Container RemoveFromGround(Guid id) {
        throw new NotImplementedException();
    }

    public virtual Container RemoveFromGround(Container c_wanted) {
        var index = CoordinateToIndex(c_wanted.transform.localPosition);
        if(Ground[index.x, index.z].Peek() == c_wanted) {
            Ground[index.x, index.z].Pop();
        }

        c_wanted.tag = "container_out";
        return c_wanted;
    }

    public virtual void RearrangeContainer(Container container) {
        throw new NotImplementedException();
    }

    /// <summary>
    /// to init containers for inField and stackField
    /// </summary>
    public virtual void InitContainers() {
        throw new NotImplementedException();
    }

    /// <summary>
    /// this function is to destory the field and containers belongs to it
    /// </summary>
    public void DestroyField() {
        Destroy(gameObject);
    }

    public bool IsAbleToAddContainerToIndex(int x, int z) {
        if (x >= DimX || z >= DimZ) return false;
        if (Ground[x, z].Count + 1 > MaxLayer) return false;
        return true;
    }
    public bool IsAbleToAddContainerToIndex(IndexInStack index) {
        if (!index.IsValid) return false;
        return IsAbleToAddContainerToIndex(index.x, index.z);
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
        throw new NotImplementedException();
    }
    #endregion

    #region private methods
    private void generateContainer() {

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
                        x=x,
                        z=z
                    };
                }
            }
        }
        return new IndexInStack();
    }
    #endregion
}
