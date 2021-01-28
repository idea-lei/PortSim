using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// this is the base class of the fields (ioField, stackField)
/// </summary>
public abstract class Field : MonoBehaviour
{
    #region properties
    public Guid Id;
    public int DimX;
    public int DimZ;
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
    public virtual void AddToGround(Container container) {
        throw new NotImplementedException();
    }

    public virtual Container RemoveFromGround(Container container) {
        throw new NotImplementedException();
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
        
    }
    #endregion

    #region public minor methods
    public Vector3 IndexToCoordinate() {
        throw new NotImplementedException();
    }

    public override string ToString() {
        throw new NotImplementedException();
    }
    #endregion

    #region private methods
    private void generateContainer() {

    }

    private bool dimCheck(IndexInStack index) {
        throw new NotImplementedException();
    }
    #endregion
}
