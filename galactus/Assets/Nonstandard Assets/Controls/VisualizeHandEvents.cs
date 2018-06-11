using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualizeHandEvents : MonoBehaviour {
	public Transform grip, trigger;
	public float angle;
	public Vector3 axis = Vector3.up;

	public void DoGrip() { grip.transform.Rotate (axis, angle); }
	public void UndoGrip() { grip.transform.Rotate (axis, -angle); }
	public void DoTrigger() { trigger.transform.Rotate (axis, angle); }
	public void UndoTrigger() { trigger.transform.Rotate (axis, -angle); }
}
