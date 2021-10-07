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
	private static ArrayList results;
	public static bool raceInProgress = true;
	
	private static ControlCenter instance;
	private static PlayerRaceur player;
	public GameObject zapBubble;
	public GUIStyle posnStyle;
	public GUIStyle lapCountStyle;
	public GUIStyle msgStyle;
	public GUIStyle finalPosnStyle;
	public float interval=0.5f;
	private static float nextUpdateTime;
	private static int playerPos=1;
	public int lapsThisLevel = 2; //will aid in level design
	public int qualifying = 3; //so will this
	
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
		if(results!=null) {
			DisplayResults();
		}
		
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
		if(finished.Count < instance.qualifying) { 
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
		float interval = (finished[instance.qualifying-1] as Raceur).TotalTime() - (finished[0] as Raceur).TotalTime();
		if(player.ProjectedLapTime() > (finished[instance.qualifying-1] as Raceur).TotalTime()+interval)
		{
			DidNotQualify(finished);
		}
			
		
	}
	public static void PlayerFinished() {
		ArrayList sortedfield = new ArrayList(Circuit.instance.field);
		foreach(Raceur car in sortedfield) {
			car.CalculateTimes();
		}
		sortedfield.Sort(new ByTime());
		int netPlace = sortedfield.IndexOf(player)+1;
		if(netPlace > instance.qualifying) {
			DidNotQualify(sortedfield);
		}
		else {
			Qualified(netPlace,sortedfield);
		}
		//in qualifying (9-car field), if netPlace>6, did not qualify
		
	}	
	public static void DidNotQualify(ArrayList sortedfield) {
		
		foreach(Raceur car in Circuit.instance.field) {
			car.BeginShutdown();
		}
		raceInProgress = false;
		results = sortedfield;
	}
	
	public static void Qualified(int position,ArrayList sortedfield) {
		foreach(Raceur car in Circuit.instance.field) {
			car.BeginShutdown();
		}
		raceInProgress=false;
		results = sortedfield;
	}
	
	public void DisplayResults() {
		int playerIsIn = -1; //can mean "in the ensuing race" or "on the podium"
		//List the X lowest times, if the player is among them, highlight it
		int i=0;
		foreach(Raceur r in results) {
			string phrase = "";
			if(r == player) {
				phrase = "YOUR TIME:";
				playerIsIn = i+1;
			}
			phrase += FormatTime(r.TotalTime());
			GUI.Label(new Rect (Screen.width/2 - 120,60+35*i, 200, 30), phrase,finalPosnStyle);
			i++;
			if(i == qualifying) {
				break;
			}
		}
		//if not, show the player's time down below
		if(playerIsIn == -1) {
			//Debug.Log();
			float playerTime = player.TotalTime();
			if(playerTime == 0f) {
				playerTime =player.ProjectedLapTime();
			}
			GUI.Label(new Rect (Screen.width/2 - 120,95+35*i, 200, 30), "Your Time:"+FormatTime(playerTime),finalPosnStyle);
			GUI.Label(new Rect (Screen.width/2 - 120,130+35*i, 200, 30), "Didn't qualify",finalPosnStyle);
		
		}
		else
		{
			string msg="Your grid position:"+playerIsIn;
			if(playerIsIn == 1) {
				msg="POLE POSITION!";
			}
			GUI.Label(new Rect (Screen.width/2 - 120,95+35*i, 200, 30), msg,finalPosnStyle);
		}
		
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
	
	public static string FormatTime(float rawsecs) {
		int min=(int)(rawsecs/60);
		int sec=(int)rawsecs-min*60;
		int fraction = (int)(1000*(rawsecs - (sec+min*60)));
		
		return min.ToString("D2")+":"+sec.ToString("D2")+"."+fraction.ToString("D3");
	}
}
