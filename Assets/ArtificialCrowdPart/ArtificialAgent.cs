using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;

/* 
 * Written by Yalım Doğan
 * 
 * The logic of the artificial agents
 */

namespace RVO
{

    public class ArtificialAgent : MonoBehaviour
    {
        //Properties

        #region PROPERTY

        //The material to be set when user selects this agent
        public static Material selectedMat;

        private Renderer[] childrenRenderers;
        private Material[] defaultMaterials;
        private Material defaultMaterial = null;
        private Material defaultHairMaterial = null;
        private Animator anim;
        
        private bool selected; //Is this agent currently selected by user to modify its RVO properties

        public struct UserCoefficients
        {
            public float speedCoefficient;
            public float rangeCoefficient;
            public float reactionCoefficient;
        } 

        public UserCoefficients coefficients;

        private bool forced; //When the agent is forced to move from its original position in order to solely avoid collision
                            // it should return to its original position after certain time has passed 
        private Vector3 stableLocation;
        private float stableTimer; //Timer for returning to original position

        public int AgentId { get; set; }
        public Agent AgentReference { get; set; }

        #endregion

        public void createAgent(int id, Agent agentReference)
        {
            AgentId = id;
            AgentReference = agentReference;
            anim = transform.GetComponent<Animator>();

            childrenRenderers = GetComponentsInChildren<Renderer>();
            defaultMaterials = new Material[childrenRenderers.Length];
            for(int i = 0; i < childrenRenderers.Length; i++)
            {
                defaultMaterials[i] = childrenRenderers[i].material;
            }

            defaultMaterial = transform.Find("body").GetComponent<Renderer>().material;
            if (transform.Find("hair"))
                defaultHairMaterial = transform.Find("hair").GetComponent<Renderer>().material;

            goal = transform.position;
            path = null;
            pathStatus = -1;
            
            coefficients = new UserCoefficients();
            coefficients.speedCoefficient = 1f;
            coefficients.rangeCoefficient = 1f;
            coefficients.reactionCoefficient = 1f;

            forced = false;
            stableLocation = Vector3.zero;
            stableTimer = 0f;

            // Set type for walk and idle animations to create variety
            anim.SetInteger("Type1", (int)Math.Floor(UnityEngine.Random.value * 4));
            anim.SetInteger("Type2", (int)Math.Floor(UnityEngine.Random.value * 7));
        }

        public void Step()
        {

            // Updating the animations
            // anim.SetFloat("Velocity", (RVOMath.abs(AgentReference.velocity_) / AgentReference.maxSpeed_) * coefficients.speedCoefficient);

            anim.SetBool("Walking", (RVOMath.abs(AgentReference.velocity_) / AgentReference.maxSpeed_) * coefficients.speedCoefficient > 0);

            if (!AgentBehaviour.Instance.Visibility)
            {
                transform.Find("body").GetComponent<Renderer>().enabled = false;
                if (defaultHairMaterial)
                    transform.Find("hair").GetComponent<Renderer>().enabled = false;
            }
            else
            {
                transform.Find("body").GetComponent<Renderer>().enabled = true;
                if (defaultHairMaterial)
                    transform.Find("hair").GetComponent<Renderer>().enabled = true;
            }

            //Assign the stable position (before migrating because of collision)
            if (forced)
                stableTimer++;

            if (stableTimer == 30f)
            {
                Debug.Log("Stabilizing");
                setDestination(stableLocation,0.01f);
                forced = false;
                stableTimer = 0f;
            }

            //Debug.Log("Artificial Pedestrian with velocity " + agentReference.velocity_ + " and position " + agentReference.position_);   
        }

        private Vector3 goal;
        private NavMeshPath path;
        private int pathStatus;
        private float goalOffset;

        public void setDestination(Vector3 destination,float offset = 0.1f)
        {
            goal = destination;
            path = new NavMeshPath();
            NavMesh.CalculatePath(transform.position, goal, NavMesh.AllAreas, path);
            pathStatus = 0;

            goalOffset = offset;

            forced = false;
            stableTimer = 0f;

            Debug.Log("Going with range " + AgentReference.neighborDist_);
            Debug.Log("Going with speed " + AgentReference.maxSpeed_);


        }
        Vector2 goalDirection = new Vector2(0.0f, 0.0f);

        public void setPreferred()
        {
            goalDirection = new Vector2(0.0f, 0.0f);
            if (path != null && pathStatus < path.corners.Length)
            {
                goalDirection = new Vector2(path.corners[pathStatus].x, path.corners[pathStatus].z) - new Vector2(transform.position.x,transform.position.z);

                if (RVOMath.absSq(goalDirection) < goalOffset)
                {
                    pathStatus++;

                }
                if (RVOMath.absSq(goalDirection) > AgentReference.maxSpeed_ * 0.9)
                {
                    //Choose between two; max speed or less. (This provides somewhat smooth movement)
//                    float speed = userCoefficient * (float)((RVOMath.abs(RVOMath.normalize(goalDirection) / 1f) > 0.1) ? 0.1 : RVOMath.abs(RVOMath.normalize(goalDirection) / 1f));
                    float speed = (float)((RVOMath.abs(RVOMath.normalize(goalDirection)) > AgentReference.maxSpeed_ * 0.9) ? AgentReference.maxSpeed_ * 0.9 : RVOMath.abs(RVOMath.normalize(goalDirection)) * AgentReference.maxSpeed_);
                    goalDirection = new Vector2(RVOMath.normalize(goalDirection).x() * speed, RVOMath.normalize(goalDirection).y() * speed);

                }
 
            }
            else
            {
                pathStatus = -1;
                path = null;
                //setDestination(transform.position);
            }

            AgentReference.prefVelocity_ = goalDirection *  RVOMagnify.Magnify;
        }

        public void updateVelo()
        {
            //Debug.Log("Agent " + agentId + " - " + RVOMath.abs(agentReference.velocity_) + " with path length" + path);

            //If the agent doesnt have a path but it needs to move to avoid collision
            if (pathStatus == -1 && RVOMath.abs(AgentReference.velocity_) >= 0.01f && !forced )
            {
                forced = true;
                stableLocation = transform.position;
            }

            goalDirection = new Vector2(AgentReference.velocity_.x() /  RVOMagnify.Magnify,
                                        AgentReference.velocity_.y() /  RVOMagnify.Magnify);
            transform.position = new Vector3(AgentReference.position_.x_ /  RVOMagnify.Magnify,
                                             transform.position.y, AgentReference.position_.y_ /  RVOMagnify.Magnify);

            if (new Vector3(goalDirection.x(), 0f, goalDirection.y()).magnitude != 0)
            {
                Quaternion rotation = Quaternion.LookRotation(new Vector3(goalDirection.x(), 0f, goalDirection.y()));
                rotation.x = 0;
                rotation.z = 0;
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * RVOMagnify.Magnify);
            }

         //   }
       //    Debug.Log("goal direction:" + goalDirection + "with length" + RVOMath.abs(goalDirection)+" Velocity is:" + new Vector3(agentReference.velocity_.x(), 0, agentReference.velocity_.y()));
            //Debug.Log("Pos" + transform.position + "with RVO" + agentReference.position_ );

        }

        //Change the material's emission to highlight the agent
        internal void setSelected()
        {
            selected = true;
            for (int i = 0; i < childrenRenderers.Length; i++)
            {
                childrenRenderers[i].material = selectedMat;
            }

            /*transform.Find("body").GetComponent<Renderer>().material = selectedMat;
            if (defaultHairMaterial)
                transform.Find("hair").GetComponent<Renderer>().material = selectedMat;*/
        }

        internal void deSelect()
        {
            selected = false;
            for (int i = 0; i < childrenRenderers.Length; i++)
            {
                childrenRenderers[i].material = defaultMaterials[i];
            }
            /*
            transform.Find("body").GetComponent<Renderer>().material = defaultMaterial;
            if (defaultHairMaterial)
                transform.Find("hair").GetComponent<Renderer>().material = defaultHairMaterial;
                */
        }

        internal bool isSelected()
        {
            return selected;
        }


    }

}