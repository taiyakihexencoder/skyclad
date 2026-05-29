using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace skyclad {
	using internalProc;
	public static partial class SkycladUtility {
		public static class Async {
			[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
			private static void SetupMainContext() {
				AsyncUtilityInternal.Setup();
			}

			public static T Send<T>(System.Func<T> function)
				=> AsyncUtilityInternal.Send(function);

			public static void Send(System.Action action)
				=> AsyncUtilityInternal.Send(action);

			public static void Post(System.Action action)
				=> AsyncUtilityInternal.Post(action);

			public static void Log(object log)
				=> AsyncUtilityInternal.Log(log);

			public static void LogError(object log)
				=> AsyncUtilityInternal.LogError(log);

			/// <summary>
			/// 指定した秒数待機してから実行する。
			/// 
			/// 戻り値のCancellationTokenSourceのCancel()を呼び出すことで処理をキャンセルできる。
			/// </summary>
			public static CancellationTokenSource InvokeCancellable(int milliseconds, System.Action action)
				=> AsyncUtilityInternal.InvokeCancellable(milliseconds, action);
		}
	}
}