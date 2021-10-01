using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Raceur : MonoBehaviour, IComparable
{
	
	public GameObject stunBlastPrototype;
	public float topSpeed;
	public int laps = -1;
	protected int nextWaypoint = 0;
	protected int curWaypoint =0;
	protected NavMeshAgent agent;
	protected bool handlingCollision = false;
	protected bool started=false;
	protected AudioSource audiodeck;
	protected Transform muzzle;
	protected float zappedTime;
	protected bool zapped = false;
	protected GameObject zappedEffect;
	protected float shutdownTimer = -2f;
	protected float deceleration;
	protected float lapStart;
	protected ArrayList lapTimes = new ArrayList();
	protected float penaltyTime = 0f;
    // Start is called before the first frame update
    protected virtual void ActualStart()
    {
		agent = GetComponent<NavMeshAgent>();
		lapStart = -1f;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
		if(!started) {
			if(Circuit.instance != null) {
				audiodeck = GetComponent<AudioSource>();
				muzzle = transform.Find("Muzzle");
				ActualStart();
				started = true;
				//Go();
			}
			else {
				return;
			}
		}
		if(shutdownTimer==-1f) {
			return;
		}
		if(shutdownTimer > 0) {
			ProcessShutdown();
			return;
		}
		if(zapped){
			DoZapCycle();
			return;
		} 
        CheckPosition();
    }
    
    protected virtual void DoZapCycle() {
		zapped = zappedTime> Time.time;
		if(!zapped) {
			Destroy(zappedEffect);
			nextWaypoint = Mathf.Max(nextWaypoint,PlayerRaceur.Waypoint()+1);
			if(nextWaypoint > Circuit.instance.turns.Length-1) {
				nextWaypoint = Circuit.instance.turns.Length-1;
			}
			agent.SetDestination(Circuit.Waypoint(nextWaypoint));
			Debug.Log(name+":Back in the race");
			
		}
	}
	
    //in practice, ControlCenter calls Go() on all Raceurs upon "green light"
    public virtual void Go() {
        agent.SetDestination(Circuit.Waypoint(nextWaypoint));
	}
	
    protected virtual void CheckPosition() {
		Raceur ahead = Circuit.InFrontOf(this);
		
		if(ahead != null) {
			//Try to beat whoever's ahead to his next Waypoint
			//For this to work, at a minimum, the Circuit needs three Waypoints per turn (entry, apex, and exit), as well as two per straightaway
			nextWaypoint = GetNextWaypoint(ahead.GetWaypoint());
			Accelerate();
			return;
		}
		
		Decelerate();
		
		
	}
	
	void HandleWaypointChange() {
		if(curWaypoint >= Circuit.instance.turns.Length) {
			laps++;
			float rawSec = (float)(Time.time - lapStart);
			lapTimes.Add(rawSec);
			lapStart = Time.time;
			LapCompletion();
			
			curWaypoint = 0;
			if(laps == ControlCenter.LapsThisLevel()) {
				shutdownTimer = 4f;
				deceleration=GetVelocity().magnitude/4f; //we'll be explicitly subtracting, so deceleration > 0
				return;
			}
		}
		//if we're in the lead, aim at the next waypoint
		nextWaypoint = GetNextWaypoint(curWaypoint);
	//	Debug.Log("starting :"+name+":"+curWaypoint+" "+nextWaypoint);
		CheckPosition();
		//Debug.Log("ending :"+name+":"+curWaypoint+" "+nextWaypoint);
		agent.SetDestination(Circuit.Waypoint(nextWaypoint-1));
	}
	
	public int GetWaypoint() {
		return curWaypoint;
	}
	
	public int CompareTo (object obj) {
		int myWaypoint = (GetWaypoint()-1)+laps*Circuit.instance.turns.Length;
		Raceur him = (obj as Raceur);
		int hisWaypoint = (him.GetWaypoint()-1)+him.laps*Circuit.instance.turns.Length;
		if(myWaypoint > hisWaypoint) {
			return -1;
		}
		if(myWaypoint < hisWaypoint) {
			return 1;
		}
		//a finer grain: who's farther past the current waypoint? (FLJ, 9/23/2021)
		
		Vector3 coords = Circuit.Waypoint(GetWaypoint());
		
		float hisDistance = HorizDistance(coords,him.transform.position);
		float myDistance = HorizDistance(coords,transform.position);
		if(myDistance <  hisDistance ) {
			return -1;
		}
		if(myDistance > hisDistance) {
			return 1;
		}
		return 0;
			
	}
	
	protected virtual void OnTriggerEnter(Collider other) {
		if(lapStart == -1f) {
			if(Array.IndexOf(Circuit.instance.turns,other.transform) == Circuit.instance.turns.Length-1) {
				lapStart = Time.time;
				return;
			}
		}
		if(shutdownTimer > -2f) {
			return;
		}
		int hitWaypoint = Array.IndexOf(Circuit.instance.turns,other.transform)+1;
		if(laps > -1 && AlreadyThere(hitWaypoint)) {
			Debug.Log(name+":Already there:"+curWaypoint+" vs "+hitWaypoint);
			return;
		}
		
		curWaypoint = hitWaypoint;
		
		HandleWaypointChange();
		
	}
	
	bool AlreadyThere(int waypoint) {
		if(waypoint == 0 && curWaypoint >= Circuit.instance.turns.Length-1) {
			return false;
		}
		if(curWaypoint == 0 && waypoint >= Circuit.instance.turns.Length-1) {
			return true;
		}
		return(waypoint <=curWaypoint);
	}
	
	int GetNextWaypoint(int waypoint) {
		int rv = waypoint+1;
		if(rv > Circuit.instance.turns.Length) {
			return 1;
		}
		return rv;
	}
	
	protected virtual void ProcessShutdown() {
		
		
		float dSpeed = deceleration*Time.deltaTime;
		shutdownTimer-=Time.deltaTime ;
		if(shutdownTimer<0 || dSpeed >= agent.speed) {
			Stop();
			shutdownTimer= -1f;
			Debug.Log(name+" has finished");
			return;
		}
		
		agent.speed -= dSpeed;
		agent.velocity=(Circuit.Waypoint(0)-transform.position).normalized*agent.speed;
		SetEngineAudio(agent.speed/topSpeed);
	}
	
	protected virtual void Accelerate() {
		agent.speed = topSpeed;
		SetEngineAudio(agent.speed/topSpeed);
	}
	
	protected virtual void Decelerate() {
		agent.speed = 7f;
		
		SetEngineAudio(agent.speed/topSpeed);
	}
	
	protected void SetEngineAudio(float factor) {
		if(audiodeck == null) {
			return;
		}
		if(factor == 0 && audiodeck.isPlaying) {
			audiodeck.Stop();
			return;
		}
		if(factor > 0 && !audiodeck.isPlaying) {
			audiodeck.Play();
		}
		audiodeck.pitch = factor*2.7f;	
	}
	
	public float HorizDistance(Vector3 end, Vector3 start) {
		end.y=start.y;
		return (end-start).magnitude;
	} 
	
	protected virtual void LapCompletion() {
		
		Debug.Log(name+" "+((float)lapTimes[laps-1]).ToString("F3"));
	}
	
	public virtual Vector3 GetVelocity() {
		return agent.velocity;
	}
	
	protected void Fire() {
		GameObject orb = Instantiate(stunBlastPrototype,muzzle.position,Quaternion.identity);
		orb.GetComponent<Stunshot>().Launch(this);
	}
	
	public void ImHit(float secsDown) {
		Stop();
		zappedEffect = ControlCenter.GetZapBubble(transform);
		zapped = true;
		zappedTime = Time.time+secsDown;
	}
	
	public virtual void TakePenalty(float secsDown) {
		penaltyTime += secsDown;
	}
	
	protected virtual void Stop() {
		agent.velocity=Vector3.zero;
		agent.speed = 0;
		
	}
}
