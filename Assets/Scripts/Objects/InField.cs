using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class InField : Field
{
    public DateTime TimePlaned;
    public DateTime TimeReal;
    public Vector2 GenerationPoint;

    protected override void initContainers() {
        for(int x = 0; x < DimX; x++) {
            for (int z = 0; z < DimZ; z++) {
                Ground[x, z] = new Stack<Container>();
                for (int k = 0; k <= UnityEngine.Random.Range(-1, Parameters.MaxLayer); k++) {
                    var pos = IndexToLocalPosition(x, z, Ground[x, z].Count());
                    var container = generateContainer(pos);
                    container.InField = this;
                    Ground[x, z].Push(container);
                }
            }
        }
    }
}
