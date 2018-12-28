using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;

namespace OMU {
	public class Serializer
	{
		/// <summary>converts an object into JSON-like notation</summary>
		/// <param name="obj">any object</param>
		/// <param name="indentation">wheather or not to use whitespace. null for none</param>
		/// <returns>A JSON encoded string, or null if object 'json' is not serializable</returns>
		public static string Stringify (object obj, string indentation = "\t", 
			bool hideZeroOrNull = true, bool compressNames = false, string[] ignoreFieldPrefixes = null) {
			return SerializeInternal (obj, indentation, hideZeroOrNull, compressNames, ignoreFieldPrefixes);
		}

		/// <returns>OM equivalent of given object, in a compressed form</returns>
		/// <param name="obj">Object.</param>
		/// <param name="ignoreFieldPrefixes">Ignores fields with these prefixes. null means include all possible fields</param>
		public static string StringifyTiny(object obj, string[] ignoreFieldPrefixes = null) {
			return Stringify (obj, null, true, true, ignoreFieldPrefixes);
		}
		
		public static string StringifyExpression (object obj, bool whitespace, bool compressNames = false) {
			return SerializeInternal (obj, whitespace?" ":null, false, compressNames, null, 1);
		}

		int indentationLevel = 0;
		// TODO what is this variable for again?
		int expressionDepth = 0;
		StringBuilder builder;
		Coord coord;
		/// <summary>weather or not to use whitespace. null for none</summary>
		string indentation = "\t";
		int pageWidth = 80;
		string[] ignoreFieldPrefixes;
		bool wordWrap = true;
		bool wordWrapForLongHomogeneousArrays = false;
		bool hideZeroOrNull;
		bool compressNames;
		public const string metadataOrder = "order";
		public const string metadataIncludetype = "includeType";

		void Append (string str) {
			Coord lastPosition = coord;
			for (int i = 0; i < str.Length; ++i) { coord.AdvanceBy (str [i]); }
			// if printing this string would cause a line overflow
			if (indentation != null && wordWrap && coord.col >= pageWidth && !(str.Length == 1 
              && (str[0] == '\n' || str[0] == ','))) {
				coord = lastPosition;
				// if indentation is causing the overflow... ignore it
				if (str != indentation) {
					wordWrap = false;
					Append ('\n');
					indentationLevel++;
					Indent ();
					Append (str);
					indentationLevel--;
					wordWrap = true;
				}
			} else {
				builder.Append (str);
			}
		}
		
		void Append (char c) { Append (c.ToString()); }

		void Indent () {
			if (indentation != null) {
				for (int i = 0; i < indentationLevel; ++i) { Append (indentation); }
			}
		}
		
		Serializer () { builder = new StringBuilder(); }

		private static string SerializeInternal (object obj, string indentation = "\t", bool hideZeroOrNull = true,
		bool compressNames = false, string[] ignoreFieldPrefixes = null, int expressionDepth = 0, 
		string metadataElement = "__meta") {
			Serializer instance = new Serializer();
			instance.indentation = indentation;
			instance.hideZeroOrNull = hideZeroOrNull;
			instance.compressNames = compressNames;
			instance.ignoreFieldPrefixes = ignoreFieldPrefixes;
			instance.expressionDepth = expressionDepth;
			instance.SerializeValue (obj, metadataElement);
			instance.ExtractTypes(true);
			string str = instance.builder.ToString ();
			return str;
		}

		struct TypeAtIndex { public Type t; public int i; }
		List<TypeAtIndex> typeAtLocation = new List<TypeAtIndex>();
		Dictionary<Type, List<int>> locationsOfType = new Dictionary<Type, List<int>>();

		void FoundAnother(Type t, int atIndex) {
			typeAtLocation.Add(new TypeAtIndex { t = t, i = atIndex });
			List<int> list;
			if(!locationsOfType.TryGetValue(t, out list)) {
				locationsOfType[t] = new List<int>() { atIndex }; }
			else { list.Add(atIndex); }
		}

		public static string ShortenTypeName(string fullname, bool compressed, int attempt = 0){
			// get the name without namespaces
			string letters;
			if(compressed) {
				int lastDot = fullname.LastIndexOf('.');
				string name = fullname.Substring(lastDot + 1);
				int limit = attempt+1;
				if(limit > name.Length) { limit = name.Length; }
				letters = name.Substring(0, limit);
				// get the first 'wordFragmentLength' letters of each word
				for(int i = limit; i < name.Length; ++i) {
					if(Char.IsUpper(name[i])) {
						limit = attempt+1;
						if(i + limit > name.Length) { limit = name.Length - i; }
						letters += name.Substring(i, limit);
						i += limit;
					}
				}
			} else {
				int index = fullname.Length-1;
				for(int i = 0; i < attempt; ++i){
					index = fullname.LastIndexOf('.', index);
				}
				letters = fullname.Substring(index + 1);
			}
			return letters;
		}

		private void ExtractTypes(bool prefaceAllTypes = false) {
			//string outp = "----\n";
			// get the types in order of most common
			List<Type> typesToAlias = new List<Type>();
			foreach(var kvp in locationsOfType) {
				if(prefaceAllTypes || kvp.Value.Count > 1) { typesToAlias.Add(kvp.Key); }
				//outp += kvp.Key + OMU.Util.ToScript(kvp.Value) + "\n";
			}
			//Debug.Log(outp);
			typesToAlias.Sort((a, b) => { return locationsOfType[a].Count < locationsOfType[b].Count ? 1 : -1; });
			List<string> aliases = new List<string>();
			for(int i = 0; i < typesToAlias.Count; ++i){
				Type t = typesToAlias[i];
				bool renamed = false;
				int attempts = 0;
				string fullname = t.ToString(), lastAttempt = null;
				bool tryingFullname = false;
				while(attempts < fullname.Length && !renamed) {
					string shorthand = null;
					if(!tryingFullname) {
						shorthand = ShortenTypeName(fullname, compressNames, attempts);
						if(lastAttempt == shorthand) {
							tryingFullname = true;
						}
					} if(tryingFullname) {
						shorthand = fullname;
					}
					if(lastAttempt == fullname) { throw new Exception("Exhausted short names!"); }
					//Debug.Log("trying "+shorthand+" for "+fullname);
					lastAttempt = shorthand;
					if(!string.IsNullOrEmpty(shorthand)) {
						if(compressNames) {
							for(int letters = 0; letters < shorthand.Length && !renamed; letters++) {
								string abbrev = shorthand.Substring(0, letters + 1);
								if(aliases.IndexOf(abbrev) < 0) {
									aliases.Add(abbrev);
									renamed = true;
								}
							}
						} else {
							if(aliases.IndexOf(shorthand) < 0) {
								aliases.Add(shorthand);
								renamed = true;
							}
						}
					}
					attempts++;
				}
			}
			if(aliases.Count > 0) {
				// create the preface
				string preface = Parser.typeReplaceToken + "{";
				for(int i = 0; i < aliases.Count; ++i) {
					if(i > 0) preface += "\n";
					preface += aliases[i] + ":" + typesToAlias[i];
				}
				preface += "}\n";
				// replace names backwards, so indexes don't get shuffled around
				for(int i = typeAtLocation.Count - 1; i >= 0; --i) {
					Type t = typeAtLocation[i].t;
					int index = typesToAlias.IndexOf(t);
					if(index >= 0) {
						string fullname = t.ToString();
						builder.Replace(fullname, aliases[index], typeAtLocation[i].i, fullname.Length);
					}
				}
				// add the preface
				builder.Insert(0, preface);
			}
		}

		void SerializeValue (object value, string metadataElement) {
			IList asList;
			IDictionary asDict;
			if (value == null) {
				Append("null");
			} else if (Data.IsStringType(value.GetType())) {
				SerializeString(value.ToString(), expressionDepth>0);
			} else if (value is bool) {
				Append((bool)value ? "true" : "false");
			} else if ((asList = value as IList) != null) {
				SerializeList(asList, metadataElement);
			} else if ((asDict = value as IDictionary) != null) {
				SerializeObject(asDict, metadataElement);
			} else if (value is char) {
				SerializeString(new string((char)value, 1), expressionDepth>0);
			} else if (value is Expression) {
				Append ((value as Expression).ToString (indentation != null));
			} else if (value is float) {
				Append (((float)value).ToString ("R"));
			} else if (value is double || value is decimal) {
				Append (Convert.ToDouble (value).ToString ("R"));
			} else if (Data.IsNumericType(value.GetType())) {
				Append (value.ToString ());
			} else if (value is System.DateTime) {
				DateTime dt = (System.DateTime)value;
				Append (dt.ToString (Parser.dateFormats[0]));
			} else if (value is UnityEngine.Color) {
				Color c = (UnityEngine.Color)value;
				Color32 c32 = c;
				Append ("0x"+c32.a.ToString("X2")+c32.b.ToString("X2")+c32.g.ToString("X2")+c32.r.ToString("X2"));
			} else {
				object om = Data.SerializeToOm(value, hideZeroOrNull, compressNames, ignoreFieldPrefixes, metadataElement);
				Type t = value.GetType();
				if(!Data.IsNativeType(t) && t != arrayTypeToIgnore) {
					FoundAnother(t, builder.Length);
					SerializeString(t.ToString(), false);
					if(indentation != null) Append(' ');
				}
				SerializeValue (om, metadataElement);
				//SerializeString (value.ToString ());
			}
		}
		private Type arrayTypeToIgnore = null;
		
		void SerializeObject (IDictionary obj, string metadataElement) {
			bool needsWhitespace = indentation != null;
			if (obj.Count == 0) { needsWhitespace = false; }
			if (needsWhitespace && obj.Count < 5) {
				int countTablesInTable = 0;
				object o;
				foreach (object e in obj.Keys) {
					o = obj[e]; if (o is IDictionary || o is IList) { countTablesInTable++; }
				}
				needsWhitespace = (countTablesInTable != 0);
			}
			
			bool first = true;
			// commented out for egyptian curly-braces
			//if (needsWhitespace) { Append('\n'); Indent(); }
			Append ('{');
			if (needsWhitespace) {
				Append ('\n');
				indentationLevel += 1;
				Indent ();
			}
			List<object> orderOfElements = null;
			Dictionary<object, Type> includeType = null;
			if(metadataElement != null) {
				object e;
				Dictionary<string, object> metadata = obj[metadataElement] as Dictionary<string, object>;
				if(metadata != null) {
					if(metadata.TryGetValue(metadataOrder, out e)) {
						orderOfElements = e as List<object>;
					}
					if(metadata.TryGetValue(metadataIncludetype, out e)) {
						includeType = e as Dictionary<object, Type>;
					}
				}
			}
			if(orderOfElements == null) {
				ICollection keys = obj.Keys;
				orderOfElements = new List<object>();
				foreach(object e in keys) { orderOfElements.Add(e); }
			}
			for(int i = 0; i < orderOfElements.Count; ++i){
				object e = orderOfElements[i];
				if(e != null && e.Equals(metadataElement)){ continue; }
				if (!first) {
					Append (',');
					if (indentation != null) Append (' ');
					if (needsWhitespace) {
						Append ('\n');
						Indent ();
					}
				}
				SerializeString (e.ToString (), expressionDepth>0);
				Append ((indentation == null)?":":" : ");
				if(includeType != null){
					Type t;
					if(includeType.TryGetValue(e, out t)) {
						FoundAnother(t, builder.Length);
						SerializeString(t.ToString(), false);
						if(indentation != null) Append(' ');
					}
				}
				SerializeValue (obj[e], metadataElement);
				first = false;
			}
			if (needsWhitespace) {
				Append ('\n');
				indentationLevel -= 1;
				Indent ();
			}
			Append ('}');
		}
		
		void SerializeList (IList anArray, string metadataElement) {
			Type typeExpectedByList = anArray.GetType().GetElementType();
			if(typeExpectedByList == null){
				typeExpectedByList = anArray.GetType().GetGenericArguments()[0];
			}
			bool needsWhitespace = indentation != null;
			if (anArray.Count == 0) {
				needsWhitespace = false;
			}
			if (needsWhitespace && anArray.Count < 5) {
				int countTablesInTable = 0;
				foreach (object o in anArray) {
					if (o is IDictionary || o is IList) {
						countTablesInTable++;
					}
				}
				needsWhitespace = (countTablesInTable != 0);
			}
			bool allTheSame = true;
			if (needsWhitespace && wordWrap) {
				Type t = null;
				foreach (object value in anArray) {
					if(value != null) {
						if (t == null)
							t = value.GetType ();
						if (value.GetType () != t) {
							allTheSame = false;
							break;
						}
					}
				}
				needsWhitespace = !allTheSame;
			}
			bool oldWordwrapValue = wordWrap;
			if (allTheSame && !wordWrapForLongHomogeneousArrays) {
				needsWhitespace = false;
				wordWrap = false;
			}
			Append ('[');
			if (needsWhitespace) {
				Append ('\n');
				indentationLevel += 1;
				Indent ();
			}
			
			bool first = true;
			foreach (object obj in anArray) {
				if (!first) {
					Append (',');
					if (needsWhitespace) {
						Append ('\n');
						Indent ();
					} else if (indentation != null) Append (' ');
				}
				arrayTypeToIgnore = typeExpectedByList; // strict arrays don't need their types written
				SerializeValue (obj, metadataElement);
				arrayTypeToIgnore = null;
				first = false;
			}
			if (needsWhitespace) {
				Append ('\n');
				indentationLevel -= 1;
				Indent ();
			}
			Append (']');
			wordWrap = oldWordwrapValue;
		}
		void SerializeString (string str, bool inExpression) {
			bool oldWrapSetting = wordWrap; // disable wordwrap within strings
			wordWrap = false;
			double number;
			bool needsQuotes = str=="null" || str=="true" || str=="false" || Double.TryParse(str, out number);
			if(!needsQuotes){
				for (int i = 0; !needsQuotes && i < str.Length; ++i) {
					needsQuotes = (!inExpression)?Parser.IsWordBreak (str [i]):Parser.IsExpressionBreak(str[i]);
				}
			}
			if (needsQuotes) { Append ('\"'); }
			char[] charArray = str.ToCharArray ();
			StringBuilder token = new StringBuilder();
			foreach (var c in charArray) {
				switch (c) {
				case '\'':  token.Append ("\\\'");  break;
				case '"':   token.Append ("\\\"");  break;
				case '\\':  token.Append ("\\\\");  break;
				case '\b':  token.Append ("\\b");   break;
				case '\f':  token.Append ("\\f");   break;
				case '\n':  token.Append ("\\n");   break;
				case '\r':  token.Append ("\\r");   break;
				case '\t':  token.Append ("\\t");   break;
				default:
					int codepoint = Convert.ToInt32 (c);
					if ((codepoint >= 32) && (codepoint <= 126)) {
						token.Append (c);
					} else {
						token.Append ("\\u");
						token.Append (codepoint.ToString ("x4"));
					}
					break;
				}
			}
			Append (token.ToString ());
			if (needsQuotes) {
				Append ('\"');
			}
			wordWrap = oldWrapSetting;
		}
	}

	/// <summary>
	/// row/col struct, used to get meta data about where parsing is happening during parsing.
	/// </summary>
	public struct Coord {
		public int row, col;
		public static Coord INVALID = new Coord (0, 0);
		public Coord (int col, int row) { this.col = col; this.row = row; }
		public override string ToString () {	return "[" + row + ", " + col + "]";	}
		public static bool operator== (Coord a, Coord b) { return a.row == b.row && a.col == b.col; }
		public static bool operator!= (Coord a, Coord b) { return !(a == b); }
		
		public override bool Equals (System.Object obj) {
			if (obj == null) return false;
			Coord p = (Coord)obj;
			if ((System.Object)p == null) return false;
			return (col == p.col) && (row == p.row);
		}
		public bool Equals (Coord p) {
			if ((object)p == null) return false;
			return (col == p.col) && (row == p.row);
		}
		public override int GetHashCode () {	return col ^ row;	}
		public void NextCol () {	col++;	}
		public void NextRow () {	col = 0;	row++;	}

		public void AdvanceBy(char c) {
			switch (c) {
			case '\t': col += Parser.SPACES_PER_TAB; break;
			case '\n': NextRow(); break;
			default: NextCol (); break;
			}
		}
	}
}