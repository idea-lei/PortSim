using Ilumisoft.VisualStateMachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

public abstract class AgentBase : Agent {
    protected StackField stackField;
    protected Crane crane;
    protected StateMachine stateMachine;
    protected BufferSensorComponent bufferSensor;

    // reward coefficients
    protected const float c_outOfRange = 1f;
    protected const float c_full = 1f;
    protected const float c_stacked = 1f;

    protected const float c_time = 0.5f;
    protected const float c_rearrange = 0.5f;
    protected const float c_layer = 0.2f;
    protected const float c_energy = 0.1f;
    protected const float c_weight = 0.05f;

    protected

    // Start is called before the first frame update
    void Start() {
        stackField = GetComponentInParent<ObjectCollection>().StackField;
        crane = GetComponentInParent<ObjectCollection>().Crane;
        stateMachine = crane.GetComponent<StateMachine>();
        bufferSensor = GetComponent<BufferSensorComponent>();
    }
}
