using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Raceur : MonoBehaviour, IComparable
{
	
	public GameObject stunBlastPrototype;
	public float topSpeed;
	public int laps = 0;
	protected int nextWaypoint = 0;
	protected int curWaypoint =0;
	protected NavMeshAgent agent;
	protected bool handlingCollision = false;
	protected bool started=false;
	protected AudioSource audiodeck;
	protected Transform muzzle;
    // Start is called before the first frame update
    protected virtual void ActualStart()
    {
		agent = GetComponent<NavMeshAgent>();
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
        CheckPosition();
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
			LapCompletion();
			//Debug.Log(name+":Lap "+laps+" completed");
			curWaypoint = 0;
			if(laps == ControlCenter.LapsThisLevel()) {
				DoShutdown();
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
		int hitWaypoint = Array.IndexOf(Circuit.instance.turns,other.transform)+1;
		if(AlreadyThere(hitWaypoint)) {
			return;
		}
		
		//Debug.Log(name+":curWaypoint:"+curWaypoint+"; hitWaypoint:"+hitWaypoint);
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
	
	void DoShutdown() {
		Debug.Break();
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
	}
	
	public virtual Vector3 GetVelocity() {
		return agent.velocity;
	}
	
	protected void Fire() {
		GameObject orb = Instantiate(stunBlastPrototype,muzzle.position,Quaternion.identity);
		orb.GetComponent<Stunshot>().Launch(this);
	}
}
