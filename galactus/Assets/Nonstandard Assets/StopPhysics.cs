using UnityEngine;

/// <summary>A utility script to pause and unpause active elements in a Unity game
/// <description>Public Domain!</description>
/// <author email="mvaganov@hotmail.com">Michael Vaganov</author>
public class StopPhysics : MonoBehaviour {
	private interface IUnfreezable {
		void Unfreeze();
		bool IsUnfreezable();
		object GetFrozen();
	}
	private struct StasisPhysics : IUnfreezable {
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
	private struct StasisAnimation : IUnfreezable {
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
	private struct StasisParticle : IUnfreezable {
		public ParticleSystem ps;
		public StasisParticle(ParticleSystem ps){ this.ps = ps; ps.Pause(); }
		public bool IsUnfreezable() { return ps != null; }
		public object GetFrozen() { return ps; }
		public void Unfreeze() { ps.Play(); }
	}

	/// <summary>list of frozen things, saved before the objects are halted.</summary>
	private static IUnfreezable[] snapshot = null;

	/// <returns><c>true</c> if the physics is frozen; otherwise, <c>false</c>.</returns>
	public static bool IsStopped() { return snapshot != null; }

	public void TogglePhysics() { Toggle (); }

	public void disablePhysics() {
		SetupIfNeeded ();
		whenDeactivates.Invoke ();
		DisablePhysics ();
	}
	public static void DisablePhysics() {
		Rigidbody[] bodies = FindObjectsOfType<Rigidbody> ();
		Animation[] anims = FindObjectsOfType<Animation> ();
		ParticleSystem[] particles = FindObjectsOfType<ParticleSystem>();
		snapshot = new IUnfreezable[bodies.Length+anims.Length+ particles.Length];
		int index = 0;
		System.Array.ForEach(bodies, o => snapshot[index++] = new StasisPhysics(o));
		System.Array.ForEach(anims, o => snapshot[index++] = new StasisAnimation(o));
		System.Array.ForEach(particles, o => snapshot[index++] = new StasisParticle(o));
	}
	public void enablePhysics() {
		SetupIfNeeded ();
		whenActivates.Invoke ();
		EnablePhysics ();
	}
	public static void EnablePhysics() {
		System.Array.ForEach(snapshot, (o) => { if(o.IsUnfreezable()) { o.Unfreeze(); } });
		snapshot = null;
	}

	/// <summary>Toggles the rigibdody physics.</summary>
	public static void Toggle() {
		if (IsStopped ()) { EnablePhysics (); } else { DisablePhysics (); }
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

	[Tooltip("Will search for a MonoBehaviours with the given complete name to disable.")]
	public string[] classTypesToToggle = new string[] {
		"UnityStandardAssets.Characters.FirstPerson.FirstPersonController",
		"UnityStandardAssets.Characters.FirstPerson.RigidbodyFirstPersonController"
	};
	private System.Type[] knownTypes;
	[Tooltip("When physics re-activates.")]
	public UnityEngine.Events.UnityEvent whenActivates;
	[Tooltip("When physics de-activates.")]
	public UnityEngine.Events.UnityEvent whenDeactivates;

	private void SetupIfNeeded() {
		if (knownTypes == null) { Setup (); }
	}
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