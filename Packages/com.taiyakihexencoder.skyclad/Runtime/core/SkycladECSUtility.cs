using Unity.Entities;
using Unity.Jobs;

namespace skyclad {
	public static partial class SkycladECSUtility {
		/// <summary>
		/// Job自体をstatic関数内で呼ぼうとすると、
		/// それがたとえインスタンス向けのExtension関数であろうと、
		/// コードジェネレーターが正しく実装せずエラーが発生する
		/// </summary>
		public partial struct DestroyJob : IJobEntity {
			public EntityCommandBuffer commandBuffer;

			void Execute(in Entity entity) { 
				commandBuffer.DestroyEntity(entity); 
			}
		}
	}
}