using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;

public class ControlCenter : MonoBehaviour
{
	private static bool greenFlag = false; //should be instance
	private static float countdown = 5f; //should be instance
	private static string message;
	private static ArrayList finished = new ArrayList();
	private static ArrayList results; //should be instance
	public static bool raceInProgress = true;
	private static Dictionary<int,string> startingGrid = new Dictionary<int,string>(); //names of racers
	
	private static ControlCenter instance;
	private static PlayerRaceur player;
	public GameObject zapBubble;
	public GUIStyle posnStyle;
	public GUIStyle lapCountStyle;
	public GUIStyle msgStyle;
	public GUIStyle finalPosnStyle;
	public GUIStyle instructionsStyle;
	public float interval=0.5f;
	private static float nextUpdateTime;
	private static int playerPos=1;
	public int lapsThisLevel = 2; //will aid in level design
	public int minimumPlace = 3; //so will this
	public static bool qualifyMode = true;
	static float sinkInTime =5f;
	static float playerTime;
	static int raceMode = 4;
	
    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        player=GameObject.FindWithTag("Player").GetComponent<PlayerRaceur>();
    }

    // Update is called once per frame
    void Update()
    {
		if(raceMode > 2 || player == null) {
			return;
		}
		if(Input.GetKey(KeyCode.Escape)) {
			#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
			#endif
			Application.Quit();
		}
		if(!greenFlag) {
			StartRace();
		}
		if(!raceInProgress && sinkInTime > 5f) {
			SetupRace();
			return;
		}
			
        if(Time.time > nextUpdateTime) {
			playerPos = player.GetPlace();
			nextUpdateTime = Time.time+interval;
		}
    }
    
    void OnGUI () 
    {
		if(raceMode ==4 ||player==null)
		{
			ShowInstructions();
			return;
		}
		if(!greenFlag) {

			GUI.Label(new Rect (Screen.width/2 - 30,Screen.height/4, 60, 30), message,msgStyle);
				
		}
		GUI.Label(new Rect (Screen.width - 120,0, 120, 30), "Position:"+playerPos,posnStyle);
		
		if(player.laps > 0) {
			GUI.Label(new Rect (Screen.width - 360,40, 180, 60), "Last Lap: "+player.LastLapTime(),lapCountStyle);
		}
		if(player.laps < ControlCenter.LapsThisLevel()) {
			GUI.Label(new Rect (Screen.width - 120,30, 120, 30), "Lap "+(player.laps+1) + " of "+lapsThisLevel,lapCountStyle);
			GUI.Label(new Rect (Screen.width - 360,30, 180, 30), "Lap Time: "+player.LapTime(),lapCountStyle);
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
					raceInProgress=true;
					playerTime = -1f;
					raceMode+=1;
					foreach(Raceur r in Circuit.instance.field) {
						if(r.gameObject.activeInHierarchy) {
							r.Go();
						}
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
		if(finished.Count < instance.minimumPlace) { 
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
		float interval = (finished[instance.minimumPlace-1] as Raceur).TotalTime() - (finished[0] as Raceur).TotalTime();
		if(player.ProjectedLapTime() > (finished[instance.minimumPlace-1] as Raceur).TotalTime()+interval)
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
		if(netPlace > instance.minimumPlace) {
			DidNotQualify(sortedfield);
		}
		else {
			Qualified(netPlace,sortedfield);
		}
		//TODO: in qualifying (9-car field), if netPlace>6, did not qualify
		
	}	
	public static void DidNotQualify(ArrayList sortedfield) {
		
		foreach(Raceur car in Circuit.instance.field) {
			car.BeginShutdown();
		}
		raceInProgress = false;
		results = sortedfield;
		//we didn't place, either in qualifying or the race, so enter a "Game Over" state
		raceMode = 3; 
		
	}
	
	public static void Qualified(int position,ArrayList sortedfield) {
		foreach(Raceur car in Circuit.instance.field) {
			if(car.gameObject.activeInHierarchy) {
				car.BeginShutdown();
			}
		}
		raceInProgress=false;
		results = sortedfield;
		sinkInTime+=Time.time;
		if(raceMode == 2) { //the actual race has been run
			raceMode = 3; //so enter a "Game Over"-ish state
		}
	}
	
	public void DisplayResults() {
		int playerIsIn = -1; //can mean "in the ensuing race" or "on the podium"
		//List the X lowest times, if the player is among them, highlight it
		int i=0;
		foreach(Raceur r in results) {
			string phrase = "";
			if(r == player) {
				
				playerIsIn = i+1;
				if(playerTime < 0f) {
					playerTime = player.TotalTime();
				}
				phrase = "YOUR TIME:"+FormatTime(playerTime);
			}
			else 
			{
				phrase += FormatTime(r.TotalTime());
			}
			GUI.Label(new Rect (Screen.width/2 - 120,60+35*i, 200, 30), phrase,finalPosnStyle);
			i++;
			if(i == minimumPlace) {
				break;
			}
		}
		//if not, show the player's time down below
		if(playerIsIn == -1) {
			if(playerTime < 0f) {
				playerTime = player.TotalTime();
				if(playerTime == 0f) {
					playerTime =player.ProjectedLapTime();
				}
			}
			GUI.Label(new Rect (Screen.width/2 - 120,95+35*i, 200, 30), "Your Time:"+FormatTime(playerTime),finalPosnStyle);
			if(qualifyMode) {
				GUI.Label(new Rect (Screen.width/2 - 120,130+35*i, 200, 30), "Didn't qualify",finalPosnStyle);
			}
			else
			{
				GUI.Label(new Rect (Screen.width/2 - 120,130+35*i, 200, 30), "Didn't place",finalPosnStyle);
			}
		
		}
		else
		{
			string msg ="You've made the podium:"+Ordinalize(playerIsIn)+" place";

			if(qualifyMode) {
				msg="Your grid position:"+playerIsIn;
			}
			if(playerIsIn == 1) {
				if(qualifyMode) {
					msg="POLE POSITION!";
				}
				else
				{
					msg="WINNER!";
				}
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
	
	public static string Ordinalize(int number) {
              switch(number) {
				   case 1:
						   return "first";
				   case 2:
						   return "second";
				   case 3:
						   return "third";
				   default:
						   break;
               }
               return ""+number+"th";
       }
       
       
     public static void SetupRace() {
		 if(sinkInTime > Time.time) {
			 return;
		 }
		 for(int i=0;i<instance.minimumPlace;i++) {
			 startingGrid[i]=(results[i] as Raceur).name;
		 }
		 sinkInTime = 5f;
		 qualifyMode = false;
		 results = null;
		 SceneManager.LoadScene(2);
		 countdown = 5f;
		 greenFlag = false;
	 }
	 
	 public static int GetGridPosition(string carName) {
		 for(int i=0;i<startingGrid.Count;i++){
			 if(startingGrid[i] == carName) {
				 return i;
			 }
		 }
		 return -1;
	 }
	 
	 public void ShowInstructions() {
		 GUI.Label(new Rect (Screen.width/2f - 90,0, 180, 30), "How to Play",instructionsStyle);
		 GUI.Label(new Rect (Screen.width/2f - 192,40, 512, 30), "Left and Right to steer",instructionsStyle);
		 GUI.Label(new Rect (Screen.width/2f - 192,80, 512, 30), "Forward to accelerate",instructionsStyle);
		 GUI.Label(new Rect (Screen.width/2f - 192,120, 512, 30), "Back to accelerate",instructionsStyle);
		 GUI.Label(new Rect (Screen.width/2f - 192,160, 512, 30), "Button stuns an opponent for 5 seconds, but this incurs a 2.5 second penalty",instructionsStyle);
		 
		 GUI.Label(new Rect (Screen.width/2f - 192,300, 512, 30), "Press the button to get moving",instructionsStyle);
		 if(Input.GetKeyUp(KeyCode.Space)) {
			 raceMode = 0;
			 SceneManager.LoadScene(1);
			 
		 }
			 
	 }


}
