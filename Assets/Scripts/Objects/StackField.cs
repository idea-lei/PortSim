using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class StackField : Field {
    private IoFieldsGenerator ioFieldsGenerator;
    private void Awake() {
        DimX = Parameters.DimX;
        DimZ = Parameters.DimZ;
        MaxLayer = Parameters.MaxLayer;
        ioFieldsGenerator = FindObjectOfType<IoFieldsGenerator>();
    }
    private void Start() {        
        initField();
        transform.position = new Vector3();
        initContainers();
    }

    
    protected override void initContainers() {
        base.initContainers();
        // assign outFields
        foreach (var s in Ground) {
            foreach (var c in s) {
                assignOutPort(c);
            }
        }
    }

    /// <summary>
    /// this method assign the outFields of the containers,
    /// will generate outField if not exist
    /// </summary>
    private void assignOutPort(Container container) {
        void assign(Container c, OutField f) {
            f.incomingContainers.Add(c);
            c.OutField = f;
        }

        if(UnityEngine.Random.Range(0, 1f) > Parameters.PossibilityOfNewOutField) {
            var outFields = FindObjectsOfType<OutField>();
            if (outFields.Length>0) {
                var index = UnityEngine.Random.Range(0, outFields.Length);
                if (!outFields[index].IsGroundFull) {
                    assign(container, outFields[index]);
                    return;
                }
            }
            
        }
        var (obj, field) = ioFieldsGenerator.GenerateOutField();
        assign(container, field);
    }
}
