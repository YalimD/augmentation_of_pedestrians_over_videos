using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionScript : MonoBehaviour {

    void OnCollisionExit(Collision collisionInfo)
    {
        if (collisionInfo.transform.tag == "Projection")
        {
            RVO.AgentBehaviour.Instance.incrementProjectedCollision();
        }
        else if (collisionInfo.transform.tag == "Agent")
        {
            RVO.AgentBehaviour.Instance.incrementArtificialCollision();
        }
        Debug.Log(collisionInfo.transform.tag);
    }
}
