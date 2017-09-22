using UnityEngine;
using System.Collections;

/*
 * Class : AISensor
 * Description : This class is to trigger events when the sphere trigger detects a collision to process the NPCs audio and visual threats
 */
public class AISensor : MonoBehaviour {

    // Private
    private AIStateMachine _parentStateMachine = null;
    public AIStateMachine ParentStateMachine { set { _parentStateMachine = value; } }

    void OnTriggerEnter(Collider col)
    {
        //Debug.Log("Sensor Trigger Enter");
        if (_parentStateMachine != null)
        {
            _parentStateMachine.OnTriggerEvent(AITriggerEventType.Enter, col);
        }
    }

    void OnTriggerStay(Collider col)
    {
        //Debug.Log("Sensor Trigger Stay");
        if (_parentStateMachine != null)
        {
            _parentStateMachine.OnTriggerEvent(AITriggerEventType.Stay, col);
        }
    }

    void OnTriggerExit(Collider col)
    {
        //Debug.Log("Sensor Trigger Exit");
        if (_parentStateMachine != null)
        {
            _parentStateMachine.OnTriggerEvent(AITriggerEventType.Exit, col);
        }
    }

}
