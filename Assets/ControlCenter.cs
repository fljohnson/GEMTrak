using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlCenter : MonoBehaviour
{
	private static ControlCenter instance;
	private static PlayerRaceur player;
	public GUIStyle posnStyle;
	public GUIStyle lapCountStyle;
	public float interval=0.5f;
	private static float nextUpdateTime;
	private static int playerPos=1;
	public int lapsThisLevel = 2; //will aid in level design
    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        player=GameObject.FindWithTag("Player").GetComponent<PlayerRaceur>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Time.time > nextUpdateTime) {
			playerPos = player.GetPlace();
			nextUpdateTime = Time.time+interval;
		}
    }
    
    void OnGUI () 
    {
		GUI.Label(new Rect (Screen.width - 120,0, 120, 30), "Position:"+playerPos,posnStyle);
		
		if(player.laps > 0) {
			GUI.Label(new Rect (Screen.width - 120,30, 120, 30), "Laps:"+player.laps,lapCountStyle);
			GUI.Label(new Rect (Screen.width - 300,30, 180, 30), "Lap Time: "+player.LastLapTime(),lapCountStyle);
		}
		
		GUI.Label(new Rect (Screen.width - 300,0, 120, 30), player.GetDashboardSpeed(),posnStyle);
		
    }
    
    public static int LapsThisLevel() {
		return instance.lapsThisLevel;
	}
}
