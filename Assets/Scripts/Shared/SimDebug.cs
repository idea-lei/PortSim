using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class SimDebug {
    public static void LogError(MonoBehaviour sender, string e) {
        var objectCollection = sender.GetComponentInParent<ObjectCollection>();
        //UnityEngine.Object.Destroy(objectCollection.gameObject);

        Debug.LogError(e);
    }
}
