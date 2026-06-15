using UnityEngine;

namespace skyclad.lunarscape.internalProc {
	public sealed class LunarscapeAvatarTable : ScriptableObject {
		public const string resourceAddress = "lunarscape/LunarscapeAvatarTable";

		[System.Serializable]
		public struct AvatarSettings {
			public int id;
			public string name;
			public int collider;
		}

		[SerializeField]
		private AvatarSettings[] _settings = new AvatarSettings[0];

		public AvatarSettings[] Settings => _settings;
	}
}