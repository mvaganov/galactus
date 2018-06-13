#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class ScriptableObjectUtility {
	/// <summary>
	//	This makes it easy to create, name and place unique new ScriptableObject asset files.
	/// </summary>
	public static T CreateAsset<T> () where T : ScriptableObject { return CreateAsset(typeof(T)) as T; }
	public static ScriptableObject CreateAsset (System.Type t, string filename = "", string path = "")
	{
		ScriptableObject asset = ScriptableObject.CreateInstance (t);
		string whereItWasSaved = SaveScriptableObjectAsAsset (asset, filename, path);
		asset = Resources.Load(whereItWasSaved, t) as ScriptableObject;
		return asset;
	}

	public static string SaveScriptableObjectAsAsset(ScriptableObject asset, string filename = "", string path = "") {
		System.Type t = asset.GetType ();
		if(path == "") {
			path = AssetDatabase.GetAssetPath (Selection.activeObject);
			if (path == "") {
				path = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;//"Assets";
				Debug.Log(path);
				int idx = path.LastIndexOf("/");
				if(idx < 0) {
					path = "Assets";
				} else {
					path = path.Substring(0, idx);
					if(filename == "") {
						string typename = t.ToString();
						int idx2 = typename.LastIndexOf(".");
						if(idx > 0) { typename = typename.Substring(idx2); }
						filename = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + typename + ".asset";
					}
					Debug.Log(path+" //// "+filename);
				}
//				Debug.Log(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
			} else if (System.IO.Path.GetExtension (path) != "") {
				path = path.Replace (System.IO.Path.GetFileName (AssetDatabase.GetAssetPath (Selection.activeObject)), "");
			}
		}
		if(filename.Length == 0) { filename = "New " + t.ToString() + ".asset"; }
		string fullpath = path+"/"+filename;
		string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath (fullpath);
		AssetDatabase.CreateAsset (asset, assetPathAndName);
		AssetDatabase.SaveAssets ();
		AssetDatabase.Refresh();
		EditorUtility.FocusProjectWindow ();
		Selection.activeObject = asset;
		Debug.Log("saved "+fullpath);
		return fullpath;
	}
}
#endif