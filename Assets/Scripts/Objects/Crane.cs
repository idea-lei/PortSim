using Ilumisoft.VisualStateMachine;
using System;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;

public class Crane : MonoBehaviour {
    private ObjectCollection objs;

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
        private set {
            if (value) ContainerCarrying = null;
            _containerToPick = value;
        }
    }

    public Container ContainerCarrying {
        get => _containerCarrying;
        private set {
            //value && value == ContainerToPick
            if (value) ContainerToPick = null;
            _containerCarrying = value;
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

    private bool hasOutField {
        get {
            bool res = false;
            foreach (var port in objs.IoPorts) {
                if (port.CurrentField && port.CurrentField.isActiveAndEnabled && port.CurrentField is OutField) res = true;
            }
            return res;
        }
    }

    private bool hasInField {
        get {
            bool res = false;
            foreach (var port in objs.IoPorts) {
                if (port.CurrentField && port.CurrentField.isActiveAndEnabled && port.CurrentField is InField) res = true;
            }
            return res;
        }
    }

    private StateMachine stateMachine;

    private void Awake() {
        objs = GetComponentInParent<ObjectCollection>();
        stateMachine = GetComponent<StateMachine>();

        setStateMachineGeneralEvents();
        setStateWaitEvents();
        setStateFindOutOrRearrangeEvents();
        setStateFindInEvents();
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
                if (CanPickUp_OutOrRearrange) {
                    stateMachine.TriggerByState("FindOutOrRearrange");
                    return;
                }
                if (CanPickUp_In) {
                    stateMachine.TriggerByState("FindIn");
                    return;
                }
                break;
            case "PickUp":
                if (ContainerToPick) moveTo(ContainerToPick.transform.position, false);
                else stateMachine.TriggerByState("Wait");
                break;
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

    #region find container
    // this function need to be trained
    /// <summary>
    /// find container in available containerSet to move, can be movein/out or rearrange
    /// </summary>
    /// <returns>
    /// 1. containerToPick
    /// 2. movement state (move in / out / rearrange)
    /// </returns>
    private (Container, string) findContainerToPick() {
        foreach (var p in objs.IoPorts) {
            // try to find container out
            if (p.CurrentField is OutField && p.CurrentField.enabled) {
                foreach (var s in objs.StackField.Ground) {
                    var peek = s.Peek();
                    foreach (var c in s.ToArray()) {
                        if (c.OutField == p.CurrentField)
                            return (peek, c == peek ? "MoveOut" : "Rearrange");
                    }
                }
            }
            // try to find container in
            if (p.CurrentField is InField && p.CurrentField.enabled) {
                
            }
        }
        return (null, "Wait");
    }

    // backup
    //private (Container, string) findContainerToPick() {
    //    var c = findContainerToMoveOut();
    //    if (c != null) return c;
    //    c = findContainerToMoveIn();
    //    if (c != null) return c;
    //    //c = findContainerToRearrange();
    //    return c;
    //}

    ///// <returns>
    ///// container,
    ///// state (the corresponding movement state of the container)
    ///// </returns>
    //private (Container, string) findContainerInIoField() {
    //    if (!hasOutField) return (null, "Wait");
    //    foreach (var outP in ioPorts) {
    //        if (outP.CurrentField is OutField && outP.CurrentField.enabled) {
    //            foreach (var s in stackField.Ground) {
    //                var peek = s.Peek();
    //                foreach (var c in s.ToArray()) {
    //                    if (c.OutField == outP.CurrentField)
    //                        return (peek, c == peek ? "MoveOut" : "Rearrange");
    //                }
    //            }
    //            foreach (var inP in ioPorts) {
    //                if (inP.CurrentField is InField && inP.CurrentField.enabled) {
    //                    foreach (var s in inP.CurrentField.Ground) {
    //                        var peek = s.Peek();
    //                        foreach (var c in s.ToArray()) {
    //                            if (c.OutField == outP.CurrentField)
    //                                return c;
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    }
    //    return (null, "Wait");
    //}

    //private Container findContainerToRearrange() {
    //    if (stackField.IsGroundFull) return null;
    //    foreach (var s in stackField.Ground) {
    //        if (s.Count == 0) continue;
    //        var list = s.ToArray();
    //        var min = list.First(x => x.OutField.TimePlaned == list.Min(y => y.OutField.TimePlaned));
    //        if (min != s.Peek()) return min;
    //    }
    //    return null;
    //}

    //private Container findContainerToMoveIn() {
    //    foreach (var t in tempFields) {
    //        if (t.IsGroundEmpty) continue;
    //        foreach (var s in t.Ground) {
    //            if (s.Count > 0) return s.Peek();
    //        }
    //    }
    //    if (!hasInField) return null;
    //    /*if (stackField.Count + 1 >= stackField.MaxCount) return null;*/ // avoid full stack, otherwise will be no arrange possible
    //    if (stackField.Count >= stackField.MaxCount) return null;
    //    foreach (var p in ioPorts) {
    //        if (p.CurrentField && p.CurrentField is InField && p.CurrentField.isActiveAndEnabled) {
    //            if (stackField.IsGroundFull) return null;
    //            foreach (var s in p.CurrentField.Ground) {
    //                if (s.Count > 0) return s.Peek();
    //            }
    //        }
    //    }
    //    return null;
    //}
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

    private void setStateFindOutOrRearrangeEvents() {
        var state = stateMachine.Graph.GetState("FindOutOrRearrange");
        state.OnEnterState.AddListener(() => {
            if (ContainerToPick != null) {
                SimDebug.LogError(this, "container to pick is not null when making pickup decision");
                return;
            }
            ContainerToPick = findContainerToMoveOut();
            stateMachine.TriggerByState(ContainerToPick == null ? "Wait" : "PickUp");
        });
        state.OnExitState.AddListener(() => {
        });
    }

    private void setStateFindInEvents() {
        var state = stateMachine.Graph.GetState("FindIn");
        state.OnEnterState.AddListener(() => {
            if (ContainerToPick != null) { // which means the last time 
                SimDebug.LogError(this, "container to pick is not null when making pickup decision");
                return;
            }
            ContainerToPick = findContainerToMoveIn();
            stateMachine.TriggerByState(ContainerToPick == null ? "Wait" : "PickUp");
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
            else objs.StackBehavior.RequestDecision();
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
