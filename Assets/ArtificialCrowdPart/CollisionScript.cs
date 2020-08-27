using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionScript : MonoBehaviour {

    void OnCollisionExit(Collision collisionInfo)
    {
        if (collisionInfo.transform.tag == "Projection")
        {
            RVO.AgentBehaviour.Instance.IncrementProjectedCollision();
        }
        else if (collisionInfo.transform.tag == "Agent")
        {
            RVO.AgentBehaviour.Instance.IncrementArtificialCollision();
        }
        Debug.Log(collisionInfo.transform.tag);
    }
}
