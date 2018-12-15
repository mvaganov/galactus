using UnityEngine;
using System.Collections;
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
			return Serializer.SerializeInternal (obj, indentation, hideZeroOrNull, compressNames, ignoreFieldPrefixes);
		}

		/// <returns>OM equivalent of given object, in a compressed form</returns>
		/// <param name="obj">Object.</param>
		/// <param name="ignoreFieldPrefixes">Ignores fields with these prefixes. null means include all possible fields</param>
		public static string StringifyTiny(object obj, string[] ignoreFieldPrefixes = null) {
			return Serializer.Stringify (obj, null, true, true, ignoreFieldPrefixes);
		}
		
		public static string StringifyExpression (object obj, bool whitespace, bool compressNames = false) {
			return Serializer.SerializeInternal (obj, whitespace?" ":null, false, compressNames, null, 1);
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
		
		private static string SerializeInternal (object obj, string indentation = "\t",
			bool hideZeroOrNull = true, bool compressNames = false, string[] ignoreFieldPrefixes = null, int expressionDepth = 0) {
			Serializer instance = new Serializer();
			instance.indentation = indentation;
			instance.hideZeroOrNull = hideZeroOrNull;
			instance.compressNames = compressNames;
			instance.ignoreFieldPrefixes = ignoreFieldPrefixes;
			instance.expressionDepth = expressionDepth;
			instance.SerializeValue (obj);
			string str = instance.builder.ToString ();
			return str;
		}
		void SerializeValue (object value) {
			IList asList;
			IDictionary asDict;
            if (value == null) {
                Append("null");
            } else if (Data.IsStringType(value.GetType())) {
                SerializeString(value.ToString(), expressionDepth>0);
            } else if (value is bool) {
                Append((bool)value ? "true" : "false");
            } else if ((asList = value as IList) != null) {
                SerializeList(asList);
            } else if ((asDict = value as IDictionary) != null) {
                SerializeObject(asDict);
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
				object om = Data.SerializeToOm(value, hideZeroOrNull, compressNames, ignoreFieldPrefixes);
				Type t = value.GetType();
				if(!Data.IsNativeType(t) && t != arrayTypeToIgnore) {
					SerializeString(t.ToString(), false);
					if(indentation != null) Append(' ');
				}
				SerializeValue (om);
				//SerializeString (value.ToString ());
			}
		}
		private Type arrayTypeToIgnore = null;
		
		void SerializeObject (IDictionary obj) {
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
			foreach (object e in obj.Keys) {
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
				SerializeValue (obj[e]);
				first = false;
			}
			if (needsWhitespace) {
				Append ('\n');
				indentationLevel -= 1;
				Indent ();
			}
			Append ('}');
		}
		
		void SerializeList (IList anArray) {
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
				SerializeValue (obj);
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