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
		/// <summary>
		/// ファイルが読み込めなかった場合のデフォルト
		/// </summary>
		protected abstract T Default { get; }

		/// <summary>
		/// バイナリからの変換
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected abstract T FromBinary(byte[] data);

		/// <summary>
		/// バイナリに変換
		/// </summary>
		/// <param name="component"></param>
		/// <returns></returns>
		protected abstract byte[] ToBinary(T component);

		/// <summary>
		/// 破棄される時の処理(Blobの解放など)
		/// </summary>
		/// <param name="component"></param>
		protected virtual void OnDispose(T component) { }
		protected virtual void OnLoadCompleted(Entity entity, EntityCommandBuffer commandbuffer) {
			D.Log($"Load:{_path}");
		}
		protected virtual void OnSaveCompleted() {
			D.Log($"Save:{_path}");
		}

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

		/// <summary>
		/// バックグラウンドで読込を実行
		/// </summary>
		public void Load() {
			_ = Task.Run(LoadInternal);
		}

		/// <summary>
		/// バックグラウンドで書込を実行
		/// </summary>
		public void Save() {
			_ = Task.Run(SaveInternal);
		}

		private async Task LoadInternal() {
			T component = AsyncUtilityInternal.Send(() => Default);

			try {
				if (File.Exists(_path)) {
					byte[] bytes = await File.ReadAllBytesAsync(_path);
					AsyncUtilityInternal.Send(
						() => {
							OnDispose(component);
							component = FromBinary(bytes);
						}
					);
				}
			} catch (System.Exception e) {
				AsyncUtilityInternal.Log(e);
			}

			AsyncUtilityInternal.Post(() => {
				ECSUtilityInternal.ExecuteCommandBufferTemp(
					commandBuffer => {
						Entity entity = CreateEntity(commandBuffer, component);
						OnLoadCompleted(entity, commandBuffer);
					}
				);
			});
		}

		private async Task SaveInternal() {
			T component = AsyncUtilityInternal.Send(() => Default);
			try {
				byte[] bytes = null;
				AsyncUtilityInternal.Send(() => {
					OnDispose(component);
					component = _query.GetSingleton<T>();
					bytes = ToBinary(component);
				});

				if (bytes == null) {
					AsyncUtilityInternal.Log("Failed Save.");
				} else {
					await File.WriteAllBytesAsync(_path, bytes);
				}
			} catch (System.Exception e) {
				AsyncUtilityInternal.LogError(e);
			}

			AsyncUtilityInternal.Post(() => {
				OnSaveCompleted();
			});
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

		/// <summary>
		/// データエンティティを削除
		/// </summary>
		public void Unload() {
			if (!_query.IsEmpty) {
				NativeArray<T> data = _query.ToComponentDataArray<T>(Allocator.Temp);
				for (int i = 0; i < data.Length; ++i) {
					OnDispose(data[i]);
				}
				data.Dispose();

				ECSUtilityInternal.ExecuteCommandBufferTemp(
					commandBuffer => commandBuffer.DestroyEntity(_query, EntityQueryCaptureMode.AtPlayback)
				);
			}
		}
	}
}