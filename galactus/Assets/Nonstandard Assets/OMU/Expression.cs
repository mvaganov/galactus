using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using LIST_TYPE = System.Collections.Generic.List<object>;

namespace OMU {
	/// logical expression, or even method call (via reflection). Does not use Reflection.Method/Field/Property so that it can be used with the scripted OM.Objects
	[System.Serializable]
	public class Expression {
		/// before processing, this is the raw expression from the paerser. after processing, this is a list of variable traversal to resolve.
		private LIST_TYPE expList;
		
		/// what kind of operation this is.
		public OperationType opType = OP_UNDEFINED;
		
		public Expression(){}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="OMU+Expression"/> class.<see cref="OMU+Expression+OPS"/>
		/// </summary>
		/// <param name="arrayOfExpressionComponents">Array of expression components.</param>
		/// <param name="op">Operation from the <see cref="OMU+Expression+OPS"/> array</param>
		public Expression(OperationType op, IList arrayOfExpressionComponents) {
			opType = op;
			expList = new LIST_TYPE();
			expList.Capacity = arrayOfExpressionComponents.Count;
			for(int i=0;i<arrayOfExpressionComponents.Count;++i){
				expList.Add(arrayOfExpressionComponents[i]);
			}
		}
		public Expression(OperationType op, params object[] expressionComponents) {
			opType = op;
			expList = new LIST_TYPE();
			expList.Capacity = expressionComponents.Length;
			for(int i=0;i<expressionComponents.Length;++i){
				expList.Add(expressionComponents[i]);
			}
		}
		
		public class OperationType {
			public string syntax;
			public string name;
			public Resolve resolve;
			
			public delegate object Resolve(LIST_TYPE expr, object scope, Type expectedType, FileParseResults output);
			public OperationType(string icon, string name, Resolve resolve){this.name=name;this.syntax=icon;this.resolve=resolve;}
			override public string ToString(){return ((syntax!=null)?(syntax+" "+name):name);}
		}
		
		public static OperationType OP_LINE_COMMENT = new OperationType("//","line comment",null);
		public static OperationType OP_BLOCK_COMMENT_BEGIN = new OperationType("/*","block comment begin",null);
		public static OperationType OP_BLOCK_COMMENT_END = new OperationType("*/","block comment end",null);
		public static OperationType OP_UNDEFINED = new OperationType(null,"undefined",null);
		public static OperationType OP_METHOD_CALL = new OperationType(null,"method call", OP_METHOD_CALL_DELEGATE);
		public static OperationType OP_SCRIPTED_VALUE = new OperationType(null,"scripted value", OP_RESOLVE_SCRIPTED_VALUE);
		public static OperationType OP_ASSIGN = new OperationType("=","assign",OPF_ASSIGN);
		public static OperationType[] OPS = {
			OP_ASSIGN,
			new OperationType("+","sum",OP_SUM),
			new OperationType("-","difference",OP_DIFFERENCE),
			new OperationType("*","product",OP_PRODUCT),
			new OperationType("/","quotient",OP_QUOTIENT),
			new OperationType("+=","compound add",OP_COMPOUND_ADD),
			new OperationType("-=","compound subtract",OP_COMPOUND_SUBTRACT),
			new OperationType("*=","compound multiply",OP_COMPOUND_MULTIPLY),
			new OperationType("/=","compound divide",OP_COMPOUND_DIVIDE),
			new OperationType("==","equals",OP_EQUALS),
			new OperationType("!=","not equals",OP_NOT_EQUALS),
			new OperationType("<","less than",OP_LESS_THAN),
			new OperationType(">","greater than",OP_GREATER_THAN),
			new OperationType("<=","less than or equal to",OP_LESS_THAN_OR_EQUAL_TO),
			new OperationType(">=","greater than or equal to",OP_GREATER_THAN_OR_EQUAL_TO),
			new OperationType("&&","logical and",OP_LOGICAL_AND),
			new OperationType("||","logical or",OP_LOGICAL_OR),
			new OperationType("^","exclusive or",OP_LOGICAL_XOR),
			OP_UNDEFINED,
			OP_METHOD_CALL,
			OP_SCRIPTED_VALUE,
			OP_LINE_COMMENT,
			OP_BLOCK_COMMENT_BEGIN,
			OP_BLOCK_COMMENT_END
		};
		private static Dictionary<string,OperationType> OPERATION_TYPE_BY_SYNTAX = null;
		public static OperationType GetOperationTypeBySyntax(string syntax) {
			if(OPERATION_TYPE_BY_SYNTAX == null) {
				OPERATION_TYPE_BY_SYNTAX = new Dictionary<string, OperationType>();
				for(int i = 0; i < OPS.Length; ++i) {
					if(OPS[i].syntax != null) {
						OPERATION_TYPE_BY_SYNTAX.Add (OPS[i].syntax, OPS[i]);
					}
				}
			}
			OperationType op;
			return OPERATION_TYPE_BY_SYNTAX.TryGetValue (syntax, out op)?op:null;
		}
		//***********************************************************************************
		public static object OP_METHOD_CALL_DELEGATE(LIST_TYPE expList, object scope, Type expectedType, FileParseResults output) {
			//Type expectedType = typeof(System.Reflection.MethodInfo);
			// resolve the reference
			Expression lexp = expList[0] as Expression, rexp = expList[1] as Expression;
			object lval = (lexp != null)?lexp.Resolve(scope, output, typeof(System.Reflection.MethodInfo)):expList[0];
			// if the result is an overloaded method
			Type lvalt = lval.GetType();
			System.Reflection.MethodInfo[] overloadedMethods = null;
			object[] parameters = null;
			
			// get the method at the end of this reference
			// expressions need to be executed in an array (TODO use AdvanceThrouObject instead of TryDeReferenceGetWork, to reduce overhead (eliminate this array))
			if (lvalt != typeof(LIST_TYPE)) {
				LIST_TYPE arr = new LIST_TYPE();
				arr.Add (lval);
				lval = arr;
			}
			// Debug.Log("resolving \""+Serializer.Stringify(lval)+"\"  with scope \""+scope+"\"");
			//Data.AdvanceThroughObject(scope, lval, out lval, null);
			Data.TraversalResult res = Data.TryDeReferenceGetWork(scope, lval as LIST_TYPE, out lval);
			lvalt = lval.GetType();
			// Debug.Log("TYPE                       "+lvalt+" \""+lval+"\"");
			bool isOverloadedMethod = lvalt == typeof(System.Reflection.MethodInfo[]) || (lvalt.IsArray && lvalt.GetElementType() == typeof(System.Reflection.MethodInfo));
			System.Reflection.MethodInfo mi = null;
			if(lval is System.Reflection.MethodInfo || isOverloadedMethod) {
				mi = lval as System.Reflection.MethodInfo;
				//						Debug.Log("getting parameters to Invoke with ("+rexp+")");
				parameters = rexp.ResolveParameters(scope, output, null);
				//string paramsDebugStr = "";
				//for(int i = 0; i < parameters.Length; ++i) {
				//	if(i > 0) paramsDebugStr += ", ";
				//	paramsDebugStr += parameters[i];
				//}
				//Debug.Log("invoking with: ("+paramsDebugStr+")");
			}
			// resolve overloading based on parameter type
			if(isOverloadedMethod) {
				overloadedMethods = lval as System.Reflection.MethodInfo[];
				// TODO find out which of the overloaded methods this needs to be, and set lval to that one.
				//Debug.Log("overloaded method search: \""+lvalue+"\"");
				// eliminate methods that could not be invoked
				for(int i = 0; i < overloadedMethods.Length; ++i) {
					if(!ParametersValidForMethod(parameters, overloadedMethods[i])){
						overloadedMethods[i] = null;
					}
				}
				// get the valid method (counting how many valid ones there are along the way)
				int validMethods = 0;
				for(int i = 0; i < overloadedMethods.Length; ++i) {
					if(overloadedMethods[i] != null) {
						mi = overloadedMethods[i];
						validMethods++;
						//Debug.Log("have good method: "+mi);
					}
				}
				if(validMethods > 1) {
					throw new System.Exception("multiple valid overloaded methods for "+lval);//lvalue);
				} else if (validMethods == 0) {
					throw new System.Exception("no valid overloaded methods for "+lval);//lvalue);
				}
				// reassign lvalt when finished
				lvalt = mi.GetType();
			} else {
				mi = lval as System.Reflection.MethodInfo;
			}
			// invoke the referenced method
			if(parameters != null) {
				parameters = rexp.ResolveParameters(scope, output, mi.GetParameters());
				//String DEBUGTEXT = "invoking "+mi+" with (";
				//for(int i = 0; i < parameters.Length; ++i) {
				//	if(i > 0)DEBUGTEXT += ", ";
				//	DEBUGTEXT += parameters[i].GetType();
				//}
				//DEBUGTEXT += "), expecting (";
				//for(int i = 0; i < mi.GetParameters().Length; ++i) {
				//	if(i > 0)DEBUGTEXT += ", ";
				//	DEBUGTEXT += mi.GetParameters()[i];
				//}
				//DEBUGTEXT += ")";
				//Debug.Log(DEBUGTEXT);
				object returned = mi.Invoke(scope, parameters);
				//Debug.Log(this+" returned \""+returned+"\"");
				return returned;
			}
			throw new System.Exception(res+" TODO \""+lval+"\" ");
		}
		public static object OP_LOGICAL_AND(LIST_TYPE expList,object scope, Type expectedType, FileParseResults output) {
			Expression lexp = expList[0] as Expression, rexp = expList[1] as Expression;
			if(expectedType == typeof(object)) expectedType = typeof(bool);
			if(expectedType != typeof(bool)) throw new System.Exception("can only do logical operations with boolean logic.");
			object lval = (lexp != null)?lexp.Resolve(scope, output, expectedType):expList[0], rval = null;
			Type lvalT = lval.GetType(), rvalT = null;
			if(lvalT == typeof(bool)) {
				bool lv = (bool)lval;
				if(lv == false)	return false;
				rval = (rexp != null)?rexp.Resolve(scope, output, expectedType):expList[1];
				rvalT = rval.GetType();
				if(rvalT == typeof(bool))	return (bool)rval;
			}
			throw new System.Exception("AND bool logic failed \""+lval+"\"<"+lvalT+">(from "+expList[0]+")   \""+rval+"\"<"+rvalT+">");
		}
		public static object OP_LOGICAL_OR(LIST_TYPE expList,object scope, Type expectedType, FileParseResults output) {
			Expression lexp = expList[0] as Expression, rexp = expList[1] as Expression;
			if(expectedType == typeof(object)) expectedType = typeof(bool);
			if(expectedType != typeof(bool)) throw new System.Exception("can only do logical operations with boolean logic.");
			object lval = (lexp != null)?lexp.Resolve(scope, output, expectedType):expList[0], rval = null;
			Type lvalT = lval.GetType(), rvalT = null;
			if(lvalT == typeof(bool)) {
				bool lv = (bool)lval;
				if(lv == true)	return true;
				rval = (rexp != null)?rexp.Resolve(scope, output, expectedType):expList[1];
				rvalT = rval.GetType();
				if(rvalT == typeof(bool))	return (bool)rval;
			}
			throw new System.Exception("OR bool logic failed \""+lval+"\"<"+lvalT+">(from "+expList[0]+")   \""+rval+"\"<"+rvalT+">");
		}
		public static object OP_LOGICAL_XOR(LIST_TYPE expList,object scope, Type expectedType, FileParseResults output) {
			Expression lexp = expList[0] as Expression, rexp = expList[1] as Expression;
			if(expectedType == typeof(object)) expectedType = typeof(bool); // TODO implement XOR for integers?
			if(expectedType != typeof(bool)) throw new System.Exception("can only do logical operations with boolean logic.");
			object lval = (lexp != null)?lexp.Resolve(scope, output, expectedType):expList[0], rval = null;
			Type lvalT = lval.GetType(), rvalT = null;
			if(lvalT == typeof(bool)) {
				bool lv = (bool)lval;
				rval = (rexp != null)?rexp.Resolve(scope, output, expectedType):expList[1];
				rvalT = rval.GetType();
				if(rvalT == typeof(bool))	return lv ^ ((bool)rval);
			}
			throw new System.Exception("XOR bool logic failed \""+lval+"\"<"+lvalT+">(from "+expList[0]+")   \""+rval+"\"<"+rvalT+">");
		}
		public static object OPF_ASSIGN(LIST_TYPE expList, object scope, Type expectedType, FileParseResults output) {
			Expression lexp = expList[0] as Expression, rexp = expList[1] as Expression;
			object lval = null, rval = null;
			Type lvalT = null, rvalT = null;
			rval = (rexp != null)?rexp.Resolve(scope, output, typeof(string)):expList[1];// string is the most universal type... this *may* need adjusting in the future.
			if(rval == null) rval = "";
			rvalT = rval.GetType();
			if(lexp != null && lexp.IsScriptedValue()) {
				if(scope == null) { throw new System.Exception ("can't derefernce from null scope"); }
				Data.TryDeReferenceSet(scope, lexp.expList, rval);
				return rval;
			}
			throw new System.Exception("assign logic failed \""+lval+"\"<"+lvalT+">(from "+expList[0]+")   \""+rval+"\"<"+rvalT+">");
		}
		public static object OP_COMPOUND_ADD(LIST_TYPE expList,object scope, Type expectedType, FileParseResults output) {
			Expression lexp = expList[0] as Expression;//, rexp = expList[1] as Expression;
			double lv, rv;
			if(OP_MATH_HELPER(expList, scope, output, out lv, out rv)) {
				lv += rv;
				Data.TryDeReferenceSet(scope, lexp.expList, lv);
				return lv;
			}
			throw new System.Exception("compound add logic failed");
		}
		public static object OP_COMPOUND_SUBTRACT(LIST_TYPE expList,object scope, Type expectedType, FileParseResults output) {
			Expression lexp = expList[0] as Expression;//, rexp = expList[1] as Expression;
			double lv, rv;
			if(OP_MATH_HELPER(expList, scope, output, out lv, out rv)) {
				lv -= rv;
				Data.TryDeReferenceSet(scope, lexp.expList, lv);
				return lv;
			}
			throw new System.Exception("compound subtract logic failed");
		}
		public static object OP_COMPOUND_MULTIPLY(LIST_TYPE expList,object scope, Type expectedType, FileParseResults output) {
			Expression lexp = expList[0] as Expression;//, rexp = expList[1] as Expression;
			double lv, rv;
			if(OP_MATH_HELPER(expList, scope, output, out lv, out rv)) {
				lv *= rv;
				Data.TryDeReferenceSet(scope, lexp.expList, lv);
				return lv;
			}
			throw new System.Exception("compound multiply logic failed");
		}
		public static object OP_COMPOUND_DIVIDE(LIST_TYPE expList,object scope, Type expectedType, FileParseResults output) {
			Expression lexp = expList[0] as Expression;//, rexp = expList[1] as Expression;
			double lv, rv;
			if(OP_MATH_HELPER(expList, scope, output, out lv, out rv)) {
				lv /= rv;
				Data.TryDeReferenceSet(scope, lexp.expList, lv);
				return lv;
			}
			throw new System.Exception("compound divide logic failed");
		}
		public static object OP_SUM(LIST_TYPE expList,object scope, Type expectedType, FileParseResults output) {
			double lv,rv;if(OP_MATH_HELPER(expList,scope,output,out lv,out rv)){	return lv + rv;	}throw new System.Exception("addition failed");
		}
		public static object OP_DIFFERENCE(LIST_TYPE expList,object scope, Type expectedType, FileParseResults output) {
			double lv,rv;if(OP_MATH_HELPER(expList,scope,output,out lv,out rv)){	return lv - rv;	}throw new System.Exception("subtraction failed");
		}
		public static object OP_PRODUCT(LIST_TYPE expList,object scope, Type expectedType, FileParseResults output) {
			double lv,rv;if(OP_MATH_HELPER(expList,scope,output,out lv,out rv)){	return lv * rv;	}throw new System.Exception("multiplication failed");
		}
		public static object OP_QUOTIENT(LIST_TYPE expList,object scope, Type expectedType, FileParseResults output) {
			double lv,rv;if(OP_MATH_HELPER(expList,scope,output,out lv,out rv)){	return lv / rv;	}throw new System.Exception("division failed");
		}
		public static object OP_LESS_THAN(LIST_TYPE expList,object scope, Type expectedType, FileParseResults output) {
			double lv,rv;if(OP_MATH_HELPER(expList,scope,output,out lv,out rv)){	return lv < rv;	}throw new System.Exception("< logic failed");
		}
		public static object OP_LESS_THAN_OR_EQUAL_TO(LIST_TYPE expList,object scope, Type expectedType, FileParseResults output) {
			double lv,rv;if(OP_MATH_HELPER(expList,scope,output,out lv,out rv)){	return lv <= rv;	}throw new System.Exception("<= logic failed");
		}
		public static object OP_GREATER_THAN(LIST_TYPE expList,object scope, Type expectedType, FileParseResults output) {
			double lv,rv;if(OP_MATH_HELPER(expList,scope,output,out lv,out rv)){	return lv > rv;	}throw new System.Exception("> logic failed");
		}
		public static object OP_GREATER_THAN_OR_EQUAL_TO(LIST_TYPE expList,object scope, Type expectedType, FileParseResults output) {
			double lv,rv;if(OP_MATH_HELPER(expList,scope,output,out lv,out rv)){	return lv >= rv;	}throw new System.Exception(">= logic failed");
		}
		public static object OP_EQUALS(LIST_TYPE expList,object scope, Type expectedType, FileParseResults output) {
			Expression lexp = expList[0] as Expression, rexp = expList[1] as Expression;
			object
				lval = (lexp != null)?lexp.Resolve(scope, output, typeof(double)):expList[0], 
				rval = (rexp != null)?rexp.Resolve(scope, output, typeof(double)):expList[1];
			while(lval.GetType() == typeof(Expression)) { lval = (lval as Expression).Resolve(scope, output, typeof(double)); }
			while(rval.GetType() == typeof(Expression)) { rval = (rval as Expression).Resolve(scope, output, typeof(double)); }
			// Debug.Log ("testing ("+lval+"<"+lval.GetType()+"> == "+rval+"<"+rval.GetType()+">)");
			if(Data.IsNumericType(lval.GetType()) && Data.IsNumericType(rval.GetType())) {
				double lv, rv;
				if(Data.TryParseDouble(lval, out lv) && Data.TryParseDouble(rval, out rv)) {
					return lv == rv;
				}
			} else if (lval.GetType() == rval.GetType()) {
				if(lval.GetType() == typeof(string)) {
					return (lval as string).Equals(rval as string);
				}
				return lval == rval;
			}
			throw new System.Exception("logic failed. Could not evaluate ("+lval+"<"+lval.GetType()+"> == "+rval+"<"+rval.GetType()+">)");
		}
		public static object OP_NOT_EQUALS(LIST_TYPE expList,object scope, Type expectedType, FileParseResults output) {
			return (bool)OP_EQUALS (expList, scope, expectedType, output) != true;
		}
		public static bool OP_MATH_HELPER(LIST_TYPE expList,object scope, FileParseResults output, out double lv, out double rv) {
			lv = rv = 0;
			Expression lexp = expList[0] as Expression, rexp = expList[1] as Expression;
			object lval = (lexp != null)?lexp.Resolve(scope, output, typeof(double)):expList[0], 
			       rval = (rexp != null)?rexp.Resolve(scope, output, typeof(double)):expList[1];
			bool lParsed = Data.TryParseDouble(lval, out lv);
			if(!lParsed) { Debug.LogError("could not parse L-value "+lval.ToString()+" to a double."); }
			bool rParsed = Data.TryParseDouble(rval, out rv);
			if(!rParsed) { Debug.LogError("could not parse R-value "+rval.ToString()+" to a double."); }
			return lParsed && rParsed;
		}
		public static object OP_RESOLVE_SCRIPTED_VALUE(LIST_TYPE expList, object scope, Type expectedType, FileParseResults output) {
			if(expList.Count == 1) {
				double number;
				if(Data.TryParseDouble(expList[0], out number)) {
					//Debug.Log("just a number. "+number);
					return number;
				}
			}
			object result;
			Data.TraversalResult error = Data.TryDeReferenceGetWork(scope, expList, out result);
			// Debug.Log("~~~~~~~~~ "+error+" "+Serializer.Stringify(expList));
			if(error == Data.TraversalResult.couldNotFindMember) {
				if(Data.IsNumericType(expectedType))	return 0;
				else if(expectedType == typeof(string))		return "";
				else if(expectedType == typeof(bool))		return false;
			} else {
				if(result == null) {
					if(Data.IsNumericType(expectedType))		return 0;
					else if(Data.IsStringType(expectedType))	return "";
					else if(expectedType == typeof(bool))		return false;
					else Debug.LogWarning("found null, expected <"+expectedType+">   ");
				}
				if(Data.IsNumericType(expectedType) && Data.IsStringType(result.GetType())) {
					double dub;
					if(Data.TryParseDouble(result.ToString(), out dub)){
						result = dub;
					}
				}
			}
			if(error != Data.TraversalResult.found) {
				if(output != null) output.ERROR(error.ToString());
				// else Debug.Log (error);
				throw new System.Exception(error.ToString());
				//return null;
			}
			return result;
		}
		//***********************************************************************************
		
		public static Expression Parse(string script) {
			FileParseResults r = null;
			object output = Parser.Parse (Parser.ParseType.JSON, "", script, ref r);
			if(output == null) {
				throw new System.Exception("failed to parse \""+script+"\" as OM.Expression, result was null");
			}
			if(output.GetType() != typeof(Expression)) {
				throw new System.Exception("failed to parse \""+script+"\" as OM.Expression, result was "+output.GetType());
			}
			return output as Expression;
		}
		
		public override int GetHashCode () {
			return ToString ().GetHashCode ();
		}
		
		public LIST_TYPE GetExpressionList() { return expList; }
		
		public int GetExpressionListCount() { return (expList != null)?expList.Count:0; }
		
		public override bool Equals (object obj) {
			bool isEqual = false;
			Expression other = obj as Expression;
			if(other != null) {
				// Debug.Log ("opType: \""+opType+"\" vs \""+other.opType+"\"    expList:"+GetExpressionListCount()+" vs "+other.GetExpressionListCount());
				if(other.opType == opType && GetExpressionListCount() == other.GetExpressionListCount()) {
					isEqual = true;
					for(int i = 0; i < GetExpressionListCount(); ++i) {
						if(!expList[i].Equals(other.expList[i])) {// TODO what happens if a string referencing a Field is compared to a Field?
							isEqual = false;
							// Debug.Log("failed at explist["+i+"],   \""+expList[i]+"\" vs \""+other.expList[i]+"\"");
							break;
						}
					}
				}
			}
			return isEqual;
		}
		
		public bool IsScriptedValue() { return opType == OP_SCRIPTED_VALUE; }
		
		public bool ResolveBoolean(object scope) {
			object result = Resolve (scope, null, typeof(bool));
			if(result.GetType() == typeof(bool))
				return (bool)result;
			throw new System.Exception ("cannot result non-boolean as boolean: "+this);
		}
		
		public object[] ResolveParameters(object scope, FileParseResults output, System.Reflection.ParameterInfo[] expectedParameters) {
			object[] parameters;
			if(expectedParameters != null)
				parameters = new object[expectedParameters.Length];
			else
				parameters = new object[GetExpressionListCount()];
			Expression expr;
			for(int i = 0; i < parameters.Length; ++i) {
				expr = expList[i] as Expression;
				Type t = (expectedParameters != null)?expectedParameters[i].ParameterType:typeof(object);
				if(expr != null) {
					parameters[i] = expr.Resolve (scope, output, t);
				} else {
					object arg;
					if(t != typeof(object)) {
						Data.ParseValue(expList[i], t, out arg, scope);
						parameters[i] = arg;
					} else {
						parameters[i] = expList[i];
					}
				}
				if(expectedParameters != null && parameters[i].GetType() != expectedParameters[i].ParameterType) {
					// Debug.Log("Expected "+((expectedParameters[i]==null)?"NULL":expectedParameters[i].ParameterType.ToString())+
					//          ", and getting "+((parameters[i]==null)?"NULL":parameters[i].GetType().ToString())+"...");
					parameters[i] = System.Convert.ChangeType(parameters[i], expectedParameters[i].ParameterType);
				}
			}
			return parameters;
		}
		/// <summary>
		/// Resolve the expression in the specified scope. No errors will be reported. Use the other Resolve for more detailed execution
		/// </summary>
		/// <param name="scope">Scope.</param>
		public object Resolve(object scope) {
			if(scope is Value) scope = ((Value)scope).GetRawObject();
			return Resolve (scope, null, typeof(object));
		}
		/// <summary>
		/// Resolve the specified scope, output and expectedType.
		/// </summary>
		/// <param name="scope">Scope.</param>
		/// <param name="output">Output.</param>
		/// <param name="expectedType">Expected type. if type cannot be found, a zero-value version of this type is returned</param>
		public object Resolve (object scope, FileParseResults output, Type expectedType) {
			return opType.resolve (this.expList, scope, expectedType, output);
		}
		
		public static bool ParametersValidForMethod(object[] parameters, System.Reflection.MethodInfo method) {
			// eliminate methods with different arguments than the expected parameters
			System.Reflection.ParameterInfo[] args = method.GetParameters();
			if(args.Length != parameters.Length) {
				//Debug.Log("found overload with with "+args.Length+" args, which is not correct");
				return false;
			}
			// if argument types do not match
			for(int a = 0; a < parameters.Length; ++a) {
				// TODO check if parameters[a].GetType() can be converted to args[a].ParameterType, and not only allow those, but make the conversion at the end of this method if that is the case
				if(parameters[a].GetType() != args[a].ParameterType) {
					//Debug.Log("found overload requiring "+args[a].ParameterType+" in argument  "+a+", not "+parameters[a].GetType()+" which is what is given");
					return false;
				}
			}
			//Debug.Log ("found acceptable method");
			return true;
		}
		
		/// <summary>parse into logic tree</summary>
		public void Process () {
			if(expList == null) {
				throw new System.Exception("cannot process expression without components: "+ToString ());
			}
			// check if this *could* be a method call
			if(expList.Count == 2) {
				// if the second part is argument parameters // object methodReference = expList[0], parameters = expList[1];
				if(expList[1] is Expression) {
					// if so, this is a function call (func)
					opType = OP_METHOD_CALL;
					return;
				}
			}
			opType = OP_SCRIPTED_VALUE;
			// check if this is a binary operation
			for (int i = 0; i < expList.Count; ++i) {
				System.Type t = expList[i].GetType ();
				OperationType opT = null;
				if (t == typeof(string)) {
					opT = GetOperationTypeBySyntax(expList[i] as string);
				}
				// parse binary operators
				if (opT != null) {
					LIST_TYPE binaryExpression = Data.CreateListWithSize(2);//ARRAY.CreateWithSize(2);
					if (i == 0) {
						throw new Exception("expected expression before (" + opT + ") in expression " + Serializer.Stringify (expList));
					}
					// put all expresion values to the left into their own sub expression
					if (i != 1) {
						int extraValues = i;
						Expression prevExpression = new Expression(opType, expList.GetRange (0, extraValues));
						expList[0] = prevExpression;
						expList.RemoveRange (1, extraValues - 1);
						i -= extraValues - 1;
					}
					binaryExpression[0] = expList[i - 1];
					if (i >= expList.Count - 1) {
						throw new Exception("expression missing after (" + opT + ") in expression " + Serializer.Stringify (expList));
					}
					// put all expression values to the right into their own sub expression (and continue parsing there)
					if (i != expList.Count - 2) {
						int extraValues = expList.Count - (i + 1);
						Expression nextExpression = new Expression(opType, expList.GetRange (i + 1, extraValues));
						expList[i + 1] = nextExpression;
						expList.RemoveRange (i + 2, extraValues - 1);
						nextExpression.Process ();
					}
					binaryExpression[1] = expList[i + 1];
					opType = opT;
					expList.RemoveRange (i - 1, 3);
					i -= 2;
					if(expList.Count != 0)
						throw new System.Exception("after a binary operation, the scripted value should be fully parsed away...");
					expList = binaryExpression;
					break;
				}
			}
			if (expList.Count == 0) {
				expList = null;
			}
			// else { Debug.Log("remainder: "+this+" <"+opType+">"); }
		}
		
		override public string ToString () { return ToString(false); }
		public string ToString (bool whitespace) {
			StringBuilder sb = new StringBuilder();
			if (opType == Expression.OP_SCRIPTED_VALUE) {
				sb.Append ("(");
				for (int i = 0; i < GetExpressionListCount(); ++i) {
					if (i > 0)
						sb.Append (whitespace?", ":",");
					string s;
					if(expList[i].GetType() == typeof(Expression)) { s = (expList[i] as Expression).ToString(whitespace); }
					else { s = Serializer.StringifyExpression(expList[i], whitespace); }
					sb.Append (s);
				}
				sb.Append (")");
			} else 	if(opType == OP_METHOD_CALL) {
				Expression paramExp = expList[1] as Expression;
				string lval, rval;
				if(expList[0].GetType() == typeof(Expression)) { lval = (expList[0] as Expression).ToString(whitespace); }
				else { lval = Serializer.StringifyExpression(expList[0], whitespace); }
				if(expList[1].GetType() == typeof(Expression)) { rval = (expList[1] as Expression).ToString(whitespace); }
				if(expList[1] != null && paramExp == null) throw new System.Exception("second param of method call must be the list of parameters");
				if(expList[1] == null || paramExp.GetExpressionListCount() == 0) {
					sb.Append("(");	sb.Append(lval);	sb.Append("())");
				} else {
					if(expList[1].GetType() == typeof(Expression)) { rval = (expList[1] as Expression).ToString(whitespace); }
					else { rval = Serializer.StringifyExpression(expList[1], whitespace); }
					sb.Append("(");	sb.Append(lval);
					sb.Append(rval);	sb.Append(")");
				}
			} else {
				if(expList!=null) {
					string lval, rval;
					if(expList[0].GetType() == typeof(Expression)) { lval = (expList[0] as Expression).ToString(whitespace); }
					else { lval = Serializer.StringifyExpression(expList[0], whitespace); }
					if(expList[1].GetType() == typeof(Expression)) { rval = (expList[1] as Expression).ToString(whitespace); }
					else { rval = Serializer.StringifyExpression(expList[1], whitespace); }
					sb.Append("(");	sb.Append(lval);	if(whitespace) sb.Append(" ");
					sb.Append(opType.syntax);
					if(whitespace) sb.Append(" ");	sb.Append (rval);	sb.Append(")");
				} else {
					sb.Append("/*no components*/");
				}
			}
			return sb.ToString ();
		}
	}
}