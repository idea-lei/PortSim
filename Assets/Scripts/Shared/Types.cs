using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct IndexInStack {
    public bool IsValid; // since struct is non-nullable, so need this field to determine whether the index is valid
    public int x;
    public int z;
    public IndexInStack(bool _isValid) {
        x = 0;
        z = 0;
        IsValid = _isValid;
    }
    public IndexInStack(int _x, int _z, bool _isValid = true) {
        IsValid = _isValid;
        x = _x;
        z = _z;
    }

    public static bool operator ==(IndexInStack a, IndexInStack b) {
        return a.x == b.x && a.z == b.z;
    }

    public static bool operator !=(IndexInStack a, IndexInStack b) {
        return !(a == b);
    }

    public override string ToString() {
        return IsValid ? $"x={x}, z = {z}" : "not valid!";
    }

    public override bool Equals(object obj) {
        if (obj is IndexInStack)
            return this == (IndexInStack)obj;
        return false;
    }

    public override int GetHashCode() {
        return base.GetHashCode();
    }
}

public struct ResultWithMessage {
    public string Message;
    public bool Result;
}
