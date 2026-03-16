namespace skyclad {
	public static partial class DioramaId {
		/// <summary>
		/// SetLaunchDioramasでセットされる、
		/// 開始時に読み込まれるDiorama
		/// </summary>
		private static int[] _launchDioramas = null;
		internal static int[] LaunchDioramas {
			get {
				if (_launchDioramas == null) { SetLaunchDioramas(); }
				return _launchDioramas;
			}
		}

		static partial void SetLaunchDioramas();
	}
}