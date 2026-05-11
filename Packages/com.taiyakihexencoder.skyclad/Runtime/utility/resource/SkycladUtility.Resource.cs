using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace skyclad {
	public static partial class SkycladUtility {
		public static class Resource {
			private static Dictionary<string, IResourceHolder> _resources = null;

			static Resource() {
				_resources = new Dictionary<string, IResourceHolder>();
			}

			/// <summary>
			/// リソース読み込み
			/// 重複する場合はすでに読み込んであるものを渡す
			/// </summary>
			/// <typeparam name="T"></typeparam>
			/// <param name="address"></param>
			/// <returns></returns>
			/// <exception cref="System.Exception"></exception>
			public static async Task<T> Load<T>(string address) where T : class {
				if (!_resources.TryGetValue(address, out IResourceHolder holder)) {
					AsyncOperationHandle<T> op = default;
					SkycladUtility.Async.Send(
						() =>{
							op = Addressables.LoadAssetAsync<T>(address);
							holder = new ResourceHolder<T>(op);
							_resources.Add(address, holder);
						}
					);
				}

				object res = await holder.Resource;
				if (res is T resource) {
					return resource;
				} else {
					throw new System.Exception($"resource type is not {typeof(T).Name}. :{res.GetType().Name}");
				}
			}

			/// <summary>
			/// リソースのアンロード
			/// </summary>
			/// <param name="address"></param>
			public static void Unload(string address) {
				if (_resources.TryGetValue(address, out IResourceHolder resourceHolder)) {
					resourceHolder.Dispose();
					_resources.Remove(address);
				} else {
					Debug.LogWarning($"Address:{address} not loaded.");
				}
			}

			/// <summary>
			/// すべてのリソースをアンロード
			/// </summary>
			internal static void UnloadAll() {
				foreach(IResourceHolder resourceHolder in _resources.Values) {
					resourceHolder.Dispose();
				}
				_resources.Clear();
			}

			private sealed class ResourceHolder<T> : IResourceHolder where T : class {
				private AsyncOperationHandle<T> _handle;

				Task<object> IResourceHolder.Resource {
					get {
						return Task.Run<object>(async () => await _handle.Task);
					}
				}

				internal ResourceHolder(AsyncOperationHandle<T> handle) {
					_handle = handle;
				}

				void IResourceHolder.Dispose() {
					if (_handle.IsValid()) {
						Addressables.Release(_handle);
					}
				}
			}
		}

	}
}