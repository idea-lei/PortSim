using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FindContainerAgent : AgentBase {
    private bool hasOutField {
        get {
            bool res = false;
            foreach (var port in objs.IoPorts) {
                if (port.CurrentField && port.CurrentField.isActiveAndEnabled && port.CurrentField is OutField) res = true;
            }
            return res;
        }
    }

    private bool hasInField {
        get {
            bool res = false;
            foreach (var port in objs.IoPorts) {
                if (port.CurrentField && port.CurrentField.isActiveAndEnabled && port.CurrentField is InField) res = true;
            }
            return res;
        }
    }
    void Start() {

    }

    // this function need to be trained
    /// <summary>
    /// find container in available containerSet to move, can be movein/out or rearrange
    /// </summary>
    /// <returns>
    /// 1. containerToPick
    /// 2. movement state (move in / out / rearrange)
    /// </returns>
    public (Container, string) FindContainerToPick() {
        foreach (var p in objs.IoPorts) {
            // try to find container out
            if (p.CurrentField is OutField && p.CurrentField.enabled) {
                foreach (var s in objs.StackField.Ground) {
                    var peek = s.Peek();
                    foreach (var c in s.ToArray()) {
                        if (c.OutField == p.CurrentField)
                            return (peek, c == peek ? "MoveOut" : "Rearrange");
                    }
                }
            }
            // try to find container in
            if (p.CurrentField is InField && p.CurrentField.enabled) {

            }
        }
        return (null, "Wait");
    }

    // backup
    private void findContainerToPick() {
        //var c = findContainerToMoveOut();
        //if (c != null) return c;
        //c = findContainerToMoveIn();
        //if (c != null) return c;
        //c = findContainerToRearrange();
    }

    ///// <returns>
    ///// container,
    ///// state (the corresponding movement state of the container)
    ///// </returns>
    //private (Container, string) findContainerInIoField() {
    //    if (!hasOutField) return (null, "Wait");
    //    foreach (var outP in ioPorts) {
    //        if (outP.CurrentField is OutField && outP.CurrentField.enabled) {
    //            foreach (var s in stackField.Ground) {
    //                var peek = s.Peek();
    //                foreach (var c in s.ToArray()) {
    //                    if (c.OutField == outP.CurrentField)
    //                        return (peek, c == peek ? "MoveOut" : "Rearrange");
    //                }
    //            }
    //        }
    //    }
    //    return (null, "Wait");
    //}

    //private Container findContainerToRearrange() {
    //    if (stackField.IsGroundFull) return null;
    //    foreach (var s in stackField.Ground) {
    //        if (s.Count == 0) continue;
    //        var list = s.ToArray();
    //        var min = list.First(x => x.OutField.TimePlaned == list.Min(y => y.OutField.TimePlaned));
    //        if (min != s.Peek()) return min;
    //    }
    //    return null;
    //}

    //private Container findContainerToMoveIn() {
    //    foreach (var t in tempFields) {
    //        if (t.IsGroundEmpty) continue;
    //        foreach (var s in t.Ground) {
    //            if (s.Count > 0) return s.Peek();
    //        }
    //    }
    //    if (!hasInField) return null;
    //    /*if (stackField.Count + 1 >= stackField.MaxCount) return null;*/ // avoid full stack, otherwise will be no arrange possible
    //    if (stackField.Count >= stackField.MaxCount) return null;
    //    foreach (var p in ioPorts) {
    //        if (p.CurrentField && p.CurrentField is InField && p.CurrentField.isActiveAndEnabled) {
    //            if (stackField.IsGroundFull) return null;
    //            foreach (var s in p.CurrentField.Ground) {
    //                if (s.Count > 0) return s.Peek();
    //            }
    //        }
    //    }
    //    return null;
    //}

}
