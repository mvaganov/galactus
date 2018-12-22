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
using UnityEngine;

// TODO turn this into a proper unit test

/// <summary>
/// This class encodes and decodes JSON strings.
/// Spec. details, see http://www.json.org/
///
/// JSON uses Arrays and Objects. These correspond here to the datatypes IList and IDictionary.
/// CSV is an Array of Arrays
/// All numbers are parsed to doubles.
/// </summary>

public class Pt { 
    public float x, y;
    public override string ToString(){return "{x "+x+",y "+y+"}";}
}
public class TestClass { 
    public string name, description;
    public float score; public Pt pos;
    public Dictionary<object,object> attributes;
    public Pt[] history;
    public DateTime when;
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
public class ObjectModel : MonoBehaviour{
	private void Start() {
		OMU.Test t = new OMU.Test();
		t.TestScript();
	}
}