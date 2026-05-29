using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	[CustomPropertyDrawer(typeof(ResourceAttribute))]
	public sealed class ResourcePropertyDrawer : PropertyDrawer {
		private bool initialized = false;
		private string[] selectionList;
		private string[] addressList;

		private void Init(bool isString) {
			initialized = true;
			ResourceAttribute attr = attribute as ResourceAttribute;
			if (isString) {
				List<string> candidateList = attr.path == null 
					? SkycladEditorUtility.Resource.GetAddressList()
					: SkycladEditorUtility.Resource.GetAddressList(attr.path);

				addressList = candidateList.ToArray();
			} else {
				Dictionary<string, string> candidateList = attr.path == null
					? SkycladEditorUtility.Resource.GetAddressAndGuidList()
					: SkycladEditorUtility.Resource.GetAddressAndGuidList(attr.path);

				selectionList = new List<string>(candidateList.Keys).ToArray();
				addressList = new List<string>(candidateList.Values).ToArray();
			}
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			if (!initialized) {
				Init(property.propertyType == SerializedPropertyType.String);
			}

			if (property.propertyType == SerializedPropertyType.String) {
				property.stringValue = StringSelector(position, property.stringValue, label);
			} else {
				AssetReferenceSelector(position, property, label);
			}
		}

		private string StringSelector(Rect position, string current, GUIContent label) {
			int selectedIndex = -1;
			for(int i = 0; i < addressList.Length; ++i) {
				if (addressList[i] == current) {
					selectedIndex = i;
					break;
				}
			}

			selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, addressList);
			return selectedIndex >= 0 ? addressList[selectedIndex] : current;
		}

		private void AssetReferenceSelector(Rect position, SerializedProperty property, GUIContent label) {
			int selectedIndex = -1;
			string selected = property.Of("m_AssetGUID").stringValue;
			for(int i = 0; i < selectionList.Length; ++i) {
				if (selected == selectionList[i]) {
					selectedIndex = i;
					break;
				}
			}

			selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, addressList);
			if (selectedIndex >= 0) {
				property.Of("m_AssetGUID").stringValue = selectionList[selectedIndex];
			}
		}
	}
}