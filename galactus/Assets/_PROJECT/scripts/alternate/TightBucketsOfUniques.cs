using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TightBucketsOfUniques<TKey, TValue> : Dictionary<TKey,TValue[]> {
	public bool AddUniqueBucketItem(TKey key, TValue val) {
		TValue[] bucket;
		if (TryGetValue (key, out bucket)) {
			if (System.Array.IndexOf (bucket, val) >= 0) {
				return false;
			}
			System.Array.Resize (ref bucket, bucket.Length + 1);
			bucket [bucket.Length - 1] = val;
		} else {
			bucket = new TValue[1]{val};
		}
		this [key] = bucket;
		return true;
	}

	public bool RemoveUniqueBucketItem(TKey key, TValue val) {
		TValue[] bucket;
		if (TryGetValue (key, out bucket)) {
			int index = System.Array.IndexOf (bucket, val);
			if (index >= 0) {
				bucket [index] = bucket [bucket.Length - 1];
				System.Array.Resize (ref bucket, bucket.Length - 1);
				return true;
			}
		}
		return false;
	}
	public delegate void ThingToDo(TValue uniqueThing);

	public bool ForEachUniqueInBucket(TKey key, ThingToDo thingToDo) {
		TValue[] bucket;
		if (TryGetValue (key, out bucket)) {
			for (int i = 0; i < bucket.Length; ++i) {
				thingToDo(bucket [i]);
			}
			return true;
		}
		return false;
	}

	public TValue[] this[TKey key, TValue[] defaultResult] {
		get {
			TValue[] v;
			if (TryGetValue (key, out v)) {
				return v;
			}
			return defaultResult;
		}
	}
}
