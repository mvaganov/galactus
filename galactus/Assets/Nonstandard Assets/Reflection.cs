using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

namespace NS {

	public static class Reflection {
		public static System.Type[] GetTypesInNamespace(string nameSpace, bool includeComponentTypes = false, System.Reflection.Assembly assembly = null) {
			if(assembly == null) {
				assembly = System.Reflection.Assembly.GetExecutingAssembly();
			}
			System.Type[] types = assembly.GetTypes().Where(t =>
				System.String.Equals(t.Namespace, nameSpace, System.StringComparison.Ordinal)
				&& (includeComponentTypes || !t.ToString().Contains('+'))).ToArray();
			return types;
		}
		public static string CleanFront(string str, string trimMe) {
			if(str.StartsWith(trimMe)) { return str.Substring(trimMe.Length); }
			return str;
		}
		public static List<string> TypeNamesCleaned(System.Type[] validTypes, string namespaceToClean) {
			List<string> list = new List<string>();
			for(int i = 0; i < validTypes.Length; ++i) {
				string typename = validTypes[i].ToString();
				typename = CleanFront(typename, namespaceToClean + ".");
				list.Add(typename);
			}
			return list;
		}

		public static T EditorGUI_EnumPopup<T>(Rect _position, T value) {
			System.Type t = typeof(T);
			if(t.IsEnum) {
				string[] names = System.Enum.GetNames(t);
				string thisone = value.ToString();
				int index = System.Array.IndexOf(names, thisone);
				index = EditorGUI.Popup(_position, index, names);
				value = (T)System.Enum.Parse(t, names[index]);
			}
			return value;
		}

	}

}