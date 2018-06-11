using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace NS.Contingency.Response {
	public class DoActivateSceneLoad : _NS.Contingency.Response.DoActivateBasedOnContingency {
		public string sceneName;
		[System.Serializable]
		public struct SceneLoadSettings {
			public bool additive;
			public EditorGUIObjectReference activateWhenDone;
		}
		public SceneLoadSettings settings;
		public void DoActivateTrigger () { DoActivateTrigger(null); }
		public void DoActivateTrigger (object whatTriggeredThis) {
			AsyncOperation op = SceneManager.LoadSceneAsync(sceneName,
				settings.additive?LoadSceneMode.Additive:LoadSceneMode.Single);
			if(settings.activateWhenDone.data != null) {
				op.completed += (AsyncOperation a)=> {
					NS.F.DoActivate(settings.activateWhenDone, whatTriggeredThis, this, true);
				};
			}
		}
		public void DoDeactivateTrigger(object whatTriggeredThis) {
			AsyncOperation op = SceneManager.UnloadSceneAsync(sceneName);
			if(settings.activateWhenDone.data != null) {
				op.completed += (AsyncOperation a)=> {
					NS.F.DoActivate(settings.activateWhenDone, whatTriggeredThis, this, false);
				};
			}
		}
	}
}
