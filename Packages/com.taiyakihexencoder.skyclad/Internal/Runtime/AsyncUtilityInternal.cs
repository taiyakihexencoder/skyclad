using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace skyclad.internalProc {
	public static class AsyncUtilityInternal {
		private static SynchronizationContext _mainContext;
		public static void Setup() {
			_mainContext = SynchronizationContext.Current;
		}

		public static T Send<T>(System.Func<T> function) {
			T value = default;
			_mainContext.Send((_) => value = function(), null);
			return value;
		}

		public static void Send(System.Action action) {
			_mainContext.Send((_) => action(), null);
		}

		public static void Post(System.Action action) {
			_mainContext.Post((_) => action(), null);
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void Log(object log) {
			_mainContext.Post((_) => Debug.Log(log), null);
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void LogError(object log) {
			_mainContext.Post((_) => Debug.LogError(log), null);
		}

		public static CancellationTokenSource InvokeCancellable(int milliseconds, System.Action action) {
			CancellationTokenSource tokenSource = new CancellationTokenSource();
			CancellationToken token = tokenSource.Token;
			Task task = Task.Run(
				async () => {
					await Task.Delay(milliseconds);
					if (!token.IsCancellationRequested) {
						Post(action);
					}
				},
				token
			);
			return tokenSource;
		}
	}
}