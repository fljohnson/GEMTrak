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
	private Vector3 forward = Vector3.zero;
	
	public float acceleration;
	public float braking;
	public float angularSpeed;
	public float stoppingDistance;
	
	private bool reloading = false;
	private Vector3 reloadPoint;
	
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
		agent = GetComponent<NavMeshAgent>();
		//agent.updateRotation = false;
    }
    
    //here's the new wrinkle: we set the relative destination as a function of transform.forward and agent.speed*deltaTime
    protected override void CheckPosition() {
		if(reloading) {
			FinishReload();
			return;
		}
		Vector3 dPosition;
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
			forward.x = Mathf.Sin(heading*Mathf.Deg2Rad);
			forward.z = Mathf.Cos(heading*Mathf.Deg2Rad);
			rotacion.y = dSteer;
		
			
			transform.Rotate(rotacion);
			dPosition = transform.forward*100f;
			NavMeshHit hit;
			if(agent.Raycast(transform.position+dPosition,out hit)) {
				
			/*
				if(hit.distance<0.001f) {
					Debug.Break();
				}*/
				dPosition=transform.forward*hit.distance;
			}
			agent.speed =speed;
			agent.SetDestination(transform.position+dPosition);
			
		
		
		
	}
	
	void OnCollisionEnter(Collision collision) {
		if(handlingCollision) {
			return;
		}
		handlingCollision = true;
		StartReload();
		/*
		Debug.Log("Player At:"+transform.position.ToString("F2")+" going to "+Circuit.Waypoint(curWaypoint-1).ToString("F2"));
		Debug.Break();
		*/
	}
	
	void StartReload() {
		reloading = true;
		//hide the child GameObject(s)
		for(int i=0;i<transform.childCount;i++) {
			transform.GetChild(i).gameObject.SetActive(false);
		}
		
		//float back to last waypoint
		agent.enabled = false;
		reloadPoint = (Circuit.Waypoint(curWaypoint-1));
		reloadPoint.y = transform.position.y;
		rb.velocity =4f*(reloadPoint - transform.position).normalized;
		
	}
	
	void FinishReload() {
		if((reloadPoint - transform.position).magnitude < stoppingDistance){
			
			//Debug.Log("arrived:"+transform.position.ToString("F2")+" going to "+reloadPoint.ToString("F2")+" "+stoppingDistance);
			rb.velocity = Vector3.zero;
			//Debug.Break();
			/*
			-set rotation.y to that of curWaypoint-1
			-calculate heading as done in ActualStart()
			-re-show hidden child GameObject(s)
			*/
			
			Vector3 rot = transform.eulerAngles;
			rot.y = Circuit.WaypointAngleDegrees(curWaypoint-1);
			transform.eulerAngles = rot;
			heading = Mathf.Round(Mathf.Atan2(transform.forward.x,transform.forward.z)*Mathf.Rad2Deg);
			for(int i=0;i<transform.childCount;i++) {
				transform.GetChild(i).gameObject.SetActive(true);
			}
			reloading = false;
			handlingCollision = false;
			agent.enabled = true;
			speed = 0;
		}
	}
	
	protected override void OnTriggerEnter(Collider other) {
		if(!reloading) {
			base.OnTriggerEnter(other);
		}
	}
	
}
