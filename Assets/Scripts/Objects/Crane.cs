using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crane : MonoBehaviour
{
    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("container_in")) {
            var container = other.GetComponent<Container>();
            if (container.Id.ToString()== "the next id we want") {
                // move container to outfield
            }
        }
        if (other.CompareTag("field")) {
            Debug.LogError("crane touched field!");
        }
    }
}
