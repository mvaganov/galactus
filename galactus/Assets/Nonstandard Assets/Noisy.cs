using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// author: mvaganov@hotmail.com
// license: Copyfree, public domain. This is free code! Great artists, steal this code!
// latest version at: https://pastebin.com/hGU8et8s -- added: initial version (2018/02/08)
namespace NS {
	public class Noisy : MonoBehaviour {
		[System.Serializable]
		public class NoisesOfKind {
			public string name = "explosion";
			public AudioClip[] sounds = new AudioClip[1];
			[HideInInspector]
			/// <summary>The last noise played, used to encourage variety in noises being played</summary>
			public int lastNoisePlayed = -1;
			public class Comparer : IComparer<NoisesOfKind> {
				public int Compare(NoisesOfKind x, NoisesOfKind y) { return x.name.CompareTo(y.name); }
			}
			public static Comparer compare = new Comparer();

			public void PlayRandomSound(Vector3 p, bool is3D){
				if (sounds != null && sounds.Length > 0) {
					lastNoisePlayed = RandomNumberThatIsnt(0, sounds.Length, lastNoisePlayed);
					AudioClip ac = sounds [lastNoisePlayed];
					if (ac == null) {
						Debug.LogError ("\'"+name+"\' missing AudioClip at index " + lastNoisePlayed);
					}
					PlaySound (ac, p, is3D);
				}
			}
		}
		[Tooltip("kinds of noises to manage")]
		public NoisesOfKind[] kindsOfNoises = new NoisesOfKind[1];

		private static NoisesOfKind searched = new NoisesOfKind();
		public void PlayKindOfSound(string kind, Vector3 p, bool is3D) {
			NoisesOfKind noisesOfKind = null;
			searched.name = kind;
			// assume kinds-of-noises list is sorted for faster search
			int i = System.Array.BinarySearch (kindsOfNoises, searched, NoisesOfKind.compare);
			if (i >= 0) {
				noisesOfKind = kindsOfNoises [i];
				noisesOfKind.PlayRandomSound (p, is3D);
			} else {
				Debug.LogError ("missing kind of noise \'" + kind + "\'");
			}
		}

		[Tooltip("the default background music")]
		public AudioClip bgmusic;
		public float bgmusicVolume = .1f;
		private static AudioSource bgMusicPlayer = null;

		public void UseBackGroundMusic(AudioClip song) {
			if (bgMusicPlayer != null && bgMusicPlayer.clip != song) {
				bgMusicPlayer.Stop ();
				Destroy (bgMusicPlayer.gameObject);
				bgMusicPlayer = null;
			}
			if (song != null && (bgMusicPlayer == null || !bgMusicPlayer.isPlaying)) {
				if (bgMusicPlayer != null) {
					Destroy (bgMusicPlayer.gameObject);
				}
				bgMusicPlayer = PlaySound (song, Vector3.zero, false, true, true);
				bgMusicPlayer.volume = bgmusicVolume;
			}
		}

		void Start() {
			// sort noises for faster access later
			System.Array.Sort(kindsOfNoises, NoisesOfKind.compare);
			if (bgMusicPlayer == null) {
				UseBackGroundMusic (bgmusic);
			}
		}

		private static AudioSource s_asrc;
		public static AudioSource PlaySound(AudioClip noise, Vector3 p = default(Vector3), bool is3D = false, bool independent = true, bool isLooped = false) {
			AudioSource asrc = null;
			if (!independent) {
				asrc = s_asrc;
			}
			if (asrc == null) {
				GameObject go = new GameObject ("<Noise: "+noise.name+">");
				asrc = go.AddComponent<AudioSource> ();
				if (!independent) {
					s_asrc = asrc;
				}
				asrc.transform.SetParent (Instance ().transform);
			} else {
				asrc.Stop ();
			}
			asrc.clip = noise;
			asrc.spatialBlend = (!is3D) ? 0 : 1;
			if (independent && !isLooped) {
				Destroy (asrc.gameObject, noise.length);
			}
			asrc.loop = isLooped;
			asrc.Play ();
			return asrc;
		}
		public static int RandomNumberThatIsnt(int minInclusive, int maxExclusive, int andNot = -1) {
			int index = minInclusive - 1;
			do { index = Random.Range (minInclusive, maxExclusive); } while(index == andNot && (maxExclusive-minInclusive) > 1);
			return index;
		}

		private static Noisy instance;
		public static Noisy Instance() {
			if(instance == null) {
				if((instance = FindObjectOfType(typeof(Noisy)) as Noisy) == null) {
					GameObject g = new GameObject("<" + typeof(Noisy).Name + ">");
					instance = g.AddComponent<Noisy>();
				}
			}
			return instance;
		}
	}
}