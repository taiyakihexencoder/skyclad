using System.Runtime.InteropServices;
using Unity.Entities;

namespace skyclad {
	[StructLayout(LayoutKind.Auto)]
	public partial struct UnloadDioramaJob : IJobEntity {
		public EntityCommandBuffer commandBuffer;

		void Execute(in Entity entity, RefRO<RequestUnloadDioramaComponent> request) {
			Unload(commandBuffer, request.ValueRO.id);
		}

		partial void Unload(EntityCommandBuffer commandBuffer, int dioramaId);

		private Entity CreateRequestEntity<T>(T component) where T: unmanaged, IComponentData {
			Entity entity = commandBuffer.CreateEntity();
			commandBuffer.AddComponent<DioramaUnloadElementComponent>(entity);
			commandBuffer.AddComponent(entity, component);
			return entity;
		}
	}
}