using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Raceur : MonoBehaviour, IComparable
{
	public float topSpeed;
	public int laps = 0;
	protected int nextWaypoint = 0;
	protected int curWaypoint =0;
	protected NavMeshAgent agent;
	protected bool handlingCollision = false;
	protected bool started=false;
	protected int pathIndex = 0;
	protected NavMeshPath path; 
	protected Vector3 destination;
    // Start is called before the first frame update
    protected virtual void ActualStart()
    {
		agent = GetComponent<NavMeshAgent>();
		agent.updatePosition = false;
		agent.updateRotation = false;
		path = new NavMeshPath();
		destination = Circuit.Waypoint(nextWaypoint);
		pathIndex=0;
        agent.CalculatePath(destination,path);
    }

    // Update is called once per frame
    protected virtual void Update()
    {
		if(!started) {
			if(Circuit.instance != null) {
				ActualStart();
				started = true;
			}
			else {
				return;
			}
		}
		Advance();
        CheckPosition();
    }
    
    protected virtual void CheckPosition() {
		Raceur ahead = Circuit.InFrontOf(this);
		
		if(ahead != null) {
			nextWaypoint=Mathf.Max(nextWaypoint,Mathf.Min(GetNextWaypoint(ahead.GetWaypoint()),Circuit.instance.turns.Length-1));
			
			agent.speed = topSpeed;
			return;
		}
		
		agent.speed = 7f;
		
		
		
	}
	
	void HandleWaypointChange() {
		/*
		if(curWaypoint < Circuit.instance.turns.Length) {
			agent.SetDestination(Circuit.Waypoint(nextWaypoint));
			return;
		}
		laps++;
		nextWaypoint=Mathf.Max(0,nextWaypoint - Circuit.instance.turns.Length);
		Debug.Log(name+":lap completed "+laps);
		if(laps <2) {
			return;
		}
		Debug.Break();
		*/
		if(curWaypoint >= Circuit.instance.turns.Length) {
			laps++;
			
			if(laps == 2) {
				DoShutdown();
				return;
			}
		}
		
		//if we're in the lead, aim at the next waypoint
		nextWaypoint = GetNextWaypoint(curWaypoint);
		Debug.Log("starting :"+name+":"+curWaypoint+" "+nextWaypoint);
		CheckPosition();
		Debug.Log("ending :"+name+":"+curWaypoint+" "+nextWaypoint);
		pathIndex = 0;
		path.ClearCorners();
		//path.status = NavMeshPathStatus.PathInvalid;
		destination =Circuit.Waypoint(nextWaypoint-1);
		Debug.Log("Where to:"+destination.ToString("F2"));
		agent.CalculatePath(destination,path);
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
		return 0;
			
	}
	
	void OnTriggerEnter(Collider other) {
		int hitWaypoint = Array.IndexOf(Circuit.instance.turns,other.transform)+1;
		if(AlreadyThere(hitWaypoint)) {
			Debug.Log(name+":curWaypoint:"+curWaypoint+"; hitWaypoint:"+hitWaypoint);
			return;
		}
		
		curWaypoint = hitWaypoint;
		
		HandleWaypointChange();
		/*
		if(hitWaypoint <= nextWaypoint) {
			return;
		}
		
		Debug.Log(name+":Turn "+(nextWaypoint+1)+" done");
		curWaypoint = hitWaypoint;
		nextWaypoint = hitWaypoint;
		
		HandleWaypointChange();
		*/
	}
	
	bool AlreadyThere(int waypoint) {
		if(waypoint == 0 && curWaypoint >= Circuit.instance.turns.Length-1) {
			return false;
		}
		return(waypoint ==curWaypoint);
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
	
	void Advance() {
		
		if(path.status == NavMeshPathStatus.PathInvalid) {
			return;
		}
		
		Vector3 diff;
		if(pathIndex  < path.corners.Length)
		{
			diff  = path.corners[pathIndex] - transform.position;
		}
		else {
			diff = destination-transform.position;
		}
		
		diff.y = 0;
		if(diff.magnitude >= agent.stoppingDistance) {
			if(nextWaypoint == 2) {
				Debug.Log("dTheta:"+Mathf.Atan2(diff.x,diff.z)*Mathf.Rad2Deg); //temp:90-atan2
			}
			transform.forward = diff.normalized;
			GetComponent<Rigidbody>().velocity = transform.forward*agent.speed;
			return;
		}
		pathIndex++;
		
		
		/*
		int finalPt=agent.path.corners.Length-1;
		if(finalPt > -1) {
			if((transform.position - agent.path.corners[finalPt]).magnitude >= agent.stoppingDistance) {
				return;
			}
		}
		else {
			if((transform.position - agent.destination).magnitude >= agent.stoppingDistance) {
				return;
			}
		}
		Debug.Log("Waypoint hit");
		Debug.Break();
		curWaypoint=nextWaypoint;
		nextWaypoint++;
		
		HandleWaypointChange();
		*/
		
	}
		
}
