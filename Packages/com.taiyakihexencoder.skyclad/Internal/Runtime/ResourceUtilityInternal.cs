using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace skyclad.internalProc {
	public static class ResourceUtilityInternal {
		private static Dictionary<string, IResourceHolder> _resources = null;
		static ResourceUtilityInternal() {
			_resources = new Dictionary<string, IResourceHolder>();
		}

		public static async Task<T> Load<T>(string address) where T : class {
			if (!_resources.TryGetValue(address, out IResourceHolder holder)) {
				AsyncOperationHandle<T> op = default;
				AsyncUtilityInternal.Send(
					() =>{
						op = Addressables.LoadAssetAsync<T>(address);
						holder = new ResourceHolder<T>(op);
						_resources.Add(address, holder);
					}
				);
			} else {
				AsyncUtilityInternal.Post(
					() => {
						holder.IncrementReferenceCount();
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