using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace skyclad.collider {
	public struct ColliderCollisionExclude : IComponentData{ }

	public struct ColliderCollisionEvent : IBufferElementData, ISimulationEvent<ColliderCollisionEvent> {
		public Entity EntityA { get; private set; }
		public Entity EntityB { get; private set; }
		public int BodyIndexA { get; private set; }
		public int BodyIndexB { get; private set; }
		public ColliderKey ColliderKeyA { get; private set; }
		public ColliderKey ColliderKeyB { get; private set; }

		public float3 Normal { get; private set; }

		public ColliderCollisionEvent(
			Entity entityA,
			int bodyIndexA,
			ColliderKey colliderKeyA,
			Entity entityB,
			int bodyIndexB,
			ColliderKey colliderKeyB,
			float3 normal
		) {
			EntityA = entityA;
			EntityB = entityB;
			BodyIndexA = bodyIndexA;
			BodyIndexB = bodyIndexB;
			ColliderKeyA = colliderKeyA;
			ColliderKeyB = colliderKeyB;
			Normal = normal;
		}

		public int CompareTo(ColliderCollisionEvent other) {
			return ISimulationEventUtilities.CompareEvents(this, other);
		}
	}

	public struct ColliderCollisionEnterEvent : IBufferElementData {
		public Entity Self { get; private set; }
		public Entity Other { get; private set; }
		public int SelfIndex { get; private set; }
		public int OtherIndex { get; private set; }
		public ColliderKey SelfColliderKey { get; private set; }
		public ColliderKey OtherColliderKey { get; private set; }
		public float3 Normal { get; private set; }

		public ColliderCollisionEnterEvent(ColliderCollisionEvent evt, Entity other) {
			if (other == evt.EntityA) {
				Self = evt.EntityB;
				SelfIndex = evt.BodyIndexB;
				SelfColliderKey = evt.ColliderKeyB;
				Other = evt.EntityA;
				OtherIndex = evt.BodyIndexA;
				OtherColliderKey = evt.ColliderKeyA;
				Normal = -evt.Normal;
			} else {
				Self = evt.EntityA;
				SelfIndex = evt.BodyIndexA;
				SelfColliderKey = evt.ColliderKeyA;
				Other = evt.EntityB;
				OtherIndex = evt.BodyIndexB;
				OtherColliderKey = evt.ColliderKeyB;
				Normal = evt.Normal;
			}
		}

	}

	public struct ColliderCollisionExitEvent : IBufferElementData {
		public Entity Self { get; private set; }
		public Entity Other { get; private set; }
		public int SelfIndex { get; private set; }
		public int OtherIndex { get; private set; }
		public ColliderKey SelfColliderKey { get; private set; }
		public ColliderKey OtherColliderKey { get; private set; }
		public float3 Normal { get; private set; }

		public ColliderCollisionExitEvent(ColliderCollisionEvent evt, Entity other) {
			if (other == evt.EntityA) {
				Self = evt.EntityB;
				SelfIndex = evt.BodyIndexB;
				SelfColliderKey = evt.ColliderKeyB;
				Other = evt.EntityA;
				OtherIndex = evt.BodyIndexA;
				OtherColliderKey = evt.ColliderKeyA;
				Normal = -evt.Normal;
			} else {
				Self = evt.EntityA;
				SelfIndex = evt.BodyIndexA;
				SelfColliderKey = evt.ColliderKeyA;
				Other = evt.EntityB;
				OtherIndex = evt.BodyIndexB;
				OtherColliderKey = evt.ColliderKeyB;
				Normal = evt.Normal;
			}
		}
	}

	public struct ColliderCollisionStayEvent : IBufferElementData {
		public Entity Self { get; private set; }
		public Entity Other { get; private set; }
		public int SelfIndex { get; private set; }
		public int OtherIndex { get; private set; }
		public ColliderKey SelfColliderKey { get; private set; }
		public ColliderKey OtherColliderKey { get; private set; }
		public float3 Normal { get; private set; }

		public ColliderCollisionStayEvent(ColliderCollisionEvent evt, Entity other) {
			if (other == evt.EntityA) {
				Self = evt.EntityB;
				SelfIndex = evt.BodyIndexB;
				SelfColliderKey = evt.ColliderKeyB;
				Other = evt.EntityA;
				OtherIndex = evt.BodyIndexA;
				OtherColliderKey = evt.ColliderKeyA;
				Normal = -evt.Normal;
			} else {
				Self = evt.EntityA;
				SelfIndex = evt.BodyIndexA;
				SelfColliderKey = evt.ColliderKeyA;
				Other = evt.EntityB;
				OtherIndex = evt.BodyIndexB;
				OtherColliderKey = evt.ColliderKeyB;
				Normal = evt.Normal;
			}
		}
	}

}