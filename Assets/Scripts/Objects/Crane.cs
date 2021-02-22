using Ilumisoft.VisualStateMachine;
using System;
using System.Collections;
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
    private HashSet<Container> containersToMove = new HashSet<Container>();
    private List<Container> containersToMoveList => containersToMove.ToList();
    [SerializeField] private Container containerToPick;
    private Container containerCarrying;
    private IoPort[] ioPorts;
    private StateMachine stateMachine;
    private Vector3 destination;

    private bool hasIoField {
        get {
            foreach (var port in ioPorts) {
                if (port.CurrentField.enabled) return true;
            }
            return false;
        }
    }
    private bool hasOutField {
        get {
            bool res = false;
            foreach (var port in ioPorts) {
                if (port.CurrentField.isActiveAndEnabled && port.CurrentField is OutField) res = true;
            }
            return res;
        }
    }
    private bool canPickUp {
        get {
            if (containersToMove.Count <= 0) return false;
            if (!hasIoField) return false;
            if (!stackField.NeedRearrange.Item1) return false;
            return true;
        }
    }
    private void Awake() {
        stackField = FindObjectOfType<StackField>();
        ioPorts = FindObjectsOfType<IoPort>();
        stateMachine = GetComponent<StateMachine>();

        setWaitEvents();
        setPickUpEvents();
        setMoveInEvents();
        setMoveOutEvents();
        setRearrangeEvents();
    }

    private void Start() {
        addContainersToSet(stackField.Ground);
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("container_in")) {
            stateMachine.TriggerByState("MoveIn");
            return;
        }
        if (other.CompareTag("container_stacked")) {
            containerCarrying = other.GetComponent<Container>();
            stateMachine.TriggerByState(containerCarrying == containerToPick ? "MoveOut" : "Rearrange");
            return;
        }
        throw new Exception("illegal crane touch");
    }

    private void Update() {
        if (stateMachine.CurrentState == "Wait") {
            foreach (var port in ioPorts) {
                if (port.CurrentField != null) {
                    addContainersToSet(port.CurrentField.Ground);
                }
            }
            if (canPickUp) {
                stateMachine.TriggerByState("PickUp");
            }
            return;
        }

        if (stateMachine.CurrentState == "PickUp") {
            moveTo(containerToPick.transform.position, false);
        } else {
            moveTo(destination, true);
        }
    }

    #region private methods
    private void addContainersToSet(Stack<Container>[,] field) {
        foreach (var s in field) {
            foreach (var c in s) {
                containersToMove.Add(c);
            }
        }
    }

    // this function need to be trained
    /// <summary>
    /// find container in available containerSet to move, can be movein/out or rearrange
    /// </summary>
    /// <returns></returns>
    private Container findContainerToPick() {
        Field field = null;

        if (hasIoField) {
            // first out principle
            foreach (var port in ioPorts) {
                if (!port.CurrentField.isActiveAndEnabled) continue;
                field = port.CurrentField;
                if (port.CurrentField is OutField) break;
            }
            return containersToMove.Where(x => {
                return field is OutField ? x.OutField == field : x.InField == field;
            }).First();
        } else {
            return stackField.NeedRearrange.Item2;
        }
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
        var actualPos = new Vector2(transform.position.x, transform.position.z);
        if ((actualPos - destination).sqrMagnitude < Parameters.DistanceError) return Movement.down;
        if (Mathf.Abs(transform.position.y - Parameters.TranslationHeight) > Parameters.DistanceError) return Movement.up;
        if (Mathf.Abs(transform.position.z - destination.y) > Parameters.DistanceError) return Movement.z;
        return Movement.x;
    }

    // movement squence should be up - z - x - down
    private enum Movement {
        up,
        z,
        x,
        down
    }

    #endregion

    #region statemachine events
    /// <summary>
    /// only setup onEnter Event, don't mess up
    /// </summary>
    private void setWaitEvents() {
        var state = stateMachine.Graph.GetState("Wait");
        state.OnEnterState.AddListener(() => { });
        state.OnExitState.AddListener(() => { });
    }

    private void setPickUpEvents() {
        var state = stateMachine.Graph.GetState("PickUp");
        state.OnEnterState.AddListener(() => {
            Debug.Log("pickup start");
            if (containerToPick == null) containerToPick = findContainerToPick();
            if (containerToPick == null) throw new Exception("container to pick is null");
        });
        state.OnExitState.AddListener(() => { });
    }

    private void setMoveInEvents() {
        var state = stateMachine.Graph.GetState("MoveIn");
        state.OnEnterState.AddListener(() => { });
        state.OnExitState.AddListener(() => { });
    }

    private void setMoveOutEvents() {
        var state = stateMachine.Graph.GetState("MoveOut");
        state.OnEnterState.AddListener(() => {
            Debug.Log("MoveOut start");
            containerCarrying.RemoveFromGround();
            containerCarrying.tag = "container_out";
            containerCarrying.transform.SetParent(transform);
            destination = containerCarrying.OutField.IndexToGlobalPosition(containerCarrying.OutField.FindAvailableIndexToStack(this));
            containerToPick = null;
        });

        state.OnExitState.AddListener(() => {
            Debug.Log("MoveOut finished");
            containerCarrying.OutField.AddToGround(containerCarrying);
            containersToMove.Remove(containerCarrying);

            if (!containersToMove.Any(x => x.OutField == containerCarrying.OutField)){
                containerCarrying.OutField.DestroyField();
            }
            containerCarrying = null;
        });
    }

    private void setRearrangeEvents() {
        var state = stateMachine.Graph.GetState("Rearrange");
        state.OnEnterState.AddListener(() => {
            Debug.Log("Rearrange");
            containerCarrying.tag = "container_rearrange";
            containerCarrying.RemoveFromGround();
            containerCarrying.transform.SetParent(transform);
            destination = stackField.IndexToGlobalPosition(stackField.FindAvailableIndexToStack(this));
        });
        state.OnExitState.AddListener(() => {
            containerCarrying.transform.SetParent(stackField.transform);
            stackField.AddToGround(containerCarrying);
        });
    }
    #endregion
}
