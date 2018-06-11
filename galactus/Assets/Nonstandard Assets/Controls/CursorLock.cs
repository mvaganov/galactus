using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// author: mvaganov@hotmail.com
// license: Copyfree, public domain. This is free code! Great artists, steal this code!
// latest version at: https://pastebin.com/raw/zqd0yK40
namespace NS {
	public class CursorLock : MonoBehaviour {
		void FixedUpdate() {
			LockUpdate();
		}
		// whether cursor is visible or not
		private static bool m_cursorIsLocked = false;
		public static void LockUpdate() {
			if(Input.GetKeyUp(KeyCode.Escape)) {
				m_cursorIsLocked = false;
			} else if(Input.GetMouseButtonUp(0)) {
				m_cursorIsLocked = true;
			}

			if (m_cursorIsLocked) {
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			} else if (!m_cursorIsLocked) {
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
		}
	}
}