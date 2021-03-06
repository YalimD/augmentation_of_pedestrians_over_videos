﻿using UnityEngine;
using System.Collections;

/*
 * Written by Yalım Doğan
 * 
 * A very basic agent model that has a stickman prefab and maintains a velocity starting from an initial spawn point.
 * Should include a reference to its Agent object from RVO where it determines its behaviour. 
 * 
 * The velocity of this agent is given by PedestrianProjection class, it cannot be modified by RVO (There is an option for that)
 * But the Agent reference inside should be updated with velocity read from file. There is no goal in RVO, just preferred velocity.
 * This enables wandering.
 * 
 */
namespace RVO
{
    public class ProjectedAgent : MonoBehaviour
    {
        //Timelimit of agent to be synced with Agent Projection Class
        private const float TIMELIMIT = 15f;

        private bool isSync;
        private float timer;
        private int trackId; //This is the id which is given by the projection 
        private int rvoId;

        //Accessors mutators
        public Vector3 Velocity { set; get; }
        public Vector3 Pos { get { return transform.position; } }
        public int TrackId { get; set; }
        public int RvoId { get; set; }
        public Agent AgentReference { get; private set; }
        public bool IsSync { get; set; }

        //Constructor need 1 parameter id that is assigned to the agent
        public void createAgent(Vector3 initialVelocity, int trackid, int RVOId, Agent agentReference)
        {
            Velocity = initialVelocity;
            trackId = trackid;
            rvoId = RVOId;
            AgentReference = agentReference;
            //So that this agent ignores the neighboring agents in RVO. But participates the collision avoidance

            //Is this agent stil in the output file from detection ?
            isSync = false;
            timer = 0;

            foreach (Transform child in transform)
                child.GetComponent<Renderer>().enabled = false;
        }

        public void Step()
        {
            /* Mechanism to get rid of unsyncronized agents:
             * 
             * Update counts the time which the agent didn't get any signal from projection clas. The signal is identified as a flag
             * When the flag is false, timer counts and destroys this agent upon certain limit.
             * Else, the timer is reset but the flag too, so that on next Update; timer can start again.
             * 
             */
            if (isSync)
            {
                timer = 0;
                isSync = false;
            }
            else
            {
                timer++;
                if(timer > TIMELIMIT)
                    PedestrianProjection.Instance.removeAgent(trackId, transform.gameObject);
            }

            mag = Velocity.magnitude;
            AgentReference.prefVelocity_ = new Vector2(Velocity.x, Velocity.z)  *  RVOMagnify.Magnify; //TODO: RVOmagnifiy
            AgentReference.velocity_ = new Vector2(Velocity.x, Velocity.z)  * RVOMagnify.Magnify;
            AgentReference.position_ = new Vector2(transform.position.x, transform.position.z) * RVOMagnify.Magnify; //TODO: RVOmagnifiy

            //Debug.Log("Projected Pedestrian with ID " + RvoId + " with velocity " + agentReference.velocity_ + " and position " + agentReference.position_);

            transform.Translate(Velocity, Space.World);
            Quaternion rotation = Quaternion.LookRotation(Velocity - transform.position);
            rotation.x = 0;
            rotation.z = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * RVOMagnify.Magnify);

         //   transform.LookAt(velocity);

            if (PedestrianProjection.Instance.Visibility)
            {
                foreach (Transform child in transform)
                    child.GetComponent<Renderer>().enabled = true;
            }
            else
            {
                foreach (Transform child in transform)
                    child.GetComponent<Renderer>().enabled = false;
            }

            if (Velocity.magnitude > PedestrianProjection.Instance.SpeedLimit)
            {
                Debug.Log("Too fast!");
                PedestrianProjection.Instance.removeAgent(trackId, transform.gameObject);
            }

        }

        public float mag;

        /* Destroy agent if it gets out of mesh. This is checked by removing it
         * when its collider doesn't collide with the walkable mesh anymore
         */
        void OnCollisionExit(Collision collisionInfo)
        {
            if (collisionInfo.transform.name == "walkableDebug")
            {
              //  Debug.Log("Left");
                PedestrianProjection.Instance.removeAgent(trackId, transform.gameObject);
            }
        }
        
    }
}