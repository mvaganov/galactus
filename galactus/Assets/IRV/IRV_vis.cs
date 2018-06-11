using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IRV_vis : MonoBehaviour {

	public static void IRV_deserializeVisualizationBlocData(object serialized, float x, float y, float width, float height, Transform graphicOutput = null) {
		IRV.RunoffHistory deserialized = null;
		if(serialized is string) {
			Debug.LogError ("missing string de-serialization. use objects please!");
			// TODO javascript deserialization...
			//eval("deserialized ="+serialized); // TODO safer javascript evaluation?
		} else {
			deserialized = serialized as IRV.RunoffHistory; // TODO typecast to the results object?
		}
		IRV.IRV_EX = deserialized.candidates[0];

		IRV_convertVisualizationBlocIds(deserialized.data, deserialized.candidates);
		if(height < 0) height = deserialized.data.Count*30; // FIXME this should never happen...
		Debug.Log("Time to create visualizations!");
//		IRV_createVisualizationView(deserialized.data, deserialized.colorMap, deserialized.numVotes, 
//			0, 0, width, height, graphicOutput);
	}

	/**
	 * client-side visualization
	 * filter the visualization bloc object data. allows size reduction
	 * @param allVisBlocsStates
	 * @param conversionTable if not null, used to replace ids with an alternate value
	 * @param out_conversionMade if not null, counts how many times any id was replaced
	 */
	public static void IRV_convertVisualizationBlocIds(List<List<IRV.VoteBloc>> allVisBlocsStates, 
		List<IRV.Candidate> conversionTable, Dictionary<IRV.Candidate,int> out_conversionsMade = null) {
		for(int s=0;s<allVisBlocsStates.Count;++s) {
			List<IRV.VoteBloc> state = allVisBlocsStates[s];
			for(int b=0;b<state.Count;++b){
				IRV.VoteBloc bloc = state[b];
				if(out_conversionsMade != null) {
					out_conversionsMade[bloc.candidate] = (out_conversionsMade.ContainsKey(bloc.candidate))?(out_conversionsMade[bloc.candidate]+1):1;
				}
//				if(conversionTable != null) {bloc.C = conversionTable[int.Parse(bloc.C)];}
				List<IRV.VoteBloc.BlocMigration> nextList = bloc.migrations;
				if(nextList != null) {
					for(var n=0;n<nextList.Count;++n) {
						IRV.VoteBloc.BlocMigration nextEntry = nextList[n];
						if(out_conversionsMade != null) {
							out_conversionsMade[nextEntry.newBoss] = (out_conversionsMade.ContainsKey(nextEntry.newBoss))
								?(out_conversionsMade[nextEntry.newBoss]+1):1;
						}
//						if(conversionTable != null) {nextEntry.D = conversionTable[int.Parse(nextEntry.D)];}
					}
				}
			}
		}
	}

	public class VisualComponents
	{
		public Dictionary<IRV.Candidate, GameObject> labels = new Dictionary<IRV.Candidate, GameObject>();
		public List<Dictionary<IRV.Candidate,GameObject> > blocs = new List<Dictionary<IRV.Candidate,GameObject> >();
		public List<Dictionary<IRV.Candidate,GameObject> > transitions = new List<Dictionary<IRV.Candidate,GameObject> >();
	}

	public static GameObject MakeRectangle(float x, float y, float w, float h, Color c) {
		// TODO create a rectangle mesh in a new game object, and return it.
		GameObject go = new GameObject("rect("+x+","+y+","+w+","+h+")");
		return go;
	}

	public static GameObject MakeLabel(string text, float x, float y, string align, Color c) {
		GameObject go = new GameObject ("label("+text+","+x+","+y+","+align+")");
		return go;
	}

	public static void IRV_createVisualizationView(
		List<List<IRV.VoteBloc>> visBlocs,
		//Dictionary<IRV.Candidate, Color> colorMap, 
		List<IRV.Candidate> idToName,
		int countBallots,
		float x, float y, float width, float height,
		Transform destinationForGraphic, VisualComponents out_components = null) {
		//if(!destinationForGraphic) destinationForGraphic = document.body;
		//if(typeof destinationForGraphic === 'string') destinationForGraphic = document.getElementById(destinationForGraphic);
		//if(!destinationForGraphic) throw "valid destination object required";
		//var two = new Two({ width: width, height: height }).appendTo(destinationForGraphic);
		float cursorx = x, cursory = y;
		float rowHeight = height / visBlocs.Count;
		float cursorHeight = rowHeight / 2, cursorWidth = width / countBallots;
//		float vSpace = rowHeight - cursorHeight;
		//if(out_components) {
		//	out_components.labels = {};
		//	out_components.blocs = [];
		//	out_components.transitions = [];
		//}
		float hMargin = 4, hM = 2;
		if(cursorWidth < 4) { hMargin = 0; hM = 0; }
		for(var state=0;state<visBlocs.Count;++state) {
			if(out_components != null) {
				out_components.blocs.Add(new Dictionary<IRV.Candidate, GameObject>());
				out_components.transitions.Add(new Dictionary<IRV.Candidate, GameObject>());
			}
			bool hasNext = false;
			for(int b=0;b<visBlocs[state].Count;++b) {
				IRV.VoteBloc bloc = visBlocs[state][b];
				if(bloc.candidate == IRV.IRV_EX) continue; // don't draw exhausted ballots
				bool diesHere = true;
				if(bloc.migrations != null) {
					hasNext = true;
					for(var n=0;n<bloc.migrations.Count;++n) {
						if(bloc.migrations[n].newBoss == bloc.candidate) {
							diesHere = false;
							break;
						}
					}
				}
				float rWidth = cursorWidth*bloc.voteCount;
//				IRV.Candidate blocname = bloc.candidate;
				GameObject r = MakeRectangle(cursorx+rWidth/2, cursory+cursorHeight/2, rWidth-4, cursorHeight, bloc.candidate.coloration);//colorMap[blocname]);
				if(out_components != null) out_components.blocs[state][bloc.candidate] = r;

				if(diesHere) {
					string align = "left";
					float xPos = cursorx;
					if(cursorx > width/2) {
						align = "right";
						xPos = cursorx + rWidth;
					}
					GameObject label = MakeLabel(bloc.candidate.name, xPos, cursory+cursorHeight/2, align, Color.black);
					if(out_components != null) { out_components.labels[bloc.candidate] = label; }
				}
				cursorx += rWidth;
			}
			float nextY = cursory + rowHeight;
			cursory += cursorHeight;
			if(hasNext) {
				for(int b=0;b<visBlocs[state].Count;++b) {
					IRV.VoteBloc bloc = visBlocs[state][b];
					if(bloc.migrations != null) {
						for(int n=0;n<bloc.migrations.Count;++n) {
							if(bloc.migrations[n].newBoss == IRV.IRV_EX) { continue; } // don't show shifts to exhaustion.
							float fromMin = x+hM+cursorWidth * bloc.migrations[n].fromPosition;
							float fromMax = x-hM+cursorWidth *(bloc.migrations[n].fromPosition+bloc.migrations[n].population);
							float toMin = x+hM+cursorWidth * bloc.migrations[n].toPosition;
							float toMax = x-hM+cursorWidth *(bloc.migrations[n].toPosition+bloc.migrations[n].population);
							float curveWeightY = 0, curveWeightX = 0;
							// clean up this algorithm for adding whitespace around blocs...
							float blarg = 0;//Mathf.Abs(bloc.n[n].f-bloc.n[n].t);//hMargin;
							if(blarg > hMargin) blarg = hMargin;
							Vector3[] pathV = new Vector3[] {
								new Vector3(fromMin, cursory, 0),
								new Vector3(fromMin, cursory, 0),
								new Vector3(fromMin+curveWeightX, cursory+curveWeightY, 0),
								new Vector3((fromMin*2+toMin)/3+curveWeightX*2, (cursory*2+nextY)/3-blarg, 0),
								// new Vector3((fromMin+toMin)/2+curveWeightX*2, (cursory+nextY)/2, 0),
								new Vector3((fromMin+toMin*2)/3+curveWeightX*2, (cursory+nextY*2)/3-blarg, 0),
								new Vector3(toMin+curveWeightX, nextY-curveWeightY, 0),
								new Vector3(toMin, nextY, 0),
								new Vector3(toMin, nextY, 0),
								new Vector3(toMax, nextY, 0),
								new Vector3(toMax, nextY, 0),
								new Vector3(toMax-curveWeightX, nextY-curveWeightY, 0),
								new Vector3((fromMax+toMax*2)/3-curveWeightX*2, (cursory+nextY*2)/3+blarg, 0),
								// new Vector3((fromMax+toMax)/2-curveWeightX*2, (cursory+nextY)/2, 0),
								new Vector3((fromMax*2+toMax)/3-curveWeightX*2, (cursory*2+nextY)/3+blarg, 0),
								new Vector3(fromMax-curveWeightX, cursory+curveWeightY, 0),
								new Vector3(fromMax, cursory, 0),
								new Vector3(fromMax, cursory, 0),
							};
							GameObject path = new GameObject();
//							IRV.Candidate blocname = bloc.C;//idToName [int.Parse (bloc.C)];
							LineRenderer lr = NS.Lines.Make(ref path, pathV, pathV.Length, bloc.candidate.coloration);//colorMap[blocname]);
//							var curve = two.makePath(
//								fromMin, cursory,
//								fromMin, cursory,
//								fromMin+curveWeightX, cursory+curveWeightY,
//								(fromMin*2+toMin)/3+curveWeightX*2, (cursory*2+nextY)/3-blarg,
//								// (fromMin+toMin)/2+curveWeightX*2, (cursory+nextY)/2,
//								(fromMin+toMin*2)/3+curveWeightX*2, (cursory+nextY*2)/3-blarg,
//								toMin+curveWeightX, nextY-curveWeightY,
//								toMin, nextY, toMin, nextY,
//								toMax, nextY,
//								toMax, nextY,
//								toMax-curveWeightX, nextY-curveWeightY,
//								(fromMax+toMax*2)/3-curveWeightX*2, (cursory+nextY*2)/3+blarg,
//								// (fromMax+toMax)/2-curveWeightX*2, (cursory+nextY)/2,
//								(fromMax*2+toMax)/3-curveWeightX*2, (cursory*2+nextY)/3+blarg,
//								fromMax-curveWeightX, cursory+curveWeightY,
//								fromMax, cursory, fromMax, cursory,
//								true);
//							curve.fill = "#"+colorMap[bloc.n[n].D];
//							curve.noStroke();
//							curve.opacity = 0.75;
//							if(out_components != null) out_components.transitions[state][bloc.n[n].D] = curve;
							if (out_components != null) {
								out_components.transitions[state][bloc.migrations[n].newBoss] = lr.gameObject;
								lr.transform.SetParent (destinationForGraphic);
							}
						}
					}
				}
			}
			cursory = nextY;
			cursorx = x;
		}
		//two.update();
	}

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
