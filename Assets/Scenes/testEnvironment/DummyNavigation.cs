using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;

public class DummyNavigation : MonoBehaviour {

    NavMeshAgent agent;
	// Use this for initialization
	void Start () {
		agent = gameObject.GetComponent<NavMeshAgent>();
        agent.SetDestination(new Vector3(5.0f, 0.0f, -20.0f));
	}
	
	// Update is called once per frame
	void Update () {
		if (agent)
        {
            if (agent.speed > 0.0f)
            {
                Debug.Log("Moving");
            }
        }
	}
}
