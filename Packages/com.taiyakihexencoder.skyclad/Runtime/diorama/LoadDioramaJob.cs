using System.Runtime.InteropServices;
using Unity.Entities;

namespace skyclad {
	[StructLayout(LayoutKind.Auto)]
	public partial struct LoadDioramaJob : IJobEntity {
		public EntityCommandBuffer commandBuffer;

		void Execute(in Entity entity, RefRO<RequestLoadDioramaComponent> request) {
			Load(commandBuffer, request.ValueRO.id);
		}

		partial void Load(EntityCommandBuffer commandBuffer, int dioramaId);

		private Entity CreateRequestEntity<T>(T component) where T: unmanaged, IComponentData {
			Entity entity = commandBuffer.CreateEntity();
			commandBuffer.AddComponent<DioramaLoadElementComponent>(entity);
			commandBuffer.AddComponent(entity, component);
			return entity;
		}
	}
}