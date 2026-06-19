using System.Threading.Tasks;
using skyclad.internalProc;
using skyclad.lunarscape.internalProc;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace skyclad.lunarscape {
	internal class LunarscapeFieldBehaviour : MonoBehaviour {
		private EntityQuery _createQuery;

		internal static LunarscapeFieldBehaviour CreateInstance() {
			GameObject go = new GameObject("Field");
			return go.AddComponent<LunarscapeFieldBehaviour>();
		}

		private void Awake() {
			_createQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LunarscapeFieldCreateRequest>()
				.Build(ECSUtilityInternal.EntityManager);
		}

		private void Update() {
			if (!_createQuery.IsEmpty) {
				LoadField();
			}
		}

		private async void LoadField() {
			int entityCount = _createQuery.CalculateEntityCount();
			Entity[] entities = new Entity[entityCount];
			int[] ids = new int[entityCount];

			_createQuery.ForEach<LunarscapeFieldCreateRequest>(
				(index, request) => {
					entities[index] = request.entity;
					ids[index] = request.id;
				}
			);
			ECSUtilityInternal.DestroyEntityInQuery(_createQuery);


			FieldMeshAssetLoader.FieldRes[] fieldResList = await Task.Run(
				async () => {
					FieldMeshAssetLoader.FieldRes[] fieldResList = new FieldMeshAssetLoader.FieldRes[entities.Length];
					for (int i = 0; i < entities.Length; ++i) {
						fieldResList[i] = await FieldManager.RequestLoad(ids[i]);
					}
					return fieldResList;
				}
			);

			for(int i = 0; i < entityCount; ++i) {
				ECSUtilityInternal.ExecuteCommandBufferTemp( commandBuffer => {
					FieldManager.CreateFieldMeshEntities(entities[i], ids[i], fieldResList[i]);
					Entity requestEntity = commandBuffer.CreateEntity();
					// フィールドとセットのコンテンツの読込を依頼
					commandBuffer.AddComponent(
						requestEntity, 
						new LunarscapeLoadTableGroupRequest {
							id = ids[i],
							guid = fieldResList[i].guid,
						}
					);
				});
			}
		}
	}
}