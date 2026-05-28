using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace skyclad {
	public static class EntityBuilderExtensions {
		public static EntityBuilder CreateEntityBuilder(this EntityManager entityManager) {
			return EntityBuilder.Create(entityManager);
		}

		public static EntityBuilder CreateEntityBuilder(this ref SystemState state) {
			return state.EntityManager.CreateEntityBuilder();
		}
	}

	public struct EntityBuilder {
		private EntityManager entityManager;
		private List<ComponentType> componentTypeList;

		internal static EntityBuilder Create(EntityManager entityManager) {
			return new EntityBuilder {
				entityManager = entityManager,
				componentTypeList = new List<ComponentType>(),
			};
		}

		public Entity Build() {
			return entityManager.CreateEntity(
				entityManager.CreateArchetype(componentTypeList.ToArray())
			);
		}

		public Entity Build(FixedString64Bytes name) {
			Entity entity = entityManager.CreateEntity(
				entityManager.CreateArchetype(componentTypeList.ToArray())
			);
			#if UNITY_EDITOR
			entityManager.SetName(entity, name);
			#endif

			return entity;
		}

		public Entity Build<T1>(T1 component1) 
			 where T1 : unmanaged, IComponentData {
			Entity entity = Build();
			entityManager.SetComponentData(entity, component1);
			return entity;
		}

		public Entity Build<T1, T2>(T1 component1, T2 component2) 
			 where T1 : unmanaged, IComponentData
			 where T2 : unmanaged, IComponentData {
			Entity entity = Build();
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			return entity;
		}

		public Entity Build<T1, T2, T3>(T1 component1, T2 component2, T3 component3) 
			 where T1 : unmanaged, IComponentData
			 where T2 : unmanaged, IComponentData
			 where T3 : unmanaged, IComponentData {
			Entity entity = Build();
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			entityManager.SetComponentData(entity, component3);
			return entity;
		}

		public Entity Build<T1, T2, T3, T4>(T1 component1, T2 component2, T3 component3, T4 component4) 
			 where T1 : unmanaged, IComponentData
			 where T2 : unmanaged, IComponentData
			 where T3 : unmanaged, IComponentData
			 where T4 : unmanaged, IComponentData {
			Entity entity = Build();
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			entityManager.SetComponentData(entity, component3);
			entityManager.SetComponentData(entity, component4);
			return entity;
		}

		public Entity Build<T1, T2, T3, T4, T5>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5) 
			 where T1 : unmanaged, IComponentData
			 where T2 : unmanaged, IComponentData
			 where T3 : unmanaged, IComponentData
			 where T4 : unmanaged, IComponentData
			 where T5 : unmanaged, IComponentData {
			Entity entity = Build();
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			entityManager.SetComponentData(entity, component3);
			entityManager.SetComponentData(entity, component4);
			entityManager.SetComponentData(entity, component5);
			return entity;
		}

		public Entity Build<T1>(FixedString64Bytes name, T1 component1) 
			 where T1 : unmanaged, IComponentData {
			Entity entity = Build(name);
			entityManager.SetComponentData(entity, component1);
			return entity;
		}

		public Entity Build<T1, T2>(FixedString64Bytes name, T1 component1, T2 component2) 
			 where T1 : unmanaged, IComponentData
			 where T2 : unmanaged, IComponentData {
			Entity entity = Build(name);
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			return entity;
		}

		public Entity Build<T1, T2, T3>(FixedString64Bytes name, T1 component1, T2 component2, T3 component3) 
			 where T1 : unmanaged, IComponentData
			 where T2 : unmanaged, IComponentData
			 where T3 : unmanaged, IComponentData {
			Entity entity = Build(name);
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			entityManager.SetComponentData(entity, component3);
			return entity;
		}

		public Entity Build<T1, T2, T3, T4>(FixedString64Bytes name, T1 component1, T2 component2, T3 component3, T4 component4) 
			 where T1 : unmanaged, IComponentData
			 where T2 : unmanaged, IComponentData
			 where T3 : unmanaged, IComponentData
			 where T4 : unmanaged, IComponentData {
			Entity entity = Build(name);
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			entityManager.SetComponentData(entity, component3);
			entityManager.SetComponentData(entity, component4);
			return entity;
		}

		public Entity Build<T1, T2, T3, T4, T5>(FixedString64Bytes name, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5) 
			 where T1 : unmanaged, IComponentData
			 where T2 : unmanaged, IComponentData
			 where T3 : unmanaged, IComponentData
			 where T4 : unmanaged, IComponentData
			 where T5 : unmanaged, IComponentData {
			Entity entity = Build(name);
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			entityManager.SetComponentData(entity, component3);
			entityManager.SetComponentData(entity, component4);
			entityManager.SetComponentData(entity, component5);
			return entity;
		}

		public EntityBuilder Prefab => AddRO<Prefab>();

		public EntityBuilder Transform => AddRW<LocalTransform, LocalToWorld>();

		public EntityBuilder AddRW<T1>() {
			componentTypeList.Add(ComponentType.ReadWrite<T1>());
			return this;
		}
		public EntityBuilder AddRW<T1, T2>() {
			componentTypeList.AddRange(
				new ComponentType[] {
					ComponentType.ReadWrite<T1>(),
					ComponentType.ReadWrite<T2>(),
				}
			);
			return this;
		}

		public EntityBuilder AddRW<T1, T2, T3>() {
			componentTypeList.AddRange(
				new ComponentType[] {
					ComponentType.ReadWrite<T1>(),
					ComponentType.ReadWrite<T2>(),
					ComponentType.ReadWrite<T3>(),
				}
			);
			return this;
		}

		public EntityBuilder AddRW<T1, T2, T3, T4>() {
			componentTypeList.AddRange(
				new ComponentType[] {
					ComponentType.ReadWrite<T1>(),
					ComponentType.ReadWrite<T2>(),
					ComponentType.ReadWrite<T3>(),
					ComponentType.ReadWrite<T4>(),
				}
			);
			return this;
		}

		public EntityBuilder AddRW<T1, T2, T3, T4, T5>() {
			componentTypeList.AddRange(
				new ComponentType[] {
					ComponentType.ReadWrite<T1>(),
					ComponentType.ReadWrite<T2>(),
					ComponentType.ReadWrite<T3>(),
					ComponentType.ReadWrite<T4>(),
					ComponentType.ReadWrite<T5>(),
				}
			);
			return this;
		}

		public EntityBuilder AddRO<T1>() {
			componentTypeList.Add(ComponentType.ReadOnly<T1>());
			return this;
		}
		public EntityBuilder AddRO<T1, T2>() {
			componentTypeList.AddRange(
				new ComponentType[] {
					ComponentType.ReadOnly<T1>(),
					ComponentType.ReadOnly<T2>(),
				}
			);
			return this;
		}

		public EntityBuilder AddRO<T1, T2, T3>() {
			componentTypeList.AddRange(
				new ComponentType[] {
					ComponentType.ReadOnly<T1>(),
					ComponentType.ReadOnly<T2>(),
					ComponentType.ReadOnly<T3>(),
				}
			);
			return this;
		}

		public EntityBuilder AddRO<T1, T2, T3, T4>() {
			componentTypeList.AddRange(
				new ComponentType[] {
					ComponentType.ReadOnly<T1>(),
					ComponentType.ReadOnly<T2>(),
					ComponentType.ReadOnly<T3>(),
					ComponentType.ReadOnly<T4>(),
				}
			);
			return this;
		}

		public EntityBuilder AddRO<T1, T2, T3, T4, T5>() {
			componentTypeList.AddRange(
				new ComponentType[] {
					ComponentType.ReadOnly<T1>(),
					ComponentType.ReadOnly<T2>(),
					ComponentType.ReadOnly<T3>(),
					ComponentType.ReadOnly<T4>(),
					ComponentType.ReadOnly<T5>(),
				}
			);
			return this;
		}
	}
}