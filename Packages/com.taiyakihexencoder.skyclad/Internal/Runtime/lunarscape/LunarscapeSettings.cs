using System.IO;
using skyclad.internalProc;
using UnityEngine;

namespace skyclad.lunarscape.internalProc {
	public sealed class LunarscapeSettings : OnMemoryScriptable<LunarscapeSettings> {
		private char sep => Path.DirectorySeparatorChar;
		protected override string assetPath => $"Skyclad{sep}auto-generated{sep}res{sep}lunarscape{sep}LunarscapeSettings.asset";

		[System.Serializable]
		public class _Field {
			[SerializeField]
			private float _loadFieldDistance = 20.0f;
			public float LoadFieldDistance => _loadFieldDistance;

			[SerializeField]
			private float _unloadFieldDistance = 40.0f;
			public float UnloadFieldDistance => _unloadFieldDistance;

			[SerializeField]
			private int _fieldMeshCacheSize = 10;
			public int FieldMeshCacheSize => _fieldMeshCacheSize;
		}
		
		[SerializeField]
		private _Field _field = new _Field();
		public _Field Field => _field;
	}
}