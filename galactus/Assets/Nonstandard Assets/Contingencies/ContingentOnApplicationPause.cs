namespace NS.Contingency {
	public class ContingentOnApplicationPause : ContingentScript {
		void OnApplicationPause(bool pauseStatus) {
			if (pauseStatus == true) { DoActivateTrigger (); }
		} 
		void Start() {
			NS.F.EquateUnityEditorPauseWithApplicationPause (OnApplicationPause);
		}
	}
}