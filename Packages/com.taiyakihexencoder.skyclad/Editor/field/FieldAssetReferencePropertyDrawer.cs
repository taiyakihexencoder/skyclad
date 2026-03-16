using System.Collections.Generic;
using skyclad.field;
using UnityEditor;
using UnityEngine;

namespace skyclad.editor {
	[CustomPropertyDrawer(typeof(FieldAssetReference))]
	internal class FieldAssetReferencePropertyDrawer : PropertyDrawer {
		private string[] guidList;
		private string[] addressList;
		private bool initialized;
		private void Init() {
			Dictionary<string, string> table = SkycladEditorUtility.Resource.GetAddressAndGuidList(typeof(FieldAsset));
			guidList = new List<string>(table.Keys).ToArray();
			addressList = new List<string>(table.Values).ToArray();
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			if (!initialized) {
				Init();
				initialized = true;
			}

			int selected = -1;
			SerializedProperty assetGUIDProperty = property.FindPropertyRelative("m_AssetGUID");
			string guid = assetGUIDProperty.stringValue;
			for(int i = 0; i < guidList.Length; ++i) {
				if (guidList[i] == guid) {
					selected = i;
					break;
				}
			}

			selected = EditorGUI.Popup(position, label.text, selected, addressList);

			if (0 <= selected && selected < guidList.Length) {
				assetGUIDProperty.stringValue = guidList[selected];
			}

		}
	}
}