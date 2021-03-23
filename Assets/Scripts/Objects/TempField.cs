using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// this field is only for rearrange, do not stack 
/// </summary>
public class TempField : Field {
    private void Awake() {
        DimX = Parameters.DimX;
        DimZ = 1;
        MaxLayer = Parameters.MaxLayer;
    }
    private void Start() {
        initField(GetComponentInParent<ObjectCollection>().IoFieldsGenerator);
        transform.position = new Vector3(0, 0,
            Mathf.Sign(transform.position.z) * (Parameters.DimZ / 2f * (Parameters.ContainerWidth + Parameters.Gap_Container) + Parameters.Gap_Field));
    }

    public override void AddToGround(Container container) {
         base.AddToGround(container);
        container.tag = "container_temp";
    }
}
