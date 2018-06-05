using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// TODO after the first rank is chosen, set up some kind of de-valuation algorithm for everyone who voted for the winner, so that otherwise disenfranchised voters would be more likely to have a say in representation beyond the first winner.
public class IRV : MonoBehaviour {

	[System.Serializable]
	public class Candidate {
		public string name;
		public Color coloration = Color.clear;
		public float tieWeight;
		override public string ToString() { return name; }
		public Candidate(string name){this.name=name;}
		public Candidate(string name, Color color){this.name=name;this.coloration=color;}
	}

	[System.Serializable]
	public class Ballot {
		[Tooltip("who is voting")]
		public string id;
		[Tooltip("order being voted for")]
		public Candidate[] vote;
		[Tooltip("how much this vote should count")]
		public float weight = 1;
	}

	public List<Ballot> votes;

	public class RunoffHistory {
		/// how many votes total were recorded
		public int numVotes;
		/// <summary>who the candidates are.</summary>
		public List<Candidate> candidates;
		public string notes;
		/// <summary>data to describe graphical representation [IRV rank][candidate]</summary>
		public List<List<IRV.VoteBloc>> data;
		public RunoffHistory(int numVotes, List<Candidate> candidates, string notes, List<List<IRV.VoteBloc>> data) {
			this.numVotes = numVotes;
			this.candidates = candidates;
			this.notes = notes;
			this.data = data;
		}
	}

	class RunoffResult {
		public int rank;
		public List<Candidate> winner;
		public int voteCount;
		public RunoffHistory showme;
		public RunoffResult(int r, List<Candidate> C, int v, RunoffHistory showme) {
			this.rank=r;this.winner=C;this.voteCount=v;this.showme=showme;
		}
	}

	public class VoteBloc {
		public Candidate candidate;
		public int startPosition;
		public int voteCount;
		public class BlocMigration {
			public Candidate newBoss;
			public int population, fromPosition, toPosition;
			public BlocMigration(Candidate destination, int voteCount, int indexFrom, int indexTo) {
				this.newBoss = destination; this.population = voteCount; this.fromPosition = indexFrom; this.toPosition = indexTo;
			}
		}
		/// the next blocs that these votes go into
		public List<BlocMigration> migrations;
		public VoteBloc(Candidate candidate, int start, int votes) {
			this.candidate = candidate; this.startPosition = start; this.voteCount = votes; this.migrations = null;
		}
	}

	/// <summary>exhausted ballot token: where votes go when none of their candidates survived the runoff.
	/// regenerated to ensure no collision with candidate names</summary>
	public static Candidate IRV_EX = new Candidate("`", new Color(.875f,.875f,.875f));
	public List<Color> IRV_colorList = new List<Color>(new Color[]{
		Color.red, Color.green, Color.blue, //"888",
		Color.yellow, Color.cyan, Color.magenta, //"222",
		new Color(.5f,0,0), new Color(.75f,1,.75f), new Color(0,0,.5f), //"666", 
		new Color(1,1,.75f),new Color(0,.5f,.5f),new Color(1,.75f,1),
		new Color(.5f,.5f,0),new Color(.75f,1,1),new Color(.5f,0,.5f),
		new Color(1,.75f,.75f),new Color(0,.5f,0),new Color(.75f,.75f,1),
		new Color(1,.5f,0),new Color(0,1,.5f),new Color(.5f,0,1),
		new Color(1,0,.5f),new Color(.5f,1,0),new Color(0,.5f,1),
		new Color(.25f,.5f,0),new Color(0,.25f,.5f),new Color(.5f,0,.25f)
	});

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
	void tryEveryString(ReturnTrueToContinue returnsTrueToContinue, char minchar = (char)33, char maxchar = (char)126) {
		bool collision;
		string test = minchar.ToString();
		do {
			collision = returnsTrueToContinue(test);
			if(collision) {
				int index = 0;
				char v = (char)minchar;
				do {
					test = nextCharAtIndex(test, index);
					v = test[index];
					if(v >= maxchar) {
						test = replaceAt(test, index,' ');
						index++;
						while(index >= test.Length) { test += ' '; }
					}
				} while(v >= maxchar);
			}
		} while(collision);
	}
	/// <summary>make sure the EX code is unique amoung the list of candidates, by brute force if necessary</summary>
	/// <param name="listOfCandidates">List of candidates.</param>
	void IRV_ensure_EX_code(List<Candidate> listOfCandidates) {
		tryEveryString((str)=>{
			IRV_EX.name=str;
			// if this string is in the listOfCandidates, return true, to keep looking for a new string.
			return listOfCandidates.FindIndex((Candidate c)=>{return c.name == str;}) >= 0;
		});
	}

	float distanceBetweenColors(Color a, Color b){
		float R = b.r - a.r, G = b.g - a.g, B = b.b - a.b;
		Vector3 v3 = new Vector3 (R, G, B) * 256;
		return v3.magnitude;
	}

	/// <summary>Generates a default color for each candidate, if needed.</summary>
	/// <param name="listing">out_Listing. the list of Candidates. If the Candidate has no coloration, it will have one after this method</param>
	void IRV_ColorAssignment(List<Candidate> out_listing) {
		// remove auto-colors that are too close to the existing candidates
		for (int i = 0; i < out_listing.Count; ++i) {
			if (out_listing [i].coloration != Color.clear) {
				var mostSimilarColors = IRV_colorList.OrderBy (c => distanceBetweenColors (c, out_listing [i].coloration));
				Color co = mostSimilarColors.First();
				float dist = distanceBetweenColors (co, out_listing [i].coloration);
				if (dist < 32) {
					IRV_colorList.Remove(co);
				}
			}
		}
		// assign colors to candidates without coloration
		int colorindex = 0;
		int startingIndex = 0;
		for(int i=startingIndex; i<out_listing.Count; ++i) {
			Candidate k = out_listing[i];
			if (k.coloration == Color.clear) {
				k.coloration = IRV_colorList [(colorindex++) % IRV_colorList.Count];
			}
		}
	}

	/// <returns>The order choices of choices based on the tally, using tieBreakerData weighting to separate ties.</returns>
	/// <param name="tally">Tally.</param>
	/// <param name="tieBreakerData">Tie breaker data.</param>
	/// <param name="allChoicesPossible">All choices possible.</param>
	/// <param name="forceTieBreakerDataAsOrder">Force tie breaker data as order.</param>
	List<Candidate> IRV_orderChoices(Dictionary<Candidate, List<Ballot> > tally, Dictionary<Candidate, float> tieBreakerData, 
		bool allChoicesPossible = false, bool forceTieBreakerDataAsOrder = false) {
		List<Candidate> order = new List<Candidate>();
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
				List<Ballot> ballots = tally[k.Key];
				for(int v=0;v<ballots.Count; ++v) {
					Candidate[] possibleChoices = ballots[v].vote;
					for(int c=0;c<possibleChoices.Length;++c) {
						Candidate possibleChoice = possibleChoices[c];
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
	private int _indexOfVoter(List<Ballot> list, string voterID, int start, int end) {
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
	protected string IRV_whoVotedMoreThanOnce(List<Ballot> allBallots) {
		for(int i=0;i<allBallots.Count;++i){
			if(_indexOfVoter(allBallots, allBallots[i].id, i+1, allBallots.Count) >= 0) {
				return allBallots[i].id;
			}
		}
		return null;
	}

	List<VoteBloc> calculateBlocs(List<Candidate> sorted, Dictionary<Candidate, List<Ballot> > voteState, Dictionary<Candidate, float> candidateWeight) {
		List<VoteBloc> blocsThisState = new List<VoteBloc>();
		int cursor = 0;
		for(int s=0;s<sorted.Count;++s) {
			List<Ballot> thisGuysVotes = voteState[sorted[s]];
			if(thisGuysVotes != null && thisGuysVotes.Count != 0) {
				VoteBloc bloc = new VoteBloc (sorted [s], cursor, thisGuysVotes.Count);
				blocsThisState.Add(bloc);
				cursor += thisGuysVotes.Count;
			}
		}
		return blocsThisState;
	}

	// finds where a bloc is in a given bloc state
	private int findBlocIndex(Candidate candidateName, List<VoteBloc> blocList) {
		for(int i=0;i<blocList.Count;++i) {
			if(blocList[i].candidate == candidateName) { return i; }
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
		List< Dictionary<Candidate, List<Ballot> > > voteStateHistory, 
		List< Dictionary <Candidate, Dictionary<Candidate, List<Ballot> > > > voteMigrationHistory)
	{
		List<VoteBloc> blocsThisState;
		List<VoteBloc> blocsLastState = null;

		// to make the visualization more coherent, calculate the order in which candidates are exhausted, and weight them visually that way
		Dictionary<Candidate, float> weightsForThisVisualization = new Dictionary<Candidate, float>();
		for(int s=0;s<voteStateHistory.Count;++s) {
			Dictionary<Candidate, List<Ballot> > state = voteStateHistory[s];
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

		for(int stateIndex=0; stateIndex < voteStateHistory.Count; ++stateIndex) {
			// sort the candidates based on who is likely to win right now
			List<Candidate> sorted = IRV_orderChoices(voteStateHistory[stateIndex], weightsForThisVisualization, false, true);
			// organize those candidates into blocs, and put all those blocs into a list. this is a vote state
			blocsThisState = calculateBlocs(sorted, voteStateHistory[stateIndex], weightsForThisVisualization);
			// add the vote state to a list of vote states
			out_visBlocs.Add(blocsThisState);
			// if we can discover how the last vote state turned into this one
			if(blocsLastState != null) {
				// for each block in the current state
				for(int c=0; c < blocsThisState.Count; ++c) {
					VoteBloc thisBloc = blocsThisState[c];
					Candidate thisBlocName = thisBloc.candidate;
					if(thisBlocName == IRV_EX) continue; // don't describe exhausted ballot continuation
					// find where it was in the previous state
					int oldBlocIndex = findBlocIndex(thisBlocName, blocsLastState);
					VoteBloc lastBloc = null;
					int delta = thisBloc.voteCount;
					if(oldBlocIndex != -1) {
						lastBloc = blocsLastState[oldBlocIndex];
						if(lastBloc.candidate != thisBlocName) {
							throw new System.Exception("we got a naming and/or searching problem...");
							//return IRV_err("we got a naming and/or searching problem...");
						}
						delta = thisBloc.voteCount - delta;
					}
					// if the size is the same, do an easy shift.
					if(delta == 0) {
						//lastBloc.n = [{ // next
						//	D:thisBloc.C, // destination
						//	v:lastBloc.v, // vote count
						//	f:lastBloc.s, // index From
						//	t:thisBloc.s // index To
						//}];
						lastBloc.migrations = new List<VoteBloc.BlocMigration>();
						lastBloc.migrations.Add(new VoteBloc.BlocMigration(thisBloc.candidate,lastBloc.voteCount,lastBloc.startPosition,thisBloc.startPosition));
					}
				}
				// the complex shifts were not calculated in the last forloop. but they were calculated in voteMigrationHistory
				Dictionary<Candidate, Dictionary<Candidate, List<Ballot> > > thisTransition = voteMigrationHistory[stateIndex-1];
				Dictionary<Candidate,int>
				lastStateBlocAcct = new Dictionary<Candidate, int>(), 
				thisStateBlocAcct = new Dictionary<Candidate, int>(); // keeps track of how much is being transfer from/to
				foreach(var k in thisTransition) { // from
					VoteBloc lastBloc = blocsLastState[findBlocIndex(k.Key, blocsLastState)];
					int countBloc;
					if(!lastStateBlocAcct.TryGetValue(lastBloc.candidate, out countBloc)){
						//!lastStateBlocAcct[lastBloc.C]) lastStateBlocAcct[lastBloc.C] = 0;
						countBloc = 0;
						lastStateBlocAcct[lastBloc.candidate] = 0;
					}
					foreach(var n in thisTransition[k.Key]) { // to
						VoteBloc thisBloc = blocsThisState[findBlocIndex(n.Key, blocsThisState)];
						if(!thisStateBlocAcct.TryGetValue(thisBloc.candidate, out countBloc)) {
							//thisStateBlocAcct[thisBloc.C]) {
							int blocIndex = findBlocIndex(n.Key, blocsLastState);
							VoteBloc lastThisBloc = (blocIndex >= 0)?blocsLastState[blocIndex]:null;
							if(lastThisBloc != null) {
								thisStateBlocAcct[thisBloc.candidate] = lastThisBloc.voteCount;
							} else {
								thisStateBlocAcct[thisBloc.candidate] = 0;
							}
						}
						List<Ballot> movingVotes = n.Value;//thisTransition[k.Key][n.Key];
						if(lastBloc.migrations == null) lastBloc.migrations = new List<VoteBloc.BlocMigration>();
						//lastBloc.n.Add({ // next
						//	D:thisBloc.C, // destination
						//	v:movingVotes.length, // vote count
						//	f:lastBloc.s + lastStateBlocAcct[lastBloc.C], // index From
						//	t:thisBloc.s + thisStateBlocAcct[thisBloc.C] // index To
						//});
						lastBloc.migrations.Add (new VoteBloc.BlocMigration (thisBloc.candidate, movingVotes.Count,
							lastBloc.startPosition + lastStateBlocAcct [lastBloc.candidate], thisBloc.startPosition + thisStateBlocAcct [thisBloc.candidate]));
						lastStateBlocAcct[lastBloc.candidate] = lastStateBlocAcct[lastBloc.candidate] + movingVotes.Count;
						thisStateBlocAcct[thisBloc.candidate] = thisStateBlocAcct[thisBloc.candidate] + movingVotes.Count;
					}
				}
			}
			blocsLastState = blocsThisState;
		}
	}

	RunoffHistory IRV_serializeVisualizationBlocData (
		List<List<VoteBloc>> visBlocs,
		List<Candidate> candidatesListing, 
		//Dictionary<Candidate, Color> colorMap,
		int voteCount,
		string title) {
		// create a lookup table for unique IDs to reduce serialized data. only use IDs that are in this bloc visualization.
		Dictionary<Candidate,int> actuallyNeeded = new Dictionary<Candidate,int>();
		Dictionary<Candidate,int> idToIndexInUse = new Dictionary<Candidate,int>();
		List<Color> colorListToSend = new List<Color> ();
		List<Candidate> indexToIdToSend = new List<Candidate> ();
		actuallyNeeded[IRV_EX]=1; // make sure IRV_EX is in the list (will be first if it is).
		IRV_convertVisualizationBlocIds(visBlocs, null, actuallyNeeded);
		for(int i=0;i<candidatesListing.Count;++i) {
			if(actuallyNeeded.ContainsKey(candidatesListing[i])) {
				idToIndexInUse[candidatesListing[i]] = indexToIdToSend.Count;
				indexToIdToSend.Add(candidatesListing[i]);
				// FIXME make sure that hex codes are printed here...
				colorListToSend.Add(candidatesListing[i].coloration);
			}
		}
		IRV_convertVisualizationBlocIds(visBlocs, idToIndexInUse);
		RunoffHistory sr = new RunoffHistory(voteCount, indexToIdToSend, title, visBlocs);
		return sr;
	}


	/// <summary>client-side visualization
	/// filter the visualization bloc object data. allows size reduction</summary>
	/// <param name="allVisBlocsStates">All vis blocs states.</param>
	/// <param name="conversionTable">if not null, used to replace ids with an alternate value</param>
	/// <param name="out_conversionsMade">if not null, counts how many times any id was replaced</param>
	public static void IRV_convertVisualizationBlocIds (List<List<VoteBloc>> allVisBlocsStates, 
		Dictionary<Candidate,int> conversionTable, Dictionary<Candidate,int> out_conversionsMade = null) {
		for(int s=0;s<allVisBlocsStates.Count;++s) {
			List<VoteBloc> state = allVisBlocsStates[s];
			for(int b=0;b<state.Count;++b) {
				VoteBloc bloc = state[b];
				if(out_conversionsMade != null) {
					out_conversionsMade[bloc.candidate] = (out_conversionsMade.ContainsKey(bloc.candidate))
						?(out_conversionsMade[bloc.candidate]+1):1;
				}
				List<VoteBloc.BlocMigration> nextList = bloc.migrations;
				if(nextList != null) {
					for(int n=0;n<nextList.Count;++n) {
						VoteBloc.BlocMigration nextEntry = nextList[n];
						if(out_conversionsMade != null) {
							out_conversionsMade[nextEntry.newBoss] = (out_conversionsMade.ContainsKey(nextEntry.newBoss))
								?(out_conversionsMade[nextEntry.newBoss]+1):1;
						}
//						if(conversionTable != null) {nextEntry.D = conversionTable[nextEntry.D].ToString();}
					}
				}
			}
		}
	}

	void IRV_standardOutput (List<RunoffResult> results, Transform graphicOutput = null) {
		for(int i=0;i<results.Count;++i){
			IRV_vis.IRV_deserializeVisualizationBlocData(results[i].showme, 0, 0, 500, -1, graphicOutput);
		}
	}

	/// <returns>list of Candidates by weight, which is used for tie-breaking when multiple candidates are about to be removed</returns>
	List<Candidate> IRV_weightedVoteCalc(List<Ballot> ballots) {
		// calculate a weighted score, which is a simpler algorithm than Instant Runoff Voting
		Dictionary<Candidate,float> weightedScore = new Dictionary<IRV.Candidate,float>();
		for(int v = 0; v < ballots.Count; ++v) {
			Candidate[] voterRanks = ballots[v].vote;
			for(int i=0;i<voterRanks.Length; ++i){
				weightedScore[voterRanks[i]] = 0;
			}
		}
		float max = 0;
		int totalCandidateCount = weightedScore.Count;
		// first-pick adds 1 point. 2nd pick adds half a point. 3rd pick 1/3, 4th pick 1/4, 5 pick 1/5, ...
		for(int v = 0; v < ballots.Count; ++v) {
			Candidate[] voterRanks = ballots[v].vote;
			for(int i=0;i<voterRanks.Length;++i){
				Candidate candidate = voterRanks[i];
				float currentScore = weightedScore [candidate];
				weightedScore[candidate] = currentScore + 1/(i+1.0f);
				if(currentScore > max) max = currentScore;
			}
		}
		// put the weights directly into the candidate pool
		foreach(KeyValuePair<Candidate, float> entry in weightedScore) {
			entry.Key.tieWeight = entry.Value;
		}
		List<Candidate> candidateList = new List<Candidate>();
		foreach(var k in weightedScore) {
			candidateList.Add(k.Key);
		}
		candidateList.Sort((a,b) => {
			return (int)((b.tieWeight - a.tieWeight) * 1024);
		});
		return candidateList;
	}

	delegate void WhatToDoWithResults(List<RunoffResult> results);
	delegate void InstantRunoff(WhatToDoWithResults cb);

	public string Stringify(object obj, string indentation="    "){
		return OMU.Serializer.Stringify (obj, indentation);
	}

	void IRV_calc(List<Ballot> allBallots, Transform outputContainer, int maxWinnersCalculated = -1, WhatToDoWithResults cb = null) {
		List<Ballot> originalBallots = allBallots; // reverence to source data. originalBallots may be marked up.
		allBallots = new List<Ballot>(originalBallots);

		// if anyone voted more than once...
		string votedMoreThanOnce = IRV_whoVotedMoreThanOnce(allBallots);
		if(votedMoreThanOnce != null) {
			//return irv_error(votedMoreThanOnce+" voted more than once."); // stop the whole process. one bad vote invalidates everything.
			Debug.LogError(votedMoreThanOnce+" voted more than once.");
			return;
			// TODO do some logic to pick which vote is the correct one and remove the others?
		}
		List<Candidate> candidates = IRV_weightedVoteCalc(allBallots); // do a simple guess of who will win using a weighted vote algorithm
		List<Candidate> winners = new List<Candidate>(); // simple list of candidates who have won
		List<RunoffResult> results = new List<RunoffResult>(); // detailed results: {r:Number (rank),C:String||Array (winning candidates),v:Number (vote count),showme:String (how the results were developed visual)
		int place = 0; // keeps track of which rank is being calculated right now
		RunoffResult best = null; // the most recent best candidate(s).

		IRV_ensure_EX_code(candidates);
		IRV_ColorAssignment(candidates); // master color lookup table. will be rebuilt for each visualization
		candidates.Insert(0,IRV_EX);

		InstantRunoff calcIteration = null;
		calcIteration = (WhatToDoWithResults calcCb) => {
			// start with the winners from the system. they can't win again.
			List<Candidate> exhastedCandidates = new List<Candidate>(winners);
			// how votes move during the instant-runoff-vote
			List< Dictionary<Candidate, List<Ballot> > > voteStateHistory = new List< Dictionary<Candidate, List<Ballot> > >();
			// array of rounds, each round has an array of shifts, each shift is an array with the voter ID and the choice.
			List< Dictionary <Candidate, Dictionary<Candidate, List<Ballot> > > > voteMigrationHistory = 
				new List< Dictionary <Candidate, Dictionary<Candidate, List<Ballot> > > >();
			// do process!
			best = IRV_calcBestFrom(exhastedCandidates, allBallots, voteStateHistory, voteMigrationHistory);

//			Debug.Log(Stringify(best));
//			Debug.Log(Stringify(voteStateHistory));
//			Debug.Log(Stringify(voteMigrationHistory));

			if(best != null) {
				// array of voting blocs {candidate:id, indexRange:[#,#], color:"#XXXXXX", votes:[]}
				List<List<VoteBloc>> visBlocs = new List<List<VoteBloc>>();
				// create serializable easily expression of the Instant Run-off Vote
				IRV_calculateVisualizationModel(visBlocs, voteStateHistory, voteMigrationHistory);

				RunoffHistory serialized = 
					IRV_serializeVisualizationBlocData(visBlocs, candidates, allBallots.Count, "rank"+place);

				// IRV_out(place+ "> "+best.winner);
				best.rank = place;
				best.showme = serialized;
				results.Add(best);
				if(best.winner.Count > 1) {
					place += best.winner.Count-1; // the -1 is because place gets an automatic ++ in the main loop
					winners.AddRange(best.winner); //winners = winners.concat(best.winner);
				} else {
					winners.Add(best.winner[0]); //winners.push(best.winner);
				}
			}
			place++;
			if(best != null && (maxWinnersCalculated < 0 || place < maxWinnersCalculated)) {
				NS.Timer.setTimeout(()=>{calcIteration(calcCb);}, 1); // TODO set timer to 0
			} else {
				if(calcCb != null) { calcCb(results); }
			}
		};
		if(cb == null) {
			cb = (List<RunoffResult> r) => {
				IRV_standardOutput (r, outputContainer);
			};
		}
		NS.Timer.setTimeout(()=>{ calcIteration(cb); }, 1);
	}

	/// <returns>a clone of the given table of lists. used to store logs of vote state TODO rename cloneVoteCollection</returns>
	Dictionary<Candidate,List<Ballot>> IRV_cloneTableOfLists(Dictionary<Candidate,List<Ballot>> tally) {
		Dictionary<Candidate,List<Ballot>> cloned = new Dictionary<Candidate,List<Ballot>>();
		foreach(var k in tally) {
			cloned[k.Key] = new List<Ballot>(tally[k.Key]); // TODO k.Value
		}
		return cloned;
	}

	/// <returns>[countOfVotes, winner(could be an array if tied)]</returns>
	/// <param name="exhastedCandidates">who is not allowed to be counted as a winner (because they're already ranked as winners, or they currently have no chance)</param>
	/// <param name="allBallots">all of those votes, as an array of ballots. It's a list of votes, where each vote is a voter [id], the ranked [vote] (another list). ballot:{id:String, vote:Array}</param>
	/// <param name="tieBreakerData">Tie breaker data.</param>
	/// <param name="out_voteState">if not null, make it a list of vote states, where each state is "the name of the choice":"the votes for that choice"</param>
	/// <param name="out_voteMigrationHistory">if not null, make a list of voting rounds, where each round has a table of vote shifts, and each vote shift is a {[key] choice that was displaced and [value] a table of {[key] choices that votes moved to and [value] votes that made it there}}</param>
	RunoffResult IRV_calcBestFrom(List<Candidate> exhastedCandidates, List<Ballot> allBallots, //Dictionary<Candidate,float> tieBreakerData, 
		List< Dictionary<Candidate, List<Ballot> > > out_voteState, 
		List< Dictionary <Candidate, Dictionary<Candidate, List<Ballot> > > > out_voteMigrationHistory) {
		bool doHtmlOutput = true;
		string htmlOutput = "";
		Dictionary<Candidate,List<Ballot>> tally = new Dictionary<Candidate,List<Ballot>>(); // the table of votes per candidate
		// do an initial count, to find out how things rank
		IRV_tallyVotes(allBallots, exhastedCandidates, tally);
		int iterations = 0;
		List<Candidate> winner = new List<Candidate>();
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
				List<Candidate> tie = null;
				if(mostVotes == leastVotes){ tie = new List<Candidate>(); }

				// find out which candidate gets exhausted this round
				// identify which ballots need to be recalculated
				Dictionary<Candidate,List<Ballot>> displacedVotes = new Dictionary<IRV.Candidate,List<Ballot>>();
				List<Candidate> losers = new List<Candidate>(); // the list of losing candidates
				foreach(var k in tally) {
					if(k.Key==IRV_EX) continue; // the exhausted candidate is already lost, no need to use them in logic
					if(tally[k.Key].Count == leastVotes) { // TODO k.Value.Count
						if(tie != null) { tie.Add(k.Key); }
						losers.Add(k.Key);
						if(k.Key == null) { Debug.LogError("why is null losing?... how is null a valid key?"); return null; }
					}
				}
				if(losers.Count > 0) {
					losers = IRV_untie(losers, (Candidate c) => {return c.tieWeight; }, //tieBreakerData, 
						true);
					// disqualify candidate and displace the candidate's ballots
					for(int i=losers.Count-1; i>=0; --i) {
						Candidate k = losers[i];
						exhastedCandidates.Add(k); // needs to be disqualified now, because ties are reprocessed otherwise...
						// move them to a list of uncounted votes
						displacedVotes[k] = tally[k];
						tally[k] = new List<Ballot>(); // clear the votes for this disqualified candidate
					}

					// if there was a tie, but not all of them were losers
					if(tie != null && tie.Count != losers.Count) {
						tie = null; // there is no tie, because ties can only exist with complete equality
					}
				}
				// in the rare case that all of the remaining candidates have the exact same score, even after weight calculations
				if(tie != null) {
					winner.AddRange(tie);
				} else {
					// if there is no tie, reassign votes.
					Dictionary<Candidate,Dictionary<Candidate,List<Ballot>>> votingRoundAdjust = null;
					if(out_voteMigrationHistory != null) {
						votingRoundAdjust = new Dictionary<Candidate,Dictionary<Candidate,List<Ballot>>>();
					}
					foreach(var k in displacedVotes){
						// do standard logic to find out where to put displaced votes, who's current best choices have been disqualified
						Dictionary<Candidate,List<Ballot>> reassignedVotes = new Dictionary<Candidate,List<Ballot>>();
						IRV_tallyVotes(displacedVotes[k.Key], exhastedCandidates, reassignedVotes); // TODO k.Value

						if(doHtmlOutput) htmlOutput+=("moved "+displacedVotes[k.Key].Count+" votes from "+k.Key+" ("+//tieBreakerData[k.Key]
							k.Key.tieWeight+") to: ");
						if(out_voteMigrationHistory != null) {
							votingRoundAdjust[k.Key] = reassignedVotes;
						}
						// move the displaced votes to their new tally location
						foreach(var newchoice in reassignedVotes){
							if(doHtmlOutput) htmlOutput += (reassignedVotes[newchoice.Key].Count+": "+newchoice.Key+", ");
							if(!tally.ContainsKey(newchoice.Key) || tally[newchoice.Key] == null) {
								tally[newchoice.Key]=new List<Ballot>();
							}
							tally[newchoice.Key].AddRange(reassignedVotes[newchoice.Key]); // TODO newchoice.Value
						}
						if(doHtmlOutput) htmlOutput+=("\n");
					}
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
			return new RunoffResult(-1, winner, mostVotes, null);
		}
		return null;
	}

	delegate float Scorer<TYPE>(TYPE toScore);
	/// <returns>The true lowest/highest from the tied list</returns>
	/// <param name="tied">the list of tied candidates</param>
	/// <param name="tieBreakerData">a table giving a score to compare for each tied member</param>
	/// <param name="wantLowest">if false, will return lowest-scoring-member(s) of the tie. otherwise, returns highest.</param>
	List<Candidate> IRV_untie(List<Candidate> tied, //Dictionary<Candidate,float> tieBreakerData, 
		Scorer<Candidate> scoreCandidate,
		bool wantLowest) {
		List<Candidate> setApart = new List<Candidate>(); // who has broken the tie
		float dividingScore = scoreCandidate(tied[0]);//tieBreakerData[tied[0]];
		// find out what the differentiating score is in the group
		for(int i=1;i<tied.Count;++i) {
			float score = scoreCandidate(tied[i]);//tieBreakerData[tied[i]];
			// TODO XOR would simplify this if statement to: (wantLoser ^ tieBreakerData[tied[i]] > dividingScore)
			if((wantLowest && score < dividingScore) || (!wantLowest && score > dividingScore)) {
				dividingScore = score;
			}
		}
		// once the superaltive score is known (lowest or highest, based on wantLowest), add the member(s) to the setApart list
		for(int i=0;i<tied.Count;++i) {
			if(scoreCandidate(tied[i])//tieBreakerData[tied[i]]
				== dividingScore) setApart.Add(tied[i]);
		}
		return setApart;
	}

	/// <returns>the index of the highest priority choice from list, with choices eliminated if they are in the exhastedCandidates list. -1 if no valid choices exist, identifying an exhausted ballot.</returns>
	/// <param name="ballot">Ballot.</param>
	/// <param name="exhastedCandidates">which choices are disqualified, prompting the next choice to be taken</param>
	int IRV_getBestChoice(Ballot ballot, List<Candidate> exhastedCandidates) {
		Candidate[] list = ballot.vote;
		if(list != null) {
			for(int i=0;i<list.Length;++i) {
				if(exhastedCandidates.IndexOf(list[i]) < 0) {
					return i;
				}
			}
		}
		return -1;
	}

	/// <param name="ballots">a list of ballots. A ballot is a {id:"unique voter id", vote:["list","of","candidates","(order","matters)"]}.</param>
	/// <param name="exhastedCandidates">list of which candidates should not count (move to the next choice in the vote's ranked list)</param>
	/// <param name="out_tally">a table of all of the votes, seperated by vote winner. {<candidate name>: [list of ballots]}</param>
	void IRV_tallyVotes(List<Ballot> ballots, List<Candidate> exhastedCandidates, Dictionary<Candidate,List<Ballot>> out_tally) {
		for(int i=0;i<ballots.Count;++i) {
			Ballot b = ballots[i];
			int choiceIndex = IRV_getBestChoice(b, exhastedCandidates);
			Candidate bestChoice = (choiceIndex != -1)?b.vote[choiceIndex]:IRV_EX;
			List<Ballot> supportForChoice = out_tally.ContainsKey(bestChoice) ? out_tally[bestChoice] : null;
			if(supportForChoice == null) { // if nobody is supporting this candidate yet
				supportForChoice = new List<Ballot>();
				out_tally[bestChoice] = supportForChoice; // now there is support
			}
			supportForChoice.Add(b);
		}
	}
	public int randomlyGenerateTest = 100;
	public GameObject basicBar;
	// Use this for initialization
	void Start () {
		if(randomlyGenerateTest > 0) {
			List<Candidate> candidates = new List<Candidate> ();
			candidates.Add (new Candidate("Mr. V", Color.cyan));
			candidates.Add (new Candidate("Professor V"));
			candidates.Add (new Candidate("Vaganov"));
			candidates.Add (new Candidate("V", Color.red));
			candidates.Add (new Candidate("Sensei"));
			candidates.Add (new Candidate("Cheif"));
			candidates.Add (new Candidate("Chort"));
			candidates.Add (new Candidate("Nunov"));
			candidates.Add (new Candidate("Glokglok"));
			candidates.Add (new Candidate("Naltron"));
			candidates.Add (new Candidate("Dunhab"));
			candidates.Add (new Candidate("Princes Hamster"));
			for (int i = 0; i < randomlyGenerateTest; ++i) {
				int picks = (int)(Random.value * Random.value * (candidates.Count-1)+2);
				picks = (int)Mathf.Min (picks, candidates.Count);
				Candidate[] ranked = new Candidate[picks];
				for(int r = 0; r < ranked.Length; ++r) {
					int pick;
					do {
						pick = (int)(Random.value * Random.value * (candidates.Count));
					} while(System.Array.IndexOf(ranked, candidates[pick]) >= 0);
					ranked[r] = candidates[pick];
				}
				Ballot v = new Ballot ();
				v.id = "rand" + i.ToString ();
				v.vote = ranked;
				votes.Add (v);
			}
			IRV_calc (votes, transform, -1, (List<RunoffResult> results) => {
				MakeVisualization(results);
			});
		}
	}

	void MakeVisualization(List<RunoffResult> r) {
		for (int rank = 0; rank < 1/*r.Count*/; ++rank) {
			RunoffHistory d = r[rank].showme;
			GameObject rankObject = new GameObject ("rank " + rank);
			rankObject.transform.position = new Vector3 (0, 0, rank * 3);

			IRV_vis.VisualComponents vc = new IRV_vis.VisualComponents ();
			IRV_vis.IRV_createVisualizationView (d.data, r[rank].showme.candidates, d.numVotes, 0, 0, r[rank].voteCount, 100, rankObject.transform, vc);

			for(int round = 0; round < d.data.Count; ++round){
				for (int b = 0; b < d.data [round].Count; b++) {
					VoteBloc bloc = d.data [round] [b];
					GameObject cube = Instantiate (this.basicBar);
					cube.name = bloc.candidate.name;//d.candidates [int.Parse (bloc.C)];
					cube.transform.SetParent (rankObject.transform);
//					Debug.Log (bloc.C);
					cube.GetComponent<Renderer> ().material.color = bloc.candidate.coloration;//d.colors[int.Parse(bloc.C)];//d.colorMap [bloc.C];
					int width = bloc.voteCount, start = bloc.startPosition;
					cube.transform.localScale = new Vector3 (width, cube.transform.localScale.y, cube.transform.localScale.z);
					cube.transform.localPosition = new Vector3 (start + (width / 2.0f), -round * 2, 0);
					TMPro.TextMeshPro tmpro = cube.GetComponentInChildren<TMPro.TextMeshPro> ();
					if (tmpro != null) {
//						if (b != d.data [round].Count - 1) {
//							Destroy (tmpro.gameObject);
//						} else {
							tmpro.text = cube.name;
							tmpro.transform.SetParent (null);
							tmpro.transform.localScale = Vector3.one;
							tmpro.transform.SetParent (cube.transform);
							float f = tmpro.transform.localPosition.z;
							tmpro.transform.localPosition = Vector3.zero + tmpro.transform.forward * f;
//						}
					}
				}
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
