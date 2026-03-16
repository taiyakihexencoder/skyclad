using Unity.Entities;
using Unity.Physics;

namespace skyclad.collider {
	public struct ColliderTriggerExclude : IComponentData { }

	public struct ColliderTriggerEvent : IBufferElementData, ISimulationEvent<ColliderTriggerEvent> {
		public Entity EntityA { get; private set; }
		public Entity EntityB { get; private set; }
		public int BodyIndexA { get; private set; }
		public int BodyIndexB { get; private set; }
		public ColliderKey ColliderKeyA{ get; private set; }
		public ColliderKey ColliderKeyB { get; private set; }

		public ColliderTriggerEvent(
			Entity entityA,
			int bodyIndexA,
			ColliderKey colliderKeyA,
			Entity entityB,
			int bodyIndexB,
			ColliderKey colliderKeyB
		) {
			EntityA = entityA;
			EntityB = entityB;
			BodyIndexA = bodyIndexA;
			BodyIndexB = bodyIndexB;
			ColliderKeyA = colliderKeyA;
			ColliderKeyB = colliderKeyB;
		}

		public int CompareTo(ColliderTriggerEvent other) {
			return ISimulationEventUtilities.CompareEvents(this, other);
		}
	}

	public struct ColliderTriggerEnterEvent : IBufferElementData {
		public Entity Self { get; private set; }
		public Entity Other { get; private set; }
		public int SelfIndex { get; private set; }
		public int OtherIndex { get; private set; }
		public ColliderKey SelfColliderKey { get; private set; }
		public ColliderKey OtherColliderKey { get; private set; }

		public ColliderTriggerEnterEvent(ColliderTriggerEvent evt, Entity other) {
			if (other == evt.EntityA) {
				Self = evt.EntityB;
				SelfIndex = evt.BodyIndexB;
				SelfColliderKey = evt.ColliderKeyB;
				Other = evt.EntityA;
				OtherIndex = evt.BodyIndexA;
				OtherColliderKey = evt.ColliderKeyA;
			}
			else {
				Self = evt.EntityA;
				SelfIndex = evt.BodyIndexA;
				SelfColliderKey = evt.ColliderKeyA;
				Other = evt.EntityB;
				OtherIndex = evt.BodyIndexB;
				OtherColliderKey = evt.ColliderKeyB;
			}
		}
	}

	public struct ColliderTriggerExitEvent : IBufferElementData {
		public Entity Self { get; private set; }
		public Entity Other { get; private set; }
		public int SelfIndex { get; private set; }
		public int OtherIndex { get; private set; }
		public ColliderKey SelfColliderKey { get; private set; }
		public ColliderKey OtherColliderKey { get; private set; }

		public ColliderTriggerExitEvent(ColliderTriggerEvent evt, Entity other) {
			if (other == evt.EntityA) {
				Self = evt.EntityB;
				SelfIndex = evt.BodyIndexB;
				SelfColliderKey = evt.ColliderKeyB;
				Other = evt.EntityA;
				OtherIndex = evt.BodyIndexA;
				OtherColliderKey = evt.ColliderKeyA;
			} else {
				Self = evt.EntityA;
				SelfIndex = evt.BodyIndexA;
				SelfColliderKey = evt.ColliderKeyA;
				Other = evt.EntityB;
				OtherIndex = evt.BodyIndexB;
				OtherColliderKey = evt.ColliderKeyB;
			}
		}
	}

	public struct ColliderTriggerStayEvent : IBufferElementData {
		public Entity Self { get; private set; }
		public Entity Other { get; private set; }
		public int SelfIndex { get; private set; }
		public int OtherIndex { get; private set; }
		public ColliderKey SelfColliderKey { get; private set; }
		public ColliderKey OtherColliderKey { get; private set; }

		public ColliderTriggerStayEvent(ColliderTriggerEvent evt, Entity other) {
			if (other == evt.EntityA) {
				Self = evt.EntityB;
				SelfIndex = evt.BodyIndexB;
				SelfColliderKey = evt.ColliderKeyB;
				Other = evt.EntityA;
				OtherIndex = evt.BodyIndexA;
				OtherColliderKey = evt.ColliderKeyA;
			} else {
				Self = evt.EntityA;
				SelfIndex = evt.BodyIndexA;
				SelfColliderKey = evt.ColliderKeyA;
				Other = evt.EntityB;
				OtherIndex = evt.BodyIndexB;
				OtherColliderKey = evt.ColliderKeyB;
			}
		}
	}
}