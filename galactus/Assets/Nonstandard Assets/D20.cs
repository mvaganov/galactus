using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class D20 : MonoBehaviour {
	public enum RollAdvantage { none=0, advantage=1, disadvantage=2,
		advantageFirstRollOnly =3, disadvantageFirstRollOnly =4, advantageAfterFirstRoll=5 };

	[ReadOnly]
	public int sidedDice = 20;
	public int entertainDiceSize = 6;
	public int psychicCostDiceSize = 6;
	public int maxTries = 12;
	[InspectorButton("PrintTable", 250)]
	public bool generateEntertainmentTable;
	public int entertainDice = 5;
	public int[] statBonuses = new int[] { 4, 3, 2, 1 };
	public RollAdvantage advantage = RollAdvantage.none;
	[Tooltip("is this performer naturally reducing the complexity of their performance on a failure?")]
	public bool reduceDCOnDifficulty = true;
	[Tooltip("is this performer nominally entertaining in an absolute sense, even if they don't perform?")]
	public bool alwaysGetDiceBonus = true;

	[Tooltip("from https://www.fantasynamegenerators.com/song-title-generator.php")]
	public NS.TextPtr.ObjectPtr randomNames;

	public enum S {
		Athletics_s, Acrobatics_d, SleightOfHand_d, Stealth_d,
		Arcana_i, History_i, Investigation_i, Nature_i, Religion_i,
		AnimalHandling_w, Insight_w, Medicine_w, Perception_w, Survival_w,
		Deception_c, Intimidation_c, Performance_c, Persuasion_c
	};
	/*
		S.Athletics_s, S.Acrobatics_d, S.SleightOfHand_d, S.Stealth_d,
		S.Arcana_i, S.History_i, S.Investigation_i, S.Nature_i, S.Religion_i,
		S.AnimalHandling_w, S.Insight_w, S.Medicine_w, S.Perception_w, S.Survival_w,
		S.Deception_c, S.Intimidation_c, S.Performance_c, S.Persuasion_c
	 */
	[System.Serializable]
	public class Instrumentation
	{
		public string type;
		public string[] examples;
		public S[] skill;
	}
	public Instrumentation[] instruments = {
		new Instrumentation{ type="vocal",
			examples=new string[]{"ballad","spoken word","chant","choir","opera", "pop", "folk", "blues","R&B","country","gospel","rap","techno"},
			skill = new S[]{S.Arcana_i, S.History_i, S.Nature_i, S.Religion_i,
				S.AnimalHandling_w, S.Medicine_w, S.Survival_w, S.Deception_c,
				S.Intimidation_c, S.Performance_c, S.Persuasion_c}
		},
		new Instrumentation{ type ="string",
			examples=new string[]{"guitar","lute","banjo","violin","fiddle","bass","electric bass guitar","viola","harp","mandolin","eukalele","electric guitar","sitar","shamisen"},
			skill = new S[]{S.Athletics_s, S.Acrobatics_d, S.SleightOfHand_d, S.Stealth_d,
				S.Arcana_i, S.Medicine_w, S.Survival_w, S.Deception_c, S.Intimidation_c,
				S.Performance_c, S.Persuasion_c}
		},
		new Instrumentation{ type="precussion",
			examples=new string[]{"drum","drum set","snare drum","hand drum","marching drum","taiko drum","cymbals","triangle","xylaphone","bongo","bells","chimes","gong","tamborine","cowbell","box","buckets"},
			skill = new S[]{S.Athletics_s, S.Acrobatics_d, S.SleightOfHand_d, S.Stealth_d,
				S.Investigation_i, S.Nature_i, S.Religion_i,S.Deception_c, S.Intimidation_c,
				S.Performance_c, S.Persuasion_c}
		},
		new Instrumentation{ type="wind",
			examples=new string[]{"flute","ocarina","harmonica","pan pipes","recorder","shawm","trumpet","clarinet","saxaphone","oboe","bass clarinet","whistle","bag pipes"},
			skill = new S[]{S.Athletics_s, S.SleightOfHand_d, S.Stealth_d,
				S.Arcana_i, S.Investigation_i, S.Nature_i, S.Religion_i, S.AnimalHandling_w,
				S.Medicine_w, S.Performance_c, S.Persuasion_c}
		},
		//new Instrumentation{ type="keyboard",
		//	examples=new string[]{"piano","grand piano","electric keyboard","sound machine","organ","harpsaccord","accordian"},
		//	skill = new S[]{S.SleightOfHand_d, S.Stealth_d, S.Arcana_i, S.History_i,
		//		S.Investigation_i, S.Insight_w, S.Medicine_w, S.Performance_c, S.Persuasion_c}
		//},
		//new Instrumentation{ type="dance",
		//	examples=new string[]{"choreography","contemporary","acrobatic","headbanging","parkour","break dance","line dance","tap","jazz","interpretive","exotic","ballet","hip-hop","balroom","swing"},
		//	skill = new S[]{S.Athletics_s, S.Acrobatics_d, S.SleightOfHand_d, S.Stealth_d,
		//		S.Nature_i, S.AnimalHandling_w, S.Medicine_w, S.Survival_w, S.Deception_c, S.Intimidation_c,
		//		S.Performance_c, S.Persuasion_c}
		//},
	};

	// Use this for initialization
	void Start() {
		PrintTable();
	}

	public void PrintTable()
	{
		CalculateTable().Print();
	}

	public struct DecisionTable
	{
		public int diceCount, diceSize, diceBonus, rollBonus;
		public float[,] averageSuccessRate;
		public float[,] averageCosts;
		public float[,] averageGains;
		public int easyDC, hardDC, hardDiceCount;
		public override string ToString()
		{
			string table = ((rollBonus>=0)?"+":"")+rollBonus+" "+diceCount + "d"+diceSize
				+ ((diceBonus>0) ? "+" : "")+((diceBonus!=0)? diceBonus.ToString():"");
			float averageDice = (diceSize + 1) / 2f;
			for (int col = 0; col < averageCosts.GetLength(1); ++col)
			{
				table += "\t" + (col + 1)+" ("+(int)(averageDice*col)+")";
			}
			for (int row = 0; row < averageCosts.GetLength(0); row++)
			{
				table += "\nDC" + (row + 1).ToString("00");
				if (row == easyDC) table += "E"+(diceCount);
				if (row == hardDC) table += "H"+(hardDiceCount);
				bool likelyFound = false;
				for (int col = 0; col < averageCosts.GetLength(1); ++col)
				{
					float entertainmentValue = averageGains[row, col];
					float psychicCost = averageCosts[row, col];
					//table += "\t" + (successPercents[row, col] * 100).ToString("00.0");
					table += "\t";
					if (psychicCost > entertainmentValue)
					{
						table += "X";
					}
					table += (entertainmentValue).ToString("00.0");
					if (!likelyFound && averageSuccessRate[row, col] > 0.5f)
					{
						likelyFound = true;
						table += "~";// + ((int)psychicCost) + ")";
					}
				}
			}
			return table;
		}
		public void Print() {
			Debug.Log(ToString());
		}
	}

	[System.Serializable]
	public class SongPart {
		public string instrumentation;
		public string specificInstrument;
		public bool instrumentationNeedsWork;
		[Tooltip("how many d6 of entertainment")]
		public int entertainmentDice;
		[Tooltip("DC to succeed")]
		public int difficultyCheck;
		[Tooltip("expected bonus of the person performing this part")]
		public int intendedSkillLevel;
	}

	[System.Serializable]
	public class Song {
		public string name;
		public float songLevel;
		public int totalEntertainment;
		public float averageDifficulty;
		public float entertainmentPerDifficultyRatio;
		public SongPart[] parts;
		public void CalcStats()
		{
			totalEntertainment = 0;
			averageDifficulty = 0;
			for (int i = 0; i < parts.Length; ++i)
			{
				totalEntertainment += parts[i].entertainmentDice;
				averageDifficulty += parts[i].difficultyCheck;
			}
			averageDifficulty /= parts.Length;
			entertainmentPerDifficultyRatio = totalEntertainment / averageDifficulty;
			float expectedAudienceEntertainmentDemand = (totalEntertainment-(parts.Length+1)) * 7;
			songLevel = (expectedAudienceEntertainmentDemand / 15) - 4;
		}
	}

	[InspectorButton("MakeTheSong")]
	public bool makeTheSong;

	[ContextMenuItem("Rework Selected Instrumentation or Stringify", "ReworkTheSong")]
	public Song song;

	[InspectorButton("ReworkTheSong")]
	public bool reworkSelectedInstrumentation;

	public void MakeTheSong()
	{
		song = GenerateSong();
	}

	public void ReworkTheSong()
	{
		int reworked = 0;
		// go into the song parts and re-randomize instrumentation
		for(int i = 0; i < song.parts.Length; ++i)
		{
			if (song.parts[i].instrumentationNeedsWork)
			{
				RandomizeInstrumentation(song.parts[i], song.parts);
				reworked++;
			}
		}
		song.CalcStats();
		if (reworked == 0)
		{
			print(OMU.Serializer.Stringify(song));
		}
	}

	public Song GenerateSong()
	{
		// load random song name from file
		string[] names = randomNames.ToString().Split('\n');
		float level = 0;
		for (int i = 0; i < statBonuses.Length; ++i) { level += statBonuses[i]; }
		level /= statBonuses.Length;
		// create statBonuses.Length parts
		song = new Song {
			name = names[Random.Range(0, names.Length)],
			songLevel = level, // modified again by CalcStats
			parts = new SongPart[statBonuses.Length]
		};
		// for each part
		for (int i = 0; i < song.parts.Length; ++i) {
			song.parts[i] = GenerateSongPart(statBonuses[i], 1);// (int)Mathf.Round( (level + 0.5f)/2 ) + 1 );
			RandomizeInstrumentation(song.parts[i], song.parts);
		}
		song.CalcStats();
		return song;
	}
	public SongPart GenerateSongPart(int statBonus, int baseEntertainment)
	{
		SongPart part = new SongPart();
		int diceCount = statBonus;// Random.Range(0, statBonus+1);
		DecisionTable dt = CalculateTable(statBonus, statBonus, 4+diceCount, RollAdvantage.none, false, false);
		float difficultyDelta = dt.hardDC - dt.easyDC;
		float difficulty = statBonus;// difficultyDelta * ((float)diceCount / statBonus);
		//if(difficultyDelta > statBonus && diceCount < statBonus)
		//{
		//	// add some difficulty wiggle
		//	difficulty += Random.Range(0, difficultyDelta / statBonus - 0.5f);
		//}
		// difficultyCheck
		part.difficultyCheck = 12//dt.easyDC 
			+ (int)difficulty;
		part.intendedSkillLevel = statBonus;
		part.entertainmentDice = baseEntertainment + diceCount;
		return part;
	}
	public int InstrumentationIn(Instrumentation instrum, SongPart[] otherParts) {
		for (int i = 0; i < otherParts.Length; ++i) {
			if (otherParts[i] != null && otherParts[i].instrumentation == instrum.type) return i;
		}
		return -1;
	}
	public void RandomizeInstrumentation(SongPart part, SongPart[] otherParts) {
		// generate an instrumentation
		Instrumentation instrum;
		int index = Random.Range(0, instruments.Length);
		do {
			instrum = instruments[index% instruments.Length];
			if (otherParts != null && InstrumentationIn(instrum, otherParts) >= 0) { index++; } 
			else { break; }
		} while (index < instruments.Length*2);
		part.instrumentation = instrum.type;
		// generate a specific instrument
		part.specificInstrument = instrum.examples[Random.Range(0, instrum.examples.Length)];
	}

	public DecisionTable CalculateTable()
	{
		return CalculateTable(statBonuses[0], statBonuses[0], entertainDice, 
			advantage, reduceDCOnDifficulty, alwaysGetDiceBonus);
	}

	public DecisionTable CalculateTable(int performBonus, int statBonus, int entertainDice, 
		RollAdvantage advantage, bool reduceDCOnDifficulty, bool alwaysGetDiceBonus)
	{
		float averageEntertainDiceRoll = (entertainDiceSize + 1) / 2f;
		float averagePsychicCostDiceRoll = (psychicCostDiceSize + 1) / 2f;
		float[] standardPercentage = new float[sidedDice];
		for (int i = 0; i < standardPercentage.Length; ++i) {
			standardPercentage[i] = 1f - ((float)i) / sidedDice;
		}
		float[] advantagePercentage = new float[sidedDice];
		float[] disadvantagePercentage = new float[sidedDice];

		float[][] probabilityDistribution = new float[][] {
			standardPercentage,
			advantagePercentage,
			disadvantagePercentage,
			advantagePercentage,
			disadvantagePercentage,
			standardPercentage,
		};
		// generate statistics tables, including for advantage/disadvantage
		for (int i = 0; i < disadvantagePercentage.Length; ++i)
		{
			float val = (i + 1f) / sidedDice;
			disadvantagePercentage[disadvantagePercentage.Length - 1 - i] = val * val;
		}
		for (int i = 0; i < disadvantagePercentage.Length; ++i)
		{
			float val = ((float)i / sidedDice);
			advantagePercentage[i] = 1f - val * val;
		}
		float[] distribution = probabilityDistribution[(int)advantage];

		int rows = sidedDice + (performBonus > 0 ? performBonus : 0) + (reduceDCOnDifficulty ? maxTries : 0);
		float[,] successPercents = new float[rows, maxTries];
		for (int dc = 0; dc < successPercents.GetLength(0); ++dc)
		{
			float percentFailure = 1, percentSuccess;
			switch (advantage) {
				case RollAdvantage.advantageFirstRollOnly:
					distribution = probabilityDistribution[(int)RollAdvantage.advantage]; break;
				case RollAdvantage.disadvantageFirstRollOnly:
					distribution = probabilityDistribution[(int)RollAdvantage.disadvantage]; break;
				case RollAdvantage.advantageAfterFirstRoll:
					distribution = probabilityDistribution[(int)RollAdvantage.none]; break;
			}
			for (int t = 0; t < successPercents.GetLength(1); ++t)
			{
				float p;
				if (reduceDCOnDifficulty) {
					p = (dc >= (sidedDice+t)) ? 0 : distribution[dc - (dc > t ? t : 0)];
				} else {
					p = (dc >= sidedDice) ? 0 : distribution[dc];
				}
				percentFailure *= 1 - p;
				percentSuccess = 1 - percentFailure;
				successPercents[dc, t] = percentSuccess;
				switch (advantage) {
					case RollAdvantage.advantageFirstRollOnly:
					case RollAdvantage.disadvantageFirstRollOnly:
						distribution = probabilityDistribution[(int)RollAdvantage.none]; break;
					case RollAdvantage.advantageAfterFirstRoll:
						distribution = probabilityDistribution[(int)RollAdvantage.advantage]; break;
				}
			}
		}
		//string test = "";
		//for(int row = 0; row < rows; ++row)
		//{
		//	test += "["+(row + 1) + "]";
		//	for(int col = 0; col < successPercents.GetLength(1); ++col)
		//	{
		//		test += "\t"+successPercents[row,col].ToString(".000");
		//	}
		//	test += "\n";
		//}
		//print(test);
		DecisionTable dt = new DecisionTable {
			diceCount = entertainDice,
			diceSize = entertainDiceSize,
			diceBonus = statBonus,
			rollBonus = performBonus,
			averageSuccessRate = new float[rows,
				successPercents.GetLength(1)],
			averageGains = new float[rows, successPercents.GetLength(1)],
			averageCosts = new float[rows, successPercents.GetLength(1)]
		};
		for (int row = 0; row < dt.averageSuccessRate.GetLength(0); row++)
		{
			for (int col = 0; col < dt.averageSuccessRate.GetLength(1); ++col)
			{
				float successChance;
				if (row - performBonus < 0) {
					successChance = 1;
				} else
				if (row - performBonus >= rows) {
					successChance = 0;
				} else {
					successChance = successPercents[row - performBonus, col];
				}
				dt.averageSuccessRate[row, col] = successChance;
			}
		}
		for (int row = 0; row < dt.averageSuccessRate.GetLength(0); row++)
		{
			for (int col = 0; col < dt.averageSuccessRate.GetLength(1); ++col)
			{
				float entertainmentValue = dt.averageSuccessRate[row, col] * 
					((Mathf.Max(entertainDice - col, 0) * averageEntertainDiceRoll) 
					+ (alwaysGetDiceBonus?0:statBonus)) + (alwaysGetDiceBonus?statBonus:0);
				float psychicCost = col * averagePsychicCostDiceRoll;
				bool isSweetSpot = dt.averageSuccessRate[row, col] > 0.5f;
				if (col == 0 && isSweetSpot) {
					dt.easyDC = row;
				} else if(psychicCost < entertainmentValue && col < maxTries-1 &&
					dt.averageSuccessRate[row, col+1] > 0.5f) {
					float nextPsychicCost = (col + 1) * averagePsychicCostDiceRoll;
					float nextEntertainmentValue = dt.averageSuccessRate[row, col+1] *
						((Mathf.Max(entertainDice - (col+1), 0) * averageEntertainDiceRoll)
						+ (alwaysGetDiceBonus ? 0 : statBonus)) + (alwaysGetDiceBonus ? statBonus : 0);
					if (nextPsychicCost >= nextEntertainmentValue) {
						dt.hardDC = row;
						dt.hardDiceCount = entertainDice - col;
					}
				}
				dt.averageCosts[row, col] = psychicCost;
				dt.averageGains[row, col] = entertainmentValue;
			}
		}
		return dt;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
