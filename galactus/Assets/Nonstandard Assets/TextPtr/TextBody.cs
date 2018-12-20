using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NS.TextPtr.Sources {
	public class TextBody : MonoBehaviour {
		[TextArea(3, 15)]
		public string text;
		public override string ToString() { return text; }
	}
}