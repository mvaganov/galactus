using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NS {
	public class NoisyCollision : MonoBehaviour {
		[System.Serializable]
		public class NoiseByForce
		{
			public string kindOfNoise = "explosion";
			public float minimumForce = 0;
			public class Comparer : IComparer<NoiseByForce> {
				public int Compare(NoiseByForce x, NoiseByForce y) { return x.minimumForce.CompareTo(y.minimumForce); }
			}
			public static Comparer compare = new Comparer();
		}
		public NoiseByForce[] noises = new NoiseByForce[1];
		void Start() {
			System.Array.Sort(noises, NoiseByForce.compare);
		}
		private static NoiseByForce searched = new NoiseByForce();
		void OnCollisionEnter(Collision col) {
			float force = Vector3.Dot(col.contacts[0].normal, col.relativeVelocity);
			searched.minimumForce = force;
			string kindOfNoise = null;
			if (noises.Length > 1) {
				int i = System.Array.BinarySearch (noises, searched, NoiseByForce.compare);
				if (i < 0) {
					i = ~i;
					if (i > 0) {
						i = i - 1;
						kindOfNoise = noises [i].kindOfNoise;
					}
				}
			} else if (noises[0].minimumForce <= force) {
				kindOfNoise = noises [0].kindOfNoise;
			}
			if (kindOfNoise != null) {
				Noisy.Instance ().PlayKindOfSound (kindOfNoise, col.contacts [0].point, true);
			}
		}
	}
}