using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RotaryHeart.Lib.SerializableDictionary;

[System.Serializable]
public class GameObjectDictionary : SerializableDictionaryBase<string, GameObject> { }

[System.Serializable]
public class FloatDictionary : SerializableDictionaryBase<string, float> { }

[System.Serializable]
public class NSDictionary : SerializableDictionaryBase<string, NS.ObjectPtr> { }

public class DataStore : GeneralDataStore<NSDictionary, string, NS.ObjectPtr> {}

public class GeneralDataStore <T, TKey, TValue> 
: MonoBehaviour where T : SerializableDictionaryBase<TKey, TValue> 	{
	public T data;

	public TValue this[TKey key] { get {return data[key]; } set { data[key] = value; } }

	public ICollection<TKey> Keys { get { return data.Keys; } }
	public ICollection<TValue> Values { get { return data.Values; } }

	public void Add(TKey key, TValue value) { data.Add(key, value); }
	public bool ContainsKey(TKey key) { return data.ContainsKey(key); }
	public bool Remove(TKey key) { return data.Remove(key); }
	public bool TryGetValue(TKey key, out TValue value) { return data.TryGetValue(key, out value); }

	public IEnumerator GetEnumerator() { return data.GetEnumerator(); }

	public int Count { get { return data.Count; } }
	public bool IsReadOnly { get { return false; } }

	public void Add(KeyValuePair<TKey, TValue> item) {
		(data as ICollection<KeyValuePair<TKey, TValue>>).Add(item);
	}
	public void Clear() {data.Clear();}
}
