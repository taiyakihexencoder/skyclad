using System.Threading.Tasks;

namespace skyclad.lunarscape.internalProc {
	using skyclad.internalProc;
	using UnityEngine;

	public static class LunarscapeLoadTableManager {
		private static LunarscapeLoadTable table = null;

		public static async Task Init() {
			await ResourceUtilityInternal.LoadTemp<LunarscapeLoadTable>(
				LunarscapeLoadTable.resourceAddress,
				loadTable => { 
					AsyncUtilityInternal.Send(() => {
						table = Object.Instantiate(loadTable); 
					});
				}
			);
		}

		public static async Task LoadGlobal(System.Func<LunarscapeLoadTable.LoadGroup, Task> task) {
			await table.Load(LunarscapeLoadTable.GLOBAL_GUID, task);
		}
	}
}