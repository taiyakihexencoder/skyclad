using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;

namespace skyclad.collider {
	[UpdateInGroup(typeof(SkycladColliderGroup))]
	public partial struct ColliderTriggerSystem : ISystem {
		private EntityQuery stayQuery;
		private EntityQuery enterQuery;
		private EntityQuery exitQuery;

		private NativeList<ColliderTriggerEvent> prev;
		private NativeList<ColliderTriggerEvent> curr;
		private BufferLookup<ColliderTriggerEvent> lookup;
		private BufferLookup<ColliderTriggerStayEvent> stayLookup;
		private BufferLookup<ColliderTriggerEnterEvent> enterLookup;
		private BufferLookup<ColliderTriggerExitEvent> exitLookup;

		private ComponentLookup<ColliderTriggerExclude> excludeLookup;

		void ISystem.OnCreate(ref SystemState state) {
			stayQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAllRW<ColliderTriggerStayEvent>()
				.WithNone<ColliderTriggerExclude>()
				.Build(ref state);
			enterQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAllRW<ColliderTriggerEnterEvent>()
				.WithNone<ColliderTriggerExclude>()
				.Build(ref state);
			exitQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAllRW<ColliderTriggerExitEvent>()
				.WithNone<ColliderTriggerExclude>()
				.Build(ref state);


			prev = new NativeList<ColliderTriggerEvent>(Allocator.Persistent);
			curr = new NativeList<ColliderTriggerEvent>(Allocator.Persistent);
			lookup = state.GetBufferLookup<ColliderTriggerEvent>();
			stayLookup = state.GetBufferLookup<ColliderTriggerStayEvent>();
			enterLookup = state.GetBufferLookup<ColliderTriggerEnterEvent>();
			exitLookup = state.GetBufferLookup<ColliderTriggerExitEvent>();

			excludeLookup = state.GetComponentLookup<ColliderTriggerExclude>();

			state.RequireForUpdate(
				new EntityQueryBuilder(Allocator.Temp)
					.WithAllRW<ColliderTriggerEvent>()
					.WithNone<ColliderTriggerExclude>()
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

			NativeList<ColliderTriggerEvent> temp = prev;
			prev = curr;
			curr = temp;
			curr.Clear();

			state.Dependency = new CollectTriggerEventJob {
				triggerEvents = curr,
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

		void ISystem.OnDestroy(ref SystemState state)
		{
			if (prev.IsCreated) prev.Dispose();
			if (curr.IsCreated) curr.Dispose();
		}

		public partial struct ClearColliderStayEventJob : IJobEntity {
			public void Execute(ref DynamicBuffer<ColliderTriggerStayEvent> eventBuffer) {
				eventBuffer.Clear();
			}
		}

		public partial struct ClearColliderEnterEventJob : IJobEntity {
			public void Execute(ref DynamicBuffer<ColliderTriggerEnterEvent> eventBuffer) {
				eventBuffer.Clear();
			}
		}

		public partial struct ClearColliderExitEventJob : IJobEntity {
			public void Execute(ref DynamicBuffer<ColliderTriggerExitEvent> eventBuffer) {
				eventBuffer.Clear();
			}
		}

		public partial struct CollectTriggerEventJob : ITriggerEventsJob {
			public NativeList<ColliderTriggerEvent> triggerEvents;
			void ITriggerEventsJobBase.Execute(TriggerEvent triggerEvent) {
				triggerEvents.Add(
					new ColliderTriggerEvent(
						triggerEvent.EntityA,
						triggerEvent.BodyIndexA,
						triggerEvent.ColliderKeyA,
						triggerEvent.EntityB,
						triggerEvent.BodyIndexB,
						triggerEvent.ColliderKeyB
					)
				);
			}
		}

		public partial struct EvaluateColliderEventJob : IJob {
			[ReadOnly] public NativeList<ColliderTriggerEvent> prev;
			public NativeList<ColliderTriggerEvent> curr;

			[ReadOnly] public BufferLookup<ColliderTriggerEvent> lookup;
			public BufferLookup<ColliderTriggerStayEvent> stayLookup;
			public BufferLookup<ColliderTriggerEnterEvent> enterLookup;
			public BufferLookup<ColliderTriggerExitEvent> exitLookup;

			[ReadOnly] public ComponentLookup<ColliderTriggerExclude> excludeLookup;

			void IJob.Execute() {
				curr.Sort();

				int cIdx = 0;
				int pIdx = 0;

				while (cIdx < curr.Length && pIdx < prev.Length) {
					int comp = prev[pIdx].CompareTo(curr[cIdx]);
					if (comp == 0) {
						// TriggerStay
						ColliderTriggerEvent evt = curr[cIdx];
						if (stayLookup.HasBuffer(evt.EntityA) && !excludeLookup.HasComponent(evt.EntityA)) {
							stayLookup[evt.EntityA].Add(new ColliderTriggerStayEvent(evt, evt.EntityB));
						} else if (stayLookup.HasBuffer(evt.EntityB) && !excludeLookup.HasComponent(evt.EntityB)) {
							stayLookup[evt.EntityB].Add(new ColliderTriggerStayEvent(evt, evt.EntityA));
						}
						cIdx++;
						pIdx++;
					} else if (comp < 0) {
						// TriggerExit
						ColliderTriggerEvent evt = prev[pIdx];
						if (exitLookup.HasBuffer(evt.EntityA)) {
							exitLookup[evt.EntityA].Add(new ColliderTriggerExitEvent(evt, evt.EntityB));
						}
						else if (exitLookup.HasBuffer(evt.EntityB)) {
							exitLookup[evt.EntityB].Add(new ColliderTriggerExitEvent(evt, evt.EntityA));
						}
						pIdx++;
					} else {
						//TriggerEnter
						ColliderTriggerEvent evt = curr[cIdx];
						if (enterLookup.HasBuffer(evt.EntityA)) {
							enterLookup[evt.EntityA].Add(new ColliderTriggerEnterEvent(evt, evt.EntityB));
						} else if (enterLookup.HasBuffer(evt.EntityB)) {
							enterLookup[evt.EntityB].Add(new ColliderTriggerEnterEvent(evt, evt.EntityA));
						}
						cIdx++;
					}
				}

				while (pIdx < prev.Length) {
					// TriggerExit
					ColliderTriggerEvent evt = prev[pIdx];
					if (exitLookup.HasBuffer(evt.EntityA)) {
						exitLookup[evt.EntityA].Add(new ColliderTriggerExitEvent(evt, evt.EntityB));
					} else if (exitLookup.HasBuffer(evt.EntityB)) {
						exitLookup[evt.EntityB].Add(new ColliderTriggerExitEvent(evt, evt.EntityA));
					}
					pIdx++;
				}

				while (cIdx < curr.Length) {
					//TriggerEnter
					ColliderTriggerEvent evt = curr[cIdx];
					if (enterLookup.HasBuffer(evt.EntityA)) {
						enterLookup[evt.EntityA].Add(new ColliderTriggerEnterEvent(evt, evt.EntityB));
					} else if (enterLookup.HasBuffer(evt.EntityB)) {
						enterLookup[evt.EntityB].Add(new ColliderTriggerEnterEvent(evt, evt.EntityA));
					}
					cIdx++;
				}
			}
		}
	}
}
