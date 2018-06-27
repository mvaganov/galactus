namespace NS.Contingency {
	public class ContingentOnDestroy : ContingentScript {
		private bool appQuitting = false;
		void OnApplicationQuit() {
			appQuitting = true;
		}
		void OnDestroy() {
			if(!appQuitting) DoActivateTrigger();
		}
	}
}