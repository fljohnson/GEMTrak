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
	
	public GameObject crashDebris;
	public float wreckageWait;
	
	private bool reloading = false;
	private Vector3 reloadPoint;
	private Vector3 priorPosition; //to help turn the right way during the reload cycle
	private bool atReloadPoint = false;
	
	private float wreckageTimer;
	private Vector3 crashAngle;
	private NavMeshPath reloadPath;
	
	public float maxDisplaySpeed = 173.984f;
	private ArrayList lapTimes = new ArrayList();
	private float lapStart;
	
	private static PlayerRaceur instance;
	
	 	
    // Start is called before the first frame update
    void Start()
    {
		instance = this;
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
		reloadPath = new NavMeshPath();
		//Go();
    }
    
    //see notes on Raceur.Go()
    public override void Go() {
		lapStart = Time.time;
	}
    //here's the new wrinkle: we set the relative destination as a function of transform.forward and agent.speed*deltaTime
    protected override void CheckPosition() {
		if(lapStart == 0f) {
			return;
		}
		if(reloading) {
			if(wreckageTimer < Time.time) {
				FinishReload();
			}
			else
			{
				StartReload();
			}
			return;
		}
		if(Input.GetKeyDown("space")) {
			Fire();
		}
		
		float pedals = Input.GetAxis("Vertical");
		
		float steeringWheel = Input.GetAxis("Horizontal");
		
		float speed1 = Mathf.Min(topSpeed,speed+pedals*acceleration*Time.deltaTime);
		if(pedals < 0) {
			speed1 = Mathf.Max(0,speed+pedals*braking*Time.deltaTime);
		}
		float dSpeed = speed1 -speed;
		
		speed = speed1;
		if(dSpeed > 0) {
			Accelerate();
		}
		if(dSpeed < 0) {
			Decelerate();
		}
		if(!agent.updateRotation) {
			agent.updateRotation = true;
		}
		
			float dSteer = 0;
			if(steeringWheel !=0 ) {
				 dSteer = Mathf.Sign(steeringWheel)*(angularSpeed*Time.deltaTime);
			 }
			heading+=dSteer;
			forward.x = Mathf.Sin(heading*Mathf.Deg2Rad);
			forward.z = Mathf.Cos(heading*Mathf.Deg2Rad);
			rotacion.y = dSteer;
		
			
			transform.Rotate(rotacion);
			agent.velocity = transform.forward*speed;
			/*
			dPosition = transform.forward*speed*Time.time;
			NavMeshHit hit;
			if(agent.Raycast(transform.position+dPosition,out hit)) {
				
				dPosition=transform.forward*hit.distance;
			}
			agent.speed =speed;
			agent.SetDestination(transform.position+dPosition);
			*/
		
		
		
	}
	
	protected override void ProcessShutdown() {
		
		
		float dSpeed = deceleration*Time.deltaTime;
		shutdownTimer-=Time.deltaTime ;
		if(shutdownTimer<0 || dSpeed >= speed) {
			Stop();
			shutdownTimer= -1f;
			Debug.Log(name+" has finished");
			return;
		}
		
		speed -= dSpeed;
		agent.velocity=(Circuit.Waypoint(0)-transform.position).normalized*speed;
		SetEngineAudio(speed/topSpeed);
	}
	
	void OnCollisionEnter(Collision collision) {
		if(handlingCollision) {
			return;
		}
		handlingCollision = true;
		/* overcomplication
		Stunshot s = collision.gameObject.GetComponent<Stunshot>();
		if(s != null) {
			s.CheckHittee(this);
			return;
		}
		*/
		//hide the child GameObject(s)
		for(int i=0;i<transform.childCount;i++) {
			transform.GetChild(i).gameObject.SetActive(false);
		}
		if(audiodeck != null) {
			audiodeck.Stop();
		}
		Instantiate(crashDebris,transform.position,Quaternion.identity);
		wreckageTimer = Time.time + wreckageWait;
		reloading = true;
		//StartReload();
		/*
		Debug.Log("Player At:"+transform.position.ToString("F2")+" going to "+Circuit.Waypoint(curWaypoint-1).ToString("F2"));
		Debug.Break();
		*/
	}
	
	void StartReload() {
		//reloading = true;
		
		//float back to last waypoint
		atReloadPoint = false;
		agent.updateRotation = false;
		agent.updatePosition = false;
		reloadPoint = (Circuit.Waypoint(Mathf.Max(curWaypoint-1,0)));
		reloadPoint.y = transform.position.y;
		agent.speed = (reloadPoint - transform.position).magnitude/6f; //make the trip in about 4s
		//agent.SetDestination(reloadPoint);
		agent.velocity = Vector3.zero;
		//agent.CalculatePath(reloadPoint,reloadPath);
		priorPosition = transform.position;
		crashAngle = transform.forward;
		//Debug.Log("Break "+crashAngle.ToString("F2")+" "+transform.eulerAngles.y);
		//Debug.Break();
		
	}
	
	void FinishReload() {
		
		if(atReloadPoint) {	
			//Debug.Log("arrived:"+transform.position.ToString("F2")+" going to "+reloadPoint.ToString("F2")+" "+agent.stoppingDistance);
			rb.velocity = Vector3.zero;
			//Debug.Break();
			/*
			-set rotation.y to that of curWaypoint-1
			-calculate heading as done in ActualStart()
			-re-show hidden child GameObject(s)
			*/
			
			Vector3 rot = transform.eulerAngles;
			if(curWaypoint > 0) {
				rot.y = Circuit.WaypointAngleDegrees(curWaypoint-1);
			}
			else {
				rot.y = Circuit.WaypointAngleDegrees(0);
			}
			transform.eulerAngles = rot;
			heading = Mathf.Round(Mathf.Atan2(transform.forward.x,transform.forward.z)*Mathf.Rad2Deg);
			for(int i=0;i<transform.childCount;i++) {
				transform.GetChild(i).gameObject.SetActive(true);
			}
			//reloadPathIndex=-1;
			reloading = false;
			handlingCollision = false;
			agent.speed=0;
			agent.Warp(transform.position);
			agent.updatePosition = true;
			speed = 0;
			//Debug.Break();
		}
		else
		{
			/*
			if(reloadPath.status !=  NavMeshPathStatus.PathComplete) {
				return;
			}
			
			if(reloadPathIndex <0) {
				reloadPathIndex=1;
				agent.updatePosition = false;
			}
			*/
			
			
			if(MoveForReload())
			{
				//reloadPathIndex++;
				//if(reloadPathIndex==reloadPath.corners.Length){
					atReloadPoint = true;
				//}
			}
			
		}
	}
	
	bool MoveForReload() {
		Vector3 dPos = (reloadPoint-transform.position);
		dPos.y = 0;
			
		if(dPos.magnitude < .707f) { //1/square root of 2 seemed sufficient
			//Debug.Log("Brakes:"+reloadPath.corners[reloadPathIndex].ToString("F2"));
			return true;
		}
		transform.position+=dPos.normalized*agent.speed*Time.deltaTime;
		transform.forward=-dPos.normalized;
		return false;
	}
	protected override void OnTriggerEnter(Collider other) {
		if(!reloading) {
			base.OnTriggerEnter(other);
		}
		else {
			
			int hitWaypoint = Array.IndexOf(Circuit.instance.turns,other.transform)+1;
			if(hitWaypoint > curWaypoint) { //TODO:get actual next waypoint
				curWaypoint = hitWaypoint;
				Debug.Log("whoopsie");
				return;
			}
		}
	}
	
	protected override void Accelerate() {
		
		SetEngineAudio(speed/topSpeed);
	}
	
	protected override void Decelerate() {
		
		SetEngineAudio(speed/topSpeed);
	}
	
	
	
	public int GetPlace() {
		return Circuit.Place(this);
	}
	
	public String GetDashboardSpeed() {
		float displaySpeed = (speed/topSpeed)*maxDisplaySpeed;
		return displaySpeed.ToString("f1")+"mph";
	}
	
	protected override void LapCompletion() {
		int rawSec = (int)(Time.time - lapStart);
		lapTimes.Add(rawSec);
		lapStart = Time.time;
	}
	
	public String LastLapTime() {
		int i = lapTimes.Count-1;
		if(i<0) {
			return "FAIL";
		}
		int rawSecs = (int)lapTimes[i];
		int mins = (int)(rawSecs/60);
		int secs = rawSecs-mins*60;
		return mins+":"+secs.ToString("d2");
	}
	
	public static int Waypoint() {
		return instance.GetWaypoint();
	}
}
