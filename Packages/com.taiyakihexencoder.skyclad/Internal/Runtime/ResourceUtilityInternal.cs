using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace skyclad.internalProc {
	public static class ResourceUtilityInternal {
		private static Dictionary<string, IResourceHolder> _resources = null;
		static ResourceUtilityInternal() {
			_resources = new Dictionary<string, IResourceHolder>();
		}

		public static async Task<bool> Exists(string address) {
			AsyncOperationHandle<IList<IResourceLocation>> op = default;
			AsyncUtilityInternal.Send(() => {
				op = Addressables.LoadResourceLocationsAsync(address);
			});
			await op.Task;
			bool exists = op.Status == AsyncOperationStatus.Succeeded && op.Result.Count > 0;
			Addressables.Release(op);
			return exists;
		}

		/// <summary>
		/// 読込後にcallbackを実行し、即破棄する。
		/// その後フレームをまたいで保持したい場合はInstantiateを使い、
		/// 不要になったらDestroyで削除すること。
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="address"></param>
		/// <param name="callback"></param>
		/// <returns></returns>
		public static async Task LoadTemp<T>(
			string address,
			System.Action<T> callback
		) where T : class {
			AsyncOperationHandle<T> op = default;
			AsyncUtilityInternal.Send(
				() => {
					op = Addressables.LoadAssetAsync<T>(address);
				}
			);
			callback(await op.Task);
			Addressables.Release(op);
		}
		
		public static async Task<T> Load<T>(string address) where T : class {
			if (!_resources.TryGetValue(address, out IResourceHolder holder)) {
				AsyncOperationHandle<T> op = default;
				AsyncUtilityInternal.Send(
					() => {
						op = Addressables.LoadAssetAsync<T>(address);
						holder = new ResourceHolder<T>(op);
						_resources.Add(address, holder);
					}
				);
			} else {
				AsyncUtilityInternal.Post(
					() => { holder.IncrementReferenceCount(); }
				);
			}

			object res = await holder.Resource;
			if (res is T resource) {
				return resource;
			} else {
				throw new System.Exception($"resource type is not {typeof(T).Name}. :{res.GetType().Name}");
			}
		}

		public static async Task<T[]> LoadSubAssets<T>(string mainAddress, string[] subassets) where T : class {
			if (!_resources.TryGetValue(mainAddress, out IResourceHolder holder)) {
				AsyncOperationHandle<T>[] ops = new AsyncOperationHandle<T>[subassets.Length];
				AsyncUtilityInternal.Send(
					() => {
						for(int i = 0; i < subassets.Length; ++i) {
							ops[i] = Addressables.LoadAssetAsync<T>($"{mainAddress}[{subassets[i]}]");
						}
						holder = new ListResourceHolder<T>(ops);
						_resources.Add(mainAddress, holder);
					}
				);
			} else {
				AsyncUtilityInternal.Post(
					() => { holder.IncrementReferenceCount(); }
				);
			}

			object res = await holder.Resource;
			if (res is T[] resource) {
				return resource;
			} else {
				throw new System.Exception($"resource type is not {typeof(T).Name}[]. :{res.GetType().Name}[]");
			}
		}

		public static void Unload(string address) {
			if (_resources.TryGetValue(address, out IResourceHolder resourceHolder)) {
				if(!resourceHolder.DecrementReferenceCount()) {
					resourceHolder.Dispose();
					_resources.Remove(address);
					D.Log($"Resource:{address} unloaded.");
				}
			} else {
				D.LogW($"Address:{address} not loaded.");
			}
		}

		public static void UnloadAll() {
			foreach(IResourceHolder resourceHolder in _resources.Values) {
				resourceHolder.Dispose();
			}
			_resources.Clear();
		}
	}

	public interface IResourceHolder {
		int ReferenceCount{ get; }

		void IncrementReferenceCount();
		bool DecrementReferenceCount();

		Task<object> Resource{ get; }
		void Dispose();
	}

	internal sealed class ListResourceHolder<T> : IResourceHolder where T : class {
		private AsyncOperationHandle<T>[] _handles;
		private int _referenceCount;
		int IResourceHolder.ReferenceCount => _referenceCount;

		Task<object> IResourceHolder.Resource {
			get {
				return Task.Run(async () => {
					Task<T>[] tasks = new Task<T>[_handles.Length];
					for(int i = 0; i < _handles.Length; ++i) {
						tasks[i] = _handles[i].Task;
					}
					T[] result = await Task.WhenAll(tasks);
					return result as object;
				});
			}
		}

		internal ListResourceHolder(AsyncOperationHandle<T>[] handles) {
			_handles = handles;
			_referenceCount = 1;
		}

		void IResourceHolder.Dispose() {
			foreach(AsyncOperationHandle<T> handle in _handles) {
				if (handle.IsValid()) {
					Addressables.Release(handle);
				}
			}
		}

		void IResourceHolder.IncrementReferenceCount() {
			_referenceCount++;
		}

		bool IResourceHolder.DecrementReferenceCount() {
			_referenceCount--;
			return _referenceCount > 0;
		}
	}


	internal sealed class ResourceHolder<T> : IResourceHolder where T : class {
		private AsyncOperationHandle<T> _handle;
		private int _referenceCount;
		int IResourceHolder.ReferenceCount => _referenceCount;

		Task<object> IResourceHolder.Resource {
			get {
				return Task.Run<object>(async () => await _handle.Task);
			}
		}

		internal ResourceHolder(AsyncOperationHandle<T> handle) {
			_handle = handle;
			_referenceCount = 1;
		}

		void IResourceHolder.Dispose() {
			if (_handle.IsValid()) {
				Addressables.Release(_handle);
			}
		}

		void IResourceHolder.IncrementReferenceCount() {
			_referenceCount++;
		}

		bool IResourceHolder.DecrementReferenceCount() {
			_referenceCount--;
			return _referenceCount > 0;
		}
	}

}