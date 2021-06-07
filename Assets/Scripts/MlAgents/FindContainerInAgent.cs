using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class FindContainerInAgent : AgentBase
{
    private class FindContainerInObservationObject {
        public IndexInStack index; //this one is not a observation variable

        public DateTime timeOut;
        public float energy;

        // n means normalized
        public float n_timeOut;
        public float n_energy;
    }

    private List<FindContainerInObservationObject> obList = new List<FindContainerInObservationObject>();

    public override void CollectObservations(VectorSensor sensor) {
        var inFields = objs.IoPorts
            .Where(i => i.CurrentField && i.CurrentField is InField)
            .Select(i => i.CurrentField);
        // select the infield with min amount of containers
        int min = Parameters.DimX * Parameters.DimZ + 1;
        var inField = inFields.Aggregate((curMin, x) => {
            int amount = x.GetComponentsInChildren<Container>().Count();
            if (amount < min) {
                min = amount;
                return x;
            } else return curMin;
        });

        foreach (var s in inField.Ground) {
            if (s.Count == 0) continue;
            obList.Add(new FindContainerInObservationObject() {
                energy = CalculateEnergy(s.Peek()),
                timeOut = s.Peek().OutField.TimePlaned
            });
        }
    }

    private Container findContainerToMoveIn() {
        foreach (var t in objs.TempFields) {
            if (t.IsGroundEmpty) continue;
            foreach (var s in t.Ground) {
                if (s.Count > 0) return s.Peek();
            }
        }
        /*if (stackField.Count + 1 >= stackField.MaxCount) return null;*/ // avoid full stack, otherwise will be no arrange possible
        if (objs.StackField.Count >= objs.StackField.MaxCount) return null;
        foreach (var p in objs.IoPorts) {
            if (p.CurrentField && p.CurrentField is InField && p.CurrentField.isActiveAndEnabled) {
                if (objs.StackField.IsGroundFull) return null;
                foreach (var s in p.CurrentField.Ground) {
                    if (s.Count > 0) return s.Peek();
                }
            }
        }
        return null;
    }
}
