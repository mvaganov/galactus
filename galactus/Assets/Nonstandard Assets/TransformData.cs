using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct TransformData {
	public Vector3 position;
	public Quaternion rotation;
	public Vector3 scale;

	public TransformData(Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion), Vector3 scale = default(Vector3)) {
		if (scale == default(Vector3)) { scale = Vector3.one; }
		this.position = position; this.rotation = rotation; this.scale = scale;
	}

	public TransformData(Transform t, bool useLocalRotation = false){
		this.position = Vector3.zero; this.rotation = Quaternion.identity; this.scale = Vector3.one;
		Set (t, useLocalRotation);
	}

	public void Set(Transform t, bool useLocalRotation = false) {
		if(!useLocalRotation) {
			this.position = t.position;		this.rotation = t.rotation;		this.scale = t.lossyScale;
		} else {
			this.position = t.localPosition;this.rotation = t.localRotation;this.scale = t.localScale;
		}
	}

	public static void Lerp (Transform t, 
		Vector3 targetPosition, Quaternion targetRotation, Vector3 targetScale, float duration, 
		System.Action finishedCallback) {
		float timer = 0;
		Vector3 startPosition = t.position, startScale = t.localScale;
		Quaternion startRotation = t.rotation;
		Quaternion tRotation = targetRotation;
		System.Action lerping = null;
		lerping = () => {
			timer += UnityEngine.Time.deltaTime;
			if(timer >= duration) {
				t.position = targetPosition;
				t.rotation = targetRotation;
				t.localScale = targetScale;
				if(finishedCallback != null) { finishedCallback.Invoke(); }
			} else {
				float progress = timer/duration;
				Quaternion oldRotation = t.rotation;
				t.rotation = Quaternion.Lerp(startRotation, tRotation, progress);
				t.position = Vector3.Lerp(startPosition, targetPosition, progress);
				t.localScale = Vector3.Lerp(startScale, targetScale, progress);
				NS.Timer.setTimeout (lerping, 1);
			}
		};
		NS.Timer.setTimeout (lerping, 0);
	}

	public static void LerpLocal (Transform t, 
		Vector3 targetLocalPosition, Quaternion targetLocalRotation, Vector3 targetLocalScale, float duration, 
		System.Action finishedCallback) {
		float timer = 0;
		Vector3 startPosition = t.localPosition, startScale = t.localScale;
		Quaternion startRotation = t.localRotation;
		Quaternion tRotation = targetLocalRotation;
		System.Action lerping = null;
		lerping = () => {
			timer += UnityEngine.Time.deltaTime;
			if(timer >= duration) {
				t.localPosition = targetLocalPosition;
				t.localRotation = targetLocalRotation;
				t.localScale = targetLocalScale;
				if(finishedCallback != null) { finishedCallback.Invoke(); }
			} else {
				float progress = timer/duration;
				Quaternion oldRotation = t.rotation;
				t.localRotation = Quaternion.Lerp(startRotation, tRotation, progress);
				t.localPosition = Vector3.Lerp(startPosition, targetLocalPosition, progress);
				t.localScale = Vector3.Lerp(startScale, targetLocalScale, progress);
				NS.Timer.setTimeout (lerping, 1);
			}
		};
		NS.Timer.setTimeout (lerping, 0);
	}

	public static void Lerp (Transform t, TransformData data, float duration, System.Action finishedCallback) {
		Lerp (t, data.position, data.rotation, data.scale, duration, finishedCallback);
	}

	public static void LerpLocal (Transform t, TransformData data, float duration, System.Action finishedCallback) {
		LerpLocal (t, data.position, data.rotation, data.scale, duration, finishedCallback);
	}

	// TODO solve local translation problem. either make a specialized Lerp method, or create methods to Transform and InverseTransform. Probably both.
}
