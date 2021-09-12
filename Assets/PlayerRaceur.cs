using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerRaceur : Raceur
{
	private Rigidbody rb;
	private float heading = 0f;
	
	private float speed = 0f;
	private Vector3 rotacion = Vector3.zero;
	
	public float acceleration;
	public float braking;
	public float angularSpeed;
	public float stoppingDistance;
	
    // Start is called before the first frame update
    void Start()
    {
       rb=GetComponent<Rigidbody>(); 
    }

    // Update is called once per frame
   /* void Update()
    {
        
    }*/
    protected override void ActualStart()
    {
		heading = Mathf.Round(Mathf.Atan2(transform.forward.x,transform.forward.z)*Mathf.Rad2Deg);
		
		//agent.updateRotation = false;
    }
    
    //here's the new wrinkle: we set the relative destination as a function of transform.forward and agent.speed*deltaTime
    protected override void CheckPosition() {
		float pedals = Input.GetAxis("Vertical");
		
		float steeringWheel = Input.GetAxis("Horizontal");
		
		float speed1 = Mathf.Min(topSpeed,speed+pedals*acceleration*Time.deltaTime);
		if(pedals < 0) {
			speed1 = Mathf.Max(0,speed+pedals*braking*Time.deltaTime);
		}
		float dSpeed = speed1 -speed;
		speed = speed1;
		
			float dSteer = 0;
			if(steeringWheel !=0 ) {
				 dSteer = Mathf.Sign(steeringWheel)*(angularSpeed*Time.deltaTime);
			 }
			heading+=dSteer;
			rotacion.y = dSteer;
		
			
			transform.Rotate(rotacion);
			if(rotacion.y !=0) {
				rb.velocity = transform.forward*speed;
			}
			else {
				rb.velocity += transform.forward*dSpeed;
				//rb.AddForce(transform.forward*dSpeed,ForceMode.VelocityChange);
			}
		/*	
		if((transform.position - Circuit.Waypoint(nextWaypoint)).magnitude < stoppingDistance) {
			Debug.Log("Turn "+(nextWaypoint+1)+" done");
			nextWaypoint++;
		}*/
		
		
	}
	
	void OnCollisionEnter(Collision collision) {
		if(handlingCollision) {
			return;
		}
		handlingCollision = true;
		Debug.Log("Schiesse");
		Debug.Break();
	}
	
	void OnTriggerExit(Collider other) {
		int hitWaypoint = Array.IndexOf(Circuit.instance.turns,other.transform)+1;
		if(hitWaypoint <= nextWaypoint) {
			return;
		}
		
		curWaypoint = hitWaypoint;
		nextWaypoint = hitWaypoint;
		if(nextWaypoint >= Circuit.instance.turns.Length) {
			laps++;
			//nextWaypoint=Mathf.Max(0,nextWaypoint - Circuit.instance.turns.Length);
			Debug.Log(name+":lap completed "+laps);
			if(laps >1) {
				Debug.Break();
				return;
			}
		}
	}
}
