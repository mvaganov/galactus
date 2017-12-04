/*
 * Copyright (c) 2014-2017 Michael Vaganov
 *
 * Object Model Stringify-er/De-serializer for Unity
 *
 * list of "features" added by Michael Vaganov, (breaks the JSON spec):
 * * C-like line comment and block comment - not explicitly part of JSON
 * * undefined tokens (outside of double quotes) treated as string tokens (TODO enum UnQuotedTokenBehavior{string, stringwarning, fail})
 * * colon is optional for value pairs while parsing (TODO enum NVPColon{optional, required})
 * * value is optional for value pairs while parsing (name will reference null) (TODO enum NVPValue{optional, required})
 * * whitespace is optional during Stringify (TODO enum WhitespaceOutput{formatted,wordwrap,wordwrapexceptarrays,none})
 * * (TODO enum ColonCommaOutput{required, ommittedwherepossible})
 * * OM.Expression used to evaluate math, logic, and even _method calls_!
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// Example usage:
//
//  using UnityEngine;
//  using System.Collections;
//  using System.Collections.Generic;
//
//  public class MiniJSONTest : MonoBehaviour {
//      void Start () {
//          var jsonString = "{ \"array\": [1.44,2,3], " +
//                          "\"object\": {\"key1\":\"value1\", \"key2\":256}, " +
//                          "\"string\": \"The quick brown fox \\\"jumps\\\" over the lazy dog \", " +
//                          "\"unicode\": \"\\u3041 Men\u00fa sesi\u00f3n\", " +
//                          "\"int\": 65536, // this is a comment\n" +
//                          "\"float\": 3.1415926, " +
//                          "\"bool\": true, /* this is also a comment */" +
//                          "\"null\": null, " +
//							"a:b, " +
//							"\" c \"d, " +
//							"e\" f \"," +
//							"g{a:b,test}," +
//							"h[1%,2x,3pigs,4] }";
//
//          var dict = OM.Deserialize(jsonString) as Dictionary<string,object>;
//
//          Debug.Log("deserialized: " + dict.GetType());
//          Debug.Log("dict['array'][0]: " + ((List<object>) dict["array"])[0]);
//          Debug.Log("dict['string']: " + (string) dict["string"]);
//          Debug.Log("dict['float']: " + (double) dict["float"]); // floats come out as doubles
//          Debug.Log("dict['int']: " + (long) dict["int"]); // ints come out as longs
//          Debug.Log("dict['unicode']: " + (string) dict["unicode"]);
//
//          var str = OM.Serialize(dict);
//
//          Debug.Log("serialized: " + str);
//      }
//  }

/// <summary>
/// This class encodes and decodes JSON strings.
/// Spec. details, see http://www.json.org/
///
/// JSON uses Arrays and Objects. These correspond here to the datatypes IList and IDictionary.
/// CSV is an Array of Arrays
/// All numbers are parsed to doubles.
/// </summary>
namespace OMU {
/*
	{
		text : "object model test",
		listOfNumbers : [0, 1, 2, 3, 4, 5, 6, 7, 8, 9],
		listOfMathConstants : [
			{
				name : "Pi",
				symbol : "\u03c0",
				value : 3.141592654,
			},{
				name : "euler's number",
				symbol : "e"
				value : 2.71828,
			}
		],
		T : true,
		F : false,
		N : null,
		testOf2 : (1 + 1),
		testOfTruth : (1 > 0),
		testOfFalse : ((F) == true),
		testOfTruth2 : ((testOf2) == 2),
	}
*/
	// usecase: reading JSON into Value, including int, string, float, unicode, bool, array, object, true/false/null, expression
	// usecase: viewing parser results (errors and warnings)
	// usecase: using JSON from value with operator[]
	// usecase: serializing back into JSON
	// usecase: show comments, show optional commas, show optional colons, show optional quotes for single-token literals
	// usecase: creating a strictly-typed object, creating an Object Model and applying it to an object
	// usecase: short-names that end with *
	// usecase: evaluating expressions, including providing a context
	// usecase: evaluating expressions to modify public variables and call public methods with parameters

}

public class Pt { 
    public float x, y;
    public override string ToString(){return "{x "+x+",y "+y+"}";}
}
public class TestClass { 
    public string name, description;
    public float score; public Pt pos;
    public Dictionary<object,object> attributes;
    public Pt[] history;
    public System.DateTime when;
    public void setPos(float n){pos.x=pos.y=n;}
}

namespace OMU {
    public class Test {

        public void TestScript () {
            string givenScript =
            "{                        // line comments work\n"+
            "   \"score\" : 12.5,     /*block comments work too*/\n"+
            "   name: fancypants,     // single-token strings don't need quotes\n"+
            "   desc*: \"Hello \\u3041\", // unicode support, shortened names with *\n"+
            "   pos:{x:(4*(3-5)),y:2} // assign values to public members of objects, even math expressions\n"+
            "   att*:{                // shortened names resolve as 'StartsWith'\n"+
            "     nums:[1,2,3,4]      // array of numbers\n"+
            "     str:5,agi:2 int 3   // add key/value pairs to obj/obj dictionaries, optional commas/colons\n"+
            "     locations:[         // create lists\n"+
            "       Pt{x:1,y:2},Pt{y:100,x:-50.2} // add optionally typecasted objects\n"+
            "     ]\n"+
            "     bool: true,   // bool type (both true and false) \n" +
            "     \"null\": null,   // null is keyword, which equates to null\n" +
            "     addToX:((pos,y)+=(\"a*\",str)) // create scripts that reference & manipulate data members\n"+
            "     setP : (setPos(0))             // scripts can also execute code\n"+
            "   }\n"+
            "   when : 2017/7/27@22.17.52        // System.DateTime is supported, yay for time stamps\n"+
            "   history:[{x:-1,y:-1},{x:1,y:1}]  // fill arrays, even of class types\n"+
            "}";
			Debug.Log (givenScript);
            TestClass t = OMU.Value.FromScript<TestClass>(givenScript); // test de-serializing
            Debug.Log(OMU.Serializer.Stringify(t)); // print out a script similar to the source script
            Debug.Log(t.description);               // test deserializied unicode
            Debug.Log("["+t.attributes["null"]+"]");// printing null should be empty
            string s = "list: ";
            for(int i = 0; i < t.history.Length; ++i) { s += t.history[i]+" "; } // test array filling
            Debug.Log(s);
            Debug.Log((t.attributes["locations"] as IList)[0].GetType()); 
            Pt pt = (t.attributes["locations"] as IList)[0] as Pt;     // test typecasting
            pt.x = 0;
            Debug.Log("pos = "+t.pos+" (initial state)");
            OMU.Expression exp = t.attributes["addToX"] as OMU.Expression;// load scripted adjustments work
            exp.Resolve(t);
            Debug.Log("pos = "+t.pos+", after "+exp+" .Resolved");     // test scripted variable adjustment
            exp = t.attributes["setP"] as OMU.Expression;              // load up function call
            exp.Resolve(t);
            Debug.Log("pos = "+t.pos+", after "+exp+" .Resolved");     // test function call
            t.when = t.when.AddMonths(10);                             // modify slightly, to see changed data
            string compressedSerial = OMU.Serializer.StringifyTiny(t); // test very compressed serialization
            Debug.Log(compressedSerial);
            t = OMU.Value.FromScript<TestClass>(compressedSerial);     // test de-serializing compressed
            Debug.Log(OMU.Serializer.Stringify(t));
        }
    }
}