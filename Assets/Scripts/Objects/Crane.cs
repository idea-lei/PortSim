using Ilumisoft.VisualStateMachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crane : MonoBehaviour
{
    private StateMachine stateMachine;
    private Container containerCarry;
    private Container containerWant;
    private void Awake() {
        stateMachine = FindObjectOfType<StateMachine>();
    }
    private void OnTriggerEnter(Collider other) {
        switch (other.tag) {
            case "container_in":
            case "container_stacked":
                // pickup and decide where to go
                break;
            default:
                throw new Exception("illegal crane touch");
        }
    }

    #region private methods
    /// <summary>
    /// find container in inField or StackField to move
    /// </summary>
    /// <returns></returns>
    public Container FindContainerToPick() {
        throw new NotImplementedException();
    }

    /// <summary>
    /// move container from inField to stackField, not for outField
    /// </summary>
    /// <returns>available index in stackField</returns>
    public IndexInStack FindIndexToStack() {
        throw new NotImplementedException();
    }
    /// <summary>
    ///  
    /// </summary>
    /// <remarks>
    /// do not modify this method unless you can really figure out 
    /// relationship between the vector2.y here and position.z
    /// </remarks>
    private void moveTo(Vector2 destination, bool isLoaded) {
        // fixedUpdate time span is 0.02f
        float timeSpan = 0.02f;
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
        transform.position += step * timeSpan;
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
}
