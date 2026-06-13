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
				CreateFieldEntity();
			}
		}

		private void CreateFieldEntity() {
			NativeArray<LunarscapeFieldCreateRequest> requests = _createQuery.ToComponentDataArray<LunarscapeFieldCreateRequest>(Allocator.Temp);
			Entity[] entities = new Entity[requests.Length];
			int[] ids = new int[requests.Length];
			for(int i = 0; i < requests.Length; ++i) {
				entities[i] = requests[i].entity;
				ids[i] = requests[i].id;
			}
			requests.Dispose();

			ECSUtilityInternal.ExecuteCommandBufferTemp( commandBuffer => {
				commandBuffer.DestroyEntity(_createQuery, EntityQueryCaptureMode.AtPlayback);
			});

			_ = Task.Run(async () => {
				for(int i = 0; i < entities.Length; ++i) {
					await FieldManager.CreateMeshEntities(entities[i], ids[i]);
				}
			});
		}
	}
}