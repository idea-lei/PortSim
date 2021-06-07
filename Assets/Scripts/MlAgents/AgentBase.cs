using Ilumisoft.VisualStateMachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

public abstract class AgentBase : Agent {
    protected BufferSensorComponent bufferSensor;
    protected ObjectCollection objs;

    // reward coefficients
    protected const float c_outOfRange = 1f;
    protected const float c_full = 1f;
    protected const float c_stacked = 1f;

    protected const float c_time = 0.5f;
    protected const float c_rearrange = 0.5f;
    protected const float c_layer = 0.2f;
    protected const float c_energy = 0.1f;
    protected const float c_weight = 0.05f;

    // Start is called before the first frame update
    void Awake() {
        bufferSensor = GetComponent<BufferSensorComponent>();
        objs = GetComponentInParent<ObjectCollection>();
    }
}
