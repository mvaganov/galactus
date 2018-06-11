namespace NS.Contingency {
	public class ContingentOnBecameVisible : ContingentScript {
		void OnBecameVisible() {
			DoActivateTrigger();
		}
	}
}