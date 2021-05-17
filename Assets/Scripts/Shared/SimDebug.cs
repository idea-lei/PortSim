using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class SimDebug {
    public static void LogError(MonoBehaviour sender, string e) {
        Debug.LogError(e);
        var objectCollection = sender.GetComponentInParent<ObjectCollection>();
        GameObject.Destroy(objectCollection.gameObject);
    }
}
