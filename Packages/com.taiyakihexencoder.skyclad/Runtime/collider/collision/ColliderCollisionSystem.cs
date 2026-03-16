using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;

namespace skyclad.collider {
	[UpdateInGroup(typeof(SkycladColliderGroup))]
	public partial struct ColliderCollisionSystem : ISystem {
		private EntityQuery stayQuery;
		private EntityQuery enterQuery;
		private EntityQuery exitQuery;

		private NativeList<ColliderCollisionEvent> prev;
		private NativeList<ColliderCollisionEvent> curr;
		private BufferLookup<ColliderCollisionEvent> lookup;
		private BufferLookup<ColliderCollisionStayEvent> stayLookup;
		private BufferLookup<ColliderCollisionEnterEvent> enterLookup;
		private BufferLookup<ColliderCollisionExitEvent> exitLookup;

		private ComponentLookup<ColliderCollisionExclude> excludeLookup;

		void ISystem.OnCreate(ref SystemState state) {
			stayQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAllRW<ColliderCollisionStayEvent>()
				.WithNone<ColliderCollisionExclude>()
				.Build(ref state);
			enterQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAllRW<ColliderCollisionEnterEvent>()
				.WithNone<ColliderCollisionExclude>()
				.Build(ref state);
			exitQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAllRW<ColliderCollisionExitEvent>()
				.WithNone<ColliderCollisionExclude>()
				.Build(ref state);


			prev = new NativeList<ColliderCollisionEvent>(Allocator.Persistent);
			curr = new NativeList<ColliderCollisionEvent>(Allocator.Persistent);
			lookup = state.GetBufferLookup<ColliderCollisionEvent>();
			stayLookup = state.GetBufferLookup<ColliderCollisionStayEvent>();
			enterLookup = state.GetBufferLookup<ColliderCollisionEnterEvent>();
			exitLookup = state.GetBufferLookup<ColliderCollisionExitEvent>();

			excludeLookup = state.GetComponentLookup<ColliderCollisionExclude>();

			state.RequireForUpdate(
				new EntityQueryBuilder(Allocator.Temp)
					.WithAllRW<ColliderCollisionEvent>()
					.WithNone<ColliderCollisionExclude>()
					.Build(ref state)
			);
		}

		void ISystem.OnUpdate(ref SystemState state) {
			lookup.Update(ref state);
			excludeLookup.Update(ref state);

			stayLookup.Update(ref state);
			enterLookup.Update(ref state);
			exitLookup.Update(ref state);

			state.Dependency = new ClearColliderStayEventJob { }.ScheduleParallel(stayQuery, state.Dependency);
			state.Dependency = new ClearColliderEnterEventJob { }.ScheduleParallel(enterQuery, state.Dependency);
			state.Dependency = new ClearColliderExitEventJob { }.ScheduleParallel(exitQuery, state.Dependency);

			NativeList<ColliderCollisionEvent> temp = prev;
			prev = curr;
			curr = temp;
			curr.Clear();

			state.Dependency = new CollectCollisionEventJob {
				CollisionEvents = curr,
			}.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);

			state.Dependency = new EvaluateColliderEventJob {
				lookup = lookup,
				prev = prev,
				curr = curr,
				stayLookup = stayLookup,
				enterLookup = enterLookup,
				exitLookup = exitLookup,
				excludeLookup = excludeLookup,
			}.Schedule(state.Dependency);
		}

		void ISystem.OnDestroy(ref SystemState state) {
			if (prev.IsCreated) prev.Dispose();
			if (curr.IsCreated) curr.Dispose();
		}

		public partial struct ClearColliderStayEventJob : IJobEntity {
			public void Execute(ref DynamicBuffer<ColliderCollisionStayEvent> eventBuffer) {
				eventBuffer.Clear();
			}
		}

		public partial struct ClearColliderEnterEventJob : IJobEntity {
			public void Execute(ref DynamicBuffer<ColliderCollisionEnterEvent> eventBuffer) {
				eventBuffer.Clear();
			}
		}

		public partial struct ClearColliderExitEventJob : IJobEntity {
			public void Execute(ref DynamicBuffer<ColliderCollisionExitEvent> eventBuffer) {
				eventBuffer.Clear();
			}
		}

		public partial struct CollectCollisionEventJob : ICollisionEventsJob {
			public NativeList<ColliderCollisionEvent> CollisionEvents;
			void ICollisionEventsJobBase.Execute(CollisionEvent CollisionEvent) {
				CollisionEvents.Add(
					new ColliderCollisionEvent(
						CollisionEvent.EntityA,
						CollisionEvent.BodyIndexA,
						CollisionEvent.ColliderKeyA,
						CollisionEvent.EntityB,
						CollisionEvent.BodyIndexB,
						CollisionEvent.ColliderKeyB,
						CollisionEvent.Normal
					)
				);
			}
		}

		public partial struct EvaluateColliderEventJob : IJob {
			[ReadOnly] public NativeList<ColliderCollisionEvent> prev;
			public NativeList<ColliderCollisionEvent> curr;

			[ReadOnly] public BufferLookup<ColliderCollisionEvent> lookup;
			public BufferLookup<ColliderCollisionStayEvent> stayLookup;
			public BufferLookup<ColliderCollisionEnterEvent> enterLookup;
			public BufferLookup<ColliderCollisionExitEvent> exitLookup;

			[ReadOnly] public ComponentLookup<ColliderCollisionExclude> excludeLookup;

			void IJob.Execute() {
				curr.Sort();

				int cIdx = 0;
				int pIdx = 0;

				while (cIdx < curr.Length && pIdx < prev.Length) {
					int comp = prev[pIdx].CompareTo(curr[cIdx]);
					if (comp == 0) {
						// CollisionStay
						ColliderCollisionEvent evt = curr[cIdx];
						if (stayLookup.HasBuffer(evt.EntityA) && !excludeLookup.HasComponent(evt.EntityA)) {
							stayLookup[evt.EntityA].Add(new ColliderCollisionStayEvent(evt, evt.EntityB));
						} else if (stayLookup.HasBuffer(evt.EntityB) && !excludeLookup.HasComponent(evt.EntityB)) {
							stayLookup[evt.EntityB].Add(new ColliderCollisionStayEvent(evt, evt.EntityA));
						}
						cIdx++;
						pIdx++;
					} else if (comp < 0) {
						// CollisionExit
						ColliderCollisionEvent evt = prev[pIdx];
						if (exitLookup.HasBuffer(evt.EntityA)) {
							exitLookup[evt.EntityA].Add(new ColliderCollisionExitEvent(evt, evt.EntityB));
						} else if (exitLookup.HasBuffer(evt.EntityB)) {
							exitLookup[evt.EntityB].Add(new ColliderCollisionExitEvent(evt, evt.EntityA));
						}
						pIdx++;
					} else {
						//CollisionEnter
						ColliderCollisionEvent evt = curr[cIdx];
						if (enterLookup.HasBuffer(evt.EntityA)) {
							enterLookup[evt.EntityA].Add(new ColliderCollisionEnterEvent(evt, evt.EntityB));
						} else if (enterLookup.HasBuffer(evt.EntityB)) {
							enterLookup[evt.EntityB].Add(new ColliderCollisionEnterEvent(evt, evt.EntityA));
						}
						cIdx++;
					}
				}

				while (pIdx < prev.Length) {
					// CollisionExit
					ColliderCollisionEvent evt = prev[pIdx];
					if (exitLookup.HasBuffer(evt.EntityA)) {
						exitLookup[evt.EntityA].Add(new ColliderCollisionExitEvent(evt, evt.EntityB));
					} else if (exitLookup.HasBuffer(evt.EntityB)) {
						exitLookup[evt.EntityB].Add(new ColliderCollisionExitEvent(evt, evt.EntityA));
					}
					pIdx++;
				}

				while (cIdx < curr.Length) {
					//CollisionEnter
					ColliderCollisionEvent evt = curr[cIdx];
					if (enterLookup.HasBuffer(evt.EntityA)) {
						enterLookup[evt.EntityA].Add(new ColliderCollisionEnterEvent(evt, evt.EntityB));
					} else if (enterLookup.HasBuffer(evt.EntityB)) {
						enterLookup[evt.EntityB].Add(new ColliderCollisionEnterEvent(evt, evt.EntityA));
					}
					cIdx++;
				}
			}
		}
	}
}