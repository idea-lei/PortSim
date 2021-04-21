using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaContainer : MonoBehaviour {
    [SerializeField] private GameObject AreaPrefab;

    private void Start() {
        InvokeRepeating(nameof(GenerateArea), 3, 3);
    }
    public void GenerateArea() {
        if (transform.childCount == 0)
            Invoke(nameof(InstantiateArea), 1);
    }

    private void InstantiateArea() {
        Instantiate(AreaPrefab, transform.position, transform.rotation, transform);
    }
}
