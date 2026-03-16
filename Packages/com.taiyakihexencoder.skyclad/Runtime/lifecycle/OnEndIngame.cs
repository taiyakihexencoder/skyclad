namespace skyclad.lifecycle {

	/// <summary>
	/// インゲーム終了処理
	/// </summary>
	internal static class OnEndIngame {
		internal static void EndIngame() {
			UnityEngine.Debug.Log("End Ingame.");

			SkycladWorld.End();

			SkycladDataTables.OnEndIngame();
		}
	}
}