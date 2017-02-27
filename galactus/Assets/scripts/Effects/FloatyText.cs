using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatyText : MonoBehaviour {

	[Tooltip("how long to show up")]
	public float duration = 1;
	private float timer;
	[Tooltip("how fast (and what direction, +up/-down) to float")]
	public float speed = 1;

	public void SetText(string text, Color color) {
		UnityEngine.UI.Text t = GetComponent<UnityEngine.UI.Text> ();
		t.text = text;
		t.color = color;
	}

	public void Reset(string text, Color color, float speed) {
		timer = 0;
		SetText (text, color);
		this.speed = speed;
	}
	
	// Update is called once per frame
	void LateUpdate () {
		timer += Time.deltaTime;
		transform.rotation = Camera.main.transform.rotation;
		transform.localPosition += Camera.main.transform.up * speed * Time.deltaTime;
		if (timer >= duration) {
			MemoryPoolItem.Destroy (gameObject);
		}
	}
}
