using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using LIST_TYPE = System.Collections.Generic.List<object>;
using OBJ_TYPE = System.Collections.Generic.Dictionary<object,object>;

namespace OMU {
	/// <summary>Value-type wrapper around:
	/// OMU.Object(Dictionary&lt;object,object&rt;)
	/// OMU.Array(List&lt;object&rt;)
	/// OMU.Expression
	/// Value types (string, int, double, bool, Vector2, Vector3, object, ...)
	/// made to simplify use of the ObjectModel system</summary>
	public class Value {
		/// <summary>the value being managed by this Adapter Facade</summary>
		private object _val;

		public Value(object v) {
			if(v is Value) v = ((Value)v)._val; // don't double-wrap
			this._val = v;
		}

		/// <returns>The value type, as <see cref="System.object.GetType()"/></returns>
		public Type GetValueType() { return _val.GetType (); }

		public Value this[object i] {
			get {
				if (i is IList) {
					object output;
					if (Data.TryDeReferenceGet (_val, i as IList, out output)) { return new Value(output); }
					throw new System.Exception("could not access [\""+i+"\"] in "+_val.GetType());
				}
				if(_val is IList) {
					return new Value ((_val as IList)[Convert.ToInt32 (i)]);
				} else {
					OBJ_TYPE obj = _val as OBJ_TYPE;
					if(obj != null) {
						object o;
						if(!obj.TryGetValue(i, out o)) {
							throw new System.Exception("cannot access \""+i+"\" in table");
						}
						return new Value(o);
					}
					if (i is string) {
						object output;
						if (!Data.TryDeReferenceGet (_val, i as string, out output)) {
							throw new System.Exception("cannot mutate \""+_val+"\" for "+obj.GetType());
						}
						return new Value(output);
					}
				}
				throw new System.Exception("cannot access \""+_val+"\" ["+i+"]");
			}
			set {
				if (i is IList) {
					if (Data.TryDeReferenceSet (_val, i as IList, value)) { return; }
					throw new System.Exception("could not access [\""+i+"\"] in "+_val.GetType());
				}
				IList arr = _val as IList;
				if(arr != null) {
					int index = Convert.ToInt32(i);
					if(index == arr.Count)
						arr.Add (value);
					else
						arr[index] = value;
					return;
				} else {
					OBJ_TYPE obj = _val as OBJ_TYPE;
					if(obj != null) {
						Data.Set (obj, value, i);
						return;
					}
					if (i is string) {
						if (Data.TryDeReferenceSet (_val, i as string, value)) { return; }
						throw new System.Exception("cannot mutate \""+_val+"\" for "+obj.GetType());
					}
				}
				throw new System.Exception("cannot mutate \""+_val+"\"'s ["+i+"]");
			}
		}

		/// <summary>Tries to get the value behind the given key. This could be an array index or a table key.</summary>
		/// <returns><c>true</c>, if key was found (output in value), <c>false</c> otherwise.</returns>
		/// <param name="key">Key. what is the name of the data being looked for</param>
		/// <param name="value">Value. the data being looked for, as output</param>
		public bool TryGet(object key, out object value) {
			IList arr = _val as IList;
			if(arr != null) {
				value = arr[Convert.ToInt32(key)];
				return true;
			} else {
				OBJ_TYPE obj = _val as OBJ_TYPE;
				if(obj != null) {
					if(obj.TryGetValue(key, out value)) {
						return true;
					}
				}
			}
			value = null;
			return false;
		}

		/// <summary>
		/// Set the specified value at the given path. For example, to put the number value '1' behind the member "count", call:
		/// Set(1, "count");
		/// Data in paths can be set this way as well, so if there is a "objects" sub-table, the "count" member of that table can be set with:
		/// Set(1, "objects", "count");
		/// </summary>
		/// <param name="value">Value.</param>
		/// <param name="path">Path.</param>
		public bool Set(object value, params object[] path) {
			return Data.TryDeReferenceSet(_val, path, value);
		}

		public bool Set(object value, IList path) {
			return Data.TryDeReferenceSet(_val, path, value);
		}

		/// <summary>Changes what this <see cref="Value"/> wraps around.</summary>
		/// <param name="value">Value.</param>
		public void SetRawObject(object value) { _val = value; }

		/// <summary>Gets the raw object that this <see cref="Value"/> is wrapping around.</summary>
		/// <returns>The raw object.</returns>
		public object GetRawObject() { return _val; }

		/// <returns>A <see cref="System.String"/> that represents the current <see cref="Value"/>.</returns>
		public override string ToString () {
			if(_val == null) return null;
			if(_val.GetType() == typeof(string))
				return _val as string;
			if(Data.IsNativeType(_val.GetType())) {
				return Serializer.Stringify(_val);
			}
			return _val.ToString();
		}

		/// <returns>wrapped value as a <see cref="System.Boolean"/></returns>
		public bool AsBool() { return (Boolean)_val; }
		/// <returns>wrapped value as a <see cref="System.String"/></returns>
		public string AsString() { return _val.ToString(); }
		public LIST_TYPE AsArray() { return _val as LIST_TYPE; }
		public Expression AsExpression() { return _val as Expression; }
		public TYPE As<TYPE>() where TYPE : class { return _val as TYPE; }
		public long AsLong() { long result; Data.TryParseLong (_val, out result); return result; }
		public double AsDouble() { double result; Data.TryParseDouble (_val, out result); return result; }
		public Color AsColor() { Color result; Data.TryParseColor (_val, out result); return result; }
		public Vector2 AsVector2() { Vector2 result; Data.TryParseVector2 (_val, out result); return result; }
		public Vector3 AsVector3() { Vector3 result; Data.TryParseVector3 (_val, out result); return result; }
        public Quaternion AsQuaternion() { Quaternion result; Data.TryParseQuaternion(_val, out result); return result; }

        public bool IsNull() { return _val == null; }
		public bool IsNumeric() { return Data.IsNumericType (_val.GetType ()); }
		public bool IsBool() { return _val is Boolean; }
		public bool IsTrue() { return (_val is Boolean && AsBool () == true); }
		public bool IsFalse() { return (_val is Boolean && AsBool () == false); }
		public bool IsString() { return _val is String; }
		public bool IsObject() { return _val is OBJ_TYPE; }
		public bool IsArray() { return _val is LIST_TYPE; }
		public bool IsColor() { return _val is Color; }
		public bool IsVector2() { return _val is Vector2; }
		public bool IsVector3() { return _val is Vector3; }
        public bool IsQuaternion() { return _val is Quaternion; }
        public bool IsDateTime() { return _val is System.DateTime; }
		
		public string Serialize() { return Serializer.Stringify (_val); }
		
        // automatic conversions implied by assignment operator
//		public static implicit operator Value(object value) { return new Value(value); }
		public static implicit operator Value(OBJ_TYPE value) { return new Value(value); }
		public static implicit operator Value(LIST_TYPE value) { return new Value(value); }
		public static implicit operator Value(Expression value) { return new Value(value); }
		public static implicit operator Value(String value) { return new Value(value); }
		public static implicit operator Value(Int32 value) { return new Value(value); }
		public static implicit operator Value(Int64 value) { return new Value(value); }
		public static implicit operator Value(Single value) { return new Value(value); }
		public static implicit operator Value(Double value) { return new Value(value); }
		public static implicit operator Value(Boolean value) { return new Value(value); }
		public static implicit operator Value(Dictionary<string,object> value) { return new Value(value); }
		public static implicit operator Value(System.DateTime value) { return new Value(value); }
		public static implicit operator Value(Vector2 value) { return new Value(value); }
		public static implicit operator Value(Vector3 value) { return new Value(value); }
        public static implicit operator Value(Quaternion value) { return new Value(value); }

		public static int GetInt(object fromWhat, string nameOfInt, int defaultValue = 0) {
			int returnedValue = defaultValue;
			if(!TryGetInt(fromWhat, nameOfInt, out returnedValue)){
				return defaultValue;
			}
			return returnedValue;
		}
		public static long GetLong(object fromWhat, string nameOfInt, long defaultValue = 0L) {
			long returnedValue = defaultValue;
			if(!TryGetLong(fromWhat, nameOfInt, out returnedValue)){
				return defaultValue;
			}
			return returnedValue;
		}
		public static bool TryGetInt(object fromWhat, string intName, out int value) {
			object obj;
			value = 0;
			if(OMU.Data.TryDeReferenceGet(fromWhat, intName, out obj)) {
				value = (int)obj;
				return true;
			}
			return false;
		}
		public static bool TryGetLong(object fromWhat, string intName, out long value) {
			object obj;
			value = 0;
			if(OMU.Data.TryDeReferenceGet(fromWhat, intName, out obj)) {
				value = (long)obj;
				return true;
			}
			return false;
		}

		/// <summary>generates an object from the given script, internally 'unnamed script'. no errors reported</summary>
		/// <returns>The object based on the script</returns>
		/// <param name="omScript">JSON-like script.</param>
		public static Value FromScript(string omScript) {
			return FromScript(omScript, "unnamed script");
		}

		/// <summary>generates an object from the given script</summary>
		/// <returns>The object based on the script</returns>
		/// <param name="omScript">JSON-like script.</param>
		/// <param name="sourceName">what to call the script in errors and/or warnings</param>
		public static Value FromScript(string omScript, string sourceName) {
			FileParseResults results = null;
			return new Value(Parser.Parse (Parser.ParseType.JSON, sourceName, omScript, ref results));
		}

		/// <summary>generates an object from the given script</summary>
		/// <returns>The object based on the script</returns>
		/// <param name="omScript">JSON-like script.</param>
		/// <param name="results">a list of parsing results, as errors and/or warnings</param>
		/// <param name="sourceName">what to call the script in errors and/or warnings</param>
		public static Value FromScript(string omScript, FileParseResults results = null, string sourceName = "unnamed script") {
			return new Value(Parser.Parse (Parser.ParseType.JSON, sourceName, omScript, ref results));
		}
		
		/// <summary>generates an object from the given script</summary>
		/// <returns>The object based on the script</returns>
		/// <param name="omScript">JSON-like script.</param>
		/// <param name="results">a list of parsing results, as errors and/or warnings. if null, no results will be given</param>
		/// <param name="sourceName">what to call the script in errors and/or warnings</param>
		public static TYPE FromScript<TYPE>(string omScript, FileParseResults results = null, string sourceName = "unnamed script") where TYPE : class {
			object output = null;
			FileParseResults resultsEvenIfUserDidntAskForThem = (results!=null)?results:new FileParseResults(sourceName,omScript);
			do{
				// parse the JSON tree
				object dom = Parser.Parse (Parser.ParseType.JSON, sourceName, omScript, ref resultsEvenIfUserDidntAskForThem);
				if(dom == null) break; // if parsing failed, we're done.
				Type t = typeof(TYPE);
				if(dom.GetType() == t) { output = dom; break; }// if parsing created the type being searched for, we're done
				output = Data.CreateNew (t) as TYPE; // otherwise, construct and populate data as requested
				string errorText = Data.SetObjectFromOm (ref output, dom as object, Data.JSONFieldSearchBehavior.startswith, null);
				if(errorText != null) {
					resultsEvenIfUserDidntAskForThem.ERROR(errorText, Coord.INVALID);
				}
			}while(false);
			if(resultsEvenIfUserDidntAskForThem.Count > 0 && results == null) {
				Debug.LogError(resultsEvenIfUserDidntAskForThem);
			}
			return output as TYPE;
		}
	}
}