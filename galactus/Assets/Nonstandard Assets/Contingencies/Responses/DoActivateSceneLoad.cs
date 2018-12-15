using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace NS.Contingency.Response {
	public class DoActivateSceneLoad : _NS.Contingency.Response.DoActivateBasedOnContingency {
		public string sceneName;
		public enum SceneLoadType {LoadScene, AddScene, RemoveScene};
		public SceneLoadType loadType = SceneLoadType.LoadScene;
		public ObjectPtr activateWhenDone;
		public void DoActivateTrigger () { DoActivateTrigger(null); }
		public void DoActivateTrigger (object whatTriggeredThis) {
			DoLoad(whatTriggeredThis, loadType, true);
		}
		public void DoLoad (object whatTriggeredThis, SceneLoadType loadType, bool activating = true) {
			AsyncOperation op = null;
			switch(loadType) {
			case SceneLoadType.LoadScene:
				op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
				break;
			case SceneLoadType.AddScene:
				op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
				break;
			case SceneLoadType.RemoveScene:
				op = SceneManager.UnloadSceneAsync(sceneName);
				break;
			}
			if(activateWhenDone.Data != null) {
				op.completed += (AsyncOperation a)=> {
					NS.ActivateAnything.DoActivate(activateWhenDone, whatTriggeredThis, this, activating);
				};
			}
		}
		public void DoDeactivateTrigger(object whatTriggeredThis) {
			DoLoad(whatTriggeredThis, loadType, false);
		}
		#if UNITY_EDITOR
		public override Object DoGUI(Rect _position, Object obj, Component self, PropertyDrawer_ObjectPtr p) {
			obj = PropertyDrawer_ObjectPtr.DoGUIEnumLabeledString<SceneLoadType>(
				_position, obj, self, p, ref loadType, ref sceneName);
			return obj;
		}
		#endif
	}
}
