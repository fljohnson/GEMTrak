using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Circuit : MonoBehaviour
{
	public static Circuit instance;
	
	public Raceur[] field;
	public Transform[] turns;
    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
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
		
}
