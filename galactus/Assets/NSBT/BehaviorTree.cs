using UnityEngine;
using System.Collections;

public class BehaviorTree : MonoBehaviour, BT.Behavable {

	// there should be a fancy editor for these. BTBehaviorInspector. hopefully, one day, we can use the GUI to create Behavior Trees.
	// To make that happen, we'll probably need to understand:
	// * nice lists: https://bitbucket.org/rotorz/reorderable-list-editor-field-for-unity
	// * creating custom editor elements: http://answers.unity3d.com/questions/274002/using-serializedproperty-on-custom-classes.html
	// * scriptable objects: http://unity3d.com/learn/tutorials/modules/beginner/live-training-archive/scriptable-objects
	// * property drawers: http://docs.unity3d.com/Manual/editor-PropertyDrawers.html
	[HideInInspector]
	public int[] numbers; // used as test data to access while messing with the above
	

	public string scriptDescription;

	public BT.Behavior behavior;

	// TODO make a "save script now" button
	[ContextMenuItem("Load Script Now", "LoadScript")]
	public TextAsset behaviorTreeScriptFile;

	public string GetDescription() { return scriptDescription; }

	public BT.Behavior.Status Behave(BTOwner who) {
		if(behavior == null) {
			LoadScript();
		}
		return behavior.Behave(who);
	}

	public void LoadScript()
	{
		if(behavior == null && behaviorTreeScriptFile != null) {
			Debug.Log(behaviorTreeScriptFile.text);
			behavior = OMU.Parser.Compile(behaviorTreeScriptFile.name, behaviorTreeScriptFile.text) as BT.Behavior;
			Debug.Log(OMU.Util.ToScript(behavior, true));
		} else {
			Debug.LogError("missing script file");
		}
	}
}