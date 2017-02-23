using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Agent_UI : MonoBehaviour {
	private Agent_SizeAndEffects sizeAndEffects;
	private Agent_MOB mob;
	private Agent_Properties props;
	private ValueCalculator<Agent_Properties>.ChangeListener watcher;
	private TeamMember membership;
	public UnityEngine.UI.Text uiText, uiProperties, uiTeamInfo;
	public UnityEngine.UI.Image uiTeamImage;
	private Team currentTeam;
	private bool refreshProps = true;

	public void SetSubject(GameObject subject) {
		if (subject == null) {
			if (props) {
				props.RemoveValueChangeListener ("", watcher);
			}
			sizeAndEffects = null;
			mob = null;
			props = null;
		} else {
			if (props) {
				props.RemoveValueChangeListener ("", watcher);
			}
			sizeAndEffects = subject.GetComponent<Agent_SizeAndEffects> ();
			mob = subject.GetComponent<Agent_MOB> ();
			props = subject.GetComponent<Agent_Properties> ();
			if (props) {
				watcher = delegate(Agent_Properties res, string resourceName, float oldValue, float newValue) {
					refreshProps = true;
				};
				props.AddValueChangeListener ("", watcher);
			}
			membership = sizeAndEffects.GetComponent<TeamMember> ();
		}
	}

	private bool HaveAllUiElements() {
		return uiText && uiProperties && uiTeamInfo && uiTeamImage;
	}

	void Start() {
		if (!HaveAllUiElements()) {
			Canvas[] canvases = GameObject.FindObjectsOfType<Canvas> ();
			UnityEngine.UI.Text t;
			UnityEngine.UI.Image img;
			foreach (Canvas c in canvases) {
				if (c.renderMode == RenderMode.ScreenSpaceOverlay) {
					for (int i = 0; i < c.transform.childCount; ++i) {
						t = c.transform.GetChild (i).GetComponent<UnityEngine.UI.Text> ();
						img = c.transform.GetChild (i).GetComponent<UnityEngine.UI.Image> ();
						     if (!uiText) { uiText = t; }
						else if (!uiProperties) uiProperties = t;
						else if (!uiTeamInfo) uiTeamInfo = t;
						if (!uiTeamImage) { uiTeamImage = img; }
						if (HaveAllUiElements()) { break; }
					}
					if (HaveAllUiElements()) { break; }
				}
			}
		}
	}

	void FixedUpdate () {
		if (uiText) {
			uiText.text = sizeAndEffects.name +
				"\nsize: " + System.String.Format ("{0:0.##}", sizeAndEffects.GetSize ()) +
				"\nenergy: " + System.String.Format("{0:0.##}", sizeAndEffects.GetEnergy ()) +
				"\ncurrent speed: " + System.String.Format("{0:0.##}", mob.GetSpeed()) +
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
		if (currentTeam != membership.team) {
			currentTeam = membership.team;
			if (uiTeamInfo) {
				uiTeamInfo.text = (currentTeam)?currentTeam.name:"";
			}
			if (uiTeamImage) {
				uiTeamImage.sprite = currentTeam?currentTeam.icon:null;
			}
			uiTeamImage.color = currentTeam ? currentTeam.color : Color.clear;
		}
	}
}
