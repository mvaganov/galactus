//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//
//public class IRV_vis : MonoBehaviour {
//
//	private static string IRV_EX;
//	public static void IRV_deserializeVisualizationBlocData(object serialized, float x, float y, float width, float height, Transform graphicOutput = null) {
//		IRV.SerializedResults deserialized;
//		if(serialized is string) {
//			// TODO javascript deserialization...
//			//eval("deserialized ="+serialized); // TODO safer javascript evaluation?
//		} else {
//			deserialized = serialized as IRV.SerializedResults; // TODO typecast to the results object?
//		}
//		IRV_EX = deserialized.candidates[0];
//		deserialized.colorMap = new Dictionary<string, Color>(); // TODO why is the colormap cleared here?
//		for(var i=0;i<deserialized.candidates.Count;++i) {
//			deserialized.colorMap[deserialized.candidates[i]] = deserialized.colors[i];
//		}
//		IRV_convertVisualizationBlocIds(deserialized.data, deserialized.candidates);
//		if(height < 0) height = deserialized.data.Length*30; // FIXME this should never happen...
//		IRV_createVisualizationView(deserialized.data, deserialized.colorMap, deserialized.numVotes, 
//			0, 0, width, height, graphicOutput);
//	}
//
//	/**
// * client-side visualization
// * filter the visualization bloc object data. allows size reduction
// * @param allVisBlocsStates
// * @param conversionTable if not null, used to replace ids with an alternate value
// * @param out_conversionMade if not null, counts how many times any id was replaced
// */
//	public static void IRV_convertVisualizationBlocIds(List<List<IRV.VoteBloc>> allVisBlocsStates, 
//		Dictionary<string,int> conversionTable, Dictionary<string,int> out_conversionsMade = null) {
//		for(int s=0;s<allVisBlocsStates.Count;++s) {
//			List<IRV.VoteBloc> state = allVisBlocsStates[s];
//			for(int b=0;b<state.Count;++b){
//				IRV.VoteBloc bloc = state[b];
//				if(out_conversionsMade != null) {out_conversionsMade[bloc.C] = (out_conversionsMade[bloc.C])?(out_conversionsMade[bloc.C]+1):1;}
//				if(conversionTable != null) {bloc.C = conversionTable[bloc.C];}
//				List<IRV.VoteBloc.NextBloc> nextList = bloc.n;
//				if(nextList != null) {
//					for(var n=0;n<nextList.Count;++n) {
//						IRV.VoteBloc.NextBloc nextEntry = nextList[n];
//						if(out_conversionsMade) {out_conversionsMade[nextEntry.D] = (out_conversionsMade[nextEntry.D])?(out_conversionsMade[nextEntry.D]+1):1;}
//						if(conversionTable) {nextEntry.D = conversionTable[nextEntry.D];}
//					}
//				}
//			}
//		}
//	}
//
//	public class VisualComponents
//	{
//		public Dictionary<string, GameObject> labels = new Dictionary<string, GameObject>();
//		public List<Dictionary<string,GameObject> > blocs = new List<Dictionary<string,GameObject> >();
//		public List<Dictionary<string,GameObject> > transitions = new List<Dictionary<string,GameObject> >();
//	}
//
//	public static GameObject MakeRectangle(float x, float y, float w, float h, Color c) {
//		// TODO create a rectangle mesh in a new game object, and return it.
//		GameObject go = new GameObject("rect("+x+","+y+","+w+","+h+")");
//		return go;
//	}
//
//	public static GameObject MakeLabel(string text, float x, float y, string align, Color c) {
//		GameObject go = new GameObject ("label("+text+","+x+","+y+","+align+")");
//	}
//
//	public static void IRV_createVisualizationView(
//		List<List<IRV.VoteBloc>> visBlocs,
//		Dictionary<string, Color> colorMap, 
//		int countBallots,
//		float x, float y, float width, float height,
//		Transform destinationForGraphic, VisualComponents out_components) {
//		//if(!destinationForGraphic) destinationForGraphic = document.body;
//		//if(typeof destinationForGraphic === 'string') destinationForGraphic = document.getElementById(destinationForGraphic);
//		//if(!destinationForGraphic) throw "valid destination object required";
//		//var two = new Two({ width: width, height: height }).appendTo(destinationForGraphic);
//		float cursorx = x, cursory = y;
//		float rowHeight = height / visBlocs.Count;
//		float cursorHeight = rowHeight / 2, cursorWidth = width / countBallots;
//		float vSpace = rowHeight - cursorHeight;
//		//if(out_components) {
//		//	out_components.labels = {};
//		//	out_components.blocs = [];
//		//	out_components.transitions = [];
//		//}
//		float hMargin = 4, hM = 2;
//		if(cursorWidth < 4) { hMargin = 0; hM = 0; }
//		for(var state=0;state<visBlocs.Count;++state) {
//			if(out_components != null) {
//				out_components.blocs.Add(new Dictionary<string, object>());
//				out_components.transitions.Add(new Dictionary<string,object>());
//			}
//			bool hasNext = false;
//			for(int b=0;b<visBlocs[state].Count;++b) {
//				IRV.VoteBloc bloc = visBlocs[state][b];
//				if(bloc.C == IRV_EX) continue; // don't draw exhausted ballots
//				bool diesHere = true;
//				if(bloc.n) {
//					hasNext = true;
//					for(var n=0;n<bloc.n.Count;++n) {
//						if(bloc.n[n].D == bloc.C) {
//							diesHere = false;
//							break;
//						}
//					}
//				}
//				float rWidth = cursorWidth*bloc.v;
//				GameObject r = MakeRectangle(cursorx+rWidth/2, cursory+cursorHeight/2, rWidth-4, cursorHeight, colorMap[bloc.C]);
//				//r.opacity = 0.5;
//				//r.noStroke();
//				if(out_components != null) out_components.blocs[state][bloc.C] = r;
//
//				if(diesHere) {
//					string align = "left";
//					float xPos = cursorx;
//					if(cursorx > width/2) {
//						align = "right";
//						xPos = cursorx + rWidth;
//					}
//					GameObject label = MakeLabel(bloc.C, xPos, cursory+cursorHeight/2, align, Color.black);
//					if(out_components) { out_components.labels[bloc.C] = label; }
//				}
//				cursorx += rWidth;
//			}
//			float nextY = cursory + rowHeight;
//			cursory += cursorHeight;
//			if(hasNext) {
//				for(int b=0;b<visBlocs[state].Count;++b) {
//					IRV.VoteBloc bloc = visBlocs[state][b];
//					if(bloc.n != null) {
//						for(int n=0;n<bloc.n.Count;++n) {
//							if(bloc.n[n].D == IRV_EX) { continue; } // don't show shifts to exhaustion.
//							int fromMin = x+hM+cursorWidth * bloc.n[n].f;
//							int fromMax = x-hM+cursorWidth *(bloc.n[n].f+bloc.n[n].v);
//							int toMin = x+hM+cursorWidth * bloc.n[n].t;
//							int toMax = x-hM+cursorWidth *(bloc.n[n].t+bloc.n[n].v);
//							float curveWeightY = 1, curveWeightX = 3;
//							// clean up this algorithm for adding whitespace around blocs...
//							int blarg = Mathf.abs(bloc.n[n].f-bloc.n[n].t);//hMargin;
//							if(blarg > hMargin) blarg = hMargin;
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
//							if(out_components) out_components.transitions[state][bloc.n[n].D] = curve;
//						}
//					}
//				}
//			}
//			cursory = nextY;
//			cursorx = x;
//		}
//		//two.update();
//	}
//
//	// Use this for initialization
//	void Start () {
//		
//	}
//	
//	// Update is called once per frame
//	void Update () {
//		
//	}
//}
