using UnityEngine;
using System.Collections;

public class TempSoundEffect : MonoBehaviour {
	AudioSource asrc;
	void Start() {
		asrc = GetComponent<AudioSource> ();
		asrc.Play ();
	}
	void FixedUpdate() {
		if (!asrc.isPlaying) {
			GetComponent<MemoryPoolItem> ().FreeSelf();
		}
	}
}
