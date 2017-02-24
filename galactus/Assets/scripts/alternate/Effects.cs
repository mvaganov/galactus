using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Effects : MonoBehaviour {

	[System.Serializable]
	public class Effect {
		public string name;
		public ParticleSystem ps;
		public AudioClip sound;
		public void Emit(int particleCount, Vector3 loc, Transform parent, bool emitSoundToo = true) {
			if (particleCount > 0) {
				Transform oldParent = ps.transform.parent;
				ps.transform.position = loc;
				ps.transform.parent = parent;
				ps.Emit (particleCount);
				ps.transform.parent = oldParent;
			}
			if (emitSoundToo) {
				GameObject se = Singleton.Get<Effects> ().GetSoundPool ().Alloc ();
				AudioSource asrc = se.GetComponent<AudioSource> ();
				asrc.clip = sound;
				asrc.transform.position = loc;
				asrc.gameObject.SetActive (true);
				asrc.enabled = true;
				asrc.Play ();
				asrc.transform.parent = parent;
			}
		}
	}

	private GameObjectPool soundEffects;

	public GameObjectPool GetSoundPool() {
		if(soundEffects == null) {
			soundEffects = new GameObjectPool (pfab_soundEffect.gameObject);
		}
		return soundEffects;
	}

	[SerializeField]
	private TempSoundEffect pfab_soundEffect;

	private Dictionary<string, Effect> effectTable = null;

	[SerializeField]
	private List<Effect> effects = new List<Effect>();

	public static void ResetTrailRenderer(TrailRenderer tr) {
		if (tr) { float oldTime = tr.time; tr.time = 0; TimeMS.TimerCallback(100, () => { tr.time = oldTime; }); }
	}

	void Start() {
	}

	public Effect Get(string name) {
		Effect e = null;
		if (effectTable == null) {
			effectTable = new Dictionary<string, Effect> ();
			for (int i = 0; i < effects.Count; ++i) {
				effectTable [effects [i].name] = effects [i];
			}
		}
		if (effectTable.TryGetValue (name, out e)) {
			return e;
		}
		return null;
	}
}