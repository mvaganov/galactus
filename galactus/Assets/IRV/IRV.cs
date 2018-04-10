using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IRV : MonoBehaviour {

	// TODO create a "Candidate" class, which is has the Candidate name, short code, index, icon, webpage, description, etc...

	// TODO rename Ballot
	[System.Serializable]
	public class Vote {
		[Tooltip("who is voting")]
		public string id;
		[Tooltip("order being voted for")]
		public string[] vote; // TODO replace with integer, and add a lookup table reference. or replace with 'Candidate' class
	}

	public List<Vote> votes;

	public class VoteBloc {
		public string C; // candidate name TODO rename candidateID
		public int s; // starting point in the linear representation
		public int v; // votes
		public class NextBloc {
			public string D; // destination
			public int v; // vote count
			public int f; // index from
			public int t; // intdex to
			public NextBloc(string destination, int voteCount, int indexFrom, int indexTo) {
				D = destination; v = voteCount; f = indexFrom; t = indexTo;
			}
		}
		public List<NextBloc> n;
		public VoteBloc(string candidate, int start, int votes) {
			C = candidate; s = start; v = votes; n = null;
		}
	}

	/// <summary>exhausted ballot token: where votes go when none of their candidates survived the runoff.
	/// regenerated to ensure no collision with candidate names</summary>
	string IRV_EX = "`";
	Color IRV_EX_color = new Color(.875f,.875f,.875f); // should never print, but just in case... light gray
	public Color[] IRV_colorList = new Color[]{
		Color.red, Color.green, Color.blue, //"888",
		Color.yellow, Color.cyan, Color.magenta, //"222",
		new Color(.5f,0,0), new Color(.75f,1,.75f), new Color(0,0,.5f), //"666", 
		new Color(1,1,.75f),new Color(0,.5f,.5f),new Color(1,.75f,1),
		new Color(.5f,.5f,0),new Color(.75f,1,1),new Color(.5f,0,.5f),
		new Color(1,.75f,.75f),new Color(0,.5f,0),new Color(.75f,.75f,1),
		new Color(1,.5f,0),new Color(0,1,.5f),new Color(.5f,0,1),
		new Color(1,0,.5f),new Color(.5f,1,0),new Color(0,.5f,1),
		new Color(.25f,.5f,0),new Color(0,.25f,.5f),new Color(.5f,0,.25f)
	};

	string replaceAt(string s, int i, char c) {
		return s.Substring (0, i) + c + s.Substring (i + 1);
	}
	string nextCharAtIndex(string s, int i) {
		return replaceAt(s,i,(char)(s[i]+1));
	}

	delegate bool ReturnTrueToContinue(string test);
	/// <summary>brute-force run through every string</summary>
	/// <param name="returnsTrueToContinue">the function that checks each string. keep returning true to keep the loop going.</param>
	/// <param name="minchar">Minchar.</param>
	/// <param name="maxchar">Maxchar.</param>
	void tryEveryString(ReturnTrueToContinue returnsTrueToContinue, char minchar = (char)32, char maxchar = (char)126) {
		bool collision;
		string test = minchar.ToString();
		do {
			collision = returnsTrueToContinue(test);
			if(collision) {
				int index = 0;
				char v = (char)minchar;
				do{
					test = nextCharAtIndex(test, index);
					v = test[index];
					if(v >= maxchar) {
						test = replaceAt(test, index,' ');
						index++;
						while(index >= test.Length) { test += ' '; }
					}
				}while(v >= maxchar);
			}
		} while(collision);
	}
	/// <summary>make sure the EX code is unique amoung the list of candidates, by brute force if necessary</summary>
	/// <param name="listOfCandidates">List of candidates.</param>
	/// TODO rename ensure_unique_EX_code
	void IRV_ensure_EX_code(List<string> listOfCandidates) {
		tryEveryString((str)=>{
			IRV_EX=str;
			// if this string is in the listOfCandidates, return true, to keep looking for a new string.
			return listOfCandidates.IndexOf(str) >= 0;
		});
	}

	/// <summary>Generates a default color table for the list of choices.</summary>
	/// <returns>The v create color map lookup table.</returns>
	/// <param name="listing">Listing.</param>
	Dictionary<string, Color> IRV_createColorMapLookupTable(List<string> listing) {
		Dictionary<string, Color> colorMap = new Dictionary<string, Color>();
		int colorindex = 0;
		int startingIndex = 0;
		for(int i=startingIndex; i<listing.Count; ++i) {
			string k = listing[i];
			colorMap[k] = IRV_colorList[(colorindex++) % IRV_colorList.Length];
		}
		return colorMap;
	}

	/// <returns>The order choices of choices based on the tally, using tieBreakerData weighting to separate ties.</returns>
	/// <param name="tally">Tally.</param>
	/// <param name="tieBreakerData">Tie breaker data.</param>
	/// <param name="allChoicesPossible">All choices possible.</param>
	/// <param name="forceTieBreakerDataAsOrder">Force tie breaker data as order.</param>
	List<string> IRV_orderChoices(Dictionary<string, List<Vote> > tally, Dictionary<string, float> tieBreakerData, 
		bool allChoicesPossible = false, bool forceTieBreakerDataAsOrder = false) {
		List<string> order = new List<string>();
		foreach(var k in tally) { order.Add(k.Key); }
		order.Sort((a,b)=>{
			float diff = tally[b].Count - tally[a].Count;
			if(forceTieBreakerDataAsOrder || diff == 0) {
				diff = tieBreakerData[b] - tieBreakerData[a];
			}
			return (int)(diff*1024);
		});
		if(allChoicesPossible) {
			// go through all of the votes and put non-first-order candidates in there too (people that nobody voted for first)
			foreach(var k in tally) {
				List<Vote> ballots = tally[k.Key];
				for(int v=0;v<ballots.Count; ++v) {
					string[] possibleChoices = ballots[v].vote;
					for(int c=0;c<possibleChoices.Length;++c) {
						string possibleChoice = possibleChoices[c];
						if(order.IndexOf(possibleChoice) < 0) {
							order.Add(possibleChoice);
						}
					}
				}
			}
		}
		// ensure that exhausted candidates appear at the end
		if(order[order.Count-1] != IRV_EX) {
			int exhaustedIndex = order.IndexOf(IRV_EX);
			if(exhaustedIndex >= 0) {
				order.RemoveAt (exhaustedIndex); // order.splice(exhaustedIndex, 1);
				order.Add(IRV_EX);
			}
		}
		return order;
	}

	// TODO if allBallots is very large, use a different algorithm. this is O(n^2).
	private int _indexOfVoter(List<Vote> list, string voterID, int start, int end) {
		if(list != null) {
			for(int i=start;i<end;++i) {
				if(list[i].id == voterID) {
					return i;
				}
			}
		}
		return -1;
	}
	/// <returns>The id of the voter who voted more than once.</returns>
	/// <param name="allBallots">All ballots.</param>
	protected string IRV_whoVotedMoreThanOnce(List<Vote> allBallots) {
		//var hasVote = [];
		for(int i=0;i<allBallots.Count;++i){
			// if(hasVote.indexOf(allBallots[i].id) < 0) {
			//   hasVote.push(allBallots[i].id);
			// } else {
			if(_indexOfVoter(allBallots, allBallots[i].id, i+1, allBallots.Count) >= 0) {
				return allBallots[i].id;
			}
		}
		return null;
	}

	/// <summary>converts string IDs to numeric indices, for more compressed runtime data</summary>
	void IRV_createLookupTable(Dictionary<string, float> candidateWeight, List<string> out_indexToId, Dictionary<string, int> out_idToIndex) {
		List<string> candidateList = new List<string>();
		foreach(var k in candidateWeight) {
			candidateList.Add(k.Key);
		}
		//candidateList = 
		candidateList.Sort((a,b)=>{
			return (int)((candidateWeight[b] - candidateWeight[a]) * 1024);
		});
		if(out_indexToId != null) {
			out_indexToId.AddRange (candidateList); // Array.prototype.push.apply(out_indexToId, candidateList);
		}
		if(out_idToIndex != null) {
			for(int i=0;i<out_indexToId.Count;++i) {
				out_idToIndex[out_indexToId[i]] = i;
			}
		}
	}

	List<VoteBloc> calculateBlocs(List<string> sorted, Dictionary<string, List<Vote> > voteState, Dictionary<string, float> candidateWeight) {
		List<VoteBloc> blocsThisState = new List<VoteBloc>();
		int cursor = 0;
		for(int s=0;s<sorted.Count;++s) {
			List<Vote> thisGuysVotes = voteState[sorted[s]];
			if(thisGuysVotes != null && thisGuysVotes.Count != 0) {
				VoteBloc bloc = new VoteBloc (sorted [s], cursor, thisGuysVotes.Count);
				//var bloc = {
				//	C:sorted[s], // candidate
				//	s:cursor, // start
				//	v:thisGuysVotes.length // vote count
				//};
				blocsThisState.Add(bloc);
				cursor += thisGuysVotes.Count;
			}
		}
		return blocsThisState;
	}

	// finds where a bloc is in a given bloc state
	private int findBlocIndex(string candidateName, List<VoteBloc> blocList) {
		for(int i=0;i<blocList.Count;++i) {
			if(blocList[i].C == candidateName) { return i; }
		}
		return -1;
	}

	/// <summary>calculate visualization model.</summary>
	/// <param name="out_visBlocs">where to append the visualization model.
	/// Each visualiation block explains which block moved from where to where.
	/// Every block exists at some index in a number line, and is the size of it's number of votes</param>
	/// <param name="voteStateHistory">the state of the votes at each step.</param>
	/// <param name="voteMigrationHistory">how the votes moved each state.</param>
	/// <param name="candidateWeight">the weight of each bloc, used to sort blocks of the same size (tie breaking)</param>
	void IRV_calculateVisualizationModel(
		List<List<VoteBloc>> out_visBlocs, 
		List< Dictionary<string, List<Vote> > > voteStateHistory, 
		List< Dictionary <string, Dictionary<string, List<Vote> > > > voteMigrationHistory, 
		Dictionary<string, float> candidateWeight)
	{
		List<VoteBloc> blocsThisState;// = new List<VoteBloc>();
		List<VoteBloc> blocsLastState = null;

		// to make the visualization more coherent, calculate the order in which candidates are exhausted, and weight them visually that way
		Dictionary<string, float> weightsForThisVisualization = new Dictionary<string, float>();
		for(int s=0;s<voteStateHistory.Count;++s) {
			Dictionary<string, List<Vote> > state = voteStateHistory[s];
			foreach(var c in state) {
				float val;
				if (weightsForThisVisualization.TryGetValue (c.Key, out val)) {
					val += state [c.Key].Count; // TODO replace state[c.Key] with c.Value
				} else {
					val = state[c.Key].Count;
				}
				weightsForThisVisualization[c.Key] = val;
			}
		}
		// console.log(JSON.stringify(weightsForThisVisualization));
		candidateWeight = weightsForThisVisualization;

		for(int stateIndex=0; stateIndex < voteStateHistory.Count; ++stateIndex) {
			// sort the candidates based on who is likely to win right now
			List<string> sorted = IRV_orderChoices(voteStateHistory[stateIndex], candidateWeight, false, true);
			// organize those candidates into blocs, and put all those blocs into a list. this is a vote state
			blocsThisState = calculateBlocs(sorted, voteStateHistory[stateIndex], candidateWeight);
			// add the vote state to a list of vote states
			out_visBlocs.Add(blocsThisState);
			// if we can discover how the last vote state turned into this one
			if(blocsLastState != null) {
				// for each block in the current state
				for(int c=0; c < blocsThisState.Count; ++c) {
					VoteBloc thisBloc = blocsThisState[c];
					string thisBlocName = thisBloc.C;
					if(thisBlocName == IRV_EX) continue; // don't describe exhausted ballot continuation
					// find where it was in the previous state
					int oldBlocIndex = findBlocIndex(thisBlocName, blocsLastState);
					VoteBloc lastBloc = null;
					int delta = thisBloc.v;
					if(oldBlocIndex != -1) {
						lastBloc = blocsLastState[oldBlocIndex];
						if(lastBloc.C != thisBlocName) {
							throw new System.Exception("we got a naming and/or searching problem...");
							//return IRV_err("we got a naming and/or searching problem...");
						}
						delta = thisBloc.v - delta;
					}
					// if the size is the same, do an easy shift.
					if(delta == 0) {
						//lastBloc.n = [{ // next
						//	D:thisBloc.C, // destination
						//	v:lastBloc.v, // vote count
						//	f:lastBloc.s, // index From
						//	t:thisBloc.s // index To
						//}];
						lastBloc.n = new List<VoteBloc.NextBloc>();
						lastBloc.n.Add(new VoteBloc.NextBloc(thisBloc.C,lastBloc.v,lastBloc.s,thisBloc.s));
					}
				}
				// the complex shifts were not calculated in the last forloop. but they were calculated in voteMigrationHistory
				Dictionary<string, Dictionary<string, List<Vote> > > thisTransition = voteMigrationHistory[stateIndex-1];
				Dictionary<string,int>
				lastStateBlocAcct = new Dictionary<string, int>(), 
				thisStateBlocAcct = new Dictionary<string, int>(); // keeps track of how much is being transfer from/to
				foreach(var k in thisTransition) { // from
					VoteBloc lastBloc = blocsLastState[findBlocIndex(k.Key, blocsLastState)];
					int countBloc;
					if(!lastStateBlocAcct.TryGetValue(lastBloc.C, out countBloc)){
						//!lastStateBlocAcct[lastBloc.C]) lastStateBlocAcct[lastBloc.C] = 0;
						countBloc = 0;
						lastStateBlocAcct[lastBloc.C] = 0;
					}
					foreach(var n in thisTransition[k.Key]) { // to
						VoteBloc thisBloc = blocsThisState[findBlocIndex(n.Key, blocsThisState)];
						if(!thisStateBlocAcct.TryGetValue(thisBloc.C, out countBloc)) {
							//thisStateBlocAcct[thisBloc.C]) {
							VoteBloc lastThisBloc = blocsLastState[findBlocIndex(n.Key, blocsLastState)];
							if(lastThisBloc != null) {
								thisStateBlocAcct[thisBloc.C] = lastThisBloc.v;
							} else {
								thisStateBlocAcct[thisBloc.C] = 0;
							}
						}
						List<Vote> movingVotes = n.Value;//thisTransition[k.Key][n.Key];
						if(lastBloc.n == null) lastBloc.n = new List<VoteBloc.NextBloc>();
						//lastBloc.n.Add({ // next
						//	D:thisBloc.C, // destination
						//	v:movingVotes.length, // vote count
						//	f:lastBloc.s + lastStateBlocAcct[lastBloc.C], // index From
						//	t:thisBloc.s + thisStateBlocAcct[thisBloc.C] // index To
						//});
						lastBloc.n.Add (new VoteBloc.NextBloc (thisBloc.C, movingVotes.Count,
							lastBloc.s + lastStateBlocAcct [lastBloc.C], thisBloc.s + thisStateBlocAcct [thisBloc.C]));
						lastStateBlocAcct[lastBloc.C] = lastStateBlocAcct[lastBloc.C] + movingVotes.Count;
						thisStateBlocAcct[thisBloc.C] = thisStateBlocAcct[thisBloc.C] + movingVotes.Count;
					}
				}
			}
			blocsLastState = blocsThisState;
		}
	}

	SerializedResults IRV_serializeVisualizationBlocData (
		List<List<VoteBloc>> visBlocs,
		List<string> candidatesListing, 
		Dictionary<string, Color> colorMap,
		int voteCount,
		string title) {
		// create a lookup table for unique IDs to reduce serialized data. only use IDs that are in this bloc visualization.
		Dictionary<string,int> actuallyNeeded = new Dictionary<string,int>();
		Dictionary<string,int> idToIndexInUse = new Dictionary<string,int>();
		List<Color> colorListToSend = new List<Color> ();
		List<string> indexToIdToSend = new List<string> ();
		actuallyNeeded[IRV_EX]=1; // make sure IRV_EX is in the list (will be first if it is).
		IRV_convertVisualizationBlocIds(visBlocs, null, actuallyNeeded);
		for(int i=0;i<candidatesListing.Count;++i) {
			if(actuallyNeeded.ContainsKey(candidatesListing[i])) {
				idToIndexInUse[candidatesListing[i]] = indexToIdToSend.Count;
				indexToIdToSend.Add(candidatesListing[i]);
				// FIXME make sure that hex codes are printed here...
				colorListToSend.Add(colorMap[candidatesListing[i]]);
			}
		}
		IRV_convertVisualizationBlocIds(visBlocs, idToIndexInUse);
//		string serializedCalculations = "TODO stringify visBlocs, stripped of double quotes";//JSON.stringify(visBlocs).replace(/"/g, '');
		SerializedResults sr = new SerializedResults(voteCount, indexToIdToSend, colorListToSend, title, visBlocs);
//		string serialized = "TODO stringify sr";
		//var serialized = "{numVotes:"+voteCount+","+
		//"candidates:"+JSON.stringify(indexToIdToSend)+","+ // slice off the __EXHAUSTED__ candidates
		//"colors:"+JSON.stringify(colorListToSend)+","+
		//"title:\'"+title+"\',"+"data:"+serializedCalculations+"}";
//		return serialized;
		return sr;
	}

	public class SerializedResults {
		/// how many votes total were recorded
		public int numVotes;
		/// <summary>who the candidates are. also, this is the int->string dictionary for candidate IDs</summary></summary>
		public List<string> candidates;
		/// color representation for the candidates. TODO replace with a some class that stores candidate style (color, icon, font)
		public List<Color> colors;
		public Dictionary<string, Color> colorMap; // unused during transmission
		public string title;
		/// <summary>[IRV round][candidate]</summary>
		public List<List<IRV.VoteBloc>> data;
		public SerializedResults(int numVotes, List<string> candidates, List<Color> colors, string title, List<List<IRV.VoteBloc>> data) {
			this.numVotes = numVotes;
			this.candidates = candidates;
			this.colors = colors;
			this.title = title;
			this.data = data;
		}
	}

	/// <summary>client-side visualization
	/// filter the visualization bloc object data. allows size reduction</summary>
	/// <param name="allVisBlocsStates">All vis blocs states.</param>
	/// <param name="conversionTable">if not null, used to replace ids with an alternate value</param>
	/// <param name="out_conversionsMade">if not null, counts how many times any id was replaced</param>
	public static void IRV_convertVisualizationBlocIds (List<List<VoteBloc>> allVisBlocsStates, 
		Dictionary<string,int> conversionTable, Dictionary<string,int> out_conversionsMade = null) {
		for(int s=0;s<allVisBlocsStates.Count;++s) {
			List<VoteBloc> state = allVisBlocsStates[s];
			for(int b=0;b<state.Count;++b) {
				VoteBloc bloc = state[b];
				if(out_conversionsMade != null) {
					out_conversionsMade[bloc.C] = (out_conversionsMade.ContainsKey(bloc.C))
						?(out_conversionsMade[bloc.C]+1):1;
				}
				if(conversionTable != null) {bloc.C = conversionTable[bloc.C].ToString();}
				List<VoteBloc.NextBloc> nextList = bloc.n;
				if(nextList != null) {
					for(int n=0;n<nextList.Count;++n) {
						VoteBloc.NextBloc nextEntry = nextList[n];
						if(out_conversionsMade != null) {
							out_conversionsMade[nextEntry.D] = (out_conversionsMade.ContainsKey(nextEntry.D))
								?(out_conversionsMade[nextEntry.D]+1):1;
						}
						if(conversionTable != null) {nextEntry.D = conversionTable[nextEntry.D].ToString();}
					}
				}
			}
		}
	}

	// TODO? maybe convert this? it's an interesting piece of code, but not needed...
//	/**
//	 * @param dataStructure {Object} complex multie-tiered data structure
//	 * @param propertiesToConvert {Array} if a property with this name is found... 
//	 * @param conversionTable {Map} if not null, used to replace properties with an alternate value
//	 * @param out_conversionMade {Map} if not null, counts how many times a value from the property is/would-be replaced
//	 * @param in_traversedPath {Array} keeps track of dataStructure traversal to prevent recursion.
//	 * unused, because the special-case function is faster, and also helps inform others of the structure of visualization bloc data
//	 * convertVisualizationBlocIds could just be: convertPropertyValues(allVisBlocsStates, ['C', 'D'], conversionTable, out_conversionsMade);
//	 */
//	void convertPropertyValues(dataStructure, propertiesToConvert, conversionTable, out_conversionsMade, in_traversedPath) {
//		if(!in_traversedPath) { in_traversedPath = [dataStructure]; }
//		else if(in_traversedPath.indexOf(dataStructure) >= 0) { "//silently ignore recursion"; return; }
//		else { in_traversedPath = in_traversedPath.concat([dataStructure]); }
//		if(dataStructure.constructor === Array) {
//			for(var i=0;i<dataStructure.length;++i) {
//				convertPropertyValues(dataStructure[i], propertiesToConvert, conversionTable, out_conversionsMade, in_traversedPath);
//			}
//		} else if(isObject(dataStructure)) {
//			for(var k in dataStructure) {
//				var val = dataStructure[k];
//				if (isObject(val)) {
//					convertPropertyValues(dataStructure[k], propertiesToConvert, conversionTable, out_conversionsMade, in_traversedPath);
//				} else if(propertiesToConvert.indexOf(k) >= 0) {
//					if(out_conversionsMade) {out_conversionsMade[val] = (out_conversionsMade[val])?(out_conversionsMade[val]+1):1; }
//					if(conversionTable) { dataStructure[k] = conversionTable[val]; }
//				}
//			}
//		}
//	}

	// TODO rename RunoffResult
	class Results {
		public int r; // rank
		public List<string> C; // candidate winners. probably just one, but might be 2.
		public int v; // vote count of winner
		public SerializedResults showme; // serialized data showing the work of the results.
		public Results(int r, List<string> C, int v, SerializedResults showme) {
			this.r=r;this.C=C;this.v=v;this.showme=showme;
		}
	}

	void IRV_standardOutput (List<Results> results, Transform graphicOutput = null) {
		//if(IRV_deserializeVisualizationBlocData) {
			for(int i=0;i<results.Count;++i){
				IRV_vis.IRV_deserializeVisualizationBlocData(results[i].showme, 0, 0, 500, -1, graphicOutput);
			}
		//} else {
		//	throw "Include irv_client.js please!";
		//}
	}

	/** @return table of weighted scores. used for tie-breaking when multiple candidates are about to be removed */
	Dictionary<string,float> IRV_weightedVoteCalc(List<Vote> ballots) {
		// calculate a weighted score, which is a simpler algorithm than Instant Runoff Voting
		Dictionary<string,float> weightedScore = new Dictionary<string,float>();
		for(int v = 0; v < ballots.Count; ++v) {
			string[] voterRanks = ballots[v].vote;
			for(int i=0;i<voterRanks.Length; ++i){
				weightedScore[voterRanks[i]] = 0;
			}
		}
		float max = 0;
		int totalCandidateCount = weightedScore.Count;
		// first-pick adds 1 point. 2nd pick adds half a point. 3rd pick 1/3, 4th pick 1/4, 5 pick 1/5, ...
		for(int v = 0; v < ballots.Count; ++v) {
			string[] voterRanks = ballots[v].vote;
			for(int i=0;i<voterRanks.Length;++i){
				string candidate = voterRanks[i];
				float currentScore = weightedScore [candidate];
				weightedScore[candidate] = currentScore + 1/(i+1.0f);
				if(currentScore > max) max = currentScore;
			}
		}
		return weightedScore;
	}

	delegate void WhatToDoWithResults(List<Results> results);
	delegate void InstantRunoff(WhatToDoWithResults cb);

	void IRV_calc(List<Vote> allBallots, Transform outputContainer, int maxWinnersCalculated = -1, WhatToDoWithResults cb = null) {
		if(cb == null) {
			cb = (List<Results> r) => {
				// console.log(JSON.stringify(results));
				IRV_standardOutput (r, outputContainer);
			};
		}
//		bool doHtmlOutput = true;
		List<Vote> originalBallots = allBallots; // save original data
		// heavy clone operation. will fail if ballots reference each other.
//		allBallots = JSON.parse(JSON.stringify(allBallots));
		allBallots = new List<Vote>(originalBallots);
		// TODO develop some means of outputing things...
//		var out = null;
//		if(outputContainer) {
//			out = document.getElementById(outputContainer);
//			IRV_output = out;
//			out.innerHTML = "";
//		}
		// ensure votes are all in the proper format --not needed in C#, thanks strong typing!
//		if(allBallots.constructor != Array){
//			return IRV_err("votes must be a list of ballots");
//		}
		// convert array ballots for OO format --not needed in C#, thanks strong typing!
//		for(var i=0;i<allBallots.length;++i) {
//			var ballot = allBallots[i];
//			if(ballot.constructor == Array) {
//				ballot = {id:ballot[0], vote:ballot[1]};
//				allBallots[i] = ballot;
//			}
//			if(!ballot.id || !ballot.vote) {
//				return IRV_err("incorrectly formatted ballot "+JSON.stringify(ballot));
//			}
//		}

		// if anyone voted more than once...
		string votedMoreThanOnce = IRV_whoVotedMoreThanOnce(allBallots);
		if(votedMoreThanOnce != null) {
			//return irv_error(votedMoreThanOnce+" voted more than once."); // stop the whole process. one bad vote invalidates everything.
			Debug.LogError(votedMoreThanOnce+" voted more than once.");
			return;
			// TODO do some logic to pick which vote is the correct one and remove the others?
		}
		List<string> winners = new List<string>(); // simple list of candidates who have won
		List<Results> results = new List<Results>(); // detailed results: {r:Number (rank),C:String||Array (winning candidates),v:Number (vote count),showme:String (how the results were developed visual)
		int place = 0; // keeps track of which rank is being calculated right now
		Results best = null; // the most recent best candidate(s). TODO replace with Candidate
		Dictionary<string,float> candidateWeight = IRV_weightedVoteCalc(allBallots); // do a simple guess of who will win using a weighted vote algorithm

		List<string> indexToId = new List<string>();
		Dictionary<string,int> idToIndex = new Dictionary<string, int>(); // lookup tables used in serialization/deserialization, ordered by weighted-vote weight
		IRV_createLookupTable(candidateWeight, indexToId, idToIndex);
		IRV_ensure_EX_code(indexToId);
		Dictionary<string,Color> colorMap = IRV_createColorMapLookupTable(indexToId); // master color lookup table. will be rebuilt for each visualization
		indexToId.Insert(0,IRV_EX); // indexToId.splice(0,0,IRV_EX);
		colorMap[IRV_EX] = IRV_EX_color;

		//var calcIteration = function(cb) {
		InstantRunoff calcIteration = null;
		calcIteration = (WhatToDoWithResults calcCb) => {
			// start with the winners from the system. they can't win again.
			List<string> exhastedCandidates = new List<string>(winners);
			// how votes move during the instant-runoff-vote
			List< Dictionary<string, List<Vote> > > voteStateHistory = new List< Dictionary<string, List<Vote> > >();
			// array of rounds, each round has an array of shifts, each shift is an array with the voter ID and the choice.
			List< Dictionary <string, Dictionary<string, List<Vote> > > > voteMigrationHistory = 
				new List< Dictionary <string, Dictionary<string, List<Vote> > > >();
			// do process!
			best = IRV_calcBestFrom(exhastedCandidates, allBallots, candidateWeight, voteStateHistory, voteMigrationHistory);

			if(best != null) {
				// array of voting blocs {candidate:id, indexRange:[#,#], color:"#XXXXXX", votes:[]}
				List<List<VoteBloc>> visBlocs = new List<List<VoteBloc>>();
				// create serializable easily expression of the Instant Run-off Vote
				IRV_calculateVisualizationModel(visBlocs, voteStateHistory, voteMigrationHistory, candidateWeight);

				SerializedResults serialized = 
					IRV_serializeVisualizationBlocData(visBlocs, indexToId, colorMap, allBallots.Count, "rank"+place);
//				List<List<VoteBloc>> visBlocs,
//				List<string> candidatesListing, 
//				Dictionary<string, Color> colorMap,
//				Dictionary<string, int> voteCount,
//				string title) {

				// IRV_out(place+ "> "+best.winner);
				best.r = place;
				best.showme = serialized;
				results.Add(best);
//				results.Add({
//					r:place, // rank
//					C:best.winner, // candidate identifier(s)
//					v:best.count, // vote count
//					showme:serialized // what to share with people who want to see how the results were developed
//				});
				if(best.C.Count > 1) {
					place += best.C.Count-1; // the -1 is because place gets an automatic ++ in the main loop
					// IRV_out(" <-- ");
					// if(best.winner.length > 2){ IRV_out(best.winner.length+" way "); }
					// IRV_out("TIE");
					winners.AddRange(best.C); //winners = winners.concat(best.winner);
				} else {
					winners.Add(best.C[0]); //winners.push(best.winner);
				}
				// IRV_out("<br>");
			}
			place++;
			if(best != null && (maxWinnersCalculated < 0 || place < maxWinnersCalculated)) {
				NS.Timer.setTimeout(()=>{calcIteration(calcCb);}, 1); // TODO set timer to 0
			} else {
				if(calcCb != null) { calcCb(results); }
			}
		};
		NS.Timer.setTimeout(()=>{ calcIteration(cb); }, 1);
	}


	/** @return a clone of the given table of lists. used to store logs of vote state TODO rename cloneVoteCollection */
	Dictionary<string,List<Vote>> IRV_cloneTableOfLists(Dictionary<string,List<Vote>> tally) {
		Dictionary<string,List<Vote>> cloned = new Dictionary<string,List<Vote>>();
		foreach(var k in tally) {
			cloned[k.Key] = new List<Vote>(tally[k.Key]); // TODO k.Value
		}
		return cloned;
	}

	/** TODO convert to C# XML docs
	 * @param exhastedCandidates who is not allowed to be counted as a winner (because they're already ranked as winners, or they currently have no chance)
	 * @param allBallots all of those votes, as an array of ballots. It's a list of votes, where each vote is a voter [id], the ranked [vote] (another list). ballot:{id:String, vote:Array}
	 * @param out_voteState if not null, make it a list of vote states, where each state is "the name of the choice":"the votes for that choice"
	 * @param out_voteMigrationHistory if not null, make a list of voting rounds, where each round has a table of vote shifts, and each vote shift is a {[key] choice that was displaced and [value] a table of {[key] choices that votes moved to and [value] votes that made it there}}
	 * @return [countOfVotes, winner(could be an array if tied)]
	 */
	Results IRV_calcBestFrom(List<string> exhastedCandidates, List<Vote> allBallots, Dictionary<string,float> tieBreakerData, 
		List< Dictionary<string, List<Vote> > > out_voteState, 
		List< Dictionary <string, Dictionary<string, List<Vote> > > > out_voteMigrationHistory) {
		bool doHtmlOutput = true;
		string htmlOutput = "";
		Dictionary<string,List<Vote>> tally = new Dictionary<string,List<Vote>>(); // the table of votes per candidate
		// do an initial count, to find out how things rank
		IRV_tallyVotes(allBallots, exhastedCandidates, tally);
		int iterations = 0;
		List<string> winner = new List<string>();
		int mostVotes = 0;
		int expectedMaxVoteCount = -1;
		if(out_voteState != null){
			out_voteState.Add (IRV_cloneTableOfLists (tally));
		}
		do {
			// find out how many total votes there are (to determine majority)
			int sumVotes = 0;
			foreach(var k in tally) {
				if(k.Key==IRV_EX) continue; // exhausted votes are no longer relevant for decision making
				sumVotes += tally[k.Key].Count; // TODO k.Value.Count
			}
			if(expectedMaxVoteCount >= 0 && sumVotes > expectedMaxVoteCount) {
				Debug.LogError("votes added? ... was "+expectedMaxVoteCount+", and is now "+sumVotes);
			}
			expectedMaxVoteCount = sumVotes;
			// if there are no votes to count, stop!
			if(sumVotes == 0) { break; }
			// if majority is set to sumVotes, the algorithm will exhaust all votes to determine total support
			int majority = sumVotes; // (sumVotes / 2) +1; //

			// check if any unexhausted choice got a clear majority
			foreach(var k in tally) {
				if(k.Key==IRV_EX) continue; // ignore exhausted ballots
				if(tally[k.Key].Count >= majority) { // TODO k.Value
					winner.Add(k.Key);
					mostVotes = tally[k.Key].Count; // TODO k.Value.Count
				}
			}

			// if there no clear winner, we about to drop some logic.
			if(winner.Count == 0) {
				// see who has the least votes
				int leastVotes = int.MaxValue; // how many votes the fewest vote candidate has
				mostVotes = 0;       // how many votes the leader has (used to check for tie)
				foreach(var k in tally) {
					if(k.Key==IRV_EX) continue; // ignore exhausted ballots
					int len = tally[k.Key].Count; // TODO k.Value.Count
					if (len > 0) {
						if (len < leastVotes) { leastVotes = len; }
						if (len > mostVotes) { mostVotes = len; }
					}
				}
				// check for ties, which are a tricky thing in instant-runoff-voting. ties are when *every* candidate has the same number of votes
				List<string> tie = null;
				if(mostVotes == leastVotes){ tie = new List<string>(); }

				// find out which candidate gets exhausted this round
				// identify which ballots need to be recalculated
				Dictionary<string,List<Vote>> displacedVotes = new Dictionary<string,List<Vote>>();
				List<string> losers = new List<string>(); // the list of losing candidates
				foreach(var k in tally) {
					if(k.Key==IRV_EX) continue; // the exhausted candidate is already lost, no need to use them in logic
					if(tally[k.Key].Count == leastVotes) { // TODO k.Value.Count
						if(tie != null) { tie.Add(k.Key); }
						losers.Add(k.Key);
						if(k.Key == null) { Debug.LogError("why is null losing?... how is null a valid key?"); return null; }
					}
				}
				if(losers.Count > 0) {
					losers = IRV_untie(losers, tieBreakerData, true);
					// disqualify candidate and displace the candidate's ballots
					for(int i=losers.Count-1; i>=0; --i) {
						string k = losers[i];
						exhastedCandidates.Add(k); // needs to be disqualified now, because ties are reprocessed otherwise...
						// move them to a list of uncounted votes
						displacedVotes[k] = tally[k];
						tally[k] = new List<Vote>(); // clear the votes for this disqualified candidate
					}

					// if there was a tie, but not all of them were losers
					if(tie != null && tie.Count != losers.Count) {
						tie = null; // there is no tie, because ties can only exist with complete equality
					}
				}
				// in the rare case that all of the remaining candidates have the exact same score, even after weight calculations
				if(tie != null) {
					winner.AddRange(tie);
//					// if there is only one, then there is no tie.
//					if(tie.Count == 1){
//						winner = tie[0];
//					} else {
//						winner = tie;
//					}
				} else {
					// if there is no tie, reassign votes.
					Dictionary<string,Dictionary<string,List<Vote>>> votingRoundAdjust = null;
					if(out_voteMigrationHistory != null) {
						votingRoundAdjust = new Dictionary<string,Dictionary<string,List<Vote>>>();
					}
					foreach(var k in displacedVotes){
						// do standard logic to find out where to put displaced votes, who's current best choices have been disqualified
						Dictionary<string,List<Vote>> reassignedVotes = new Dictionary<string,List<Vote>>();
						IRV_tallyVotes(displacedVotes[k.Key], exhastedCandidates, reassignedVotes); // TODO k.Value

						if(doHtmlOutput) htmlOutput+=("moved "+displacedVotes[k.Key].Count+" votes from "+k.Key+" ("+tieBreakerData[k.Key]+") to: ");
						if(out_voteMigrationHistory != null) {
							votingRoundAdjust[k.Key] = reassignedVotes;
						}
						// move the displaced votes to their new tally location
						foreach(var newchoice in reassignedVotes){
							if(doHtmlOutput) htmlOutput += (reassignedVotes[newchoice.Key].Count+": "+newchoice.Key+", ");
							if(!tally.ContainsKey(newchoice.Key) || tally[newchoice.Key] == null) {
								tally[newchoice.Key]=new List<Vote>();
							}
							tally[newchoice.Key].AddRange(reassignedVotes[newchoice.Key]); // TODO newchoice.Value
						}
						if(doHtmlOutput) htmlOutput+=("\n");
					}
//					if(doHtmlOutput) htmlOutput+=("--<br>\n");
					if(out_voteMigrationHistory != null){
						out_voteMigrationHistory.Add(votingRoundAdjust);
					}
				}
				if(out_voteState != null) {
					out_voteState.Add(IRV_cloneTableOfLists(tally));
				}
			} // if(!winner)
			iterations++;
			if(iterations > 150) { // TODO iterations > tieBreakerData.Count+1
				Debug.LogError("too many iterations!");
				break;
			}
		}while(winner.Count == 0);
		if (doHtmlOutput) Debug.Log(htmlOutput);
		if(winner.Count != 0) {
			return new Results(-1, winner, mostVotes, null);
		}
		return null;
	}

	/**
	 * @param tied the list of tied candidates
	 * @param tieBreakerData a table giving a score to compare for each tied member
	 * @param wantLowest if false, will return lowest-scoring-member(s) of the tie. otherwise, returns highest.
	 */
	List<string> IRV_untie(List<string> tied, Dictionary<string,float> tieBreakerData, bool wantLowest) {
		List<string> setApart = new List<string>(); // who has broken the tie
		float dividingScore = tieBreakerData[tied[0]];
		// find out what the differentiating score is in the group
		for(int i=1;i<tied.Count;++i) {
			float score = tieBreakerData[tied[i]];
			// TODO XOR would simplify this if statement to: (wantLoser ^ tieBreakerData[tied[i]] > dividingScore)
			if((wantLowest && score < dividingScore) || (!wantLowest && score > dividingScore)) {
				dividingScore = score;
			}
		}
		// once the superaltive score is known (lowest or highest, based on wantLowest), add the member(s) to the setApart list
		for(int i=0;i<tied.Count;++i) {
			if(tieBreakerData[tied[i]] == dividingScore) setApart.Add(tied[i]);
		}
		return setApart;
	}



	/**
	 * @param list of choices, ranked by priority
	 * @param exhastedCandidates which choices are disqualified, prompting the next choice to be taken
	 * @return the index of the highest priority choice from list, with choices eliminated if they are in the exhastedCandidates list. -1 if no valid choices exist, identifying an exhausted ballot.
	 */
	int IRV_getBestChoice(Vote ballot, List<string> exhastedCandidates) {
		string[] list = ballot.vote;
		if(list != null) {
			for(int i=0;i<list.Length;++i) {
				if(exhastedCandidates.IndexOf(list[i]) < 0) {
					return i;
				}
			}
		}
		return -1;
	}

/**
 * @param votes a list of ballots. A ballot is a {id:"unique voter id", vote:["list","of","candidates","(order","matters)"]}
 * @param exhastedCandidates list of which candidates should not count (move to the next choice in the vote's ranked list)
 * @param out_tally a table of all of the votes, seperated by vote winner. {<candidate name>: [list of ballots]}
 */
	void IRV_tallyVotes(List<Vote> ballots, List<string> exhastedCandidates, Dictionary<string,List<Vote>> out_tally) {
		for(int i=0;i<ballots.Count;++i) {
			Vote b = ballots[i];
			int choiceIndex = IRV_getBestChoice(b, exhastedCandidates);
			string bestChoice = (choiceIndex != -1)?b.vote[choiceIndex]:IRV_EX;
			List<Vote> supportForChoice = out_tally.ContainsKey(bestChoice) ? out_tally[bestChoice] : null;
			if(supportForChoice == null) { // if nobody is supporting this candidate yet
				supportForChoice = new List<Vote>();
				out_tally[bestChoice] = supportForChoice; // now there is support
			}
			supportForChoice.Add(b);
		}
	}
	public int randomlyGenerateTest = 100;
	// Use this for initialization
	void Start () {
		if(randomlyGenerateTest > 0) {
			List<string> candidates = new List<string> ();
			candidates.Add ("Mr. V");
			candidates.Add ("Professor V");
			candidates.Add ("Vaganov");
			candidates.Add ("V");
			candidates.Add ("Sensei");
			candidates.Add ("Cheif");
			candidates.Add ("Chort");
			candidates.Add ("Nunov");
			candidates.Add ("Glokglok");
			candidates.Add ("Naltron");
			candidates.Add ("Dunhab");
			candidates.Add ("Princes Hamster");
			for (int i = 0; i < randomlyGenerateTest; ++i) {
				int picks = (int)(Random.value * Random.value * (candidates.Count-1)+2);
				picks = (int)Mathf.Min (picks, candidates.Count);
				string[] ranked = new string[picks];
				for(int r = 0; r < ranked.Length; ++r) {
					int pick;
					do {
						pick = (int)(Random.value * Random.value * (candidates.Count));
					} while(System.Array.IndexOf(ranked, candidates[pick]) >= 0);
					ranked[r] = candidates[pick];
				}
				Vote v = new Vote ();
				v.id = "rand" + i.ToString ();
				v.vote = ranked;
				votes.Add (v);
			}
			IRV_calc (votes, transform, -1, (List<Results> results) => {
				Debug.Log("Finished!");
				Debug.Log(results);
			});
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
