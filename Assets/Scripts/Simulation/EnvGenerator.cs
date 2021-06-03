using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class EnvGenerator : MonoBehaviour {
    [SerializeField] private GameObject areaContainerPrefab;
    [SerializeField] int dim;
    void Start() {
        for (int x = 0; x < Parameters.TrainingDim; x++) {
            for (int z = 0; z < Parameters.TrainingDim; z++) {
                Instantiate(areaContainerPrefab, new Vector3(x * 100, 0, z * 100), Quaternion.identity, null);
            }
        }
        if (Academy.Instance.IsCommunicatorOn) {
            InvokeRepeating(nameof(CheckEnvParameter), 60, 60);
        }
        
    }

    void CheckEnvParameter() {
        dim = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("dim", Parameters.DimX);
        if (dim > Parameters.DimX) {
            Parameters.DimX = dim;
            Parameters.DimZ = dim;
            foreach (var o in FindObjectsOfType<ObjectCollection>()) {
                Destroy(o.gameObject);
            }
        }
    }
}
