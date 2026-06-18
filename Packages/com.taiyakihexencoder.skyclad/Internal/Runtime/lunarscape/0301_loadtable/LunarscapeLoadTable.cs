using System.Threading.Tasks;
using UnityEngine;

namespace skyclad.lunarscape.internalProc {
	using skyclad.internalProc;
	public sealed class LunarscapeLoadTable : ScriptableObject {
		public const string resourceAddress = "lunarscape/LunarscapeLoadTable";
		public const string GLOBAL_GUID = "global";

		[System.Serializable]
		public class LoadGroup {
			public string fieldGuid;
			public AvatarSpawn[] avatarSpawns;
		}

		[System.Serializable]
		public struct AvatarSpawn {
			public int avatarId;
			public Vector3 position;
			public Quaternion rotation;
		}

		[SerializeField]
		private LoadGroup[] _groups = new LoadGroup[0];
		public LoadGroup[] Groups => _groups;

		internal async Task Load(string groupGuid, System.Func<LoadGroup, Task> action) {
			LoadGroup targetGroup = null;

			foreach(LoadGroup group in _groups) {
				if (group.fieldGuid == GLOBAL_GUID) {
					targetGroup = group;
					break;
				}
			}

			if (targetGroup != null) {
				await action(targetGroup);
			} else {
				D.LogW("LoadGroup not found.");
			}

		}
	}
}