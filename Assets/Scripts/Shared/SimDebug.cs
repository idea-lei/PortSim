using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class SimDebug {
    public static void LogError(MonoBehaviour sender, string e) {
        var objectCollection = sender.GetComponentInParent<ObjectCollection>();
        //GameObject.Destroy(objectCollection.gameObject);

        //foreach(var t in objectCollection.TempFields) {
        //    t.GetComponent<MeshRenderer>().material.color = Color.red;
        //}
        //var c = objectCollection.StackField.gameObject;
        //c.GetComponent<MeshRenderer>().material.color = Color.red;
        Debug.LogError(e);
    }
}
