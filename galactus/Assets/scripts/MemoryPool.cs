// MIT license - TL;DR - Do what you want, this code is free!
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
	private List<T> allObjects = new List<T>();
	private int freeObjectCount = 0;

	public int Count() {
		return allObjects.Count - freeObjectCount;
	}

	public int FreeCoutn() {
		return freeObjectCount;
	}

	public delegate T DelegateAllocate();
	public delegate void DelegateStartUse(T obj);
	public delegate void DelegateEndUse(T obj);
	public delegate void DelegateDeallocate(T obj);

	private DelegateAllocate allocate;
	private DelegateDeallocate deallocate;
	private DelegateStartUse startUse;
	private DelegateEndUse endUse;

	public List<T> GetAllObjects() { return allObjects; }

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
	public void Setup(DelegateAllocate create, DelegateStartUse activate, DelegateEndUse deactivate, DelegateDeallocate destroy) {
		allocate = create; startUse = activate; endUse = deactivate; deallocate = destroy;
	}

	/// <summary>Constructs and calls <see cref="Setup"/></summary>
	public MemoryPool(DelegateAllocate create, DelegateStartUse activate, DelegateEndUse deactivate, DelegateDeallocate destroy) {
		Setup(create, activate, deactivate, destroy);
	}

	/// <summary> Be sure to call <see cref="Setup"/>!</summary>
	public MemoryPool() { }

	public T this[int index] {
		get { return allObjects [index]; }
		set { allObjects [index] = value; }
	}

	/// <summary>Returns an object from the memory pool, which may have just been created</summary>
	public T Alloc() {
		T freeObject = null;
		if(freeObjectCount == 0) {
#if FAIL_FAST
			if(allocate == null) { throw new System.Exception("Call .Setup(), and provide a create method!"); }
#endif
			freeObject = allocate();
			allObjects.Add(freeObject);
			if(typeof(T) == typeof(GameObject)) {
				GameObject go = freeObject as GameObject;
				go.AddComponent<MemoryPoolItem>().SetPool(this as MemoryPool<GameObject>);
			}
		} else {
			freeObject = allObjects[allObjects.Count - freeObjectCount];
			freeObjectCount--;
		}
		if(startUse != null) { startUse(freeObject); }
		return freeObject;
	}

	/// <summary>Which object to mark as free in the memory pool</summary>
	public void Free(T obj) {
		int indexOfObject = allObjects.IndexOf(obj);
#if FAIL_FAST
		if(indexOfObject < 0) { throw new System.Exception("woah, this isn't one of mine..."); }
		if(indexOfObject >= (allObjects.Count - freeObjectCount)) { throw new System.Exception("hey, you're freeing this twice..."+obj); }
#endif
		freeObjectCount++;
		int beginningOfFreeList = allObjects.Count - freeObjectCount;
		allObjects[indexOfObject] = allObjects[beginningOfFreeList];
		allObjects[beginningOfFreeList] = obj;
		if(endUse != null) { endUse(obj); }
	}

	public void FreeAll() {
		ForEach((item) => endUse(item));
		freeObjectCount = Count();
	}

	/// <summary>performs the given delegate on each object in the memory pool</summary>
	public void ForEach(DelegateStartUse action) {
		for(int i = 0; i < allObjects.Count; ++i) { action(allObjects[i]); }
	}

	/// <summary>Destroys all objects in the pool, after deactivating each one.</summary>
	public void DeallocateAll() {
		ForEach((item) => endUse(item));
		if(typeof(T) == typeof(GameObject)) {
			ForEach((item) => {
				GameObject go = item as GameObject;
				Object.DestroyImmediate(go.GetComponent<MemoryPoolItem>());
			});
		}
		if(deallocate != null) { ForEach((item) => deallocate(item)); }
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
	public void FreeSelf() { if(gameObjectPool != null) gameObjectPool.Free(gameObject); }
	/// <summary>If the given GameObject belongs to a memory pool, mark it as free in that pool. Otherwise, Object.Destroy()</summary>
	static public void Destroy(GameObject go) {
        MemoryPoolRelease r = go.GetComponent<MemoryPoolRelease>();
        if (r) r.FireAndForget();
        MemoryPoolItem i = go.GetComponent<MemoryPoolItem>();
        if (i != null) { i.FreeSelf(); } else { Debug.LogWarning("destroying unmanaged object "+go); Object.Destroy(go); }
	}
	// TODO use this instead of MemoryPoolRelease, since MemoryPoolRelease doesn't really work without MemoryPoolItem
	public void AddOnDecommissionCode(MemoryPool<GameObject>.DelegateEndUse decommissionCode) {
		MemoryPoolRelease.Add (gameObject, decommissionCode);
	}
}

public class MemoryPoolRelease : MonoBehaviour
{
    public static void Add(GameObject obj, MemoryPool<GameObject>.DelegateEndUse decommissionCode) {
        MemoryPoolRelease mpr = obj.GetComponent<MemoryPoolRelease>();
        if(!mpr) mpr = obj.AddComponent<MemoryPoolRelease>();
        mpr.AddCallback(decommissionCode);
    }
    private List<MemoryPool<GameObject>.DelegateEndUse> destroyBehavior;
    private void AddCallback(MemoryPool<GameObject>.DelegateEndUse decommissionCode) {
        if(destroyBehavior == null) { destroyBehavior = new List<MemoryPool<GameObject>.DelegateEndUse>(); }
        destroyBehavior.Add(decommissionCode);
    }
    public void Fire() {
        if(destroyBehavior != null)
        for(int i = 0; i < destroyBehavior.Count; ++i) { destroyBehavior[i](gameObject); }
    }
    public void Forget() {
        if (destroyBehavior != null) destroyBehavior.Clear();
    }
    public void FireAndForget() { Fire(); Forget(); }
}

/// <summary>
/// Game object pool.
/// </summary>
//  TODO add methods to make this easy.
public class GameObjectPool : MemoryPool<GameObject> {
	public void Setup(GameObject prefab) {
		Setup( // TODO use MemoryPoolItem and MemoryPoolRelease in here...?
			()    => GameObject.Instantiate(prefab),
			(obj) => obj.SetActive(true),
			(obj) => obj.SetActive(false),
			(obj) => Object.Destroy(obj)
		);
	}
	public GameObjectPool(GameObject prefab) { Setup (prefab); }
}
