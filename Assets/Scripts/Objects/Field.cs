using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// this is the base class of the fields (ioField, stackField)
/// </summary>
public abstract class Field : MonoBehaviour
{
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

    #region public methods
    public void AddToGround(Container container) {
        throw new NotImplementedException();
    }

    public Container RemoveFromGround(int x, int z) {
        throw new NotImplementedException();
    }

    public Container RemoveFromGround(Container container) {
        throw new NotImplementedException();
    }

    public Vector3 IndexToCoordinate() {
        throw new NotImplementedException();
    }

    public override string ToString() {
        throw new NotImplementedException();
    }
    #endregion

    #region private methods

    private bool dimCheck(IndexInStack index) {
        throw new NotImplementedException();
    }
    #endregion
}
