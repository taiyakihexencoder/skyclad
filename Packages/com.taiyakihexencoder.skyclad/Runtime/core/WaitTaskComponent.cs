using Unity.Entities;

namespace skyclad {
	/// <summary>
	/// 何か機能があるわけではないが、
	/// 待機中を示すフラグ用のUtility Component
	/// </summary>
	internal struct WaitTaskComponent : IComponentData {}

	/// <summary>
	/// WaitTaskComponentをアタッチするだけのタスク
	/// </summary>
	public partial struct AddWaitTaskJob : IJobEntity {
		public EntityCommandBuffer commandBuffer;

		void Execute(in Entity entity) {
			commandBuffer.AddComponent<WaitTaskComponent>(entity);
		}
	}
}