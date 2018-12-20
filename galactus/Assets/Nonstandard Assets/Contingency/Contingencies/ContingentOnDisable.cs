namespace NS.Contingency {
	public class ContingentOnDisable : ContingentScript {
		void OnDisable() {
			DoActivateTrigger();
		}
	}
}