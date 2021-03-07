using Ilumisoft.VisualStateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CraneState {
    Wait,
    PickUp,
    MoveIn,
    MoveOut,
    Rearrange
}
public class Crane : MonoBehaviour {
    private StackField stackField;
    [SerializeField] private Container _containerToPick;
    public Container ContainerToPick {
        get => _containerToPick;
        set {
            if (value) ContainerCarrying = null;
            _containerToPick = value;
        }
    }

    [SerializeField] private Container _containerCarrying;
    private Container ContainerCarrying {
        get => _containerCarrying;
        set {
            if (value && value == ContainerToPick) ContainerToPick = null;
            _containerCarrying = value;
        }
    }
    public bool CanPickUp => findContainerToPick() != null;
    private IoPort[] ioPorts;
    private TempField[] tempFields;
    private StateMachine stateMachine;
    private Vector3 destination;
    [SerializeField] private bool reachedTop; // this field is weird cuz of the move strategy, need to update this

    private bool hasIoField {
        get {
            foreach (var port in ioPorts) {
                if (port.CurrentField && port.CurrentField.isActiveAndEnabled) return true;
            }
            return false;
        }
    }
    private bool hasOutField {
        get {
            bool res = false;
            foreach (var port in ioPorts) {
                if (port.CurrentField && port.CurrentField.isActiveAndEnabled && port.CurrentField is OutField) res = true;
            }
            return res;
        }
    }

    private bool hasInField {
        get {
            bool res = false;
            foreach (var port in ioPorts) {
                if (port.CurrentField && port.CurrentField.isActiveAndEnabled && port.CurrentField is InField) res = true;
            }
            return res;
        }
    }
    private void Awake() {
        stackField = FindObjectOfType<StackField>();
        ioPorts = FindObjectsOfType<IoPort>();
        stateMachine = GetComponent<StateMachine>();
        tempFields = FindObjectsOfType<TempField>();

        setStateWaitEvents();
        setStatePickUpEvents();
        setStateMoveInEvents();
        setStateMoveOutEvents();
        setStateRearrangeEvents();
        setStateMoveTempEvents();
    }

    //private void Start() {
    //    //addContainersToSet(stackField.Ground);
    //}

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("container_in") || other.CompareTag("container_temp")) {
            ContainerCarrying = other.GetComponent<Container>();
            if (ContainerCarrying.OutField.isActiveAndEnabled) stateMachine.TriggerByState("MoveOut");
            else stateMachine.TriggerByState("MoveIn");
            return;
        }
        if (other.CompareTag("container_stacked")) {
            ContainerCarrying = other.GetComponent<Container>();
            if (ContainerToPick == null) { //check the property method to find out why
                foreach (var p in ioPorts) {
                    if (p.CurrentField && p.CurrentField.isActiveAndEnabled && p.CurrentField == ContainerCarrying.OutField) {
                        stateMachine.TriggerByState("MoveOut");
                        return;
                    }
                }
            } else if (stackField.StackableIndex(ContainerCarrying.indexInCurrentField).IsValid) {
                stateMachine.TriggerByState("Rearrange");
            } else {
                stateMachine.TriggerByState("MoveTemp");
            }
            return;
        }
        throw new Exception("illegal crane touch");
    }

    private void Update() {
        if (stateMachine.CurrentState == "Wait") {
            if (ContainerToPick || CanPickUp) {
                stateMachine.TriggerByState("PickUp");
                return;
            }
            if (!reachedTop) moveToWaitPosition();
            return;
        }

        if (stateMachine.CurrentState == "PickUp") {
            if (!ContainerToPick) ContainerToPick = findContainerToPick();
            if (ContainerToPick) moveTo(ContainerToPick.transform.position, false);
            else stateMachine.TriggerByState("Wait");
        } else {
            moveTo(destination, true);
        }
    }

    #region private methods
    private void moveToWaitPosition() {
        if (Parameters.TranslationHeight - transform.position.y > Parameters.DistanceError)
            transform.position += new Vector3(0, Parameters.Vy_Unloaded * Time.deltaTime, 0);
        else reachedTop = true;
    }

    /// <summary>
    ///  
    /// </summary>
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
        transform.position += step * Time.deltaTime;
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
    /// <returns></returns>
    private Container findContainerToPick() {
        var c = findContainerToMoveOut();
        if (c != null) return c;
        c = findContainerToMoveIn();
        if (c != null) return c;
        c = findContainerToRearrange();
        return c;
    }

    private Container findContainerToMoveOut() {
        if (!hasOutField) return null;
        foreach (var p in ioPorts) {
            if (p.CurrentField && p.CurrentField is OutField && p.CurrentField.enabled) {
                foreach (var s in stackField.Ground) {
                    foreach (var c in s.ToArray()) {
                        if (c.OutField == p.CurrentField)
                            return c;
                    }
                }
                foreach (var po in ioPorts) {
                    if (p.CurrentField is InField && p.CurrentField.enabled) {
                        foreach (var s in po.CurrentField.Ground) {
                            foreach (var c in s.ToArray()) {
                                if (c.OutField == p.CurrentField)
                                    return c;
                            }
                        }
                    }
                }
            }
        }
        return null;
    }

    private Container findContainerToRearrange() {
        if (stackField.IsGroundFull) return null;
        foreach (var s in stackField.Ground) {
            if (s.Count == 0) continue;
            var list = s.ToArray();
            var min = list.First(x => x.OutField.TimePlaned == list.Min(y => y.OutField.TimePlaned));
            if (min != s.Peek()) return min;
        }
        return null;
    }

    private Container findContainerToMoveIn() {
        foreach (var t in tempFields) {
            if (t.IsGroundEmpty) continue;
            foreach (var s in t.Ground) {
                if (s.Count > 0) return t.RemoveFromGround(s.Peek());
            }
        }
        if (!hasInField) return null;
        /*if (stackField.Count + 1 >= stackField.MaxCount) return null;*/ // avoid full stack, otherwise will be no arrange possible
        if (stackField.Count >= stackField.MaxCount) return null;
        foreach (var p in ioPorts) {
            if (p.CurrentField && p.CurrentField is InField && p.CurrentField.isActiveAndEnabled) {
                if (stackField.IsGroundFull) return null;
                foreach (var s in p.CurrentField.Ground) {
                    if (s.Count > 0) return s.Peek();
                }
            }
        }
        return null;
    }
    #endregion

    #region statemachine events
    private void setStateWaitEvents() {
        var state = stateMachine.Graph.GetState("Wait");
        state.OnEnterState.AddListener(() => { });
        state.OnExitState.AddListener(() => { });
    }

    private void setStatePickUpEvents() {
        var state = stateMachine.Graph.GetState("PickUp");
        state.OnEnterState.AddListener(() => {
            if (ContainerToPick == null) ContainerToPick = findContainerToPick();
            if (ContainerToPick == null || !ContainerToPick.CurrentField.isActiveAndEnabled) {
                Debug.LogWarning("container to pick is null or the field is not enabled");
                stateMachine.TriggerByState("Wait");
            }
        });
        state.OnExitState.AddListener(() => {
            ContainerCarrying.RemoveFromGround();
            if (ContainerCarrying.InField != null) {
                var inField = ContainerCarrying.InField;
                ContainerCarrying.InField = null;
                if (inField.IsGroundEmpty) inField.DestroyField();
            }
        });
    }

    private void setStateMoveInEvents() {
        var state = stateMachine.Graph.GetState("MoveIn");
        state.OnEnterState.AddListener(() => {
            ContainerCarrying.transform.SetParent(transform);
            destination = stackField.IndexToGlobalPosition(stackField.FindIndexToStack());
        });
        state.OnExitState.AddListener(() => {
            stackField.AddToGround(ContainerCarrying);
            ContainerCarrying = null;
        });
    }

    private void setStateMoveOutEvents() {
        var state = stateMachine.Graph.GetState("MoveOut");
        state.OnEnterState.AddListener(() => {
            ContainerCarrying.tag = "container_out";
            ContainerCarrying.transform.SetParent(transform);
            destination = ContainerCarrying.OutField.IndexToGlobalPosition(ContainerCarrying.OutField.FindIndexToStack());
        });

        state.OnExitState.AddListener(() => {
            ContainerCarrying.OutField.AddToGround(ContainerCarrying);
            ContainerCarrying = null;
            if (ContainerCarrying.OutField.Count == ContainerCarrying.OutField.IncomingContainersCount) {
                ContainerCarrying.OutField.DestroyField();
            }
        });
    }

    private void setStateRearrangeEvents() {
        var state = stateMachine.Graph.GetState("Rearrange");
        state.OnEnterState.AddListener(() => {
            ContainerCarrying.tag = "container_rearrange";
            ContainerCarrying.transform.SetParent(transform);
            var index = stackField.FindIndexToStack(ContainerCarrying.indexInCurrentField);
            if (index.IsValid) destination = stackField.IndexToGlobalPosition(index);
            else stateMachine.TriggerByState("Wait");
        });
        state.OnExitState.AddListener(() => {
            var diffVec = transform.position - destination;
            var diffVec2 = new Vector2(diffVec.x, diffVec.z);
            if (diffVec2.sqrMagnitude < Parameters.SqrDistanceError) {
                ContainerCarrying.transform.SetParent(stackField.transform);
                stackField.AddToGround(ContainerCarrying);
                ContainerCarrying = null;
            }
        });
    }

    private void setStateMoveTempEvents() {
        TempField tempField = tempFields[0];
        var state = stateMachine.Graph.GetState("MoveTemp");
        state.OnEnterState.AddListener(() => {
            ContainerCarrying.tag = "container_temp";
            ContainerCarrying.transform.SetParent(transform);
            tempField = tempFields[UnityEngine.Random.Range(0, tempFields.Length)];
            var index = tempField.FindIndexToStack();
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
