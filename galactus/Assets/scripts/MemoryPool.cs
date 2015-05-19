// MIT license - TL;DR - Do whatever you want with it, I'm not liable for what you do with it!
#define FAIL_FAST
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// <para>Used for memory recycling of reference types, like GameObjects</para>
/// <para>Example usage:</para>
/// <para>MemoryPool&lt;GameObject&gt; memPool = new MemoryPool&lt;GameObject&gt;(); // construction</para>
/// <para>memPool.Setup(
///		()    => Instantiate(prefab),
///		(obj) => { obj.SetActive(true);  },
///		(obj) => { obj.SetActive(false); },
///		(obj) => { Object.Destroy(obj);  }
/// );</para>
/// <para>GameObject gobj = memPool.Alloc();  // allocate object in pool</para>
/// <para>memPool.Free(gobj); // deallocate object in pool</para>
/// <para>MemoryPoolItem.Destroy(gobj); // deallocate object in pool OR Object.Destroy non-MemoryPool object (for GameObjects only)</para>
/// </summary>
public class MemoryPool<T> where T : class {
	private List<T> allObjects = null;
	private int freeObjectCount = 0;

	public delegate T DelegateBeginLife();
	public delegate void DelegateCommission(T obj);
	public delegate void DelegateDecommission(T obj);
	public delegate void DelegateEndLife(T obj);

	public DelegateBeginLife birth;
	public DelegateEndLife death;
	public DelegateCommission commission;
	public DelegateDecommission decommission;

	/// <summary>
	/// Example usage:
	/// <para>memPool.Setup(
	///		()    => Instantiate(prefab),
	///		(obj) => obj.SetActive(true),
	///		(obj) => obj.SetActive(false),
	///		(obj) => Object.Destroy(obj)
	/// );</para>
	/// </summary>
	/// <param name="create">callback function or delegate used to create a new object of type T</param>
	/// <param name="activate">callback function or delegate used to activate an object of type T</param>
	/// <param name="deactivate">callback function or delegate used to de-activate an object of type T</param>
	/// <param name="destroy">callback function or delegate used to destroy an object of type T</param>
	public void Setup(DelegateBeginLife create, DelegateCommission activate, DelegateDecommission deactivate, DelegateEndLife destroy) {
		birth = create; commission = activate; decommission = deactivate; death = destroy;
	}

	/// <summary>Constructs and calls <see cref="Setup"/></summary>
	public MemoryPool(DelegateBeginLife create, DelegateCommission activate, DelegateDecommission deactivate, DelegateEndLife destroy) {
		Setup(create, activate, deactivate, destroy);
	}

	/// <summary> Be sure to call <see cref="Setup"/>!</summary>
	public MemoryPool() { }

	/// <summary>Returns an object from the memory pool, which may have just been created</summary>
	public T Alloc() {
		T freeObject = null;
		if(freeObjectCount == 0) {
#if FAIL_FAST
			if(birth == null) { throw new System.Exception("Call .Setup(), and provide a create method!"); }
#endif
			if(allObjects == null) { allObjects = new List<T>(); }
			freeObject = birth();
			allObjects.Add(freeObject);
			if(typeof(T) == typeof(GameObject)) {
				GameObject go = freeObject as GameObject;
				go.AddComponent<MemoryPoolItem>().SetPool(this as MemoryPool<GameObject>);
			}
		} else {
			freeObject = allObjects[allObjects.Count - freeObjectCount];
			freeObjectCount--;
		}
		if(commission != null) { commission(freeObject); }
		return freeObject;
	}

	/// <summary>Which object to mark as free in the memory pool</summary>
	public void Free(T obj) {
		int indexOfObject = allObjects.IndexOf(obj);
#if FAIL_FAST
		if(indexOfObject < 0) { throw new System.Exception("woah, this isn't one of mine..."); }
		if(indexOfObject >= (allObjects.Count - freeObjectCount)) { throw new System.Exception("hey, you're freeing this twice..."); }
#endif
		freeObjectCount++;
		int beginningOfFreeList = allObjects.Count - freeObjectCount;
		allObjects[indexOfObject] = allObjects[beginningOfFreeList];
		allObjects[beginningOfFreeList] = obj;
		if(decommission != null) { decommission(obj); }
	}

	/// <summary>performs the given delegate on each object in the memory pool</summary>
	public void ForEach(DelegateCommission action) {
		for(int i = 0; i < allObjects.Count; ++i) { action(allObjects[i]); }
	}

	/// <summary>Destroys all objects in the pool, after deactivating each one.</summary>
	public void DeallocateAll() {
		ForEach((item) => decommission(item));
		if(typeof(T) == typeof(GameObject)) {
			ForEach((item) => {
				GameObject go = item as GameObject;
				Object.DestroyImmediate(go.GetComponent<MemoryPoolItem>());
			});
		}
		if(death != null) { ForEach((item) => death(item)); }
		allObjects.Clear();
	}
}

public class MemoryPoolItem : MonoBehaviour {
	private MemoryPool<GameObject> gameObjectPool;
	public MemoryPoolItem SetPool(MemoryPool<GameObject> pool) { gameObjectPool = pool; return this; }
	static private bool shuttingDown = false;
	static public void SetShutdown(bool sceneIsEnding) { shuttingDown = sceneIsEnding; }
#if FAIL_FAST
	void OnApplicationQuit() { SetShutdown(true); }
	void Start() { SetShutdown(false); }
	void OnDestroy() {
		if(!shuttingDown) throw new System.Exception("Instead of Object.Destroy(" + gameObject + "), call MemoryPoolItem.Destroy(" + gameObject + ")\n"
			+ "When changing levels, call MemoryPoolItem.SetShutdown(true) first");
	}
#endif
	public void FreeSelf() { gameObjectPool.Free(gameObject); }
	/// <summary>If the given GameObject belongs to a memory pool, mark it as free in that pool. Otherwise, Object.Destroy()</summary>
	static public void Destroy(GameObject go) {
		MemoryPoolItem i = go.GetComponent<MemoryPoolItem>();
		if(i != null) { i.FreeSelf(); } else { Object.Destroy(go); }
	}
}