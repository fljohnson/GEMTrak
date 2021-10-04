using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class ControlCenter : MonoBehaviour
{
	private static bool greenFlag = false;
	private static float countdown = 5f;
	private static string message;
	private static ArrayList finished = new ArrayList();
	public static bool raceInProgress = true;
	
	private static ControlCenter instance;
	private static PlayerRaceur player;
	public GameObject zapBubble;
	public GUIStyle posnStyle;
	public GUIStyle lapCountStyle;
	public GUIStyle msgStyle;
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
		if(!greenFlag) {
			StartRace();
		}
        if(Time.time > nextUpdateTime) {
			playerPos = player.GetPlace();
			nextUpdateTime = Time.time+interval;
		}
    }
    
    void OnGUI () 
    {
		if(!greenFlag) {

			GUI.Label(new Rect (Screen.width/2 - 30,Screen.height/4, 60, 30), message,msgStyle);
				
		}
		GUI.Label(new Rect (Screen.width - 120,0, 120, 30), "Position:"+playerPos,posnStyle);
		
		if(player.laps > 0) {
			GUI.Label(new Rect (Screen.width - 120,30, 120, 30), "Laps:"+player.laps + " of "+lapsThisLevel,lapCountStyle);
			GUI.Label(new Rect (Screen.width - 300,30, 180, 60), "Last Lap: "+player.LastLapTime(),lapCountStyle);
		}
		if(player.laps < ControlCenter.LapsThisLevel()) {
			GUI.Label(new Rect (Screen.width - 300,30, 180, 30), "Lap Time: "+player.LapTime(),lapCountStyle);
		}
		
		GUI.Label(new Rect (Screen.width - 300,0, 120, 30), player.GetDashboardSpeed(),posnStyle);
		
    }
    
    public static int LapsThisLevel() {
		return instance.lapsThisLevel;
	}
	
	static void StartRace() {
		if(countdown <  1f) {
				if(!instance.GetComponent<AudioSource>().isPlaying) {
					instance.GetComponent<AudioSource>().Play();
				}
				message = "GO!";
				instance.msgStyle.normal.textColor = Color.green;
				if(countdown <= Time.deltaTime) {
					greenFlag=true;
						foreach(Raceur r in Circuit.instance.field) {
							r.Go();
					}
				}
		}
		else {
			message = countdown.ToString("F0");
			
		}
		
			countdown -= Time.deltaTime;
	}
	
	public static GameObject GetZapBubble(Transform carWorldXform) {
		return Instantiate(instance.zapBubble,carWorldXform);
	}
	
	public static void NotifyFinished(Raceur car) {
		//if there are at least three finishers, sort them by time to decide qualification
		//allow for the scenario that one of the first three cars MAY have a longer time than the player
		car.CalculateTimes();
		finished.Add(car);
		if(finished.Count < 3) { //TODO:replace with max position (6 in qualifying  field of 9)
			return;
		}
		finished.Sort(new ByTime());
		int netPlace = finished.IndexOf(player)+1;
		if(netPlace > 0) {
			return; //let PlayerFinished() deal
		}
		//o-kay, give the player a little longer to cross the finish line (coming in "live last")
		//if the player is really far behind ("dead last"), end this madness
		//In the '80s and '90s, developers simply set a time limit to completing the course
		//Then again, their "CPU" cars weren't actually competing against the player
		float interval = (finished[2] as Raceur).TotalTime() - (finished[0] as Raceur).TotalTime();
		if(player.ProjectedLapTime() > (finished[2] as Raceur).TotalTime()+interval)
		{
			DidNotQualify();
		}
			
		
	}
	public static void PlayerFinished() {
		ArrayList sortedfield = new ArrayList(Circuit.instance.field);
		foreach(Raceur car in sortedfield) {
			car.CalculateTimes();
		}
		sortedfield.Sort(new ByTime());
		int netPlace = sortedfield.IndexOf(player)+1;
		if(netPlace > 3) {
			DidNotQualify();
		}
		else {
			Qualified(netPlace);
		}
		//in qualifying (9-car field), if netPlace>6, did not qualify
		
	}	
	public static void DidNotQualify() {
		
		foreach(Raceur car in Circuit.instance.field) {
			car.RaceEnded();
		}
		raceInProgress = false;
		DisplayResults();
		Debug.Log("Didn't qualify");
	}
	
	public static void Qualified(int position) {
		raceInProgress=false;
		DisplayResults();
		Debug.Log("Grid position will be "+position);
	}
	
	public static void DisplayResults() {
		//TODO:List the X lowest times, if the player is among them, highlight it
		//TODO: If the player didn't qualify, display "YOUR TIME" below
	}
	
	
	private class ByTime:IComparer {
		public int Compare (object x, object y) {
			Raceur carX=x as Raceur;
			Raceur carY=y as Raceur;
			float timeX = carX.TotalTime();
			float timeY = carY.TotalTime();
			
			if(timeX< timeY)
			{
				return -1;
			}
			if(timeX > timeY)
			{
				return 1;
			}
			
			//okay, try fastest lap as a tiebreaker
			if(carX.FastestLap() < carY.FastestLap())
			{
				return -1;
			}
			if(carX.FastestLap() > carY.FastestLap())
			{
				return 1;
			}
			return 0;
		}
	}
}
