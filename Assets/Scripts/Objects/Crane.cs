using Ilumisoft.VisualStateMachine;
using System;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;

/// <summary>
/// the operation object
/// each crane operation contains:
/// 1. move to pick up position
/// 2. move to stack position
/// </summary>
[Serializable]
public class OpObject {
    public Container Container;
    public Field TargetField;
    public string State;
    public Vector3 PickUpPos;
    public Vector3 StackPos;
}

public class Crane : MonoBehaviour {
    private ObjectCollection objs;
    public OpObject OpObj;

    #region serialized fields for test
    // do not assign values to these fields
    [Header("fields visible for debugging")]
    [Space(15)]
    [SerializeField] private Container _containerToPick;
    [SerializeField] private Container _containerCarrying;
    [SerializeField] private bool reachedTop; // this field is weird cuz of the move strategy, need to update this
    #endregion

    public Container ContainerCarrying {
        get => _containerCarrying;
        set {
            //value && value == ContainerToPick
            _containerCarrying = value;
        }
    }

    private bool canPickUp_In =>
        objs.HasInField && !objs.StackField.IsGroundFull;

    private bool canPickUp_Out =>
        objs.HasOutField && objs.OutContainersOnPeak.Length > 0;

    private Vector3 destination;

    private StateMachine stateMachine;

    public float TranslationHeight;

    private void Awake() {
        objs = GetComponentInParent<ObjectCollection>();
        stateMachine = GetComponent<StateMachine>();

        setStateMachineGeneralEvents();
        setStateWaitEvents();
        //setStateFindPickUpEvents();
        setStateOperateEvents();
        setStatePickUpEvents();
        //setStateMoveInEvents();
        setStateDecisionEvents();
        //setStateMoveOutEvents();
        //setStateRearrangeEvents();
        //setStateMoveTempEvents();
    }

    private void Start() {
        TranslationHeight = 20;
    }

    private void OnTriggerEnter(Collider other) {
        reachedTop = false;

        if (ContainerCarrying == null) {
            if (other.tag.Contains("container")) {
                if (OpObj.Container == other.GetComponent<Container>()) {
                    ContainerCarrying = other.GetComponent<Container>();
                    destination = OpObj.StackPos;
                } else {
                    SimDebug.LogError(this, "target is not the touched");
                }
                objs.StateMachine.TriggerByState("Operate");
                return;
            }
        }
        //if (other.CompareTag("container_in") || other.CompareTag("container_temp")) {
        //    ContainerCarrying = other.GetComponent<Container>();
        //    if (ContainerCarrying.OutField.isActiveAndEnabled) stateMachine.TriggerByState("MoveOut");
        //    else stateMachine.TriggerByState("StackDecision");
        //    return;
        //}
        //if (other.CompareTag("container_stacked")) {
        //    ContainerCarrying = other.GetComponent<Container>();
        //    foreach (var p in objs.IoPorts) {
        //        if (p.CurrentField && p.CurrentField.isActiveAndEnabled && p.CurrentField == ContainerCarrying.OutField) {
        //            stateMachine.TriggerByState("MoveOut");
        //            return;
        //        }
        //    }
        //    // this ensures there is at least one stackable index
        //    if (objs.StackField.StackableIndex(ContainerCarrying.StackedIndices).IsValid) {
        //        stateMachine.TriggerByState("StackDecision");
        //    } else {
        //        stateMachine.TriggerByState("MoveTemp");
        //    }
        //    return;
        //}
        SimDebug.LogError(this, $"illegal crane touch with {other.name}");
    }

    private void FixedUpdate() {
        switch (stateMachine.CurrentState) {
            case "Wait":
                if (!reachedTop) {
                    moveToWaitPosition();
                    return;
                }
                if (OpObj?.Container != null) {
                    SimDebug.LogError(this, "container to pick is not null when making pickup decision");
                    return;
                }
                if (objs.HasOutField || objs.HasInField) {
                    stateMachine.TriggerByState("Decision");
                    return;
                }
                break;
            case "PickUp":
                if (OpObj?.Container) moveTo(OpObj.PickUpPos, false);
                else stateMachine.TriggerByState("Wait");
                break;
            case "Decision":
                break;
            default:
                moveTo(destination, true);
                break;
        }
    }

    #region private methods
    private void moveToWaitPosition() {
        if (TranslationHeight - transform.position.y > Parameters.DistanceError)
            transform.position += new Vector3(0, Parameters.Vy_Unloaded * Time.deltaTime, 0);
        else reachedTop = true;
    }

    /// <remarks>
    /// do not modify this method unless you can really figure out 
    /// relationship between the vector2.y here and position.z
    /// </remarks>
    private void moveTo(Vector2 destination, bool isLoaded) {
        Vector3 step = new Vector3();
        switch (moveState(destination)) {
            case Movement.up:
                step.y = isLoaded ? Parameters.Vy_Loaded : Parameters.Vy_Unloaded;
                break;
            case Movement.x:
                step.x = Mathf.Sign(destination.x - transform.position.x) * (isLoaded ? Parameters.Vx_Loaded : Parameters.Vx_Unloaded);
                break;
            case Movement.z:
                step.z = Mathf.Sign(destination.y - transform.position.z) * (isLoaded ? Parameters.Vz_Loaded : Parameters.Vz_Unloaded);
                break;
            case Movement.down:
                step.y = -(isLoaded ? Parameters.Vy_Loaded : Parameters.Vy_Unloaded);
                break;
        }
        transform.position += step * Time.fixedDeltaTime;
        //objs.CRPAgent.AddReward(-0.002f);
    }

    private void moveTo(Vector3 position, bool isLoaded) {
        var vec2 = new Vector2(position.x, position.z);
        moveTo(vec2, isLoaded);
    }

    /// <summary>
    /// this function is to determine the movement state of hook
    /// </summary>
    /// <param name="destination">the destination.y is the Vector3.z!</param>
    /// <returns>the movement period</returns>
    private Movement moveState(Vector2 destination) {
        if (!reachedTop) {
            if (TranslationHeight - transform.position.y > Parameters.DistanceError) return Movement.up;
            else reachedTop = true;
        }
        var actualPos = new Vector2(transform.position.x, transform.position.z);
        if ((actualPos - destination).sqrMagnitude < Parameters.DistanceError) return Movement.down;
        //if (Parameters.TranslationHeight - transform.position.y > Parameters.DistanceError) return Movement.up;
        if (Mathf.Abs(transform.position.z - destination.y) > Parameters.DistanceError) return Movement.z;
        return Movement.x;
    }

    // movement squence should be up - z - x - down
    private enum Movement {
        up,
        z,
        x,
        down,
        wait
    }

    #endregion

    #region statemachine events
    private void setStateMachineGeneralEvents() {
        stateMachine.OnEnterState += _ => { reachedTop = false; };
    }

    private void setStateWaitEvents() {
        var state = stateMachine.Graph.GetState("Wait");
        state.OnEnterState.AddListener(() => { });
        state.OnExitState.AddListener(() => { });
    }

    private void setStateDecisionEvents() {
        var state = stateMachine.Graph.GetState("Decision");
        state.OnEnterState.AddListener(() => {
            if (OpObj?.Container != null) {
                SimDebug.LogError(this, "container to pick is not null when making pickup decision");
                return;
            }
            //var agent = objs.FindNextOperationAgent;
            var agent = objs.CRPAgent;

            // move out
            if (canPickUp_Out) {
                var Ob = new ObservationFindOperation() {
                    ContainerField = objs.StackField,
                    Containers = objs.OutContainersOnPeak.ToList(),
                    TargetField = objs.IoPorts[0].CurrentField,
                    State = "container_out",
                    AvailableIndices = objs.IoPorts[0].CurrentField.AvailableIndices
                };
                //agent.RequestDecision();
                OpObj = new OpObject();
                OpObj.State = Ob.State;
                OpObj.Container = Ob.Containers[0];
                OpObj.PickUpPos = Ob.Containers[0].transform.position;
                OpObj.StackPos = Ob.TargetField.IndexToGlobalPosition(objs.IoPorts[0].CurrentField.AvailableIndices[0]);
                OpObj.TargetField = Ob.TargetField;
                objs.StateMachine.TriggerByState("PickUp");
                //agent.RequestDecision();
                return;
            }
            // relocation
            if (objs.OutContainers.Length > 0) {
                OpObj = new OpObject {
                    State = "container_rearrange",
                    TargetField = objs.StackField
                };

                agent.RequestDecision();
                return;
            }

            // move in
            if (canPickUp_In) {
                agent.Ob = new ObservationFindOperation() {
                    ContainerField = objs.Infields[0],
                    Containers = objs.InContainersOnPeak.ToList(),
                    TargetField = objs.StackField,
                    State = "container_in",
                    AvailableIndices = objs.StackField.AvailableIndices
                };
                agent.RequestDecision();
                return;
            }
        });
        state.OnExitState.AddListener(() => { });
    }

    //private void setStateFindPickUpEvents() {
    //    var state = stateMachine.Graph.GetState("FindPickUp");
    //    state.OnEnterState.AddListener(() => {
    //        if (ContainerToPick != null) {
    //            SimDebug.LogError(this, "container to pick is not null when making pickup decision");
    //            return;
    //        }
    //        if (canPickUp_Out) {
    //            objs.FindContainerOutAgent.RequestDecision();
    //            return;
    //        }
    //        if (canPickUp_In) {
    //            objs.FindContainerInAgent.RequestDecision();
    //            return;
    //        }
    //        SimDebug.LogError(this, "has no out / in Field");
    //    });
    //    state.OnExitState.AddListener(() => {
    //    });
    //}

    private void setStatePickUpEvents() {
        var state = stateMachine.Graph.GetState("PickUp");
        state.OnEnterState.AddListener(() => {

            //if (OpObj?.Container == null || OpObj.Container.CurrentField.isActiveAndEnabled) {
            //    Debug.LogWarning("container to pick is null or the field is not enabled");
            //    stateMachine.TriggerByState("Wait");
            //}
        });
        state.OnExitState.AddListener(() => {
            if (ContainerCarrying) {
                ContainerCarrying.RemoveFromGround();
                ContainerCarrying.transform.SetParent(transform);
                if (ContainerCarrying.InField != null) {
                    var inField = ContainerCarrying.InField;
                    ContainerCarrying.InField = null;
                    if (inField.Finished) inField.DestroyField();
                }
            }
        });
    }

    //private void setStackDecisionEvents() {
    //    var state = stateMachine.Graph.GetState("StackDecision");
    //    state.OnEnterState.AddListener(() => {
    //        if (!ContainerCarrying) SimDebug.LogError(this, "container carrying is null");
    //        else objs.FindIndexAgent.RequestDecision();
    //    });
    //}

    private void setStateOperateEvents() {
        var state = stateMachine.Graph.GetState("Operate");
        state.OnEnterState.AddListener(() => {
            if(ContainerCarrying == null) {
                SimDebug.LogError(this, "container carrying is null");
                return;
            }
            ContainerCarrying.tag = OpObj.State;
            destination = OpObj.StackPos;
        });
        state.OnExitState.AddListener(() => {
            OpObj.TargetField.AddToGround(ContainerCarrying);
            ContainerCarrying = null;
            OpObj = null;
            objs.FindNextOperationAgent.Ob = null;
        });
    }

    private void setStateMoveInEvents() {
        var state = stateMachine.Graph.GetState("MoveIn");
        state.OnEnterState.AddListener(() => {
            destination = OpObj.StackPos;
        });
        state.OnExitState.AddListener(() => {
            objs.StackField.AddToGround(ContainerCarrying);
            ContainerCarrying = null;
            OpObj = null;
            objs.FindNextOperationAgent.Ob = null;
        });
    }

    private void setStateRearrangeEvents() {
        var state = stateMachine.Graph.GetState("Relocate");
        state.OnEnterState.AddListener(() => {
            ContainerCarrying.tag = "container_rearrange";
            var index = objs.StackField.TrainingResult;
            if (index.IsValid) {
                destination = objs.StackField.IndexToGlobalPosition(index);
            } else stateMachine.TriggerByState("Wait");
        });
        state.OnExitState.AddListener(() => {
            var diffVec = transform.position - destination;
            var diffVec2 = new Vector2(diffVec.x, diffVec.z);
            if (diffVec2.sqrMagnitude < Parameters.SqrDistanceError) {
                ContainerCarrying.transform.SetParent(objs.StackField.transform);
                objs.StackField.AddToGround(ContainerCarrying);
                ContainerCarrying = null;
                OpObj = null;
                objs.FindNextOperationAgent.Ob = null;
            }
        });
    }

    private void setStateMoveOutEvents() {
        var state = stateMachine.Graph.GetState("MoveOut");
        state.OnEnterState.AddListener(() => {
            ContainerCarrying.tag = "container_out";
            destination = ContainerCarrying.OutField.IndexToGlobalPosition(ContainerCarrying.OutField.NearestStackableIndex(transform.position));
        });

        state.OnExitState.AddListener(() => {
            ContainerCarrying.OutField.AddToGround(ContainerCarrying);
            ContainerCarrying = null;
            OpObj = null;
            objs.FindNextOperationAgent.Ob = null;
        });
    }

    //private void setStateMoveTempEvents() {
    //    TempField tempField = objs.TempFields[0];
    //    var state = stateMachine.Graph.GetState("MoveTemp");
    //    state.OnEnterState.AddListener(() => {
    //        ContainerCarrying.tag = "container_temp";
    //        tempField = objs.TempFields[UnityEngine.Random.Range(0, objs.TempFields.Length)];
    //        var index = tempField.NearestStackableIndex(transform.position);
    //        if (index.IsValid) destination = tempField.IndexToGlobalPosition(index);
    //        else stateMachine.TriggerByState("Wait");
    //    });
    //    state.OnExitState.AddListener(() => {
    //        var diffVec = transform.position - destination;
    //        var diffVec2 = new Vector2(diffVec.x, diffVec.z);
    //        if (diffVec2.sqrMagnitude < Parameters.SqrDistanceError) {
    //            ContainerCarrying.transform.SetParent(tempField.transform);
    //            tempField.AddToGround(ContainerCarrying);
    //            ContainerCarrying = null;
    //        }
    //    });
    //}
    #endregion
}
