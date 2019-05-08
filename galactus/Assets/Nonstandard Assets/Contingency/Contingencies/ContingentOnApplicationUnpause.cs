namespace NS.Contingency {
	public class ContingentOnApplicationUnpause : ContingentScript {
		void OnApplicationPause(bool pauseStatus) {
			if (pauseStatus == false) { DoActivateTrigger (); }
		}
		void Start() {
			NS.Chrono.EquateUnityEditorPauseWithApplicationPause (OnApplicationPause);
		}
	}
}