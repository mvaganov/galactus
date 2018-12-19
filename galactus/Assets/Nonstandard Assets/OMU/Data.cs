using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Text;
using System.IO;

using LIST_TYPE = System.Collections.Generic.List<object>;
// TODO replace with IDictionary.
// check if IDictionay with objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(Dictionary<,>)
using OBJ_TYPE = System.Collections.Generic.Dictionary<object,object>;

namespace OMU {
	/// <summary>
	/// Utility code for converting data, serialization and deserialization, and traversing nested Lists and Dictionaries
	/// </summary>
	public class Data {
		public const string thisDirToken = ".";
		/// <summary>the prefix used to convert strings into linked file resources</summary>
		public const string RESCALL = "R$";

		/// <returns><c>true</c> if t is easy for the ObjectModel system to deal with.</returns>
		/// <param name="t"><see cref="System.object.GetType()"/></param>
		public static bool IsNativeType(Type t) {
			return IsNumericType(t)
				|| t == typeof(String)
				|| t == typeof(OBJ_TYPE)
				|| t == typeof(LIST_TYPE)
				|| t == typeof(Expression)
				|| t == typeof(System.DateTime);
		}

		public static bool IsIntegralType(Type t) {
			switch (Type.GetTypeCode(t)) {
			case TypeCode.Boolean:	case TypeCode.Byte:		case TypeCode.SByte:
			case TypeCode.UInt16:	case TypeCode.UInt32:	case TypeCode.UInt64:
			case TypeCode.Int16:	case TypeCode.Int32:	case TypeCode.Int64:
				return true;
			default:
				return false;
			}
		}

		/// <returns><c>true</c> if is t is a numeric type; otherwise, <c>false</c>.</returns>
		/// <param name="t"><see cref="System.object.GetType()"/></param>
		public static bool IsNumericType(Type t) {
			if (IsIntegralType (t)) return true;
			switch (Type.GetTypeCode(t)) {
			case TypeCode.Decimal:	case TypeCode.Double:	case TypeCode.Single:
				return true;
			default:
				return false;
			}
		}
		public static bool IsStringType(Type t) { return t == typeof(string) || t == typeof(StringBuilder); }
		
		public static string NormalizeString(object o) { return o.ToString(); }

		public static bool IsZeroOrNull(object obj) {   
			if(obj == null) return true;
			Type t = obj.GetType ();
			switch (Type.GetTypeCode(t))
			{
			case TypeCode.Boolean:return (bool)obj == false;
			case TypeCode.Byte:   return (Byte)obj == 0;
			case TypeCode.SByte:  return (SByte)obj == 0;
			case TypeCode.UInt16: return (UInt16)obj == 0;
			case TypeCode.UInt32: return (UInt32)obj == 0;
			case TypeCode.UInt64: return (UInt64)obj == 0;
			case TypeCode.Int16:  return (Int16)obj == 0;
			case TypeCode.Int32:  return (Int32)obj == 0;
			case TypeCode.Int64:  return (Int64)obj == 0;
			case TypeCode.Decimal:return (Decimal)obj == 0;
			case TypeCode.Double: return (Double)obj == 0;
			case TypeCode.Single: return (Single)obj == 0;
			case TypeCode.String: return (String)obj == "";
			}
			if(t == typeof(LIST_TYPE)) { return (obj as LIST_TYPE).Count == 0; }
			else if(t == typeof(OBJ_TYPE)) { return (obj as OBJ_TYPE).Count == 0; }
			else if(t == typeof(Vector2)){ return ((Vector2)obj) == Vector2.zero; }
			else if(t == typeof(Color)){ return ((Color)obj) == Color.clear; }
			else if(t == typeof(Vector2)){ return ((Vector2)obj) == Vector2.zero; }
			else if(t == typeof(Vector3)){ return ((Vector3)obj) == Vector3.zero; }
			else if(t == typeof(Quaternion)){ return ((Quaternion)obj) == Quaternion.identity; }
			else if(t == typeof(Dictionary<string,OBJ_TYPE>)) { return (obj as Dictionary<string,OBJ_TYPE>).Count == 0; }
			return false;
		}

		// TODO testme
		public static object DeepCopyIfPossible(object o) {
			object obj = o;
			if(o is IList) {
				IList olist = o as IList;
				LIST_TYPE list = new LIST_TYPE();
				list.Capacity = olist.Count;
				for(int i = 0; i < olist.Count; ++i) {
					list.Add(DeepCopyIfPossible(olist[i]));
				}
				obj = list;
			} else if(o is OBJ_TYPE) {
				OBJ_TYPE odict = o as OBJ_TYPE;
				OBJ_TYPE dict = new OBJ_TYPE();
				foreach(var kvp in odict) {
					dict[kvp.Key] = DeepCopyIfPossible(kvp.Value);
				}
				obj = dict;
				// TODO use reflection to create a new default object of the given type, and copy public members and Properties with get&set
			} else if(o is ICloneable) {
				obj = (o as ICloneable).Clone();
			}
			return obj;
		}
		public static LIST_TYPE CreateListWithSize(int size) {
			LIST_TYPE o = new LIST_TYPE();
			o.Capacity = size;
			for(int i=0;i<size;++i){o.Add(null);}
			return o;
		}

		public static string GetPropertyString(OBJ_TYPE dict, string propertyName) {
			string str = null; object obj;
			dict.TryGetValue (propertyName, out obj); Data.TryParseString (obj, out str);
			return str;
		}
		public static OBJ_TYPE GetPropertyObject(OBJ_TYPE dict, string propertyName) {
			OBJ_TYPE result = null; object obj;
			dict.TryGetValue (propertyName, out obj);
			if(obj != null && obj.GetType() == typeof(OBJ_TYPE)) { result = obj as OBJ_TYPE; }
			return result;
		}
		/// <summary>convenience set method for Dictionary</summary>
		public static bool Set(OBJ_TYPE dict, object value, params object[] path) {
			return Data.TryDeReferenceSet(dict, path, value);
		}
		/// <summary>convenience set method for Dictionary</summary>
		public static bool Set(OBJ_TYPE dict, object value, IList path) {
			return Data.TryDeReferenceSet(dict, path, value);
		}

		public static bool TryParseString(object obj, out string str) {
			str = null;
			bool parsed = false;
			if(obj != null) {
				Type t = obj.GetType ();
				if(Data.IsStringType(t) || Data.IsNumericType(t) || t == typeof(char)) {
					str = obj.ToString();
					parsed = true;
				}
			}
			return parsed;
		}

		/// <summary>Tries the given value as a long. Will handle hex values behind 0x or #</summary>
		/// <returns><c>true</c>, if parsed, <c>false</c> otherwise.</returns>
		/// <param name="o">the object to parse</param>
		/// <param name="number">where to place the output</param>
		public static bool TryParseLong(object o, out long number) {
			number = 0;
			bool parsed = false;
			if(o != null) {
				Type t = o.GetType ();
				if(Data.IsNumericType(t)) {
					number = Convert.ToInt64(o);
					parsed = true;
				} else if (Data.IsStringType(t)) {
					string s = o.ToString();
					if (!parsed) {
						parsed = Int64.TryParse(s, out number);
					} if (!parsed) {
						int hexStartsAt = -1;
						if (s.StartsWith("0x")) {
							hexStartsAt = 2;
						} else if (s.StartsWith("#")) {
							hexStartsAt = 1;
						}
						if (hexStartsAt != -1) {
							parsed = Int64.TryParse(s.Substring(hexStartsAt),
								System.Globalization.NumberStyles.HexNumber,
								null, out number);
						}
					}
				}
			}
			return parsed;
		}
		public static bool TryParseDouble(object o, out double number) {
			number = 0;
			bool parsed = false;
			if (o != null) {
				Type t = o.GetType ();
				if (Data.IsNumericType(t)) {
					number = Convert.ToDouble(o);
					parsed = true;
				} else if (Data.IsStringType(t)) {
					parsed = Double.TryParse(o.ToString(), out number);
					if (!parsed) {
						long iNumber;
						parsed = TryParseLong(o, out iNumber);
						if (parsed) {
							number = (double)iNumber;
						}
					}
				}
			}
			return parsed;
		}
		public static bool TryParseColor(object o, out Color color) {
			if(o is Color) { color = (Color)o; return true; }
			color = Color.white;
			long number;
			bool parsed = TryParseLong(o, out number);
			if(parsed) {
				float r = ((number >> 0) & 0xff) / 256.0f;
				float g = ((number >> 8) & 0xff) / 256.0f;
				float b = ((number >> 16) & 0xff) / 256.0f;
				float a = ((number >> 24) & 0xff) / 256.0f;
				string s = NormalizeString(o);
				if(s != null) {
					// if no alpha was specified, full opacity.
					if(s.StartsWith("0x") && s.Length <= 8
					|| s.StartsWith("#")  && s.Length <= 7)
						a = 1;
				}
				color = new Color(r,g,b,a);
			}
			return parsed;
		}

		public static bool TryParseVectorX(object o, out object v, int twoOrThree) {
			bool parsed = false;
			v = null;
			if (o is IList) {
				IList list = o as IList;
				float x = 0, y = 0, z = 0;
				double d;
				if (list.Count > 0) {
					TryParseDouble(list[0], out d); x = (float)d;
				}
				if (list.Count > 1) {
					TryParseDouble(list[1], out d); y = (float)d;
				}
				if (list.Count > 2) {
					TryParseDouble(list[2], out d); z = (float)d;
				}
				parsed = true;
				switch (twoOrThree) {
					case 2:	v = new Vector2(x, y);	break;
					case 3:	v = new Vector3(x, y, z); break;
					default: parsed = false; break;
				}
			} else if(o is Vector2 && twoOrThree == 2){ v = (Vector2)o; parsed = true;
			} else if(o is Vector3 && twoOrThree == 3){ v = (Vector3)o; parsed = true;
			}
			return parsed;
		}

		public static bool TryParseVector2(object o, out Vector2 v) {
			if (o is Vector2) { v = (Vector2)o; return true; }
			else if (o is Vector3) { v = (Vector2)o; return true; }
			object ob;
			bool parsed = TryParseVectorX(o, out ob, 2);
			v = parsed ? ((Vector2)ob) : Vector2.zero;
			return parsed;
		}

		public static bool TryParseVector3(object o, out Vector3 v) {
			if (o is Vector2) { v = (Vector3)o; return true; }
			else if (o is Vector3) { v = (Vector3)o; return true; }
			object ob;
			bool parsed = TryParseVectorX(o, out ob, 3);
			v = parsed ? ((Vector3)ob) : Vector3.zero;
			return parsed;
		}

        public static bool TryParseQuaternion(object o, out Quaternion v) {
            if (o is Quaternion) { v = (Quaternion)o; return true; }
            else if (o is Vector3) { v = Quaternion.Euler((Vector3)o); return true; }
            object ob;
            bool parsed = TryParseVectorX(o, out ob, 3);
            v = parsed ? (Quaternion.Euler((Vector3)ob)) : Quaternion.identity;
            return parsed;
        }

        public static bool TryParseDateTime(object o, out DateTime dt) {
			if(o is DateTime) { dt = (DateTime)o; return true; }
			string str = NormalizeString(o);
			if(str != null) {
				System.Globalization.CultureInfo provider = System.Globalization.CultureInfo.InvariantCulture;
				dt = DateTime.ParseExact(str,Parser.dateFormats,provider, System.Globalization.DateTimeStyles.AdjustToUniversal);
				return true;
			}
			dt = DateTime.UtcNow;
			return false;
		}

		public static bool TryParseAsset(string resname, out object a) {
			if(resname == null || !resname.StartsWith(RESCALL)) {a=null;return false;}
			int len = RESCALL.Length;
			resname = resname.Substring(len, resname.Length-len);
			a = Resources.Load (resname);
			return a != null;
		}

		private static Dictionary<string, long> timingTable;
		/// <param name="s">h23m59s59ms999 is 1 millisecond shy of 1 day</param>
		/// <param name="number">in milliseconds</param>
		/// <returns></returns>
		public static bool TryParseSpecialTimeFormat(String s, out long number) {
			if (timingTable == null) {
				timingTable = new Dictionary<string, long>();
				timingTable.Add("ms", 1);
				timingTable.Add("s", timingTable["ms"]*1000);
				timingTable.Add("m", timingTable["s"]*60);
				timingTable.Add("h", timingTable["m"]*60);
				timingTable.Add("d", timingTable["h"]*24);
				timingTable.Add("y", (long)(timingTable["d"]*365.25));
				timingTable.Add("mon", timingTable["y"]/12);
			}
			number = 0;
			int cursor = 0;
			string str = s.ToLower();
			int tokenStart = 0;
			bool foundNumber, readingType = true;
			char c;
			long msMultiple = 0;
			while (cursor <= str.Length) {
				if (cursor < str.Length) {
					c = str[cursor];
					foundNumber = (c >= '0' && c <= '9');
				} else {
					foundNumber = false;
				}
				if (foundNumber && readingType) { // finished reading a type token
					string type = str.Substring(tokenStart, cursor - tokenStart);
					if (!timingTable.TryGetValue(type, out msMultiple)) {
						UnityEngine.Debug.Log("unknown time type: "+type);
						return false;
					}
					UnityEngine.Debug.Log("-----------------type: " + type);
					readingType = false;
					tokenStart = cursor;
				} else if (!foundNumber && !readingType) { // finished reading number
					string num = str.Substring(tokenStart, cursor - tokenStart);
					double value;
					if (!Double.TryParse(num, out value))
						return false;
					UnityEngine.Debug.Log("-----------------number: " + num);
					number += (long)(value * msMultiple);
					readingType = true;
					tokenStart = cursor;
				}
				cursor++;
			}
			UnityEngine.Debug.Log("~~~~~~~~~~~~~~~~~~~~number "+number);
			return true;
		}

		public enum TraversalResult {undefined, found, arrayIndexOOB, unacceptablePathCreationVariable, 
			couldNotBeParsedAsInteger, couldNotFindMember, expectedStringNameOfMember, memberTypeNotTraversable, 
			unableToCreateNewMember, failedMidTraversal };
		/// <summary>traverses an object</summary>
		/// <param name="obj">current object (array, dictionary, or arbitrary object)</param>
		/// <param name="door">path to traverse along next (a member, array index, or dictionary entry)</param>
		/// <param name="nextObject">the object on the other side of this path</param>
		/// <param name="createPathAsNeeded">if no object exists on the other side, what kind of object should be created there? null if this is a pure search, without the intent of modifying the tree</param>
		/// <returns>TraversalResult.found if all is well, or a TraversalResult error code otherwise</returns>
		static public TraversalResult AdvanceThroughObject(object obj, object door, out object nextObject, System.Type createPathAsNeeded) {
			if (NormalizeString(door) == thisDirToken) {
				nextObject = obj;
				return TraversalResult.found;
			}
			if(obj == null) {
				throw new System.Exception ("cannot advance through \""+door.ToString ()+"\" of null object");
			}
			TraversalResult result = TraversalResult.found;
			nextObject = null;
			Type objType = obj.GetType ();
			if(obj is IList) {
				IList list = obj as IList;
				long index;
				if (TryParseLong(door, out index)) {
					if (index >= 0 && (index < list.Count || (createPathAsNeeded != null && index == list.Count))) {
						if(index == list.Count) {
							//list.Add (nextObject = Data.CreateNew (createPathAsNeeded));
							// TODO replace the if/else block below with the line above, and test it!
							if (createPathAsNeeded == typeof(OBJ_TYPE)) {
								list.Add(nextObject = new OBJ_TYPE());
							} else if (createPathAsNeeded == typeof(LIST_TYPE)) {
								list.Add(nextObject = new LIST_TYPE());
							} else {
								result = TraversalResult.unacceptablePathCreationVariable;
							}
						} else {
							nextObject = list[(int)index];
						}
					} else {
						result = TraversalResult.arrayIndexOOB;
					}
				} else {
					result = TraversalResult.couldNotBeParsedAsInteger;
				}
			} else if (objType == typeof(OBJ_TYPE)) {
				OBJ_TYPE table = obj as OBJ_TYPE;
				string str;
				if (TryParseString(door, out str)) {
					if (!table.TryGetValue(str, out nextObject)) {
						if (createPathAsNeeded != null) {
							//table[str] = nextObject = Data.CreateNew (createPathAsNeeded);
							// TODO replace the if/else block below with the line above, and test it!
							if (createPathAsNeeded == typeof(OBJ_TYPE)) {
								table[str] = nextObject = new OBJ_TYPE();
							} else if (createPathAsNeeded == typeof(LIST_TYPE)) {
								table[str] = nextObject = new LIST_TYPE();
							} else {
								result = TraversalResult.unacceptablePathCreationVariable;
							}
						} else {
							result = TraversalResult.couldNotFindMember;
						}
					}
				} else {
					result = TraversalResult.expectedStringNameOfMember;
				}
			} else if (objType.IsArray) {
				System.Array a = obj as System.Array;
				long index;
				if(Data.TryParseLong(door, out index)) {
					if(index < a.Length) {
						nextObject = a.GetValue(index);
					} else {
						result = TraversalResult.arrayIndexOOB;
					}
				} else {
					result = TraversalResult.couldNotBeParsedAsInteger;
				}
			} else {
				if(Data.IsStringType(door.GetType())) {
					string memberName = door.ToString();
					MemberInfo[] members = objType.GetMember(memberName);
					MemberInfo found = null;
					FieldInfo fi = null;
					PropertyInfo pi = null;
					MethodInfo mi = null;
					for(int i = 0; i < members.Length; ++i) {
						if(members[i] is FieldInfo) {			fi = (found = members[i]) as FieldInfo;		break;
						} else if(members[i] is PropertyInfo) {	pi = (found = members[i]) as PropertyInfo;	break;
						} else if(members[i] is MethodInfo)   {	mi = (found = members[i]) as MethodInfo;	break;
						}
					}
					if(found == null) {
						string sresult = "unable to find public member \"" + door.ToString()+"\" in " + obj;
						Debug.LogError (sresult);
						Debug.LogError (sresult + "\n"+Serializer.Stringify(obj));
						result = TraversalResult.couldNotFindMember;
					} else {
						if(fi != null) {
							nextObject = fi.GetValue(obj);
						} else if(pi != null) {
							nextObject = pi.GetValue(obj, null);
						} else if(mi != null) {
							// check for overloaded methods
							MethodInfo[] methods = objType.GetMethods();
							List<MethodInfo> overloadedMethods = new List<MethodInfo>();
							for(int i = 0; i < methods.Length; ++i) {
								if(methods[i].Name == memberName) {
									overloadedMethods.Add (methods[i]);
								}
							}
							if(overloadedMethods.Count > 1)
								nextObject = overloadedMethods.ToArray();
							else
								nextObject = mi;
						} else {
							//result = "could not traverse through <"+found.GetType()+">";
							result = TraversalResult.memberTypeNotTraversable;
						}
						if(result == TraversalResult.found && nextObject == null && createPathAsNeeded != null) {
							Type memberType = null;
							if(fi != null) {
								memberType = fi.FieldType;
								nextObject = CreateNew(memberType);
								fi.SetValue(obj, nextObject);
							} else if(pi != null) {
								memberType = pi.PropertyType;
								nextObject = CreateNew(memberType);
								pi.SetValue(obj, nextObject, null);
							}
							if(nextObject == null) {
								//esult = "could not create a new object of type <" + memberType + "> for member \"" +memberName+"\"";
								result = TraversalResult.unableToCreateNewMember;
							}
						}
						//Debug.Log (found+" SET TO "+nextObject);
					}
				} else if(door is Expression) {
					// Debug.Log("advance through "+door+" on "+obj);
					Expression expr = door as Expression;
					FileParseResults results = new FileParseResults(null,expr);
					nextObject = expr.Resolve(obj, results, typeof(object));
					if(results.Count > 0) Debug.Log(results);
					//Debug.Log (nextObject);
					//throw new System.Exception(door.ToString());
				} else if(door is System.Reflection.MemberInfo) {
					throw new System.Exception(door.ToString());
				} else if(door is System.Reflection.PropertyInfo) {
					throw new System.Exception(door.ToString());
				} else if(door is System.Reflection.MethodInfo) {
					throw new System.Exception(door.ToString());
				} else {
					string sresult = "unable to traverse " + obj + " with \"" + door.ToString()+"\" of type <"+door.GetType()+">";
					Debug.LogWarning(sresult);
					result = TraversalResult.expectedStringNameOfMember;
				}
			}
			return result;
		}
		/// <returns>why this specific traversal failed, or the success code</returns>
		public static bool TryDeReferenceGet(object objectModel, string propertyName, out object found) {
			return TryDeReferenceGet (objectModel, new object[]{ propertyName }, out found);
		}

		/// <returns>true if entire traversal worked correctly</returns>
		public static bool TryDeReferenceGet(object objectModel, IList pathExpanded, out object found) {
			TraversalResult errorMessage = TryDeReferenceGetWork(objectModel, pathExpanded, out found);
			return errorMessage == TraversalResult.found;
		}
		public static TraversalResult TryDeReferenceGetWork(object objectModel, IList pathExpanded, out object found) {
			if(objectModel == null) {
				throw new System.Exception("cannot de-reference without a scope");
			}
			found = objectModel;
			int cursorIndex = 0;
			TraversalResult errorMessage = TraversalResult.found;
			while (errorMessage == TraversalResult.found && cursorIndex < pathExpanded.Count) {
				if(found == null) {
					return TraversalResult.failedMidTraversal;
				}
				//print(cursorIndex+" door: "+pathExpanded[cursorIndex]);
				errorMessage = AdvanceThroughObject(found, pathExpanded[cursorIndex], out found, null);
				//if (errorMessage != null)
				//	print(errorMessage);
				cursorIndex++;
			}
			return errorMessage;
		}

		public static bool TryDeReferenceSetAmbiguous(object objectModel, object path, object value) {
			if(path is string) return TryDeReferenceSet(objectModel, new object[]{path}, value);
			if(path is Expression) return TryDeReferenceSet(objectModel, path as Expression, value);
			if(path is IList) return TryDeReferenceSet(objectModel, path as IList, value);
			throw new System.Exception ("path of variable being assigned must be a string, OM.Expression, or IList of strings/fields");
		}

		public static bool TryDeReferenceSet(object obj, string propertyName, object value) {
			return TryDeReferenceSet (obj, new LIST_TYPE (new object[]{ propertyName }), value);
		}

		public static bool TryDeReferenceSet(object objectModel, Expression path, object value) {
			if (objectModel == null)
				return false;
			return TryDeReferenceSet (objectModel, path.GetExpressionList(), value);
		}

		public static bool TryDeReferenceSet(object objectModel, IList pathExpanded, object value) {
			int cursorIndex = 0;
			TraversalResult errorMessage = TraversalResult.found;
			object cursor = objectModel;
			long result;
			while (errorMessage == TraversalResult.found && cursorIndex < pathExpanded.Count - 1) {
				bool nextIsInt = Data.TryParseLong(pathExpanded[cursorIndex+1], out result);
				//Debug.Log("next is " + pathExpanded[cursorIndex + 1] + ", " + nextIsInt);
				errorMessage = AdvanceThroughObject(cursor, pathExpanded[cursorIndex], out cursor,
					(!nextIsInt)?typeof(OBJ_TYPE):typeof(LIST_TYPE));
				cursorIndex++;
			}
			if (errorMessage != TraversalResult.found) {
				string s = "";
				for(int i = 0; i < pathExpanded.Count; ++i) {
					if(i > 0) s += ", ";
					s += pathExpanded[i];
				}
				UnityEngine.Debug.Log("could not set \"" + s + "\" to \""+value+"\": "+errorMessage);
				return false;
			}
			if (pathExpanded.Count == 0) {
				UnityEngine.Debug.Log("de-referencing no path? ...");
				return false;
			}
			object lastDoor = pathExpanded[pathExpanded.Count - 1];
			Type ctype = cursor.GetType ();
			if (ctype == typeof(OBJ_TYPE)) {
				OBJ_TYPE dict = cursor as OBJ_TYPE;
				string key;
				if (!Data.TryParseString(lastDoor, out key))
				{
					Debug.Log("no table element " + lastDoor);
					return false;
				}
				dict[key] = value;
			} else if (cursor is IList) {
				IList list = cursor as IList;
				long key;
				if (!Data.TryParseLong(lastDoor, out key)) {
					Debug.Log("no list element " + lastDoor);
					return false;
				}
				if (key == list.Count) {
					list.Add(value);
				} else {
					list[(int)key] = value;
				}
			// } else if( ctype.IsArray ) {
			// 	System.Array a = cursor as System.Array;
			// 	long index;
			// 	if(Data.TryParseLong(lastDoor, out index)) {
			// 		if(index < a.Length) {
			// 			a.SetValue(value, index);
			// 		} else {
			// 			errorMessage = TraversalResult.arrayIndexOOB;
			// 		}
			// 	} else {
			// 		errorMessage = TraversalResult.couldNotBeParsedAsInteger;
			// 	}
			} else {
				if(Data.IsStringType(lastDoor.GetType())) {
					string memberName = lastDoor.ToString();
					MemberInfo[] members = ctype.GetMember(memberName);
					MemberInfo found = null;
					FieldInfo fi = null;
					PropertyInfo pi = null;
					Type mtype = null;
					for(int i = 0; i < members.Length; ++i) {
						if(members[i] is FieldInfo) {
							fi = (found = members[i]) as FieldInfo;		mtype = fi.FieldType;		break;
						} else if(members[i] is PropertyInfo) {
							pi = (found = members[i]) as PropertyInfo;	mtype = pi.PropertyType;	break;
						}
					}
					if(found == null) {
						//errorMessage = "unable to find member \"" + lastDoor.ToString()+"\" in " + cursor;
						errorMessage = TraversalResult.couldNotFindMember;
						Debug.LogError("unable to find member \"" + lastDoor.ToString()+"\" in " + cursor);
					}
					if(mtype == typeof(float)) {
						double d; Data.TryParseDouble(value, out d); value = (float)d;
					} else if(mtype == typeof(double)) {
						double d; Data.TryParseDouble(value, out d); value = d;
					} else if(mtype == typeof(long)) {
						long d; Data.TryParseLong(value, out d); value = d;
					} else if(mtype == typeof(int)) {
						long d; Data.TryParseLong(value, out d); value = (int)d;
					} else if(Data.IsStringType(mtype)) {
						value = value.ToString();
					}
					if (value.GetType () == typeof(Value)) {
						value = ((Value)value).GetRawObject ();
					}
					if(fi != null) {
						fi.SetValue(cursor, value);
					} else if(pi != null) {
						pi.SetValue(cursor, value, null);
					} else {
						//errorMessage = "could not traverse through member of type <"+found.GetType()+">";
						errorMessage = TraversalResult.memberTypeNotTraversable;
					}
				} else {
					//errorMessage = "unable to traverse " + ctype + " with \"" + lastDoor.ToString()+"\"";
					errorMessage = TraversalResult.expectedStringNameOfMember;
				}
			}
			if (errorMessage != TraversalResult.found)
				Debug.Log(errorMessage);
			return true;
		}
		
		public static string ParseValue(object value, Type typeToParseInto, out object parsedValue, object contextForReferences) {
			if(value == null) {
				if(Data.IsNumericType(typeToParseInto)) {
					parsedValue = Convert.ChangeType(0, typeToParseInto); return null;
				}
			}
			string errorMessage = null;
			parsedValue = null;
			Type vt = value.GetType();
			if(vt == typeof(Expression) && typeToParseInto != typeof(Expression)) {
				if(typeToParseInto.IsArray && typeToParseInto.GetElementType() == typeof(Expression)) {
					throw new System.Exception("expected an array of Expressions, found only one.");
				}
				//Debug.Log("resolving \""+value+"\" as <"+typeToParseInto+"> in context of \""+contextForReferences+"\"");
				FileParseResults output = new FileParseResults(null, value);
				while(vt == typeof(Expression)) {
					Expression exp = value as Expression;
					value = exp.Resolve(contextForReferences, output, typeof(string));
					if(output.Count > 0)
						return output[0].ToString();
					vt = (value != null)?value.GetType():null;
				}
			}
			if (typeToParseInto == null) {
				errorMessage = "null type was passed";
			} else if (typeToParseInto == typeof(object) || (value != null && typeToParseInto == value.GetType())) {
				parsedValue = value;
			} else if (typeToParseInto == typeof(int)) {
				long number;
				if (TryParseLong(value, out number)) {
					parsedValue = (Int32)number;
				} else {
					errorMessage = "\"" + value + "\" won't convert to a Int32!";
				}
			} else if (typeToParseInto == typeof(float)) {
				double number;
				if (TryParseDouble(value, out number)) {
					parsedValue = (float)number;
				} else {
					string str = NormalizeString(value);
					if(str != null && str == "")
						parsedValue = (float)0;
					else
						errorMessage = "\"" + value + "\" won't convert to a float!";
				}
			} else if (typeToParseInto == typeof(double)) {
				double number;
				if (TryParseDouble(value, out number)) {
					parsedValue = number;
				} else {
					string str = NormalizeString(value);
					if(str != null && str == "")
						parsedValue = (double)0;
					else
						errorMessage = "\"" + value + "\" won't convert to a double!";
				}
			} else if (typeToParseInto == typeof(string)) {
				string str;
				if (TryParseString(value, out str)) { parsedValue = str; }
				else { errorMessage = "\"" + value + "\", wont convert to a string! Did you mean to actually put quotes around \""+Serializer.Stringify(value)+"\"?"; }
			} else if (typeToParseInto == typeof(Color)) {
				Color c; // string in decimal or hex, or raw integer
				if (TryParseColor(value, out c)) { parsedValue = c; }
				else { errorMessage = "\"" + value + "\", needs to be a hex value!"; }
			} else if (typeToParseInto == typeof(Vector2) && value is IList) {
				Vector2 v; // array of numbers
				if (TryParseVector2(value, out v)) { parsedValue = v; }
				else { errorMessage = "\"" + value + "\" can't convert to Vector2 (list of numbers)!"; }
			} else if (typeToParseInto == typeof(Vector3) && value is IList) {
				Vector3 v; // array of numbers
				if (TryParseVector3(value, out v)) { parsedValue = v; }
				else { errorMessage = "\"" + value + "\" can't onvert to Vector3 (list of numbers)!"; }
			} else if (typeToParseInto == typeof(Quaternion) && value is IList) {
				Quaternion v; // array of numbers
				if (TryParseQuaternion(value, out v)) { parsedValue = v; }
				else { errorMessage = "\"" + value + "\" can't onvert to Quaternion (list of numbers)!"; }
            } else if (typeToParseInto == typeof(System.DateTime) && Data.IsStringType(value.GetType())) {
				DateTime v;
				if (TryParseDateTime (NormalizeString(value), out v)) { parsedValue = v; }
				else { errorMessage = "\"" + value + "\" can't onvert to DateTime!"; }
			} else if(typeToParseInto.IsEnum) {
				try{
					parsedValue = Enum.Parse(typeToParseInto, NormalizeString(value));
				}catch(Exception e){
					string[] names = Enum.GetNames(typeToParseInto);
					StringBuilder sb = new StringBuilder();
					for(int i = 0; i < names.Length; ++i) {
						if(i > 0)sb.Append (", ");
						sb.Append(names[i]);
					}
					Debug.Log (e);
					errorMessage = ("type <"+typeToParseInto+"> cannot be \""+value+"\", valid values include:\n"+sb);
				}
			} else if(typeToParseInto.IsArray && value is IList) {
				// Debug.Log (":"+typeToParseInto);
				// Debug.Log ("basetype:"+typeToParseInto.BaseType);
				// Debug.Log ("declaringtype"+typeToParseInto.DeclaringType);
				// Debug.Log ("arrayrank:"+typeToParseInto.GetArrayRank());
				// Debug.Log ("elementtype:"+typeToParseInto.GetElementType());
				System.Type elementType = typeToParseInto.GetElementType();
				if(value == null) {
					throw new System.Exception("need to parse into array, but have null data.");
				}
				IList list = value as IList;
				System.Array a = System.Array.CreateInstance(elementType, list.Count);// = new Array.
				// Debug.Log("parsing ("+list+") to <"+elementType+"> in context \""+contextForReferences+"\"");
				for(int i = 0; i < a.Length; ++i) {
					object parsed;
					ParseValue(list[i], elementType, out parsed, contextForReferences);
					if(parsed == null) {
						throw new System.Exception("ParseValue cannot convert ("+list[i]+")<"+list[i].GetType()+"> to <"+elementType+">");
					}
					// Debug.Log ("a["+i+"] = "+parsed.ToString());
					a.SetValue(parsed, i);
				}
				parsedValue = a;
			} else {
				if(vt == typeof(OBJ_TYPE)) {
					// Debug.Log("creating <"+typeToParseInto+"> "+OM.Serialize(value));
					parsedValue = CreateNew(typeToParseInto);
					if(parsedValue == null) { // if the expected type could not be created (because it is abstract)
					// <:Item+attack
					// >:{
					// 	target:["$[_target]"]
					// 	damage:[{damage:0,type:strength},{damage:1,type:pierce}]
					// }
					// 	print (OM.Serialize(value));
						OBJ_TYPE typelayer = value as OBJ_TYPE;
						string typename = Data.GetPropertyString(typelayer, "<"); // find out the true type name, and create one of those
						// if(typename == null) { // if there is no "<" parameter
						// 	// look for a parameter whose name starts with "<" and ends with ">"
						// 	// this parameter holds the typename
						// 	// and it points at the data to serialize from
						// }
						// Debug.Log("creating "+typename);
						if(typename == null) {
							throw new System.Exception("<"+typeToParseInto+"> isn't constructable for \""+Serializer.Stringify(value)+"\", it may need to be explicit, wrapped by \"<:"+typeToParseInto+",>:{...}\"");
						}
						// Debug.Log("need <"+typename+"> "+OM.Serialize(value));
						Type subtype = Type.GetType(typename);
						if(subtype != null){
							parsedValue = CreateNew(subtype);
							if(parsedValue != null) {
								value = Data.GetPropertyObject(typelayer, ">");
								if(value == null) {
									throw new System.Exception("explicit script for type <"+subtype+"> needs a \">\" property in \""+Serializer.Stringify(value)+"\"");
								}
							}
						}
						if(parsedValue == null) {
							errorMessage = "could not create type \""+typename+"\"";
						}
					}
					if(parsedValue != null) {
						errorMessage = SetObjectFromOmObject(ref parsedValue, value as OBJ_TYPE, JSONFieldSearchBehavior.failfast, contextForReferences);
					}
				} else if (Data.IsStringType(vt) && NormalizeString(value).StartsWith("R$")) {
					errorMessage = SetObjectFromOmResourceReference(ref parsedValue, typeToParseInto, 
						NormalizeString(value), JSONFieldSearchBehavior.failfast, contextForReferences);
				} else {
					Type valueType = value.GetType();
					bool thisTypeIsFine = valueType == typeToParseInto;
					// if the type isn't good, try parsing it into a valid interface or base type
					if(!thisTypeIsFine) {
						Type t = valueType;
						while(t != null) {
							Type[] interfaces = t.GetInterfaces();
							if(interfaces != null) {
								for(int i = 0; i < interfaces.Length; ++i) {
									if(interfaces[i] == typeToParseInto) {
										//Debug.Log("it appears that <" + value.GetType() + "> interfaces as a <" + typeToParseInto + ">!");
										thisTypeIsFine = true;
									}
								}
								if(thisTypeIsFine) break;
							}
							t = t.BaseType;
							if(t == typeToParseInto) {
								thisTypeIsFine = true;
								//Debug.Log("it appears that <" + value.GetType() + "> is a <" + typeToParseInto + ">!");
								break;
							}
						}
					}
					// if the base-type or interface isn't good, check if it's a generic list that can be recast
					if(!thisTypeIsFine) {
						if(valueType.IsGenericType && typeToParseInto.IsGenericType
						  && valueType == typeof(List<object>)
						  && typeToParseInto.GetGenericTypeDefinition() == typeof(List<>)) {
							// check if all of the elements in value are able to be assigned as typeToParseInto
							List<object> list = value as List<object>;
							Type targetType = typeToParseInto.GetGenericArguments()[0];
							for(int i = 0; i < list.Count; i++){
								if(!targetType.IsAssignableFrom(list[i].GetType())){
									Type elementT = list[i].GetType();
									bool convertedCorrectly = false;
									// if elementT is a dictionary
									if(elementT == typeof(OBJ_TYPE)) {
										// try to populate a new object of the expected type with this dictionary data
										object o = CreateNew(targetType);
										string error = SetObjectFromOm(ref o, list[i], JSONFieldSearchBehavior.failfast, null);
										if(error != null){
											Debug.LogError(error);
										} else {
											convertedCorrectly = true;
										}
										list[i] = o;
									}
									if(!convertedCorrectly) {
										throw new System.Exception(elementT + " at index " + i + " can't be assigned as " + targetType);
									}
								}
							}
							System.Collections.IList targetList = CreateNew(typeToParseInto) as System.Collections.IList;
							for(int i = 0; i < list.Count; i++) {
								targetList.Add(list[i]);
							}
							value = targetList;
							thisTypeIsFine = true;
						}
					}
					if(!thisTypeIsFine) {
						errorMessage = "(" + value + ")<" + valueType + "> can't be parsed into <" + typeToParseInto + ">! "+contextForReferences+"\n"+Serializer.Stringify(value);
						throw new System.Exception(errorMessage);
					}
					parsedValue = value;
				}
			}
			if(errorMessage != null) { // TODO pull this crazy bit of fail-fast out, so the results list can work correctly again
				throw new System.Exception(errorMessage);
			}
			return errorMessage;
		}

		public enum JSONFieldSearchBehavior {failfast, ignoremissing, ignoremissing_withwarning, startswith}

		class MemberSearch {
			public System.Reflection.FieldInfo[] fields = null;
			public System.Reflection.PropertyInfo[] props = null;
			public System.Reflection.MethodInfo[] methods = null;
			Type objectType;
			private void InternalChecks(Type t) {
				if(this.objectType != t) {	this.objectType = t; fields = null; props = null; methods = null;	}
			}
			System.Reflection.FieldInfo[] GetFields(Type t) {
				InternalChecks (t);
				return (fields == null)?(fields = t.GetFields ()):fields;
			}
			System.Reflection.PropertyInfo[] GetProps(Type t) {
				InternalChecks (t);
				return (props == null)?(props = t.GetProperties ()):props;
			}
			System.Reflection.MethodInfo[] GetMethods(Type t) {
				InternalChecks (t);
				return (methods == null)?(methods = t.GetMethods ()):methods;
			}
			/// <returns>the field fname owned by object of type t</returns>
			/// <param name="t">Type of object obj.</param>
			/// <param name="fname">Fname.</param>
			/// <param name="behavior">Behavior.</param>
			public System.Reflection.MemberInfo GetMemberReferencedBy(System.Type t, ref string fname, JSONFieldSearchBehavior behavior) {
				if(t == null) {
					throw new System.Exception("null types have no members...");
				}
				InternalChecks (t);
				System.Reflection.MemberInfo member = t.GetField(fname);
				if(member == null) member = t.GetProperty(fname);
				JSONFieldSearchBehavior thisbehavior = behavior;
				if(fname.EndsWith("*")) {
					fname = fname.Substring(0, fname.Length-1);
					thisbehavior = JSONFieldSearchBehavior.startswith;
				}
				if(member == null) {
					switch(thisbehavior) {
					case JSONFieldSearchBehavior.ignoremissing_withwarning://	
					case JSONFieldSearchBehavior.ignoremissing://	continue;
						Debug.Log(t+" missing field \""+fname+"\"");
						return null;
					case JSONFieldSearchBehavior.startswith:
					{
						GetFields (t);
						for(int i = 0; i < fields.Length; ++i) {
							if(fields[i].Name.StartsWith(fname)) {
								member = fields[i];	break;
							}
						}
						if(member == null) {	GetProps(t);
							for(int i = 0; i < props.Length; ++i) {
								if(props[i].Name.StartsWith(fname)) {
									member = props[i];	break;
								}
							}
						}
						if(member == null) {	GetMethods(t);
							for(int i = 0; i < methods.Length; ++i) {
								if(methods[i].Name.StartsWith(fname)) {
									member = methods[i];	break;
								}
							}
						}
						if(member != null) {
							fname = member.Name;
						}
						//print ("\""+datakey+"\" could be \""+f.Name+"\"");
					}
						break;
					}
				}
				return member;
			}
			/// for debug output
			public string ListValid() {
				StringBuilder sb = new StringBuilder();
				if(objectType == null) {
					sb.Append("<no members>");
				} else {
					GetFields(objectType);
					for(int i = 0; i < fields.Length; ++i) {
						if(i > 0) sb.Append(", ");
						sb.Append("\""+fields[i].Name+"\"");
					}
					GetProps(objectType);
					for(int i = 0; i < props.Length; ++i) {
						if(i > 0) sb.Append(", ");
						sb.Append("\""+props[i].Name+"\"");
					}
					GetMethods(objectType);
					for(int i = 0; i < methods.Length; ++i) {
						if(i > 0) sb.Append(", ");
						sb.Append("\""+methods[i].Name+" (");//"\"");
						Type[] args = methods[i].GetGenericArguments();
						for(int a = 0; a < args.Length; ++i) {
							if(a > 0)sb.Append(", ");
							sb.Append("<"+args[a]+">");
						}
						sb.Append(")\"");
					}
				}
				return sb.ToString ();
			}
		}

		public static string AssignObjectMember(object obj, MemberInfo member, object value, JSONFieldSearchBehavior thisbehavior, object contextForReferences) {
			System.Type memberType = null;
			if(member is System.Reflection.FieldInfo) {
				memberType = (member as System.Reflection.FieldInfo).FieldType;
			} else if(member is System.Reflection.MemberInfo) {
				memberType = (member as System.Reflection.PropertyInfo).PropertyType;
			} else if(member is System.Reflection.MethodInfo) {
				memberType = (member as System.Reflection.MethodInfo).ReturnType;
			}
			if(memberType.IsEnum) {
				string vstring = NormalizeString(value);
				//Debug.Log ("assigning "+member+" = "+value);
				bool enumStartsWith = vstring.EndsWith("*");
				if(thisbehavior == JSONFieldSearchBehavior.startswith || enumStartsWith) {
					if(enumStartsWith) {
						vstring = vstring.Substring(0, vstring.Length-1);
					}
					string[] names = Enum.GetNames(memberType);
					for(int i = 0; i < names.Length; ++i) {
						if(names[i].StartsWith(vstring)) {
							value = names[i];
							break;
						}
					}
				}
			}
			// convert the current field into what it needs to be
			object parsedValue;
			string errorMessage = Data.ParseValue(value, memberType, out parsedValue, contextForReferences);
			if(errorMessage == null) {
				if(member is System.Reflection.FieldInfo) {
					(member as System.Reflection.FieldInfo).SetValue(obj, parsedValue);
				} else if(member is System.Reflection.PropertyInfo) {
					(member as System.Reflection.PropertyInfo).SetValue(obj, parsedValue, null);
				} else {
					errorMessage = "unable to assign \""+member+"\" <"+member+">";
					throw new System.Exception(errorMessage);
				}
			}
			return errorMessage;
		}

		public static string SetObjectFromOmResourceReference(ref object obj, Type typeToParseInto, string srcName, JSONFieldSearchBehavior behavior, object contextForReferences) {
			if (TryParseAsset (srcName, out obj)) {
				if (obj is TextAsset) {
					FileParseResults results = new FileParseResults (srcName, (obj as TextAsset).text);
					object fileReferencedValue = Parser.DeserializeJSON (srcName, (obj as TextAsset).text, results);
					if (results.Count > 0)
						Debug.Log (results);
					// TODO prevent recursion with circular file references...
					// TODO allow the system to write directly into obj (with SetObjectFromOm) rather than replacing it (with ParveValue), to reduce memory allocation
					// if (obj != null) {
					// 	SetObjectFromOm(ref obj, fileReferencedValue, behavior, contextForReferences);
					// } else if (typeToParseInto != null) {
						ParseValue (fileReferencedValue, typeToParseInto, out obj, contextForReferences);
					// }
				} else {
					throw new System.Exception ("could not deal with "+srcName+" of type "+obj.GetType());
				}
			} else {
				throw new System.Exception ("could not find "+srcName);
			}
			return null;
		}

		public static string SetObjectFromOm(ref object obj, object srcData, JSONFieldSearchBehavior behavior, object contextForReferences) {
			Type t = srcData.GetType ();
			if (t == typeof(OBJ_TYPE)) {
				return SetObjectFromOmObject (ref obj, srcData as OBJ_TYPE, behavior, contextForReferences);
			} else if (t == typeof(LIST_TYPE)) {
				return SetObjectFromOmArray (ref obj, srcData as LIST_TYPE, behavior, contextForReferences);
			} else if (Data.IsStringType(t) && NormalizeString(srcData).StartsWith (RESCALL)) {
				return SetObjectFromOmResourceReference (ref obj, obj.GetType(), NormalizeString(srcData), 
					behavior, contextForReferences);
			}
			// TODO if (t == typeof(OM.Expression))?
			throw new System.Exception ("Don't know how to parse a "+t+"("+srcData.ToString()+") into a "+obj.GetType());
		}

		public static string SetObjectFromOmArray(ref object obj, LIST_TYPE srcData, JSONFieldSearchBehavior behavior, object contextForReferences) {
			LIST_TYPE arr = srcData as LIST_TYPE;
			Type listType = obj.GetType ();
			string result = null;
			if (listType.IsArray) {
				Type etype = listType.GetElementType ();
				System.Array a = Activator.CreateInstance (listType, new object[]{ arr.Count }) as System.Array;
				obj = a;
				for (int i = 0; i < a.Length; ++i) {
					object output = Data.CreateNew (etype);
					result = Data.SetObjectFromOm (ref output, arr [i], Data.JSONFieldSearchBehavior.startswith, null);
					if (result != null && result.Length > 0) { break; }
					if (output.GetType () != etype) {
						throw new System.Exception ("Array elements do not match for " + listType + ". element " + i + "is of type " + output.GetType ());
					}
					a.SetValue (output, i);
				}
			} else if (listType.IsGenericType) {
				Type genericType = listType.GetGenericTypeDefinition ();
				Type glistType = typeof(List<>);
				Type[] gtypes = listType.GetGenericArguments();
				if (genericType != listType && gtypes.Length != 1) {
					throw new SystemException ("Don't know how to create a "+listType+" (non List<>) generic with "+gtypes.Length+" generic parameters");
				}
				Type resultType = glistType.MakeGenericType (gtypes);
				object list = Activator.CreateInstance (resultType);
				obj = list;
				Type etype = gtypes [0];
				for (int i = 0; i < srcData.Count; ++i) {
					object output = Data.CreateNew (etype);
					result = Data.SetObjectFromOm (ref output, arr [i], Data.JSONFieldSearchBehavior.startswith, null);
					if (result != null && result.Length > 0) { break; }
					if (output.GetType () != etype) {
						throw new System.Exception ("elements do not match for " + listType + ". element " + i + "is of type " + output.GetType ());
					}
					list.GetType().GetMethod("Add").Invoke(list, new[] {output});
				}
				obj = list;
			}
			return result;
		}

		/// <summary>
		/// Sets the object from compiled Object Model.
		/// </summary>
		/// <param name="obj">Object to assign to</param>
		/// <param name="srcData">script to use to do the assigning</param>
		/// <param name="behavior">Behavior.</param>
		/// TODO when this fails, find some way to tell caller what Coord in the script the failure came from... NONTRIVIAL
		public static string SetObjectFromOmObject(ref object obj, OBJ_TYPE srcData, JSONFieldSearchBehavior behavior, object contextForReferences
		//, FileParseResults presults
		) {
			string errorMessage = null;
			if(obj == null) {
				errorMessage = "primary object (obj) was passed in null! Do error checking before this method.";
				throw new System.Exception(errorMessage);
			}
			if (srcData == null) {
				errorMessage = "source data (data) was passed in null! Do error checking before this method.";
				throw new System.Exception(errorMessage);
			}
			MemberSearch ms = new MemberSearch();
			System.Type objectType = obj.GetType ();
			JSONFieldSearchBehavior thisbehavior = behavior;
			// for each key
			foreach (object datakey in srcData.Keys) {
				System.Reflection.MemberInfo member = null;
				string fname = null;
				object value = srcData[datakey];
				// if assigning regular member
				if(Data.IsStringType(datakey.GetType())) {
					fname = datakey.ToString();
					member = ms.GetMemberReferencedBy(objectType, ref fname, behavior);
					// Debug.LogWarning(objectType+"."+datakey+" is a <"+member+">");//return"";
                    // if setting a dictionary object
                    if (member == null) {
						if(objectType == typeof(OBJ_TYPE)) {
							OBJ_TYPE dict = obj as OBJ_TYPE;
							dict[fname] = value;
						} else
						// or a special nested-dictionary type
						if(objectType == typeof(Dictionary<string, OBJ_TYPE >)) {
							Dictionary<string,OBJ_TYPE> dict = obj as Dictionary<string,OBJ_TYPE>;
							// Debug.Log ("should be dict<s,o>\n"+vt);
							if(value.GetType() != typeof(OBJ_TYPE)) {
								throw new System.Exception("double dictionary needs sub-dictionary");
							}
							dict[fname] = value as OBJ_TYPE;
						} else {
							errorMessage = obj.GetType()+" does not have public member \""+fname+"\" in <"+objectType+">\nvalid members include:"+ms.ListValid()+"\nerror originated from script:\n"+Serializer.Stringify(srcData);
							break;
						}
					} else { // if setting an actual member
						if(value != null) {
							//Debug.LogWarning("assigning "+objectType+"."+datakey+" = "+value);//return"";
							// figure out what the field needs to be
							errorMessage = AssignObjectMember(obj, member, value, thisbehavior, contextForReferences);
						} else {
						// if setting the member to null
							if(member is System.Reflection.FieldInfo) {
								(member as System.Reflection.FieldInfo).SetValue(obj, null);
							} else if(member is System.Reflection.PropertyInfo) {
								(member as System.Reflection.PropertyInfo).SetValue(obj, null, null);
							} else {
								errorMessage = "unable to deal with \""+datakey+"\" <"+member+">";
								throw new System.Exception(errorMessage);
							}
						}
					}
				} else 
				// if assigning object at the end of an expression (probably an accessor like GetComponent("ComponentTypeName")
				if(datakey is Expression) {
					Expression expr = datakey as Expression;
					FileParseResults results = new FileParseResults(null,datakey);
					//Debug.Log ("resolving "+expr+" in context \""+contextForReferences+"\"");
					object existingObjectValue = expr.Resolve(contextForReferences, results, typeof(object));
					//Debug.Log("~~~~~~~~~~~~~~going to set "+existingObjectValue+" to "+OM.Serialize(value));
					OBJ_TYPE valueData = value as OBJ_TYPE;
					if(valueData == null) {
						errorMessage = "cannot handle \""+value+"\" as data source for \""+fname+"\"\n"+Serializer.Stringify(srcData);
						throw new System.Exception(errorMessage);
					}
					string errormsg = SetObjectFromOmObject(ref existingObjectValue, valueData, behavior, contextForReferences);//, presults);
					if(errormsg != null) {
						Debug.LogError(errormsg);
					}
				} else {
					throw new System.Exception("need to handle <"+datakey+"> correctly");
				}
			}
			return errorMessage;
		}
		/// <returns>A new object of the given type, using a default constructor if available</returns>
		/// <param name="t">T.</param>
		public static object CreateNew(Type t) {
			if(t == null) return null;
			// create arrays as zero-length arrays. hopefully they will be resized/replaced soon enough.
			if (t.IsArray) { return Activator.CreateInstance (t, new object[]{ 0 }) as System.Array; }
			// for other objects, look for default constructors
			object o = null;
			System.Reflection.ConstructorInfo[] constructors = t.GetConstructors();
			System.Reflection.ConstructorInfo defaultConstructor = null;
			for(int i = 0; i < constructors.Length; ++i) {
				if(constructors[i].GetParameters().Length == 0) {
					defaultConstructor = constructors[i];
					break;
				}
			}
			if(defaultConstructor != null) {
				o = defaultConstructor.Invoke(new object[]{});
			} else if(t.IsValueType) {
				o = Activator.CreateInstance(t);
			} else {
				for(int i = 0; i < constructors.Length; ++i) {
					Debug.Log (constructors[i].Name+" "+constructors[i].GetParameters().Length);
				}
				Debug.Log("could not create a new <"+t+">"+(t.IsValueType?" (value type)":"."));
			}
			return o;
		}
		/// <returns>compiled Document Object Model (made of Dictionary, List, and primitive objects, ready for string serialization)</returns>
		/// <param name="obj">Object.</param>
		/// <param name="hideZeroNull">If set to <c>true</c> hide zero/null values.</param>
		/// <param name="compressNames">If set to <c>true</c> compress names.</param>
		/// <param name="ignoreFieldsPrefixedWith">if not null, ignores fields with these prefixes.</param>
		/// <param name="objectHierarchy">keeps track of nested objects, to cause errors in the case of recursion</param> 
		public static object SerializeToOm(object obj, bool hideZeroNull, bool compressNames, string[] ignoreFieldsPrefixedWith) {
			// return SerializeToOm (obj, hideZeroNull, compressNames, ignoreFieldsPrefixedWith, null);
			return SerializedToOm(obj, obj.GetType(), hideZeroNull, compressNames, ignoreFieldsPrefixedWith, null);
		}
		/// <returns>compiled Document Object Model (made of Dictionary, List, and primitive objects, ready for string serialization)</returns>
		/// <param name="obj">Object.</param>
		/// <param name="hideZeroNull">If set to <c>true</c> hide zero/null values.</param>
		/// <param name="compressNames">If set to <c>true</c> compress names.</param>
		/// <param name="ignoreFieldsPrefixedWith">if not null, ignores fields with these prefixes.</param>
		/// <param name="objectHierarchy">keeps track of nested objects, to cause errors in the case of recursion</param> 
		public static object SerializeToOm(object obj, bool hideZeroNull, bool compressNames, string[] ignoreFieldsPrefixedWith, LIST_TYPE objectHierarchy) {
			Type t = obj.GetType ();
			System.Reflection.FieldInfo[] fields = t.GetFields ();
			string[] fieldNames = null;
			OBJ_TYPE dict = new OBJ_TYPE ();
			for(int i = 0; i < fields.Length; ++i) { // TODO make a method called "serialize field"
				string fname = fields[i].Name;
				bool ignoreThisField = false;
				if(ignoreFieldsPrefixedWith != null) {
					for(int a = 0; a < ignoreFieldsPrefixedWith.Length; ++a) {
						if(fname.StartsWith(ignoreFieldsPrefixedWith[a])) {
							ignoreThisField = true;
							break;
						}
					}
				}
				// ignore constants
				if(fields[i].IsLiteral)
					ignoreThisField = true;
				if(ignoreThisField) continue;
				if(compressNames) {
					if(fieldNames == null) {
						fieldNames = new string[fields.Length];
						for(int a = 0; a < fields.Length; ++a) {
							fieldNames[a] = fields[a].Name; 
						}
					}
					int limit = Data.AmbiguousPrefixCheck(fname, fieldNames);
					if(limit+1 < fname.Length) {
						fname = fname.Substring(0, limit+1) + "*";
					}
				}
				object value = fields[i].GetValue(obj);
				if(hideZeroNull && Data.IsZeroOrNull(value)) {
					// Type ft = null;
					// if(fields[i] != null) ft = fields[i].FieldType;
					// Debug.Log("\""+fname+"\" <"+ft+">: ("+value+") <-- ignored");
				} else {
					Type ft = fields[i].FieldType;
					// Debug.Log ("in:  \""+fname+"\" <"+ft+">: ("+value+")");
					value = SerializedToOm(value, ft, hideZeroNull, compressNames, ignoreFieldsPrefixedWith, objectHierarchy);
					// Debug.Log ("out: \""+fname+"\" <"+ft+">: ("+value+")");
					dict[fname] = value;
				}
			}
			return dict;
		}
		/// <summary></summary>
		/// <returns>The compiled value (made of Dictionary&lt;object,object&rt;, List&lt;object&rt;, and basic types including OM.Expression)</returns>
		/// <param name="value">Value.</param>
		/// <param name="ft">what type is being parsed (not the type that will be returned)</param>
		static public object SerializedToOm(object objToCompile, Type ft, bool hideZeroNull, bool compressNames, string[] ignoreFieldsPrefixedWith, LIST_TYPE objectHierarchy) {
			if(Data.IsNativeType(ft)) {
				// no need to do anything special, these are natively recognized by the system
			} else if(ft == typeof(Vector2)) {
				Vector2 v2 = (Vector2)objToCompile;
				List<float> v2list = new List<float>(2);
				v2list.Add (v2.x);	v2list.Add (v2.y);
				objToCompile = v2list;
			} else if(ft == typeof(Vector3)){
				Vector3 v3 = (Vector3)objToCompile;
				List<float> v3list = new List<float>(3);
				v3list.Add (v3.x);	v3list.Add (v3.y);	v3list.Add (v3.z);
				objToCompile = v3list;
			} else if(ft == typeof(Color)) {
				Color color = (Color)objToCompile;
				Int32 num = (int)(color.r*255) | ((int)(color.g*255) << 8) | ((int)(color.g*255) << 16) | ((int)(color.a*255) << 24);
				objToCompile = "0x"+num.ToString("X8");
			} else if(ft.IsEnum) {
				string vstring = objToCompile.ToString();
				if(compressNames) {
					string[] names = Enum.GetNames(ft);
					int limit = Data.AmbiguousPrefixCheck(vstring, names);
					if(limit+1 < vstring.Length) {
						vstring = vstring.Substring(0, limit+1) + "*";
					}
				}
				objToCompile = vstring;
			} else if (objToCompile is IList) {
				// leave lists as is, the rest of the serialization code knows how to deal with ILists just fine.
				//IList inList = objToCompile as IList; 
				//LIST_TYPE outArr = Data.CreateListWithSize(inList.Count);
				//Type elementType = typeof(object);
				//if (ft.IsArray) { elementType = ft.GetElementType(); }
				//if (ft.IsGenericType) { elementType = ft.GetGenericArguments () [0]; }
				//for(int a = 0; a < inList.Count; ++a) {
				//	//outArr[a] = (SerializedToOm(inList[a], elementType, hideZeroNull, compressNames, ignoreFieldsPrefixedWith, objectHierarchy));
				//	outArr[a] = inList[a];
				//}
				//objToCompile = outArr;
			} else {
				if(objToCompile != null) {
					if(objectHierarchy != null && objectHierarchy.IndexOf(objToCompile) >= 0) {
						StringBuilder sb = new StringBuilder();
						for(int a = 0; a < objectHierarchy.Count; ++a) {
							sb.Append(objectHierarchy[a]+" -> ");
						}
						// TODO replace with reference via OM.Expression?
						throw new System.Exception("found recursion while parsing "+objectHierarchy[0]+"\n"+sb+" {"+objToCompile+"}");
					}
					if(objectHierarchy == null) {
						objectHierarchy = new LIST_TYPE();
					}
					objectHierarchy.Add(objToCompile);
					// Debug.Log("----------------------- serializing ("+value+")");
					objToCompile = SerializeToOm(objToCompile, hideZeroNull, compressNames, ignoreFieldsPrefixedWith, objectHierarchy);
					// Debug.Log("----------------------- done serializing ("+value+")");
					objectHierarchy.RemoveAt(objectHierarchy.Count-1);
				} else {
					Debug.Log("----------------------- ignoring null");
				}
			}
			return objToCompile;
		}
		/// <returns>how many letters collide with at least one other field name. this return+1 must be included to make the field name's prefix unique enough for zero-ambiguity</returns>
		/// <param name="fieldCheck">which field is being analyzed</param>
		/// <param name="fields">all of the fields.</param>
		private static int AmbiguousPrefixCheck(string fieldCheck, string[] fields) {
			string check;
			int len = fieldCheck.Length;
			int closestMatchLetterCount = 0, thisMatch;
			for(int i = 0; i < fields.Length; ++i) {
				check = fields[i];
				int minSize = Mathf.Min(len, check.Length);
				for(thisMatch = 0; thisMatch < minSize && fieldCheck[thisMatch] == check[thisMatch]; ++thisMatch);
				if(thisMatch == len && check.Length == len) continue; // ignore perfect matches
				if(thisMatch > closestMatchLetterCount) {
					closestMatchLetterCount = thisMatch;
				}
			}
			return closestMatchLetterCount;
		}
	}
}
