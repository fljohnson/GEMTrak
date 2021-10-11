using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Circuit : MonoBehaviour
{
	public static Circuit instance;
	
	public Vector3[] gridPositions;
	public Raceur[] field;
	public Transform[] turns;
	private ArrayList distanceBetweenTurns = new ArrayList();
    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        CalculateDistanceBetweenTurns();
        GetField();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    void CalculateDistanceBetweenTurns() {
		for(int i=1;i<turns.Length;i++) {
			distanceBetweenTurns.Add(Raceur.HorizDistance(turns[i].position,turns[i-1].position));
		}
	}
	
	public float LapDistance(int end=-1){
		float rv=0f;
		if(end==-1) {
			end+=distanceBetweenTurns.Count;
		}
		for(int i=0;i<(end+1);i++) {
			rv+=(float)distanceBetweenTurns[i];
		}
		return rv;
	}
    
    //returns coordinates of the zero-based ith waypoint (turn or finish line)
    public static Vector3 Waypoint(int i) {
		return  instance.turns[i].position;
	}
	
	public static float WaypointAngleDegrees(int i) {
		return instance.turns[i].eulerAngles.y;
	}
	
	public static Raceur InFrontOf(Raceur inquirer) {
		Array.Sort(instance.field); //leader will be at posn 0; trailer will be at posn field.Length-1
		int posn = Array.IndexOf(instance.field,inquirer);
		//eliminate the ties
		/*while(posn > 0 && instance.field[posn].GetWaypoint() == instance.field[posn-1].GetWaypoint()) {
			posn--;
		}*/
		
		if(posn == 0) {
			return null;
		}
		return instance.field[posn-1];
	}
	
	public static int Place(Raceur inquirer) {
		return Array.IndexOf(instance.field, inquirer)+1;
	}
	
	
	void GetField() {
		field = FindObjectsOfType<Raceur>();	
		if(ControlCenter.qualifyMode) {
			return;
		}
		//Deactivate all cars that didn't qualify
		foreach(Raceur r in field) {
			int position = ControlCenter.GetGridPosition(r.name);
			if(position == -1) {
				r.gameObject.SetActive(false);//why not destroy?
			}
			else {
				r.transform.position = gridPositions[position];
			}
		}
	}
	
	public static int PriorWaypointID(int current,int howfarback=-1) {
		if(current==0) {
			return Circuit.instance.turns.Length+howfarback;
		}
		return current+howfarback;
	}
}
