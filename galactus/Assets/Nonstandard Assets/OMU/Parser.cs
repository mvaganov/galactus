using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using LIST_TYPE = System.Collections.Generic.List<object>;
using OBJ_TYPE = System.Collections.Generic.Dictionary<object,object>;

namespace OMU {
	
	public class Parser : IDisposable
	{
		/// <summary>
		/// Deserializes the script into a compiled object. Reports warnings to Debug.LogWarning, and reports errors as a System.Exception
		/// </summary>
		/// <returns>A compiled object, as though it were constructed and populated in the way defined by the script.</returns>
		/// <param name="filenameLabel">Filename.</param>
		/// <param name="scriptToParse">Script to parse.</param>
		public static object Compile(string filenameLabel, string scriptToParse) {
			FileParseResults output = new FileParseResults(filenameLabel, scriptToParse);
			if (scriptToParse == null) {
				if (output != null) output.ERROR("no data provided", Coord.INVALID, filenameLabel);
				return null;
			}
			object result = Parser.Parse (Parser.ParseType.JSON, filenameLabel, scriptToParse, ref output);
			if(output.CountWarnings() > 0) {
				Debug.LogWarning(output.ToStringWarnings());
			}
			if(output.CountErrors() > 0) {
				throw new System.Exception(output.ToStringErrors());
			}
			return result;
		}

		/// <summary></summary>
		/// <param name="json">An OM script string.</param>
		/// <param name="output">where to put results of parsing, can be null</param>
		/// <returns>An List&lt;object&gt;, a Dictionary&lt;string, object&gt;, a double, an integer,a string, null, true, or false</returns>
		public static object DeserializeJSON (string filenameLabel, string scriptToParse, FileParseResults output) {
			if (scriptToParse == null) {
				if (output != null) output.ERROR("no data provided", Coord.INVALID, filenameLabel);
				else Debug.LogError("no data provided");
				return null;
			}
			return Parser.Parse (Parser.ParseType.JSON, filenameLabel, scriptToParse, ref output);
		}
		
		/// <summary>
		/// Parses the scripted string
		/// </summary>
		/// <param name="filenameLabel">What to name this script in any error messages.</param>
		/// <param name="scriptToParse">A script as a tring, formatted in a JSON-like way.</param>
		/// <param name="output">where to put results of parsing, can be null</param>
		/// <returns>An OM.Value, which can be reflected on.</returns>
		public static Value Deserialize (string filenameLabel, string scriptToParse, FileParseResults output) {
			// save the string for debug information
			if (scriptToParse == null) {
				if (output != null) output.ERROR("no data provided", Coord.INVALID, filenameLabel);
				return new Value(null);
			}
			return new Value(Parser.Parse (Parser.ParseType.JSON, filenameLabel, scriptToParse, ref output));
		}
		
		/// <summary>
		/// Parses the string json into a value
		/// </summary>
		/// <param name="json">A JSON string.</param>
		/// <returns>An List&lt;object&gt;, a Dictionary&lt;string, object&gt;, a double, an integer,a string, null, true, or false</returns>
		public static object DeserializeCSV (string filename, string csv, FileParseResults output) {
			// save the string for debug information
			if (csv == null) {
				if (output != null)
					UnityEngine.Debug.Log ("no data provided");
				return null;
			}
			return Parser.Parse (Parser.ParseType.CSV, filename, csv, ref output);
		}
		
		public const int SPACES_PER_TAB = 4;
		
		public static string[] dateFormats = new string[]{
			"yyyy/M/d@H.m.s",
			"yyyy/M/d@H.m",
			"yyyy/M/d@H",
			"yyyy/M/d",
		};
		
		public static DateTime ParseDateTime(string s) {
			System.Globalization.CultureInfo provider = System.Globalization.CultureInfo.InvariantCulture;
			DateTime dt;// = DateTime.ParseExact(value as string, OM.dateFormats, provider, System.Globalization.DateTimeStyles.AdjustToUniversal);
			DateTime.TryParseExact (s, dateFormats, provider, System.Globalization.DateTimeStyles.AdjustToUniversal, out dt);
			return dt;
		}
		
		const string WORD_BREAK = "{}[],\":()\\"; // TODO sort and use binary search
		const string EXPRESSION_BREAK = "+-=*/<>&|%^!#@";
		string lastParsedToken;
		FileParseResults output;
		Coord coord = new Coord(1,1); // because most text editors count the first line as line 1, and the first column as column 1
		int index = 0;
		string filename;
		StringReader text;
		const string typeReplaceToken = "#type"; // similar to '#define' from C preprocessor, but only for types.
		Dictionary<string, string> typeReplace = new Dictionary<string, string>();
		public enum ParseType { JSON, CSV };
		Result.Type errorLevelOfMissingColon = Result.Type.none;

		private int usingExpressionWordBreak = 0;		
		public static bool IsWordBreak (char c) {
			return Char.IsWhiteSpace (c) || WORD_BREAK.IndexOf (c) != -1;
		}

		public static bool IsExpressionBreak(char c){ return IsWordBreak(c) || EXPRESSION_BREAK.IndexOf(c) != -1; }
		
		enum TOKEN {
			UNDEFINED,
			CURLY_OPEN, // used for tables
			CURLY_CLOSE,
			SQUARED_OPEN, // used for arrays
			SQUARED_CLOSE,
			PAREN_OPEN, // used for evaluation of expressions (references, math, boolean logic)
			PAREN_CLOSE,
			COLON, // separate name/value pairs
			COMMA, // separate listed items
			STRING,
			NUMBER,
			TRUE,
			FALSE,
			NULL,
			DATETIME,
			EOF,
			SPECIFIC_OBJECT_TYPE, // when a specific kind of object needs to be put here
		};
		
		Parser (string filename, string inputText, ref FileParseResults output) {
			this.filename = filename;
			this.output = output;
			text = new StringReader(inputText);
		}
		
        public static object Parse(string jsonString)
        {
            return Compile("<script>", jsonString);
        }

		public static object Parse (ParseType parseType, string filename, string jsonString, ref FileParseResults output) {
			object treeOfData = null;
			using (var instance = new Parser(filename, jsonString, ref output)) {
				switch (parseType) {
				case ParseType.JSON: treeOfData = instance.ParseJSON (); break;
				case ParseType.CSV: treeOfData = instance.ParseCSV (); break;
				}
			}
			return treeOfData;
		}
		
		public void Dispose () {
			text.Dispose ();
			text = null;
		}
		
		private void Log (Result.Type t, string a_text) {
			if (t != Result.Type.none && output != null) {
				output.Add (new Result(t, a_text, filename, coord));
			}
		}
		
		/// <summary>
		/// reads the next {object script}, and returns a an object of the type that immidiately precedded the object definition
		/// </summary>
		/// <returns></returns>
		object ParseSerializedObject () {
			string typename = lastParsedToken.Trim();
			EatWhitespace();
			Type subtype = Type.GetType(typename.ToString());
			object newObject = Data.CreateNew(subtype);
			//Debug.Log ("created a new <<"+typename+">> "+newObject);
			string err = null;
			if(newObject != null) {
				object value = ParseObject();
				if(value == null) {
					err = "explicit type <"+typename+"> must have a body @"+coord;
				} else {
					err = Data.SetObjectFromOmObject(ref newObject, value as OBJ_TYPE, Data.JSONFieldSearchBehavior.failfast, null);
				}
			} else {
				err = "could not create type <"+typename+"> using a default constructor @"+coord;
			}
			if(err != null) {
				Log (Result.Type.ERROR, err);
				throw new System.Exception(err);
			}
			return newObject;
		}
		
		OBJ_TYPE ParseObject () {
			OBJ_TYPE table = new OBJ_TYPE();
			object name = null;
			object v;
			// ditch opening brace
			NextChar ();
			Coord last;
			// {
			while (true) {
				last = coord;
				name = null;
				v = null;
				TOKEN t = NextToken (usingExpressionWordBreak>0);
				switch (t) {
				case TOKEN.EOF:	Log (Result.Type.ERROR, "unexpected EOF in dictionary at " + coord);
					return null;
				case TOKEN.COMMA:		NextChar ();	continue;
				case TOKEN.CURLY_CLOSE:	NextChar ();	return table;
				case TOKEN.STRING:
					name = ParseString ();
					if (name == null) {
						Log (Result.Type.ERROR, "bad dictionary string literal at " + coord);
						return null;
					}
					break;
				case TOKEN.CURLY_OPEN:
					Log (Result.Type.ERROR, "cannot use another table as a name, at " + coord);
					return null;
				case TOKEN.PAREN_CLOSE:
					break;
				case TOKEN.PAREN_OPEN:
					name = ParseByToken(t); // TODO add code that allows math ops to be read without whitespace, as though they were delimeters
					//Debug.Log("OM.Expression table property: \""+name.ToString()+"\"");
					break;
				case TOKEN.NUMBER:
					name = ParseNumber ().ToString ();
					//Log (Result.Type.warning, "converting number to string \""+name+"\" at " + coord);
					break;
				case TOKEN.UNDEFINED:
					if(usingExpressionWordBreak != 0) {
						PutBackChar(lastParsedToken); // grab the entire undefined token, in case Expression parsing cut it short
						usingExpressionWordBreak = 0;
						lastParsedToken = NextWord();
					}
					name = lastParsedToken;
					break;
				case TOKEN.DATETIME:
					name = ParseDateTime(lastParsedToken.ToString());
					break;
				default:
					UnityEngine.Debug.Log ("oh noes!"+" misunderstood token ("+t+") at " + coord + " right after \"" +lastParsedToken+"\"");
					Log (Result.Type.warning, "misunderstood token ("+t+") at " + coord + " \"" +lastParsedToken);
					break;
				}
				if (name != null) {
					//Debug.Log (index+":"+coord + "\""+name+"\":...");
					// Debug.Log("+name "+name+" "+t);
					t = NextToken (false);
					if(this.usingExpressionWordBreak != 0){
						Debug.Log("hmm... using expression for key? "+lastParsedToken);
					}
					if (t != TOKEN.COLON) {
						Log (errorLevelOfMissingColon, "missing colon at " + coord);
					} else {
						NextChar ();
					}
					if (t == TOKEN.UNDEFINED) {
						v = lastParsedToken;
					} else {
						v = ParseJSON ();
					}
					//Debug.Log (index+":"+coord + "\""+name+"\": "+OM.Serialize(v));
					table[name] = v;
				}
				if (coord == last) {
					Log (Result.Type.ERROR, "failed to advance in table at " + coord);
					return null;
				}
			}
			// while loop above must eventually break...
		}
		
		LIST_TYPE ParseArray () {
			return ParseArray (TOKEN.SQUARED_CLOSE, TOKEN.COMMA, false);
		}
		
		/// <summary>
		/// Parses the array.
		/// </summary>
		/// <returns>The array.</returns>
		/// <param name="close">Close. TOKEN.SQUARED_CLOSE</param>
		/// <param name="separate">Separate. TOKEN.COMMA</param>
		LIST_TYPE ParseArray (TOKEN close, TOKEN separate, bool useExpressionTokens) {
			//Debug.Log ("parsing array");
			LIST_TYPE array = new LIST_TYPE();
			// ditch opening bracket
			NextChar ();
			// [
			var parsing = true;
			Coord last;
			while (parsing) {
				last = coord;
				TOKEN nextToken = NextToken (useExpressionTokens);
				if (nextToken == TOKEN.EOF) {
					Log (Result.Type.ERROR, "unexpected EOF in array at " + coord);
					return null;
				} else if (nextToken == separate) {
					NextChar ();
					continue;
				} else if (nextToken == close) {
					NextChar ();
					//Debug.Log ("] at " + array.Count + " elements");
					parsing = false;
				} else if (nextToken == TOKEN.UNDEFINED) {
					array.Add (lastParsedToken); // TODO if allowed to add non-string tokens as strings
				} else {
					object value = ParseByToken (nextToken);
					//Debug.Log ("read array element: "+OM.Serialize(value));
					array.Add (value);
				}
				if (coord == last) {
					Log (Result.Type.ERROR, "failed to advance in list at " + coord);
					return null;
				}
			}
			//Debug.Log ("parsed " + array.Count + " elements");
			return array;
		}
		
		Expression ParseExpression () {
			LIST_TYPE expr = ParseArray (TOKEN.PAREN_CLOSE, TOKEN.COMMA, true);
			// Expression e = new Expression();
			// e.expList = expr;
			Expression e = new Expression(null, expr);
			// e.expList = expr;
			e.Process ();
			return e;
		}
		
		object ParseJSON () {
            return ParseByToken (NextToken (false));
		}
		
		object ParseCSV () {
			LIST_TYPE entireCSV = new LIST_TYPE();
			LIST_TYPE row = new LIST_TYPE();
			char c;
			Coord last;
			bool expectingPossibleComma = false, nullFieldGiven = false;
			do {
				last = coord;
				int citer = 0;
				nullFieldGiven = false;
				do {
					c = PeekChar ();
					if (c == ',') {
						if(expectingPossibleComma) {
							NextChar ();
							expectingPossibleComma = false;
						} else {
							nullFieldGiven = true;
							break;
						}
					}
					if (citer++ > 100000) {
						Log (Result.Type.ERROR, "crazy overflow problem in CSV-comma-dump read at " + coord);
						break;
					}
				} while(c == ',');
				if(c == '\n'
				   || c == '\r'
				   || c == (char)0) {
					if (c != (char)0)
						NextChar ();
					if (row.Count > 0) {
						Console.WriteLine ("adding " + row.Count);
						entireCSV.Add (row);
						row = new LIST_TYPE();
					}
				} else if(c == ',' && nullFieldGiven) {
					row.Add ("");
				} else {
					object o = ParseJSON ();
					if (o == null) {
						break;
					}
					row.Add (o);
				}
				if (c != (char)0 && coord == last) {
					Log (Result.Type.ERROR, "failed to advance in list at " + coord);
					return null;
				}
				expectingPossibleComma = true;
			} while(c != (char)0);
			return entireCSV;
		}

        object ParseByToken (TOKEN token) {
            object result = null;
			switch (token) {
			case TOKEN.STRING:  		result = ParseString ();    break;
			case TOKEN.NUMBER:          result = ParseNumber();     break;
			case TOKEN.CURLY_OPEN:      result = ParseObject();     break;
			case TOKEN.SQUARED_OPEN:    result = ParseArray();      break;
			case TOKEN.PAREN_OPEN:      result = ParseExpression(); break;
			case TOKEN.TRUE:            result = true;  break;
			case TOKEN.FALSE:           result = false; break;
			case TOKEN.NULL:            result = null;  break;
			case TOKEN.DATETIME:        result = ParseDateTime(lastParsedToken.ToString()); break;
			case TOKEN.UNDEFINED:       result = lastParsedToken;   break;
			case TOKEN.SPECIFIC_OBJECT_TYPE:    result = ParseSerializedObject(); break;
            default:
				Log (Result.Type.warning, "unable to parse data from <" + token + "> \"" + PeekChar () + "\" at " + coord);
                break;
			}
            //Debug.Log("TOKEN: \""+result+"\"");
            return result;
		}
		
		string ParseString () {
			StringBuilder s = new StringBuilder();
			char c, openingChar = PeekChar();
			if (openingChar == '\"' || openingChar == '\'') {
				// ditch opening quote
				NextChar ();
			} else {
				Log (Result.Type.ERROR, "malformed string literal at " + coord);
			}
			bool parsing = true;
			while (parsing) {
				if (Peek () == -1) {
					parsing = false;
					break;
				}
				c = NextChar ();
				if (c == openingChar) {
					parsing = false;
				} else switch (c) {
				case '\\':
					if (Peek () == -1) {
						parsing = false;
						break;
					}
					c = NextChar ();
					switch (c) {
					case '\"':
					case '\'':
					case '\\':
					case '/':
						s.Append (c);
						break;
					case 'b':	s.Append ('\b');	break;
					case 'f':	s.Append ('\f');	break;
					case 'n':	s.Append ('\n');	break;
					case 'r':	s.Append ('\r');	break;
					case 't':	s.Append ('\t');	break;
					case 'u':
						var hex = new char[4];
						for (int i=0; i< hex.Length; i++) {
							hex[i] = NextChar ();
						}
						char unicode = (char)Convert.ToInt32 (new string(hex), 16);
						//Debug.Log("-------"+(int)unicode+" "+(char)unicode);
						s.Append (unicode);
						break;
					}
					break;
				default:
					s.Append (c);
					break;
				}
			}
			return s.ToString ();
		}		

		object ParseNumber () {
			string number = NextWord();
			if (number.IndexOf ('.') == -1) {
				long parsedInt;
				if (Int64.TryParse (number.ToString(), out parsedInt))
					return parsedInt;
			} else {
				double parsedDouble;
				if (Double.TryParse (number.ToString(), out parsedDouble))
					return parsedDouble;
			}
			return number;
		}
		public static Coord CountWhitespaceToEat(string s) {
			Coord delta = new Coord(0,0);
			int i=0;
			while(i < s.Length && Char.IsWhiteSpace (s[i])) { i++; delta.AdvanceBy(s[i]); }
			return delta;
		}
		void EatWhitespace () {
			int p;
			bool whitespace;
			do {
				p = Peek ();
				if (p == -1)
					break;
				whitespace = Char.IsWhiteSpace (Convert.ToChar (p));
				if (whitespace)
					NextChar ();
			} while(whitespace);
		}
		
		void EatLineComment () {
			int p;
			char lastChar, c = ' ';
			do {
				p = Peek ();
				if (p == -1)
					break;
				lastChar = c;
				c = NextChar ();
			} while(!(c == '\n' && lastChar != '\\'));
		}
		
		void EatBlockComment () {
			int p;
			char lastChar, c = ' ';
			do {
				p = Peek ();
				if (p == -1)
					break;
				lastChar = c;
				c = NextChar ();
			} while(!(lastChar == '*' && c == '/'));
		}
		
		char PeekChar () {
			int result = Peek ();
			if (result <= 0)
				return (char)0;
			return Convert.ToChar (result);
		}
		
		private StringBuilder unreadBuffer = new StringBuilder();
		void PutBackChar(string str) {
			unreadBuffer.Insert(0, str);
			for(int i=str.Length-1;i>=0;--i){
				char c = str[i];
				if (c != '\t') { coord.col--; } 
				else { coord.col -= SPACES_PER_TAB; }
				if (c == '\n') { coord.row --; coord.col = 0; }
			}
		}
		int Peek() {
			return (unreadBuffer.Length == 0)? text.Peek() : unreadBuffer[0];
		}
		char NextChar () {
			char c;
			if(unreadBuffer.Length == 0) {
				c = Convert.ToChar (text.Read ());
			} else {
				c = unreadBuffer[0];
				unreadBuffer = unreadBuffer.Remove(0,1);
				//Debug.Log("removed "+c+", unread buffer: "+unreadBuffer);
			}
			if (c != '\t') {
				coord.NextCol ();
			} else {
				coord.col += SPACES_PER_TAB;
			}
			if (c == '\n') {
				coord.NextRow ();
			}
			index++;
			return c;
		}
		
		int OneOfTheExpressions(string token) {
			for(int i=0;i<Expression.OPS.Length; ++i) {
				string syntax = Expression.OPS[i].syntax;
				if(syntax != null && token.EndsWith(syntax)) { return i; }
			}
			return -1;
		}
		int[] LongerExpressionsThatStartWith(string token, int[] narrowBand = null) {
			List<int> ops = new List<int>();
			int totalChecks = (narrowBand!=null)?narrowBand.Length:Expression.OPS.Length;
			for(int i=0;i<totalChecks; ++i) {
				int index = (narrowBand!=null)?narrowBand[i]:i;
				string syntax = Expression.OPS[index].syntax;
				if(syntax != null && syntax.StartsWith(token)) { ops.Add(index); }
			}
			if(ops.Count == 0) return null;
			return ops.ToArray();
		}

		string NextWord () {
			StringBuilder word = new StringBuilder();
			int[] possibleMultiCharBreakingToken = null;
			while (!IsWordBreak(PeekChar())) {
				word.Append (NextChar ());
				if (Peek () == -1) { break; }
				if (word.Length == 2 && word[0] == '/' && (word[1] == '/' || word[1] == '*')) { break; }
				// if we're worried about finding expression tokens (like math operators)
				if(usingExpressionWordBreak > 0) {
					// check if the current word could have an operator at the end
					int expFound = OneOfTheExpressions(word.ToString());
					if(expFound >= 0) { // if this word is part of an operator...
						string syntax = Expression.OPS[expFound].syntax;
						// if the entire word isn't the operator
						if(word.Length != syntax.Length){
							// just finish up the word then...
							word.Remove(word.Length-syntax.Length, syntax.Length);
							PutBackChar(syntax);
						} else {
							// if the entire word IS the op, check to see if it might be the beginning of a multichar op
							possibleMultiCharBreakingToken = LongerExpressionsThatStartWith(word.ToString());
						}
						break;
					}
				}
			}
			// if the current word could be a multichar op
			if(possibleMultiCharBreakingToken != null) {
				int validTokenLength = 0;
				bool keepChecking = PeekChar() != 0;
				while(keepChecking) {
					//Debug.Log("Checking \'"+word.ToString()+"\' at "+coord+" for possibilities: "+possibleMultiCharBreakingToken.Length);
					// check if the current token is a valid breaking token as-is
					for(int i=0;i<possibleMultiCharBreakingToken.Length;++i){
						string syntax = Expression.OPS[possibleMultiCharBreakingToken[i]].syntax;
						if(syntax == word.ToString()) { validTokenLength = syntax.Length; break; }
					}
					// add the next letter, and see if it is still possibly a multichar op
					char c = NextChar();
					word.Append(c);
					//Debug.Log("how about "+word+" (added the "+c+")");
					possibleMultiCharBreakingToken = LongerExpressionsThatStartWith(word.ToString(), possibleMultiCharBreakingToken);
					// if there are no other possible tokens this could be
					if(possibleMultiCharBreakingToken == null) {
						// Debug.Log(word.ToString()+" not possible. reverting to "+word.ToString().Substring(0,validTokenLength)+" and returning "+word.ToString().Substring(validTokenLength));
						// go back to the match that was workable, and put the extra characters back.
						PutBackChar(word.ToString().Substring(validTokenLength));
						word = word.Remove(validTokenLength, word.Length-validTokenLength);
						// Debug.Log("giving out "+word.ToString());
						keepChecking = false;
						// Debug.Log("unused: "+unreadBuffer.ToString());
					}
					// if there is only one possible op, and it's an exact match, we're done here.
					else if(possibleMultiCharBreakingToken.Length == 1
					&& Expression.OPS[possibleMultiCharBreakingToken[0]].syntax == word.ToString()) {
						keepChecking = false;
					}
					// if there are multiple possible matches still, keep reading.
					else if(possibleMultiCharBreakingToken.Length > 1) {
						keepChecking = true;
					}
				}
			}
			return word.ToString ();
		}
		
		TOKEN NextToken (bool expressionTokens) {
			usingExpressionWordBreak = expressionTokens?1:0;
			bool getAnotherToken;
			char c;
			do {
				getAnotherToken = false;
				EatWhitespace ();
				int p = Peek ();
				if (p == -1) { return TOKEN.EOF; }
				c = Convert.ToChar (p);
				switch (c) {
				case '{':   return TOKEN.CURLY_OPEN;
				case '}':   return TOKEN.CURLY_CLOSE;
				case '[':   return TOKEN.SQUARED_OPEN;
				case ']':   return TOKEN.SQUARED_CLOSE;
				case '(':   return TOKEN.PAREN_OPEN;
				case ')':   return TOKEN.PAREN_CLOSE;
				case ',':   return TOKEN.COMMA;
				case '"':   case '\'':  return TOKEN.STRING;
				case ':':   return TOKEN.COLON;
				case '0':   case '1':   case '2':   case '3':   case '4':
				case '5':   case '6':   case '7':   case '8':   case '9':
				case '-':   return TOKEN.NUMBER;
				}
				if (!getAnotherToken) {
					lastParsedToken = NextWord();
					switch (lastParsedToken) {
					case "false":   return TOKEN.FALSE;
					case "true":    return TOKEN.TRUE;
					case "null":    return TOKEN.NULL;
					case "//":  EatLineComment ();  getAnotherToken = true; break;
					case "/*":  EatBlockComment (); getAnotherToken = true; break;
					case typeReplaceToken:
						EatWhitespace(); string shortHand = NextWord();
						EatWhitespace(); string longTypeName = NextWord();
						typeReplace[shortHand] = longTypeName;
						getAnotherToken = true; break;
					default:
						string typeToken = lastParsedToken;
						if(typeToken.StartsWith("<") && typeToken.EndsWith(">")) {
							typeToken = typeToken.Substring(1, typeToken.Length - 2);
						}
						string trueTypeName;
						if(typeReplace.TryGetValue(typeToken, out trueTypeName)) {
							typeToken = trueTypeName.Trim();
						}
						Type specificType = Type.GetType(typeToken.ToString());
						if(specificType != null) {
							lastParsedToken = typeToken;
							return TOKEN.SPECIFIC_OBJECT_TYPE;
						}
						break;
					}
				}
			} while(getAnotherToken);
			DateTime dt;
			if(DateTime.TryParse(lastParsedToken, out dt)) {
				return TOKEN.DATETIME;
			}
			return TOKEN.UNDEFINED;
		}
	}

		/// a single compile result
	public class Result {
		static public bool printResultsInConsoleAsTheyHappen = false;//true;//
		public enum Type { none, ERROR, warning };
		public Type t = Type.warning;
		public string filename;
		public string text;
		public Coord coord;
		
		public Result (Type t, string text, string filename, Coord coord) {
			this.t = t;
			this.text = text;
			this.coord = coord;
			this.filename = filename;
			if(printResultsInConsoleAsTheyHappen) {
				if(t == Type.ERROR) {	Debug.LogError(this);	}
				else if(t == Type.warning) {	Debug.LogWarning(this);	}
			}
		}
		
		public static Result ERROR (string text, string filename, Coord coord) {
			return new Result(Type.ERROR, text, filename, coord);
		}
		
		public static Result warning (string text, string filename, Coord coord) {
			return new Result(Type.warning, text, filename, coord);
		}
		
		override public string ToString () {
			return t + ((filename != null)?(" in " + filename):"")
				+ ((coord!=Coord.INVALID)?(" at " + coord):"") + ":\n" + text;
		}
	};
	/// multiple compile results. made for convenience, so classes using OM.Parse don't need to include System.Collections.Generic, or write templated class names
	public class FileParseResults : List<Result> {
		public string filename;
		public object filedata = null;
		public Coord filePosition = Coord.INVALID;
		public FileParseResults(string filename, object filedata = null) {this.filename=filename;this.filedata=filedata;}
		public void ERROR (string text, Coord coord = default(Coord), string filename = null) {
			Add (new Result (Result.Type.ERROR, text, (filename!=null)?filename:this.filename, coord));
		}
		public void warning (string text, Coord coord = default(Coord), string filename = null) {
			Add (new Result (Result.Type.warning, text, (filename!=null)?filename:this.filename, coord));
		}
		public string ToString(Result.Type t) {
			StringBuilder sb = new StringBuilder ();
			int entry = 0;
			for(int i = 0; i < this.Count; ++i) {
				if(this[i].t == t) {
					if(entry > 0) sb.Append("\n");
					sb.Append(this[i]);
					entry++;
				}
			}
			return sb.ToString ();
		}
		override public string ToString() {
			StringBuilder sb = new StringBuilder ();
			for(int i = 0; i < this.Count; ++i) {
				if(i > 0) sb.Append("\n");
				sb.Append(this[i]);
			}
			return sb.ToString ();
		}
		public int CountMessageOfType(Result.Type type) {
			int count = 0;
			for(int i = 0; i < this.Count; ++i) {
				if(this[i].t == type)
					count++;
			}
			return count;
		}
		public int CountErrors() { return CountMessageOfType (Result.Type.ERROR);}
		public int CountWarnings() { return CountMessageOfType (Result.Type.warning);}
		public string ToStringErrors() { return ToString (Result.Type.ERROR); }
		public string ToStringWarnings() { return ToString (Result.Type.warning); }
	}
}