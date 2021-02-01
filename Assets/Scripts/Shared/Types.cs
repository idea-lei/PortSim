using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct IndexInStack {
    public bool IsValid; // since struct is non-nullable, so need this field to determine whether the index is valid
    public int x;
    public int z;

    public IndexInStack(int _x, int _z, bool _isValid = true) {
        IsValid = _isValid;
        x = _x;
        z = _z;
    }

    public override string ToString() {
        return IsValid? $"x={x}, z = {z}": "not valid!";
    }
}

public struct ResultWithMessage {
    public string Message;
    public bool Result;
}
