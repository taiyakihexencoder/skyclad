namespace skyclad.userData {
	using System.IO;
	using System.Threading.Tasks;
	using internalProc;
	using Unity.Collections;
	using Unity.Entities;
	using UnityEngine;

	/// <summary>
	/// バイナリを読み込んでIComponentDataに流す
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class SingleDataLoader<T> where T : unmanaged, IComponentData {
		protected abstract T Default { get; }
		protected abstract T FromBinary(byte[] data);
		protected abstract byte[] ToBinary(T component);

		private readonly EntityQuery _query;
		private readonly string _path;

		public SingleDataLoader() {
			_query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<T>()
				.Build(ECSUtilityInternal.EntityManager);
			
			string basePath = Application.persistentDataPath + Path.DirectorySeparatorChar + "single";
			_path = basePath + Path.DirectorySeparatorChar + typeof(T).Name + ".bytes";

			// ディレクトリ作成
			if (!Directory.Exists(basePath)) {
				Directory.CreateDirectory(basePath);
			}
		}

		public void Load() {
			_ = Task.Run(LoadInternal);
		}

		public void Save() {
			_ = Task.Run(SaveInternal);
		}

		private async Task LoadInternal() {
			T component = Default;

			try {
				if (File.Exists(_path)) {
					byte[] bytes = await File.ReadAllBytesAsync(_path);
					component = FromBinary(bytes);
				}
				AsyncUtilityInternal.Log($"Load:{_path}");
			} catch (System.Exception e) {
				AsyncUtilityInternal.Log(e);
			}

			AsyncUtilityInternal.Post(() => {
				ECSUtilityInternal.ExecuteCommandBufferTemp(
					commandBuffer => {
						CreateEntity(commandBuffer, component);
					}
				);
			});
		}

		private async Task SaveInternal() {
			T component = Default;

			try {
				AsyncUtilityInternal.Send(() => {
					component = _query.GetSingleton<T>();
				});
				
				await File.WriteAllBytesAsync(_path, ToBinary(component));
				
				AsyncUtilityInternal.Log($"Save:{_path}");
			} catch (System.Exception e) {
				AsyncUtilityInternal.Log(e);
			}
		}

		private Entity CreateEntity(EntityCommandBuffer commandBuffer, T component) {
			Entity entity = commandBuffer.CreateEntity();
			commandBuffer.SetDebugName(entity, $"Single Data({typeof(T).Name})");
			commandBuffer.AddComponent(entity, component);
			return entity;
		}

		public bool Exists() {
			return !_query.IsEmpty;
		}

		public void Unload() {
			if (!_query.IsEmpty) {
				ECSUtilityInternal.ExecuteCommandBufferTemp(
					commandBuffer => commandBuffer.DestroyEntity(_query, EntityQueryCaptureMode.AtPlayback)
				);
			}
		}
	}
}