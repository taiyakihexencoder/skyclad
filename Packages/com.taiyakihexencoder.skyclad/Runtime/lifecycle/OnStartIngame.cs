using System.Threading.Tasks;

namespace skyclad.lifecycle {

	/// <summary>
	/// インゲーム開始処理
	/// </summary>
	internal static class OnStartIngame {
		internal static void StartIngame() {
			UnityEngine.Debug.Log("Start Ingame.");

			SkycladDataTables.OnStartIngame();
			SkycladWorld.Start();

			// WaitEnd(5000);
		}

		/// <summary>
		/// 仮置きの時限終了
		/// </summary>
		private static void WaitEnd(int milliseconds) {
			Task.Run(async () => {
				await Task.Delay(milliseconds);
				SkycladUtility.Async.Post(
					() => {
						OnEndIngame.EndIngame();
					}
				);
			});
		}
	}
}