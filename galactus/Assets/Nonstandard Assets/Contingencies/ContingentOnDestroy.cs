namespace NS.Contingency {
	public class ContingentOnDestroy : ContingentScript {
		void OnDestroy() {
			DoActivateTrigger();
		}
	}
}