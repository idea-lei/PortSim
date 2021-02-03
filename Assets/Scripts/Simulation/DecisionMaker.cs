using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecisionMaker : MonoBehaviour
{
    private Crane crane;
    private void Awake() {
        crane = FindObjectOfType<Crane>();
    }


}
