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
class OpObject {
    private Crane crane;

    public Container Container;
    public string State => crane.GetComponent<StateMachine>().CurrentState;

    public IndexInStack PickUpIndex;
    public Field PickUpField;

    public IndexInStack StackIndex;
    public Field StackField;

    public OpObject(Crane _c) { crane = _c; }
}

public class Crane : MonoBehaviour {
    private ObjectCollection objs;
    [SerializeField] private OpObject opObj;

    #region serialized fields for test
    // do not assign values to these fields
    [Header("fields visible for debugging")]
    [Space(15)]
    [SerializeField] private Container _containerToPick;
    [SerializeField] private Container _containerCarrying;
    [SerializeField] private bool reachedTop; // this field is weird cuz of the move strategy, need to update this
    #endregion


    public Container ContainerToPick {
        get => _containerToPick;
        set {
            if (value) ContainerCarrying = null;
            _containerToPick = value;
        }
    }

    public Container ContainerCarrying {
        get => _containerCarrying;
        set {
            //value && value == ContainerToPick
            if (value) ContainerToPick = null;
            _containerCarrying = value;
        }
    }

    private bool hasOutField {
        get {
            foreach (var port in objs.IoPorts) {
                if (port.CurrentField &&
                    port.CurrentField.isActiveAndEnabled &&
                    port.CurrentField is OutField &&
                    ((OutField)port.CurrentField).IncomingContainers.Count > port.CurrentField.GetComponentsInChildren<Container>().Length)
                    return true;
            }
            return false;
        }
    }

    private bool hasInField {
        get {
            foreach (var port in objs.IoPorts) {
                if (port.CurrentField
                    && port.CurrentField.isActiveAndEnabled
                    && port.CurrentField is InField
                    && port.CurrentField.GetComponentsInChildren<Container>().Length > 0)
                    return true;
            }
            return false;
        }
    }

    public bool CanPickUp_In {
        get {
            bool isStackFieldFull = objs.StackField.IsGroundFull;
            foreach (var io in objs.IoPorts) {
                // check container to move in
                if (!isStackFieldFull && io.CurrentField is InField)
                    return true;
            }
            return false;
        }
    }

    public bool CanPickUp => CanPickUp_In || CanPickUp_OutOrRearrange;

    public bool CanPickUp_OutOrRearrange {
        get {
            foreach (var io in objs.IoPorts) {
                // check container to move out
                if (io.CurrentField is OutField) {
                    if (((OutField)io.CurrentField).IncomingContainersCount > io.CurrentField.GetComponentsInChildren<Container>().Length)
                        return true;
                }
            }
            return false;
        }
    }

    private Vector3 destination;

    private StateMachine stateMachine;

    private void Awake() {
        objs = GetComponentInParent<ObjectCollection>();
        stateMachine = GetComponent<StateMachine>();

        setStateMachineGeneralEvents();
        setStateWaitEvents();
        setStateFindPickUpEvents();
        setStatePickUpEvents();
        setStateMoveInEvents();
        setStackDecisionEvents();
        setStateMoveOutEvents();
        setStateRearrangeEvents();
        setStateMoveTempEvents();
    }

    private void OnTriggerEnter(Collider other) {
        reachedTop = false;
        if (other.CompareTag("container_in") || other.CompareTag("container_temp")) {
            ContainerCarrying = other.GetComponent<Container>();
            if (ContainerCarrying.OutField.isActiveAndEnabled) stateMachine.TriggerByState("MoveOut");
            else stateMachine.TriggerByState("StackDecision");
            return;
        }
        if (other.CompareTag("container_stacked")) {
            ContainerCarrying = other.GetComponent<Container>();
            foreach (var p in objs.IoPorts) {
                if (p.CurrentField && p.CurrentField.isActiveAndEnabled && p.CurrentField == ContainerCarrying.OutField) {
                    stateMachine.TriggerByState("MoveOut");
                    return;
                }
            }
            // this ensures there is at least one stackable index
            if (objs.StackField.StackableIndex(ContainerCarrying.StackedIndices).IsValid) {
                stateMachine.TriggerByState("StackDecision");
            } else {
                stateMachine.TriggerByState("MoveTemp");
            }
            return;
        }
        SimDebug.LogError(this, $"illegal crane touch with {other.name}");
    }

    private void FixedUpdate() {
        switch (stateMachine.CurrentState) {
            case "Wait":
                if (!reachedTop) {
                    moveToWaitPosition();
                    return;
                }
                if (ContainerToPick != null) {
                    SimDebug.LogError(this, "container to pick is not null when making pickup decision");
                    return;
                }
                if (CanPickUp) {
                    stateMachine.TriggerByState("FindPickUp");
                    return;
                }
                break;
            case "PickUp":
                if (ContainerToPick) moveTo(ContainerToPick.transform.position, false);
                else stateMachine.TriggerByState("Wait");
                break;
            case "FindPickUp":
            case "StackDecision":
                break;
            default:
                moveTo(destination, true);
                break;
        }
    }

    #region private methods
    private void moveToWaitPosition() {
        if (Parameters.TranslationHeight - transform.position.y > Parameters.DistanceError)
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
            if (Parameters.TranslationHeight - transform.position.y > Parameters.DistanceError) return Movement.up;
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

    private void setStateFindPickUpEvents() {
        var state = stateMachine.Graph.GetState("FindPickUp");
        state.OnEnterState.AddListener(() => {
            if (ContainerToPick != null) {
                SimDebug.LogError(this, "container to pick is not null when making pickup decision");
                return;
            }
            if (hasOutField) {
                objs.FindContainerOutAgent.RequestDecision();
                return;
            }
            if (hasInField) {
                objs.FindContainerInAgent.RequestDecision();
                return;
            }
        });
        state.OnExitState.AddListener(() => {
        });
    }

    private void setStatePickUpEvents() {
        var state = stateMachine.Graph.GetState("PickUp");
        state.OnEnterState.AddListener(() => {

            if (ContainerToPick == null || !ContainerToPick.CurrentField.isActiveAndEnabled) {
                Debug.LogWarning("container to pick is null or the field is not enabled");
                stateMachine.TriggerByState("Wait");
            }
        });
        state.OnExitState.AddListener(() => {
            if (ContainerCarrying) {
                ContainerCarrying.RemoveFromGround();
                ContainerCarrying.transform.SetParent(transform);
                if (ContainerCarrying.InField != null) {
                    var inField = ContainerCarrying.InField;
                    ContainerCarrying.InField = null;
                    if (inField.IsGroundEmpty) inField.DestroyField();
                }
            }
        });
    }

    private void setStackDecisionEvents() {
        var state = stateMachine.Graph.GetState("StackDecision");
        state.OnEnterState.AddListener(() => {
            if (!ContainerCarrying) SimDebug.LogError(this, "container carrying is null");
            else objs.FindIndexAgent.RequestDecision();
        });
    }

    private void setStateMoveInEvents() {
        var state = stateMachine.Graph.GetState("MoveIn");
        state.OnEnterState.AddListener(() => {
            var index = objs.StackField.TrainingResult;
            if (index.IsValid) destination = objs.StackField.IndexToGlobalPosition(index);
            else stateMachine.TriggerByState("Wait");
        });
        state.OnExitState.AddListener(() => {
            objs.StackField.AddToGround(ContainerCarrying);
            ContainerCarrying = null;
        });
    }

    private void setStateRearrangeEvents() {
        var state = stateMachine.Graph.GetState("Rearrange");
        state.OnEnterState.AddListener(() => {
            ContainerCarrying.tag = "container_rearrange";
            var index = objs.StackField.TrainingResult;
            if (index.IsValid) destination = objs.StackField.IndexToGlobalPosition(index);
            else stateMachine.TriggerByState("Wait");
        });
        state.OnExitState.AddListener(() => {
            var diffVec = transform.position - destination;
            var diffVec2 = new Vector2(diffVec.x, diffVec.z);
            if (diffVec2.sqrMagnitude < Parameters.SqrDistanceError) {
                ContainerCarrying.transform.SetParent(objs.StackField.transform);
                objs.StackField.AddToGround(ContainerCarrying);
                ContainerCarrying = null;
                ContainerToPick = null;
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
            if (ContainerCarrying.OutField.Count == ContainerCarrying.OutField.IncomingContainersCount) {
                ContainerCarrying.OutField.DestroyField();
            }
            ContainerCarrying = null;
        });
    }

    private void setStateMoveTempEvents() {
        TempField tempField = objs.TempFields[0];
        var state = stateMachine.Graph.GetState("MoveTemp");
        state.OnEnterState.AddListener(() => {
            ContainerCarrying.tag = "container_temp";
            tempField = objs.TempFields[UnityEngine.Random.Range(0, objs.TempFields.Length)];
            var index = tempField.NearestStackableIndex(transform.position);
            if (index.IsValid) destination = tempField.IndexToGlobalPosition(index);
            else stateMachine.TriggerByState("Wait");
        });
        state.OnExitState.AddListener(() => {
            var diffVec = transform.position - destination;
            var diffVec2 = new Vector2(diffVec.x, diffVec.z);
            if (diffVec2.sqrMagnitude < Parameters.SqrDistanceError) {
                ContainerCarrying.transform.SetParent(tempField.transform);
                tempField.AddToGround(ContainerCarrying);
                ContainerCarrying = null;
            }
        });
    }
    #endregion
}
