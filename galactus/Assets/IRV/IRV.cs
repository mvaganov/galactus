//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//
//public class IRV : MonoBehaviour {
//
//	// TODO rename Ballot
//	public class Vote {
//		public string id;
//		public string[] vote; // TODO replace with integer, and add a lookup table reference.
//	}
//
//	public class VoteBloc {
//		public string C; // candidate
//		public int s; // starting point in the linear representation
//		public int v; // votes
//		public class NextBloc {
//			public string D; // destination
//			public int v; // vote count
//			public int f; // index from
//			public int t; // intdex to
//			public NextBloc(string destination, int voteCount, int indexFrom, int indexTo) {
//				D = destination; v = voteCount; f = indexFrom; t = indexTo;
//			}
//		}
//		public List<NextBloc> n;
//		public VoteBloc(string candidate, int start, int votes) {
//			C = candidate; s = start; v = votes; n = null;
//		}
//	}
//
//	string IRV_EX = "`"; // exhausted ballot token. regenerated to ensure no collision with candidate names
//	Color IRV_EX_color = new Color(.875f,.875f,.875f); // should never print, but just in case...
//	Color[] IRV_colorList = new Color[]{
//		Color.red, Color.green, Color.blue, //"888",
//		Color.yellow, Color.cyan, Color.magenta, //"222",
//		new Color(.5f,0,0), new Color(.75f,1,.75f), new Color(0,0,.5f), //"666", 
//		new Color(1,1,.75f),new Color(0,.5f,.5f),new Color(1,.75f,1),
//		new Color(.5f,.5f,0),new Color(.75f,1,1),new Color(.5f,0,.5f),
//		new Color(1,.75f,.75f),new Color(0,.5f,0),new Color(.75f,.75f,1),
//		new Color(1,.5f,0),new Color(0,1,.5f),new Color(.5f,0,1),
//		new Color(1,0,.5f),new Color(.5f,1,0),new Color(0,.5f,1),
//		new Color(.25f,.5f,0),new Color(0,.25f,.5f),new Color(.5f,0,.25f)
//	};
//
//	string replaceAt(string s, int i, char c) {
//		return s.Substring (0, i) + c + s.Substring (i + 1);
//	}
//	string nextCharAtIndex(string s, int i) {
//		return replaceAt(s,i,(char)(s[i]+1));
//	}
//
//	delegate bool ReturnTrueToContinue(string test);
//	/// <summary>brute-force run through every string</summary>
//	/// <param name="returnsTrueToContinue">the function that checks each string. keep returning true to keep the loop going.</param>
//	/// <param name="minchar">Minchar.</param>
//	/// <param name="maxchar">Maxchar.</param>
//	void tryEveryString(ReturnTrueToContinue returnsTrueToContinue, char minchar = (char)32, char maxchar = (char)126) {
//		bool collision;
//		string test = minchar.ToString();
//		do{
//			collision = returnsTrueToContinue(test);
//			if(collision) {
//				int index = 0;
//				char v = (char)minchar;
//				do{
//					test = nextCharAtIndex(test, index);
//					v = test[index];
//					if(v >= maxchar) {
//						test = replaceAt(test, index,' ');
//						index++;
//						while(index >= test.Length) { test += ' '; }
//					}
//				}while(v >= maxchar);
//			}
//		} while(collision);
//	}
//	/// <summary>make sure the EX code is unique amoung the list of candidates, by brute force if necessary</summary>
//	/// <param name="listOfCandidates">List of candidates.</param>
//	void IRV_ensure_EX_code(string[] listOfCandidates) {
//		tryEveryString((str)=>{
//			IRV_EX=str;
//			return System.Array.IndexOf(listOfCandidates, IRV_EX) >= 0;
//		});
//	}
//
//	/// <summary>Generates a default color table for the list of choices.</summary>
//	/// <returns>The v create color map lookup table.</returns>
//	/// <param name="listing">Listing.</param>
//	Dictionary<string, Color> IRV_createColorMapLookupTable(string[] listing) {
//		Dictionary<string, Color> colorMap = new Dictionary<string, Color>();
//		var colorindex = 0;
//		var startingIndex = 0;
//		for(int i=startingIndex;i<listing.Length;++i) {
//			string k = listing[i];
//			colorMap[k] = IRV_colorList[(colorindex++) % IRV_colorList.Length];
//		}
//		return colorMap;
//	}
//
//	/// <returns>The order choices of choices based on the tally, using tieBreakerData weighting to separate ties.</returns>
//	/// <param name="tally">Tally.</param>
//	/// <param name="tieBreakerData">Tie breaker data.</param>
//	/// <param name="allChoicesPossible">All choices possible.</param>
//	/// <param name="forceTieBreakerDataAsOrder">Force tie breaker data as order.</param>
//	List<string> IRV_orderChoices(Dictionary<string, List<Vote> > tally, Dictionary<string, float> tieBreakerData, 
//		bool allChoicesPossible = false, bool forceTieBreakerDataAsOrder = false) {
//		List<string> order = new List<string>();
//		foreach(var k in tally) { order.Add(k.Key); }
//		order.Sort((a,b)=>{
//			float diff = tally[b].Count - tally[a].Count;
//			if(forceTieBreakerDataAsOrder || diff == 0) {
//				diff = tieBreakerData[b] - tieBreakerData[a];
//			}
//			return (int)(diff*1024);
//		});
//		if(allChoicesPossible) {
//			// go through all of the votes and put non-first-order candidates in there too (people that nobody voted for first)
//			foreach(var k in tally) {
//				List<Vote> ballots = tally[k.Key];
//				for(int v=0;v<ballots.Count; ++v) {
//					string[] possibleChoices = ballots[v].vote;
//					for(var c=0;c<possibleChoices.Length;++c) {
//						string possibleChoice = possibleChoices[c];
//						if(order.IndexOf(possibleChoice) < 0) {
//							order.Add(possibleChoice);
//						}
//					}
//				}
//			}
//		}
//		// ensure that exhausted candidates appear at the end
//		if(order[order.Count-1] != IRV_EX) {
//			int exhaustedIndex = order.IndexOf(IRV_EX);
//			if(exhaustedIndex >= 0) {
//				order.RemoveAt (exhaustedIndex); // order.splice(exhaustedIndex, 1);
//				order.Add(IRV_EX);
//			}
//		}
//		return order;
//	}
//
//	// TODO if allBallots is very large, use a different algorithm. this is O(n^2).
//	private int _indexOfVoter(List<Vote> list, string voterID, int start, int end) {
//		if(list != null) {
//			for(int i=start;i<end;++i) {
//				if(list[i].id == voterID) {
//					return i;
//				}
//			}
//		}
//		return -1;
//	}
//	/// <returns>The id of the voter who voted more than once.</returns>
//	/// <param name="allBallots">All ballots.</param>
//	protected string IRV_whoVotedMoreThanOnce(List<Vote> allBallots) {
//		//var hasVote = [];
//		for(var i=0;i<allBallots.Count;++i){
//			// if(hasVote.indexOf(allBallots[i].id) < 0) {
//			//   hasVote.push(allBallots[i].id);
//			// } else {
//			if(_indexOfVoter(allBallots, allBallots[i].id, i+1, allBallots.Count) >= 0) {
//				return allBallots[i].id;
//			}
//		}
//		return null;
//	}
//
//	/// <summary>converts string IDs to numeric indices, for more compressed runtime data</summary>
//	void IRV_createLookupTable(Dictionary<string, float> candidateWeight, List<string> out_indexToId, Dictionary<string, int> out_idToIndex) {
//		List<string> candidateList = new List<string>();
//		foreach(var k in candidateWeight) {
//			candidateList.Add(k.Key);
//		}
//		//candidateList = 
//		candidateList.Sort((a,b)=>{
//			return (int)((candidateWeight[b] - candidateWeight[a]) * 1024);
//		});
//		if(out_indexToId != null) {
//			out_indexToId.AddRange (candidateList); // Array.prototype.push.apply(out_indexToId, candidateList);
//		}
//		if(out_idToIndex != null) {
//			for(var i=0;i<out_indexToId.Count;++i) {
//				out_idToIndex[out_indexToId[i]] = i;
//			}
//		}
//	}
//
//	List<VoteBloc> calculateBlocs(List<string> sorted, Dictionary<string, List<Vote> > voteState, Dictionary<string, float> candidateWeight) {
//		List<VoteBloc> blocsThisState = new List<VoteBloc>();
//		int cursor = 0;
//		for(var s=0;s<sorted.Count;++s) {
//			List<Vote> thisGuysVotes = voteState[sorted[s]];
//			if(thisGuysVotes != null && thisGuysVotes.Count != 0) {
//				VoteBloc bloc = new VoteBloc (sorted [s], cursor, thisGuysVotes.Count);
//				//var bloc = {
//				//	C:sorted[s], // candidate
//				//	s:cursor, // start
//				//	v:thisGuysVotes.length // vote count
//				//};
//				blocsThisState.Add(bloc);
//				cursor += thisGuysVotes.Count;
//			}
//		}
//		return blocsThisState;
//	}
//
//	// finds where a bloc is in a given bloc state
//	private int findBlocIndex(string candidateName, List<VoteBloc> blocList) {
//		for(var i=0;i<blocList.Count;++i) {
//			if(blocList[i].C == candidateName) { return i; }
//		}
//		return -1;
//	}
//
//	/**
//	 * @param out_visBlocs where to append the visualization model.
//	 * Each visualiation block explains which block moved from where to where.
//	 * Every block exists at some index in a number line, and is the size of it's number of votes
//	 * @param voteStateHistory the state of the votes at each step
//	 * @param voteMigrationHistory how the votes moved each state.
//	 * @param candidateWeight the weight of each bloc, used to sort blocks of the same size (tie breaking)
//	 */
//	void IRV_calculateVisualizationModel(
//		List<List<VoteBloc>> out_visBlocs, 
//		List< Dictionary<string, List<Vote> > > voteStateHistory, 
//		List< Dictionary <string, Dictionary<string, List<Vote> > > > voteMigrationHistory, 
//		Dictionary<string, float> candidateWeight)
//	{
//		List<VoteBloc> blocsThisState;// = new List<VoteBloc>();
//		List<VoteBloc> blocsLastState = null;
//
//		// to make the visualization more coherent, calculate the order in which candidates are exhausted, and weight them visually that way
//		Dictionary<string, float> weightsForThisVisualization = new Dictionary<string, float>();
//		for(int s=0;s<voteStateHistory.Count;++s) {
//			Dictionary<string, List<Vote> > state = voteStateHistory[s];
//			foreach(var c in state) {
//				float val;
//				if (weightsForThisVisualization.TryGetValue (c.Key, out val)) {
//					val += state [c.Key].Count;
//				} else {
//					val = state[c.Key].Count;
//				}
//				weightsForThisVisualization[c.Key] = val;
//			}
//		}
//		// console.log(JSON.stringify(weightsForThisVisualization));
//		candidateWeight = weightsForThisVisualization;
//
//		for(int stateIndex=0; stateIndex < voteStateHistory.Count; ++stateIndex) {
//			// sort the candidates based on who is likely to win right now
//			List<string> sorted = IRV_orderChoices(voteStateHistory[stateIndex], candidateWeight, false, true);
//			// organize those candidates into blocs, and put all those blocs into a list. this is a vote state
//			blocsThisState = calculateBlocs(sorted, voteStateHistory[stateIndex], candidateWeight);
//			// add the vote state to a list of vote states
//			out_visBlocs.Add(blocsThisState);
//			// if we can discover how the last vote state turned into this one
//			if(blocsLastState != null) {
//				// for each block in the current state
//				for(int c=0; c < blocsThisState.Count; ++c) {
//					VoteBloc thisBloc = blocsThisState[c];
//					string thisBlocName = thisBloc.C;
//					if(thisBlocName == IRV_EX) continue; // don't describe exhausted ballot continuation
//					// find where it was in the previous state
//					int oldBlocIndex = findBlocIndex(thisBlocName, blocsLastState);
//					VoteBloc lastBloc = null;
//					int delta = thisBloc.v;
//					if(oldBlocIndex != -1) {
//						lastBloc = blocsLastState[oldBlocIndex];
//						if(lastBloc.C != thisBlocName) {
//							throw new System.Exception("we got a naming and/or searching problem...");
//							//return IRV_err("we got a naming and/or searching problem...");
//						}
//						delta = thisBloc.v - delta;
//					}
//					// if the size is the same, do an easy shift.
//					if(delta == 0) {
//						//lastBloc.n = [{ // next
//						//	D:thisBloc.C, // destination
//						//	v:lastBloc.v, // vote count
//						//	f:lastBloc.s, // index From
//						//	t:thisBloc.s // index To
//						//}];
//						lastBloc.n = new List<VoteBloc.NextBloc>();
//						lastBloc.n.Add(new VoteBloc.NextBloc(thisBloc.C,lastBloc.v,lastBloc.s,thisBloc.s));
//					}
//				}
//				// the complex shifts were not calculated in the last forloop. but they were calculated in voteMigrationHistory
//				Dictionary<string, Dictionary<string, List<Vote> > > thisTransition = voteMigrationHistory[stateIndex-1];
//				Dictionary<string,int>
//				lastStateBlocAcct = new Dictionary<string, int>(), 
//				thisStateBlocAcct = new Dictionary<string, int>(); // keeps track of how much is being transfer from/to
//				foreach(var k in thisTransition) { // from
//					VoteBloc lastBloc = blocsLastState[findBlocIndex(k.Key, blocsLastState)];
//					int countBloc;
//					if(!lastStateBlocAcct.TryGetValue(lastBloc.C, out countBloc)){
//						//!lastStateBlocAcct[lastBloc.C]) lastStateBlocAcct[lastBloc.C] = 0;
//						countBloc = 0;
//						lastStateBlocAcct[lastBloc.C] = 0;
//					}
//					foreach(var n in thisTransition[k.Key]) { // to
//						var thisBloc = blocsThisState[findBlocIndex(n.Key, blocsThisState)];
//						if(!thisStateBlocAcct.TryGetValue(thisBloc.C, out countBloc)) {
//							//thisStateBlocAcct[thisBloc.C]) {
//							VoteBloc lastThisBloc = blocsLastState[findBlocIndex(n.Key, blocsLastState)];
//							if(lastThisBloc != null) {
//								thisStateBlocAcct[thisBloc.C] = lastThisBloc.v;
//							} else {
//								thisStateBlocAcct[thisBloc.C] = 0;
//							}
//						}
//						List<Vote> movingVotes = n.Value;//thisTransition[k.Key][n.Key];
//						if(lastBloc.n == null) lastBloc.n = new List<VoteBloc.NextBloc>();
//						//lastBloc.n.Add({ // next
//						//	D:thisBloc.C, // destination
//						//	v:movingVotes.length, // vote count
//						//	f:lastBloc.s + lastStateBlocAcct[lastBloc.C], // index From
//						//	t:thisBloc.s + thisStateBlocAcct[thisBloc.C] // index To
//						//});
//						lastBloc.n.Add (new VoteBloc.NextBloc (thisBloc.C, movingVotes.Count,
//							lastBloc.s + lastStateBlocAcct [lastBloc.C], thisBloc.s + thisStateBlocAcct [thisBloc.C]));
//						lastStateBlocAcct[lastBloc.C] = lastStateBlocAcct[lastBloc.C] + movingVotes.Count;
//						thisStateBlocAcct[thisBloc.C] = thisStateBlocAcct[thisBloc.C] + movingVotes.Count;
//					}
//				}
//			}
//			blocsLastState = blocsThisState;
//		}
//	}
//
//	string IRV_serializeVisualizationBlocData (List<List<VoteBloc>> visBlocs, List<string> candidatesListing, 
//		Dictionary<string, Color> colorMap, Dictionary<string, int> voteCount, string title) {
//		// create a lookup table for unique IDs to reduce serialized data. only use IDs that are in this bloc visualization.
//		Dictionary<string,int> actuallyNeeded = new Dictionary<string,int>();
//		Dictionary<string,int> idToIndexInUse = new Dictionary<string,int>();
//		List<string> colorListToSend = new List<string> ();
//		List<string> indexToIdToSend = new List<string> ();
//		actuallyNeeded[IRV_EX]=1; // make sure IRV_EX is in the list (will be first if it is).
//		IRV_convertVisualizationBlocIds(visBlocs, null, actuallyNeeded);
//		for(var i=0;i<candidatesListing.Count;++i) {
//			if(actuallyNeeded.ContainsKey(candidatesListing[i])) {
//				idToIndexInUse[candidatesListing[i]] = indexToIdToSend.Count;
//				indexToIdToSend.Add(candidatesListing[i]);
//				// FIXME make sure that hex codes are printed here...
//				colorListToSend.Add(colorMap[candidatesListing[i]].ToString());
//			}
//		}
//		IRV_convertVisualizationBlocIds(visBlocs, idToIndexInUse);
//		string serializedCalculations = "TODO stringify visBlocs, stripped of double quotes";//JSON.stringify(visBlocs).replace(/"/g, '');
//		SerializedResults sr = new SerializedResults(voteCount, indexToIdToSend, colorListToSend, title, serializedCalculations);
//		string serialized = "TODO stringify sr";
//		//var serialized = "{numVotes:"+voteCount+","+
//		//"candidates:"+JSON.stringify(indexToIdToSend)+","+ // slice off the __EXHAUSTED__ candidates
//		//"colors:"+JSON.stringify(colorListToSend)+","+
//		//"title:\'"+title+"\',"+"data:"+serializedCalculations+"}";
//		return serialized;
//	}
//
//	public class SerializedResults {
//		public int numVotes;
//		public List<string> candidates;
//		public List<string> colors;
//		public Dictionary<string, Color> colorMap; // unused during transmission
//		public string title;
//		public string data;
//		public SerializedResults(int numVotes, List<string> candidates, List<Color> colors, string title, string data) {
//			this.numVotes = numVotes;
//			this.candidates = candidates;
//			this.colors = colors;
//			this.title = title;
//			this.data = data;
//		}
//	}
//
//	/**
// * client-side visualization
// * filter the visualization bloc object data. allows size reduction
// * @param allVisBlocsStates
// * @param conversionTable if not null, used to replace ids with an alternate value
// * @param out_conversionMade if not null, counts how many times any id was replaced
// */
//	public static void IRV_convertVisualizationBlocIds (List<List<VoteBloc>> allVisBlocsStates, 
//		Dictionary<string,int> conversionTable, Dictionary<string,int> out_conversionsMade = null) {
//		for(int s=0;s<allVisBlocsStates.Count;++s) {
//			List<VoteBloc> state = allVisBlocsStates[s];
//			for(int b=0;b<state.Count;++b) {
//				VoteBloc bloc = state[b];
//				if(out_conversionsMade != null) {
//					out_conversionsMade[bloc.C] = (out_conversionsMade.ContainsKey(bloc.C))
//						?(out_conversionsMade[bloc.C]+1):1;
//				}
//				if(conversionTable != null) {bloc.C = conversionTable[bloc.C].ToString();}
//				List<VoteBloc.NextBloc> nextList = bloc.n;
//				if(nextList != null) {
//					for(int n=0;n<nextList.Count;++n) {
//						VoteBloc.NextBloc nextEntry = nextList[n];
//						if(out_conversionsMade != null) {
//							out_conversionsMade[nextEntry.D] = (out_conversionsMade.ContainsKey(nextEntry.D))
//								?(out_conversionsMade[nextEntry.D]+1):1;
//						}
//						if(conversionTable != null) {nextEntry.D = conversionTable[nextEntry.D].ToString();}
//					}
//				}
//			}
//		}
//	}
//
//	// TODO? maybe convert this? it's an interesting piece of code, but not needed...
////	/**
////	 * @param dataStructure {Object} complex multie-tiered data structure
////	 * @param propertiesToConvert {Array} if a property with this name is found... 
////	 * @param conversionTable {Map} if not null, used to replace properties with an alternate value
////	 * @param out_conversionMade {Map} if not null, counts how many times a value from the property is/would-be replaced
////	 * @param in_traversedPath {Array} keeps track of dataStructure traversal to prevent recursion.
////	 * unused, because the special-case function is faster, and also helps inform others of the structure of visualization bloc data
////	 * convertVisualizationBlocIds could just be: convertPropertyValues(allVisBlocsStates, ['C', 'D'], conversionTable, out_conversionsMade);
////	 */
////	void convertPropertyValues(dataStructure, propertiesToConvert, conversionTable, out_conversionsMade, in_traversedPath) {
////		if(!in_traversedPath) { in_traversedPath = [dataStructure]; }
////		else if(in_traversedPath.indexOf(dataStructure) >= 0) { "//silently ignore recursion"; return; }
////		else { in_traversedPath = in_traversedPath.concat([dataStructure]); }
////		if(dataStructure.constructor === Array) {
////			for(var i=0;i<dataStructure.length;++i) {
////				convertPropertyValues(dataStructure[i], propertiesToConvert, conversionTable, out_conversionsMade, in_traversedPath);
////			}
////		} else if(isObject(dataStructure)) {
////			for(var k in dataStructure) {
////				var val = dataStructure[k];
////				if (isObject(val)) {
////					convertPropertyValues(dataStructure[k], propertiesToConvert, conversionTable, out_conversionsMade, in_traversedPath);
////				} else if(propertiesToConvert.indexOf(k) >= 0) {
////					if(out_conversionsMade) {out_conversionsMade[val] = (out_conversionsMade[val])?(out_conversionsMade[val]+1):1; }
////					if(conversionTable) { dataStructure[k] = conversionTable[val]; }
////				}
////			}
////		}
////	}
//
//	class Results {
//		public int r; // rank
//		public List<string> C; // candidate winners. probably just one, but might be 2.
//		public int v; // vote count of winner
//		public string showme; // serialized data showing the work of the results.
//	}
//
//	void IRV_standardOutput (List<Results> results, Transform graphicOutput = null) {
//		//if(IRV_deserializeVisualizationBlocData) {
//			for(var i=0;i<results.Count;++i){
//			IRV_vis.IRV_deserializeVisualizationBlocData(results[i].showme, 0, 0, 500, -1, graphicOutput);
//			}
//		//} else {
//		//	throw "Include irv_client.js please!";
//		//}
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
