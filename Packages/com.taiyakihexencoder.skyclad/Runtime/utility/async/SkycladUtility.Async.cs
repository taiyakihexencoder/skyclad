using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace skyclad {
	public static partial class SkycladUtility {
		public static class Async {
			private static SynchronizationContext _mainContext;

			[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
			private static void SetupMainContext() {
				_mainContext = SynchronizationContext.Current;
			}

			public static T Send<T>(System.Func<T> function) {
				T value = default;
				_mainContext.Send(
					(_) => value = function(), 
					null
				);
				return value;
			}

			public static void Send(System.Action action) {
				_mainContext.Send((_) => action(), null);
			}

			public static void Post(System.Action action) {
				_mainContext.Post((_) => action(), null);
			}

			public static void Log(object log) {
				_mainContext.Post((_) => Debug.Log(log), null);
			}

			public static void LogError(object log) {
				_mainContext.Post((_) => Debug.LogError(log), null);
			}

			/// <summary>
			/// 指定した秒数待機してから実行する。
			/// 
			/// 戻り値のCancellationTokenSourceのCancel()を呼び出すことで処理をキャンセルできる。
			/// </summary>
			/// <param name="milliseconds"></param>
			/// <param name="action"></param>
			/// <returns></returns>
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
}