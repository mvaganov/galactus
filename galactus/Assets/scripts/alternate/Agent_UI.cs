using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Agent_UI : MonoBehaviour {

	private EnergyAgent owner;
	private Agent_MOB mob;
	private Agent_Properties props;
	private Agent_Properties.ResourceChangeListener watcher;
	public UnityEngine.UI.Text uiText, uiProperties;
	private bool refreshProps = true;

	public void SetSubject(GameObject subject) {
		if (subject == null) {
			if (props) {
				props.RemoveValueChangeListener ("", watcher);
			}
			owner = null;
			mob = null;
			props = null;
		} else {
			if (props) {
				props.RemoveValueChangeListener ("", watcher);
			}
			owner = subject.GetComponent<EnergyAgent> ();
			mob = subject.GetComponent<Agent_MOB> ();
			props = subject.GetComponent<Agent_Properties> ();
			if (props) {
				watcher = delegate(Agent_Properties res, string resourceName, float oldValue, float newValue) {
					refreshProps = true;
				};
				props.AddValueChangeListener ("", watcher);
			}
		}
	}

	void Start() {
		if (!uiText) {
			Canvas[] canvases = GameObject.FindObjectsOfType<Canvas> ();
			UnityEngine.UI.Text t;
			foreach (Canvas c in canvases) {
				if (c.renderMode == RenderMode.ScreenSpaceOverlay) {
					for (int i = 0; i < c.transform.childCount; ++i) {
						t = c.transform.GetChild (i).GetComponent<UnityEngine.UI.Text> ();
						      if (!uiText) { uiText = t;
						} else if(!uiProperties) uiProperties = t;
						if (uiText && uiProperties) { break; }
					}
					if (uiText && uiProperties) { break; }
				}
			}
		}
	}

	void FixedUpdate () {
		if (uiText) {
			uiText.text = owner.name +
				"\nsize: " + System.String.Format ("{0:0.##}", owner.GetRadius ()) +
				"\nenergy: " + System.String.Format("{0:0.##}", owner.GetEnergy ()) +
				"\nbrake distance: " + System.String.Format("{0:0.##}", mob.GetBrakeDistance());
		}
		if (uiProperties && refreshProps) {
			string propText = "";
			foreach (KeyValuePair<string,float> prop in props.GetProperties()) {
				propText += "\n" + prop.Key + ": " + prop.Value;
			}
			uiProperties.text = propText;
			refreshProps = false;
		}
	}
}
