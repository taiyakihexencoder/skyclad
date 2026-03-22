namespace skyclad {
	/// <summary>
	/// コード記述を整理するためのusing
	/// </summary>
	public class Process : System.IDisposable {
#if UNITY_EDITOR
		private readonly string _title;
		private readonly int _nest;
		private bool disposedValue;
#endif

		public Process(string title = null) : this(title, 0){ }

		private Process(string title, int nest) {
#if UNITY_EDITOR
			_title = title;
			_nest = nest;
			if (title != null) {
				UnityEngine.Debug.Log($"{new string(' ', nest)}-- start process {title} --");
			}
#endif
		}

		public Process Child(string title = null) {
			return new Process(title, _nest+2);
		}

		void System.IDisposable.Dispose()
		{
#if UNITY_EDITOR
			if (_title != null) {
				UnityEngine.Debug.Log($"{new string(' ', _nest)}-- end process {_title} --");
			}
#endif
		}

	}
}