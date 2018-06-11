namespace NS.Contingency {
	public class ContingentOnApplicationQuit : ContingentScript {
		void OnApplicationQuit() {
			DoActivateTrigger();
		}
	}
}