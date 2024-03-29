﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvGenerator : MonoBehaviour {
    [SerializeField] private GameObject areaContainerPrefab;
    void Start() {
        for (int x = 0; x < Parameters.TrainingDim; x++) {
            for (int z = 0; z < Parameters.TrainingDim; z++) {
                Instantiate(areaContainerPrefab, new Vector3(x * 100, 0, z * 100), Quaternion.identity, null);
            }
        }
    }
}
