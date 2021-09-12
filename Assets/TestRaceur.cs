using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class TestRaceur : Raceur
{
	private float timer = 3f;
    // Start is called before the first frame update
    protected override void ActualStart()
    {
		timer+=Time.time;
    }
    
    // Update is called once per frame
    protected override void Update()
    {
        if(timer > Time.time) {
			return;
		}
		if(agent == null) {
			agent = GetComponent<NavMeshAgent>();
			agent.updatePosition = false;
			agent.updateRotation = false;
			agent.SetDestination(Circuit.Waypoint(nextWaypoint));
			//pathIndex = 0;
		}
		base.Update();
    }
}
