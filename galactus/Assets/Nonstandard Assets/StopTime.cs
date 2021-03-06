﻿using UnityEngine;

// A utility script to pause and unpause active elements in a Unity game
// author: mvaganov@hotmail.com
// license: Copyfree, public domain. This is free code! Great artists, steal this code!
// latest version at: https://pastebin.com/raw/a79HvqbQ
// designed to work with Timer: https://pastebin.com/raw/h61nAC3E
public class StopTime : MonoBehaviour {
	private interface IUnfreezable {
		void Unfreeze();
		bool IsUnfreezable();
		object GetFrozen();
	}
	private class StasisPhysics : IUnfreezable {
		public Rigidbody rb;
		public Vector3 v, av;
		public bool useGravity, isKinematic;
		public StasisPhysics(Rigidbody rb){
			this.rb = rb;
			v = rb.velocity;
			av = rb.angularVelocity;
			useGravity = rb.useGravity;
			isKinematic = rb.isKinematic;
			rb.velocity = rb.angularVelocity = Vector3.zero;
			rb.isKinematic=true;
			rb.useGravity = false;
		}
		public bool IsUnfreezable() { return rb != null; }
		public object GetFrozen() { return rb; }
		public void Unfreeze() {
			rb.velocity = v;
			rb.angularVelocity = av;
			rb.useGravity = useGravity;
			rb.isKinematic = isKinematic;
		}
	}
	private class StasisAnimation : IUnfreezable {
		public Animation a;
		public float speed;
		public StasisAnimation(Animation a){
			this.a = a;
			speed = a[a.clip.name].speed;
			a[a.clip.name].speed = 0;
		}
		public bool IsUnfreezable() { return a != null; }
		public object GetFrozen() { return a; }
		public void Unfreeze() { a[a.clip.name].speed = speed; }
	}
	private class StasisParticle : IUnfreezable {
		public ParticleSystem ps;
		public StasisParticle(ParticleSystem ps){ this.ps = ps; ps.Pause(); }
		public bool IsUnfreezable() { return ps != null; }
		public object GetFrozen() { return ps; }
		public void Unfreeze() { ps.Play(); }
	}
	private class StasisAudioSource : IUnfreezable {
		public AudioSource asrc;
		public StasisAudioSource(AudioSource asrc){ this.asrc = asrc; asrc.Pause(); }
		public bool IsUnfreezable() { return asrc != null; }
		public object GetFrozen() { return asrc; }
		public void Unfreeze() { asrc.Play(); }
	}

	/// <summary>list of frozen things, saved before the objects are halted.</summary>
	private static IUnfreezable[] snapshot = null;

	/// <returns><c>true</c> if the physics is frozen; otherwise, <c>false</c>.</returns>
	public static bool IsStopped() { return snapshot != null; }

	public void ToggleTime() { Toggle (); }

	private static void SetChronoPaused(bool paused) {
		object chrono_t = GetType("NS.Chrono");
		if(chrono_t != null) {
			System.Type t = chrono_t as System.Type;
			System.Reflection.MethodInfo m = t.GetMethod("Instance");
			object chrono = m.Invoke(null, new object[0]);

			NS.Chrono.Instance().paused = paused;
		}
	}

	public void disableTime() {
		SetupIfNeeded();
		whenDeactivates.Invoke ();
		DisableTime ();
	}
	public static void DisableTime() {
		SetChronoPaused(true);
		Rigidbody[] bodies = FindObjectsOfType<Rigidbody> ();
		Animation[] anims = FindObjectsOfType<Animation> ();
		ParticleSystem[] particles = FindObjectsOfType<ParticleSystem>();
		AudioSource[] audios = FindObjectsOfType<AudioSource>();
		snapshot = new IUnfreezable[bodies.Length+anims.Length+ particles.Length+audios.Length];
		int index = 0;
		System.Array.ForEach(bodies, o => snapshot[index++] = new StasisPhysics(o));
		System.Array.ForEach(anims, o => snapshot[index++] = new StasisAnimation(o));
		System.Array.ForEach(particles, o => snapshot[index++] = new StasisParticle(o));
		System.Array.ForEach(audios, o => snapshot[index++] = new StasisAudioSource(o));
	}
	public void enableTime() {
		SetupIfNeeded ();
		whenActivates.Invoke ();
		EnableTime ();
	}
	public static void EnableTime() {
		SetChronoPaused(false);
		System.Array.ForEach(snapshot, (o) => { if(o.IsUnfreezable()) { o.Unfreeze(); } });
		snapshot = null;
	}

	/// Toggles time.
	public static void Toggle() {
		if (IsStopped ()) { EnableTime (); } else { DisableTime (); }
	}
	#region find-and-disable classes
	public static System.Type GetType( string typeName ) {
		// Try Type.GetType() first. This will work with types defined by the Mono runtime, in the same assembly as the caller, etc.
		System.Type type = System.Type.GetType( typeName );
		// If it worked, then we're done here
		if( type != null ) return type;
		// If we still haven't found the proper type, we can enumerate all of the loaded assemblies and see if any of them define the type
		try{
			var currentAssembly = System.Reflection.Assembly.GetExecutingAssembly();
			var referencedAssemblies = currentAssembly.GetReferencedAssemblies();
			foreach( var assemblyName in referencedAssemblies ) {
				// Load the referenced assembly
				var assembly = System.Reflection.Assembly.Load( assemblyName );
				if( assembly != null ) {
					// See if that assembly defines the named type
					type = assembly.GetType( typeName );
					if( type != null ) return type;
				}
			}
		} catch (System.Exception){ }
		// if the GetExecutingAssembly library call failed, possibly due to security constraints, try each known assembly individually
		System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies ();
		for (int i = 0; i < assemblies.Length; ++i) {
			type = assemblies [i].GetType (typeName);
			if (type != null) return type;
		}
		// If the TypeName is a full name in an un-loaded assembly, then we can try loading the defining assembly directly
		if( typeName.Contains( "." ) ) {
			try{
				// Get the name of the assembly (Assumption is that we are using fully-qualified type names)
				var assemblyName = typeName.Substring( 0, typeName.IndexOf( '.' ) );
				// Attempt to load the indicated Assembly
				var assembly = System.Reflection.Assembly.Load( assemblyName );
				if( assembly == null ) return null;
				// Ask that assembly to return the proper Type
				type = assembly.GetType( typeName );
				if( type != null ) return type;
			}catch(System.Exception ){}
		}
		// The type just couldn't be found...
		return null;
	}

	[ContextMenuItem("connect to Timer","ConnectToTimer"), Tooltip("Will search for a MonoBehaviours with the given complete name to disable.")]
	public string[] classTypesToToggle = new string[] {
		"UnityStandardAssets.Characters.FirstPerson.FirstPersonController",
		"UnityStandardAssets.Characters.FirstPerson.RigidbodyFirstPersonController"
	};
	private System.Type[] knownTypes;
	[Tooltip("When time re-activates.")]
	public UnityEngine.Events.UnityEvent whenActivates;
	[Tooltip("When time de-activates.")]
	public UnityEngine.Events.UnityEvent whenDeactivates;
	private bool connectedToTimer = false;

	private void ConnectToTimer() {
		NS.Chrono c = NS.Chrono.Instance();
		if(c.onPause == null) c.onPause = new UnityEngine.Events.UnityEvent();
		if(c.onUnpause == null) c.onUnpause = new UnityEngine.Events.UnityEvent();
		c.onPause.AddListener(disableTime);
		c.onUnpause.AddListener(enableTime);
		connectedToTimer = true;
	}

	private void SetupIfNeeded() {
		if (knownTypes == null) { Setup (); }
	}
	private void Start() { SetupIfNeeded(); }
	private void Setup () {
		knownTypes = null;
		for (int i = 0; i < classTypesToToggle.Length; ++i) {
			System.Type t = GetType(classTypesToToggle[i]);
			if (t != null) {
				if (knownTypes == null) {
					knownTypes = new System.Type[classTypesToToggle.Length];
				}
				knownTypes [i] = t;
			}
		}
		if (knownTypes != null) {
			whenActivates.AddListener(()=>{SetEnabled (true);});
			whenDeactivates.AddListener(()=>{SetEnabled (false);});
		}
		if(!connectedToTimer) { ConnectToTimer(); }
	}
	private void SetEnabledAllBehavioursOfType(System.Type type, bool a_enabled) {
		if (type == null) return;
		Object[] objs = FindObjectsOfType (type);
		for (int o = 0; o < objs .Length; ++o) {
			Behaviour b = objs [o] as Behaviour;
			if (b != null) {
				b.enabled = a_enabled;
			}
		}
	}
	public static void SetEnableMouse(bool a_enabled) {
		Cursor.visible = !a_enabled;
		Cursor.lockState = a_enabled ? CursorLockMode.Locked : CursorLockMode.None;
	}
	private void SetEnabled(bool a_enabled) {
		if (knownTypes != null) {
			for (int i = 0; i < knownTypes.Length; ++i) {
				if (knownTypes [i] != null) {
					SetEnabledAllBehavioursOfType (knownTypes [i], a_enabled);
				}
			}
		}
		SetEnableMouse (a_enabled);
	}
	#endregion // find-and-disable classes
}