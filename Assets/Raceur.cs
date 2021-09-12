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
	private NavMeshAgent agent;
	protected bool handlingCollision = false;
	protected bool started=false;
    // Start is called before the first frame update
    protected virtual void ActualStart()
    {
		agent = GetComponent<NavMeshAgent>();
        agent.SetDestination(Circuit.Waypoint(nextWaypoint));
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
        CheckPosition();
    }
    
    protected virtual void CheckPosition() {
		Raceur ahead = Circuit.InFrontOf(this);
		
		if(ahead != null) {
			nextWaypoint=Mathf.Min(ahead.GetWaypoint()+1,Circuit.instance.turns.Length-1);
			
			agent.speed = topSpeed;
			HandleWaypointChange();
			return;
		}
		
		agent.speed = 7f;
		
		/*
		if(agent.pathPending) {
			return;
		}
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
	
	void HandleWaypointChange() {
		
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
	}
	
	public int GetWaypoint() {
		return curWaypoint;
	}
	
	public int CompareTo (object obj) {
		int myWaypoint = nextWaypoint+laps*Circuit.instance.turns.Length;
		Raceur him = (obj as Raceur);
		int hisWaypoint = him.GetWaypoint()+him.laps*Circuit.instance.turns.Length;
		//Debug.Log(gameObject.name+":"+myWaypoint+" vs "+him.gameObject.name+":"+hisWaypoint);
		if(myWaypoint > hisWaypoint) {
			return -1;
		}
		if(myWaypoint < hisWaypoint) {
			return 1;
		}
		return 0;
			
	}
	
	void OnTriggerExit(Collider other) {
		int hitWaypoint = Array.IndexOf(Circuit.instance.turns,other.transform)+1;
		if(hitWaypoint >curWaypoint) {
			curWaypoint = hitWaypoint;
		}
		HandleWaypointChange();
		if(hitWaypoint <= nextWaypoint) {
			return;
		}
		
		Debug.Log(name+":Turn "+(nextWaypoint+1)+" done");
		curWaypoint = hitWaypoint;
		nextWaypoint = hitWaypoint;
		
		HandleWaypointChange();
	}
}
