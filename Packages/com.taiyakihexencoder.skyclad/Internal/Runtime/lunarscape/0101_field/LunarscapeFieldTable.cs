using UnityEngine;

namespace skyclad.lunarscape.internalProc {
	public class LunarscapeFieldTable : ScriptableObject {
		public const string RES_ADDRESS = "lunarscape/field_table";

		[System.Serializable]
		internal class Row {
			public int id;
			public string address;
			public string name;

			// メッシュ編集ScriptableObjectのguid
			public string guid;

			public Vector3 position;
			public Quaternion rotation;
			public Vector3 boundsMin;
			public Vector3 boundsMax;
		}

		[SerializeField]
		private Row[] _rows;
		internal Row[] Rows => _rows;
	}
}