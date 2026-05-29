using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace skyclad {
	public static class EntityBuilderExtensions {
		public static ISingleEntityBuilder CreateEntityBuilder(this EntityManager entityManager) {
			return EntityBuilder.Create(entityManager);
		}

		public static ISingleEntityBuilder CreateEntityBuilder(this ref SystemState state) {
			return state.EntityManager.CreateEntityBuilder();
		}

		public static IEntityBuilder CreateEntityBuilder(this EntityManager entityManager, EntityArchetype archetype) {
			return ArchetypeEntityBuilder.Create(entityManager, archetype);
		}
	}

	public interface ISingleEntityBuilder : IEntityBuilder {
		IEntityBuilder AsArchetypeBuilder { get; }

		ISingleEntityBuilder Transform => AddRW<LocalTransform, LocalToWorld>();
		ISingleEntityBuilder Prefab => AddRO<Prefab>();

		ISingleEntityBuilder AddRW<T1>();
		ISingleEntityBuilder AddRW<T1, T2>();
		ISingleEntityBuilder AddRW<T1, T2, T3>();
		ISingleEntityBuilder AddRW<T1, T2, T3, T4>();
		ISingleEntityBuilder AddRW<T1, T2, T3, T4, T5>();
		ISingleEntityBuilder AddRO<T1>();
		ISingleEntityBuilder AddRO<T1, T2>();
		ISingleEntityBuilder AddRO<T1, T2, T3>();
		ISingleEntityBuilder AddRO<T1, T2, T3, T4>();
		ISingleEntityBuilder AddRO<T1, T2, T3, T4, T5>();
	}

	public interface IEntityBuilder {
		Entity Build();
		Entity Build(FixedString64Bytes name);
		Entity Build<T1>(T1 component1) 
			where T1 : unmanaged, IComponentData;
		Entity Build<T1, T2>(T1 component1, T2 component2)
			where T1 : unmanaged, IComponentData
			where T2 : unmanaged, IComponentData;

		Entity Build<T1, T2, T3>(T1 component1, T2 component2, T3 component3)
			where T1 : unmanaged, IComponentData
			where T2 : unmanaged, IComponentData
			where T3 : unmanaged, IComponentData;
		Entity Build<T1, T2, T3, T4>(T1 component1, T2 component2, T3 component3, T4 component4)
			where T1 : unmanaged, IComponentData
			where T2 : unmanaged, IComponentData
			where T3 : unmanaged, IComponentData
			where T4 : unmanaged, IComponentData;
		Entity Build<T1, T2, T3, T4, T5>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5)
			where T1 : unmanaged, IComponentData
			where T2 : unmanaged, IComponentData
			where T3 : unmanaged, IComponentData
			where T4 : unmanaged, IComponentData
			where T5 : unmanaged, IComponentData;

		Entity Build<T1>(FixedString64Bytes name, T1 component1) 
			where T1 : unmanaged, IComponentData;
		Entity Build<T1, T2>(FixedString64Bytes name, T1 component1, T2 component2)
			where T1 : unmanaged, IComponentData
			where T2 : unmanaged, IComponentData;

		Entity Build<T1, T2, T3>(FixedString64Bytes name, T1 component1, T2 component2, T3 component3)
			where T1 : unmanaged, IComponentData
			where T2 : unmanaged, IComponentData
			where T3 : unmanaged, IComponentData;
		Entity Build<T1, T2, T3, T4>(FixedString64Bytes name, T1 component1, T2 component2, T3 component3, T4 component4)
			where T1 : unmanaged, IComponentData
			where T2 : unmanaged, IComponentData
			where T3 : unmanaged, IComponentData
			where T4 : unmanaged, IComponentData;
		Entity Build<T1, T2, T3, T4, T5>(FixedString64Bytes name, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5)
			where T1 : unmanaged, IComponentData
			where T2 : unmanaged, IComponentData
			where T3 : unmanaged, IComponentData
			where T4 : unmanaged, IComponentData
			where T5 : unmanaged, IComponentData;
	}

	internal struct ArchetypeEntityBuilder : IEntityBuilder {
		private EntityManager entityManager;
		private EntityArchetype archetype;
		internal static ArchetypeEntityBuilder Create(EntityManager entityManager, EntityArchetype archetype) {
			return new ArchetypeEntityBuilder {
				entityManager = entityManager,
				archetype = archetype,
			};
		}

		private Entity BuildInternal() {
			return entityManager.CreateEntity(archetype);
		}

		private Entity BuildInternal(FixedString64Bytes name) {
			Entity entity = entityManager.CreateEntity(archetype);
			#if UNITY_EDITOR
			entityManager.SetName(entity, name);
			#endif

			return entity;
		}

		Entity IEntityBuilder.Build() { return BuildInternal(); }

		Entity IEntityBuilder.Build(FixedString64Bytes name) { return BuildInternal(name); }

		Entity IEntityBuilder.Build<T1>(T1 component1) {
			Entity entity = BuildInternal();
			entityManager.SetComponentData(entity, component1);
			return entity;
		}

		Entity IEntityBuilder.Build<T1, T2>(T1 component1, T2 component2) {
			Entity entity = BuildInternal();
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			return entity;
		}

		Entity IEntityBuilder.Build<T1, T2, T3>(T1 component1, T2 component2, T3 component3) {
			Entity entity = BuildInternal();
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			entityManager.SetComponentData(entity, component3);
			return entity;
		}

		Entity IEntityBuilder.Build<T1, T2, T3, T4>(T1 component1, T2 component2, T3 component3, T4 component4) {
			Entity entity = BuildInternal();
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			entityManager.SetComponentData(entity, component3);
			entityManager.SetComponentData(entity, component4);
			return entity;
		}

		Entity IEntityBuilder.Build<T1, T2, T3, T4, T5>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5) {
			Entity entity = BuildInternal();
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			entityManager.SetComponentData(entity, component3);
			entityManager.SetComponentData(entity, component4);
			entityManager.SetComponentData(entity, component5);
			return entity;
		}

		Entity IEntityBuilder.Build<T1>(FixedString64Bytes name, T1 component1) {
			Entity entity = BuildInternal(name);
			entityManager.SetComponentData(entity, component1);
			return entity;
		}

		Entity IEntityBuilder.Build<T1, T2>(FixedString64Bytes name, T1 component1, T2 component2) {
			Entity entity = BuildInternal(name);
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			return entity;
		}

		Entity IEntityBuilder.Build<T1, T2, T3>(FixedString64Bytes name, T1 component1, T2 component2, T3 component3) {
			Entity entity = BuildInternal(name);
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			entityManager.SetComponentData(entity, component3);
			return entity;
		}

		Entity IEntityBuilder.Build<T1, T2, T3, T4>(FixedString64Bytes name, T1 component1, T2 component2, T3 component3, T4 component4)  {
			Entity entity = BuildInternal(name);
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			entityManager.SetComponentData(entity, component3);
			entityManager.SetComponentData(entity, component4);
			return entity;
		}

		Entity IEntityBuilder.Build<T1, T2, T3, T4, T5>(FixedString64Bytes name, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5)  {
			Entity entity = BuildInternal(name);
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			entityManager.SetComponentData(entity, component3);
			entityManager.SetComponentData(entity, component4);
			entityManager.SetComponentData(entity, component5);
			return entity;
		}
	}

	public struct EntityBuilder : ISingleEntityBuilder {
		private EntityManager entityManager;
		private List<ComponentType> componentTypeList;

		IEntityBuilder ISingleEntityBuilder.AsArchetypeBuilder => ArchetypeEntityBuilder.Create(
			entityManager, 
			entityManager.CreateArchetype(componentTypeList.ToArray())
		);

		internal static EntityBuilder Create(EntityManager entityManager) {
			return new EntityBuilder {
				entityManager = entityManager,
				componentTypeList = new List<ComponentType>(),
			};
		}

		private Entity BuildInternal() {
			return entityManager.CreateEntity(
				entityManager.CreateArchetype(componentTypeList.ToArray())
			);
		}

		private Entity BuildInternal(FixedString64Bytes name) {
			Entity entity = entityManager.CreateEntity(
				entityManager.CreateArchetype(componentTypeList.ToArray())
			);
			#if UNITY_EDITOR
			entityManager.SetName(entity, name);
			#endif

			return entity;
		}

		Entity IEntityBuilder.Build() { return BuildInternal(); }

		Entity IEntityBuilder.Build(FixedString64Bytes name) { return BuildInternal(name); }

		Entity IEntityBuilder.Build<T1>(T1 component1) {
			Entity entity = BuildInternal();
			entityManager.SetComponentData(entity, component1);
			return entity;
		}

		Entity IEntityBuilder.Build<T1, T2>(T1 component1, T2 component2) {
			Entity entity = BuildInternal();
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			return entity;
		}

		Entity IEntityBuilder.Build<T1, T2, T3>(T1 component1, T2 component2, T3 component3) {
			Entity entity = BuildInternal();
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			entityManager.SetComponentData(entity, component3);
			return entity;
		}

		Entity IEntityBuilder.Build<T1, T2, T3, T4>(T1 component1, T2 component2, T3 component3, T4 component4) {
			Entity entity = BuildInternal();
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			entityManager.SetComponentData(entity, component3);
			entityManager.SetComponentData(entity, component4);
			return entity;
		}

		Entity IEntityBuilder.Build<T1, T2, T3, T4, T5>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5) {
			Entity entity = BuildInternal();
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			entityManager.SetComponentData(entity, component3);
			entityManager.SetComponentData(entity, component4);
			entityManager.SetComponentData(entity, component5);
			return entity;
		}

		Entity IEntityBuilder.Build<T1>(FixedString64Bytes name, T1 component1) {
			Entity entity = BuildInternal(name);
			entityManager.SetComponentData(entity, component1);
			return entity;
		}

		Entity IEntityBuilder.Build<T1, T2>(FixedString64Bytes name, T1 component1, T2 component2) {
			Entity entity = BuildInternal(name);
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			return entity;
		}

		Entity IEntityBuilder.Build<T1, T2, T3>(FixedString64Bytes name, T1 component1, T2 component2, T3 component3) {
			Entity entity = BuildInternal(name);
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			entityManager.SetComponentData(entity, component3);
			return entity;
		}

		Entity IEntityBuilder.Build<T1, T2, T3, T4>(FixedString64Bytes name, T1 component1, T2 component2, T3 component3, T4 component4) {
			Entity entity = BuildInternal(name);
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			entityManager.SetComponentData(entity, component3);
			entityManager.SetComponentData(entity, component4);
			return entity;
		}

		Entity IEntityBuilder.Build<T1, T2, T3, T4, T5>(FixedString64Bytes name, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5) {
			Entity entity = BuildInternal(name);
			entityManager.SetComponentData(entity, component1);
			entityManager.SetComponentData(entity, component2);
			entityManager.SetComponentData(entity, component3);
			entityManager.SetComponentData(entity, component4);
			entityManager.SetComponentData(entity, component5);
			return entity;
		}

		ISingleEntityBuilder ISingleEntityBuilder.AddRW<T1>() {
			componentTypeList.Add(ComponentType.ReadWrite<T1>());
			return this;
		}
		ISingleEntityBuilder ISingleEntityBuilder.AddRW<T1, T2>() {
			componentTypeList.AddRange(
				new ComponentType[] {
					ComponentType.ReadWrite<T1>(),
					ComponentType.ReadWrite<T2>(),
				}
			);
			return this;
		}

		ISingleEntityBuilder ISingleEntityBuilder.AddRW<T1, T2, T3>() {
			componentTypeList.AddRange(
				new ComponentType[] {
					ComponentType.ReadWrite<T1>(),
					ComponentType.ReadWrite<T2>(),
					ComponentType.ReadWrite<T3>(),
				}
			);
			return this;
		}

		ISingleEntityBuilder ISingleEntityBuilder.AddRW<T1, T2, T3, T4>() {
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

		ISingleEntityBuilder ISingleEntityBuilder.AddRW<T1, T2, T3, T4, T5>() {
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

		ISingleEntityBuilder ISingleEntityBuilder.AddRO<T1>() {
			componentTypeList.Add(ComponentType.ReadOnly<T1>());
			return this;
		}
		ISingleEntityBuilder ISingleEntityBuilder.AddRO<T1, T2>() {
			componentTypeList.AddRange(
				new ComponentType[] {
					ComponentType.ReadOnly<T1>(),
					ComponentType.ReadOnly<T2>(),
				}
			);
			return this;
		}

		ISingleEntityBuilder ISingleEntityBuilder.AddRO<T1, T2, T3>() {
			componentTypeList.AddRange(
				new ComponentType[] {
					ComponentType.ReadOnly<T1>(),
					ComponentType.ReadOnly<T2>(),
					ComponentType.ReadOnly<T3>(),
				}
			);
			return this;
		}

		ISingleEntityBuilder ISingleEntityBuilder.AddRO<T1, T2, T3, T4>() {
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

		ISingleEntityBuilder ISingleEntityBuilder.AddRO<T1, T2, T3, T4, T5>() {
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