using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OMU {
	public static class Util {
		public static object FromScript(string omScript) {
			return FromScript(omScript, "unnamed script");
		}
		public static object FromScript(string omScript, string sourceName) {
			FileParseResults results = null;
			return Parser.Parse(Parser.ParseType.JSON, sourceName, omScript, ref results);
		}

		public static string ToScript(object obj, bool hideZeroOrNull = false) {
			return Serializer.Stringify(obj, "\t", hideZeroOrNull, false, null);
		}

		public static string ToScriptTiny(object obj) {
			return Serializer.StringifyTiny(obj);
		}

		public static object FromScriptOverwrite<TYPE>(string omScript, ref TYPE objectToOverwrite) where TYPE : class {
			return FromScriptOverwrite(omScript, ref objectToOverwrite, null, "unnamed script");
		}
		public static object FromScriptOverwrite<TYPE>(string omScript, ref TYPE objectToOverwrite, FileParseResults results, string sourceName) where TYPE : class {
			FileParseResults resultsEvenIfUserDidntAskForThem = (results != null) ? results : new FileParseResults(sourceName, omScript);
			object output = objectToOverwrite;
			do {
				// parse the JSON tree
				object dom = Parser.Parse(Parser.ParseType.JSON, sourceName, omScript, ref resultsEvenIfUserDidntAskForThem);
				if(dom == null) break; // if parsing failed, we're done.
				System.Type t = typeof(TYPE);
				if(dom.GetType() == t) { objectToOverwrite = dom as TYPE; break; }// if parsing created the type being searched for, we're done
				if(objectToOverwrite == null) {
					objectToOverwrite = Data.CreateNew(t) as TYPE; // otherwise, construct and populate data as requested
				}
				output = objectToOverwrite;
				string errorText = Data.SetObjectFromOm(ref output, dom as object, Data.JSONFieldSearchBehavior.startswith, null);
				if(errorText != null) {
					resultsEvenIfUserDidntAskForThem.ERROR(errorText, Coord.INVALID);
				}
			} while(false);
			if(resultsEvenIfUserDidntAskForThem.Count > 0 && results == null) {
				Debug.LogError(resultsEvenIfUserDidntAskForThem);
			}
			return output;
		}
	}
}