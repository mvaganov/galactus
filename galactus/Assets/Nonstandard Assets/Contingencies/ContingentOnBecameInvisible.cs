namespace NS.Contingency {
	public class ContingentOnBecameInvisible : ContingentScript {
		void OnBecameInvisible() {
			DoActivateTrigger();
		}
	}
}