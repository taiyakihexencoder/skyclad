using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace skyclad {
	/// <summary>
	/// リストを保持する構造体
	/// </summary>
	/// <typeparam entityName="T"></typeparam>
	public struct SkycladDataBlob<T> where T : unmanaged {
		public BlobArray<T> records;
	}

	public static partial class SkycladDataTables {
		private static SynchronizationContext _mainContext;

		private static BlobAssetReference<SkycladDataBlob<CharacterStatus>> _characterStatus;
		private static BlobAssetReference<SkycladDataBlob<BulletParameter>> _bulletParameters;

		static partial void InitTables();
		static partial void InitIngameTables();
		static partial void DisposeIngameTables();

		static partial void DisposeAllTables();

		/// <summary>
		/// データテーブルの初期処理
		/// </summary>
		/// <param entityName="mainContext"></param>
		internal static void Init(SynchronizationContext mainContext) {
			_mainContext = mainContext;
			_characterStatus = BlobAssetReference<SkycladDataBlob<CharacterStatus>>.Null;
			_bulletParameters = BlobAssetReference<SkycladDataBlob<BulletParameter>>.Null;

			InitTables();
		}

		/// <summary>
		/// インゲームを開始する際の準備処理
		/// 終了次第各テーブルが利用できる
		/// </summary>
		internal static void OnStartAdventure() {
			InitIngameTables();
		}

		/// <summary>
		/// インゲーム終了時の処理
		/// テーブルを使用不能にしてから破棄する
		/// </summary>
		internal static void OnEndAdventure() {
			DisposeIngameTables();
		}

		/// <summary>
		/// すべてのデータテーブルを破棄する
		/// </summary>
		internal static void Dispose() {
			if (_characterStatus.IsCreated) { _characterStatus.Dispose(); }

			DisposeAllTables();
		}

		private static async Task Load<T>(BaseTableLoader<T> loader) where T : unmanaged {
			List<T> list;
			await Task.Run(
				() => {
					try {
						string path = Application.streamingAssetsPath + 
							$"{Path.DirectorySeparatorChar}.skyclad{Path.DirectorySeparatorChar}.app{Path.DirectorySeparatorChar}{loader.TableName}.bytes";

						using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
							using (BinaryReader reader = new BinaryReader(stream, System.Text.Encoding.UTF8)) {
								list = loader.Load(reader);
							}
						}
					} catch (System.Exception e) {
						UnityEngine.Debug.LogError(e);
						// todo
						list = null;
					}

					_mainContext.Post((_) => loader.Assign(list), null);
				}
			);
		}

		internal static void SendSignal<SIGNAL>(EntityManager entityManager, FixedString64Bytes tableName) where SIGNAL : unmanaged, IComponentData {
			Entity entity = entityManager.CreateEntity();
			#if UNITY_EDITOR
			entityManager.SetName(entity, $"Load Table Complete({tableName})");
			#endif
			entityManager.AddComponent<SIGNAL>(entity);
		}

	}

	internal abstract class BaseTableLoader<T> where T : unmanaged {
		internal abstract string TableName { get; }
		internal BlobAssetReference<SkycladDataBlob<T>> assetReference { get; private set; }

		internal abstract List<T> Load(BinaryReader reader);
		protected abstract void OnComplete(EntityManager entityManager);
		protected abstract void OnDispose(EntityManager entityManager);

		internal BaseTableLoader() {
			assetReference = BlobAssetReference<SkycladDataBlob<T>>.Null;
		}

		internal void Dispose(SynchronizationContext context) {
			EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
			OnDispose(entityManager);
			Task.Run(async () => {
				await Task.Yield();
				context.Post((_) => {
					if (assetReference.IsCreated) {
						assetReference.Dispose();
						assetReference = BlobAssetReference<SkycladDataBlob<T>>.Null;
					}
				}, null);
			});

		}

		internal void Assign(List<T> list) {
			assetReference = ConvertToBlob(list);

			EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
			OnComplete(entityManager);
		}

		protected BlobAssetReference<SkycladDataBlob<T>> ConvertToBlob(List<T> raw) {
			BlobBuilder builder = new BlobBuilder(Allocator.Temp);
			ref SkycladDataBlob<T> blob = ref builder.ConstructRoot<SkycladDataBlob<T>>();
			
			BlobBuilderArray<T> arrayBuilder = builder.Allocate(ref blob.records, raw.Count);
			for (int i = 0; i < raw.Count; ++i) {
				arrayBuilder[i] = raw[i];
			}
			return builder.CreateBlobAssetReference<SkycladDataBlob<T>>(Allocator.Persistent);
		}

		protected void SendSignal<SIGNAL>(
			EntityManager entityManager, 
			FixedString64Bytes entityName
		) where SIGNAL : unmanaged, IComponentData {
			Entity entity = entityManager.CreateEntity();
			
			#if UNITY_EDITOR
			entityManager.SetName(entity, entityName);
			#endif
			entityManager.AddComponent<SIGNAL>(entity);

		}
	}

}