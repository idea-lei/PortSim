﻿using Ilumisoft.VisualStateMachine;
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
    private HashSet<Container> containersToMove {
        get {
            var set = new HashSet<Container>();
            foreach (var p in ioPorts) {
                if (p.CurrentField is InField && p.CurrentField.isActiveAndEnabled) {
                    foreach (var s in p.CurrentField.Ground) {
                        foreach (var c in s.ToArray()) {
                            set.Add(c);
                        }
                    }
                }
                if (p.CurrentField != null && p.CurrentField is OutField && p.CurrentField.isActiveAndEnabled) {
                    foreach (var s in stackField.Ground) {
                        foreach (var c in s.ToArray()) {
                            if (c.OutField == p.CurrentField) set.Add(c);
                        }
                    }
                }
            }
            return set;
        }
    }
    public Container ContainerToPick;
    [SerializeField] private Container containerCarrying;
    private IoPort[] ioPorts;
    private StateMachine stateMachine;
    private Vector3 destination;

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
    private bool canPickUp {
        get {
            if (containersToMove.Count <= 0) {
                Debug.Log("containersToMove is empty");
                return false;
            }
            if (!hasIoField) {
                Debug.Log("has no IoField");
                return false;
            }
            if (!stackField.NeedRearrange.Item1) {
                Debug.Log("no need to rearrange");
                return false;
            }
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

    //private void Start() {
    //    //addContainersToSet(stackField.Ground);
    //}

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("container_in")) {
            containerCarrying = other.GetComponent<Container>();
            stateMachine.TriggerByState("MoveIn");
            return;
        }
        if (other.CompareTag("container_stacked")) {
            containerCarrying = other.GetComponent<Container>();
            foreach (var p in ioPorts) {
                if (p.CurrentField && p.CurrentField.isActiveAndEnabled && p.CurrentField == containerCarrying.OutField) {
                    stateMachine.TriggerByState("MoveOut");
                    return;
                }
            }
            stateMachine.TriggerByState("Rearrange");
            return;
        }
        throw new Exception("illegal crane touch");
    }

    private void Update() {
        if (stateMachine.CurrentState == "Wait") {
            if (hasIoField) {
                var c = findContainerToMoveOut();
                if (c != null) stateMachine.TriggerByState("PickUp");
            }
            return;
        }

        if (stateMachine.CurrentState == "PickUp") {
            if (ContainerToPick == null) {
                ContainerToPick = findContainerToPick();
                return;
            }
            if (ContainerToPick != null)
                moveTo(ContainerToPick.transform.position, false);
            else stateMachine.TriggerByState("Wait");
        } else {
            moveTo(destination, true);
        }
    }

    #region private methods

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
        if (Mathf.Abs(transform.position.y - Parameters.TranslationHeight) > Parameters.DistanceError
            && transform.position.y < Parameters.TranslationHeight) return Movement.up;
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
                        if (c.OutField == p.CurrentField
                            && stackField.GlobalPositionToIndex(transform.position) != c.indexInCurrentField)
                            return c;
                    }
                }
                foreach (var po in ioPorts) {
                    if (p.CurrentField is InField && p.CurrentField.enabled) {
                        foreach (var s in po.CurrentField.Ground) {
                            foreach (var c in s.ToArray()) {
                                if (c.OutField == p.CurrentField
                                    && po.CurrentField.GlobalPositionToIndex(transform.position) != c.indexInCurrentField)
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
        foreach (var s in stackField.Ground) {
            if (s.Count == 0) continue;
            var list = s.ToArray();
            var min = list.First(x => x.OutField.TimePlaned == list.Min(y => y.OutField.TimePlaned));
            if (min != s.Peek()
                && stackField.GlobalPositionToIndex(transform.position) != min.indexInCurrentField)
                return min;
        }
        return null;
    }

    private Container findContainerToMoveIn() {
        if (!hasInField) return null;
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
            if (ContainerToPick == null) ContainerToPick = findContainerToPick();
            if (ContainerToPick == null || !ContainerToPick.CurrentField.isActiveAndEnabled) {
                Debug.LogWarning("container to pick is null or the field is not enabled");
                stateMachine.TriggerByState("Wait");
            }
        });
        state.OnExitState.AddListener(() => { });
    }

    private void setMoveInEvents() {
        var state = stateMachine.Graph.GetState("MoveIn");
        state.OnEnterState.AddListener(() => {
            containerCarrying.RemoveFromGround();

            var inField = containerCarrying.InField;
            containerCarrying.InField = null;
            if (inField.IsGroundEmpty) inField.DestroyField();

            containerCarrying.transform.SetParent(transform);
            destination = stackField.IndexToGlobalPosition(stackField.FindAvailableIndexToStack(this));
            ContainerToPick = null;
        });
        state.OnExitState.AddListener(() => {
            stackField.AddToGround(containerCarrying);
            containerCarrying = null;
        });
    }

    private void setMoveOutEvents() {
        var state = stateMachine.Graph.GetState("MoveOut");
        state.OnEnterState.AddListener(() => {
            containerCarrying.RemoveFromGround();
            containerCarrying.tag = "container_out";
            containerCarrying.transform.SetParent(transform);
            destination = containerCarrying.OutField.IndexToGlobalPosition(containerCarrying.OutField.FindAvailableIndexToStack(this));
            ContainerToPick = null;
        });

        state.OnExitState.AddListener(() => {
            containerCarrying.OutField.AddToGround(containerCarrying);

            if (containerCarrying.OutField.Count == containerCarrying.OutField.IncomingContainersCount) {
                containerCarrying.OutField.DestroyField();
            }
            containerCarrying = null;
        });
    }

    private void setRearrangeEvents() {
        var state = stateMachine.Graph.GetState("Rearrange");
        state.OnEnterState.AddListener(() => {
            containerCarrying.tag = "container_rearrange";
            containerCarrying.RemoveFromGround();
            containerCarrying.transform.SetParent(transform);
            var index = stackField.FindAvailableIndexToStack(this);
            if (index.IsValid) destination = stackField.IndexToGlobalPosition(index);
            else stateMachine.TriggerByState("Wait");
        });
        state.OnExitState.AddListener(() => {
            var diffVec = transform.position - destination;
            var diffVec2 = new Vector2(diffVec.x, diffVec.z);
            if (diffVec2.sqrMagnitude < Parameters.SqrDistanceError) {
                containerCarrying.transform.SetParent(stackField.transform);
                if (ContainerToPick == containerCarrying) ContainerToPick = null;
                stackField.AddToGround(containerCarrying);
            }
        });
    }
    #endregion
}
