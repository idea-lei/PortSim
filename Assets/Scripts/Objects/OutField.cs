using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutField : Field
{
    public bool IsReady; //ready to leave (destory gameobjects)

    protected override void initContainers() {
        throw new System.NotImplementedException();
    }
}
