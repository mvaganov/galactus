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
			if (parent != null && !parent.gameObject.activeInHierarchy) {
				parent = null;
			}
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
	private GameObjectPool floatyTextEffects;
	private GameObjectPool drainEffects;
	private GameObjectPool lineEffects;
	private static Effects s_effects;
	public static Effects Get() { if (!s_effects) { s_effects = Singleton.Get<Effects> (); } return s_effects; }

	public GameObjectPool GetSoundPool() {
		if(soundEffects == null) { soundEffects = new GameObjectPool (pfab_soundEffect.gameObject); } return soundEffects;
	}

	public GameObjectPool GetFloatyTextPool() {
		if (floatyTextEffects == null) { floatyTextEffects = new GameObjectPool (pfab_floatyText.gameObject); } return floatyTextEffects;
	}

	public GameObjectPool GetParticleDrainPool() {
		if (drainEffects == null) { drainEffects = new GameObjectPool (pfab_particleDrain.gameObject); } return drainEffects;
	}

	public GameObjectPool GetLinePool() {
		if (lineEffects == null) {
			GameObject newLine = null;
			NS.Lines.Make (ref newLine, Vector3.zero, Vector3.zero);
			lineEffects = new GameObjectPool (newLine);
		} return lineEffects;
	}

	[SerializeField]
	private TempSoundEffect pfab_soundEffect;
	[SerializeField]
	private FloatyText pfab_floatyText;
	[SerializeField]
	private ParticleDrain pfab_particleDrain;

	private Dictionary<string, Effect> effectTable = null;

	[SerializeField]
	private List<Effect> effects = new List<Effect>();

	public static void ResetTrailRenderer(TrailRenderer tr) {
		if (tr) { float oldTime = tr.time; tr.time = 0; TimeMS.TimerCallback(100, () => { tr.time = oldTime; }); }
	}

	public static FloatyText FloatyText(Vector3 position, Transform t, string text, Color color, float speed = 1f) {
		Effects e = Get();
		FloatyText ft = e.GetFloatyTextPool ().Alloc ().GetComponent<FloatyText> ();
		ft.transform.position = position;
		ft.transform.SetParent(t);
		ft.Reset (text, color, speed);
		ft.transform.localScale = e.pfab_floatyText.transform.localScale;
		return ft;
	}

	public static ParticleDrain ParticleDrain(Transform from_, Transform to_, Color color, int particleCount, float speed) {
		Effects e = Get ();
		ParticleDrain pd = e.GetParticleDrainPool ().Alloc ().GetComponent<ParticleDrain> ();
		pd.transform.position = from_.position;
		pd.transform.SetParent (from_);
		pd.Setup (to_, particleCount, color, speed);
		pd.transform.localScale = e.pfab_particleDrain.transform.localScale;
		return pd;
	}

	public static GameObject Line(ref GameObject obj, Vector3 start = default(Vector3), Vector3 end = default(Vector3), Color color = default(Color), float startWidth = 0.125f, float endWidth = 0.125f) {
		Effects e = Get ();
		if (obj == null) { obj = e.GetLinePool ().Alloc (); }
		NS.Lines.Make(ref obj, start, end, color, startWidth, endWidth);
		return obj;
	}

	void Start() { NS.Lines.Instance ().transform.SetParent (transform); }

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