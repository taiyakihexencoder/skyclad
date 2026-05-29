using System.Threading.Tasks;

namespace skyclad {
	using internalProc;
	public static partial class SkycladUtility {
		public static class Resource {
			/// <summary>
			/// リソース読み込み
			/// 重複する場合はすでに読み込んであるものを渡す
			/// </summary>
			/// <typeparam name="T"></typeparam>
			/// <param name="address"></param>
			/// <returns></returns>
			/// <exception cref="System.Exception"></exception>
			public static async Task<T> Load<T>(string address) where T : class {
				return await ResourceUtilityInternal.Load<T>(address);
			}

			/// <summary>
			/// リソースのアンロード
			/// </summary>
			/// <param name="address"></param>
			public static void Unload(string address) {
				ResourceUtilityInternal.Unload(address);
			}

			/// <summary>
			/// すべてのリソースをアンロード
			/// </summary>
			internal static void UnloadAll() {
				ResourceUtilityInternal.UnloadAll();
			}
		}

	}
}