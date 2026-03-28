using skyclad.collider;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace skyclad.control {
	[UpdateInGroup(typeof(SkycladFixedStepSimulationSystemGroup))]
	public partial struct CharacterControlPhysicsSystem : ISystem {
		private const float GROUND_RAD = 35.0f * math.PI / 180.0f;
		private const float SNAP_TO_GROUND_CAST_LENGTH = 0.1f;
		private const float MOVE_EPSILON = 0.01f;

		private EntityQuery updateIsGroundedQuery;
		private EntityQuery snapToGroundQuery;
		private EntityQuery gravityCorrectionQuery;
		private EntityQuery acceptInstructionQuery;
		private EntityQuery digestCharacterActionCueQuery;
		private EntityQuery applyPhysicalQuery;
		private EntityQuery resetEntityQuery;

		void ISystem.OnCreate(ref SystemState state) {
			updateIsGroundedQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<ColliderCollisionStayEvent>()
				.WithAllRW<SkycladCharacterControlComponent>()
				.Build(ref state);
			snapToGroundQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<LocalToWorld, PhysicsCollider>()
				.WithAllRW<LocalTransform, SkycladCharacterControlComponent>()
				.Build(ref state);
			gravityCorrectionQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<PhysicsMass, PhysicsGravityFactor>()
				.WithAllRW<SkycladCharacterControlComponent>()
				.Build(ref state);
			acceptInstructionQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<PhysicsVelocity>()
				.WithAllRW<CharacterControlInstruction, SkycladCharacterControlComponent>()
				.Build(ref state);
			digestCharacterActionCueQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<PhysicsVelocity, PhysicsGravityFactor, CharacterActionCueBufferElement>()
				.WithAllRW<SkycladCharacterControlComponent>()
				.Build(ref state);
			applyPhysicalQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<PhysicsMass, SkycladCharacterControlComponent, LocalToWorld>()
				.WithAllRW<PhysicsVelocity, LocalTransform>()
				.Build(ref state);
			resetEntityQuery = new EntityQueryBuilder(Allocator.Temp)
				.WithAllRW<SkycladCharacterControlComponent>()
				.Build(ref state);
			state.RequireForUpdate<SkycladCharacterControlComponent>();
		}
		void ISystem.OnUpdate(ref SystemState state) {
			if (
				SystemAPI.TryGetSingleton(out PhysicsStep physicsStep) &&
				SystemAPI.TryGetSingleton(out PhysicsWorldSingleton physicsWorld)
			) {
				float3 gravity = physicsStep.Gravity;
				float3 upward = math.normalize(-gravity);
				float groundThreshold = math.cos(GROUND_RAD);
				float dt = state.World.Time.DeltaTime;
				state.Dependency = new UpdateIsGroundedJob {
					upward = upward,
					threshold = groundThreshold,
				}.ScheduleParallel(updateIsGroundedQuery, state.Dependency);

				state.Dependency = new SnapToGroundJob {
					upward = upward,
					groundCos = groundThreshold,
					castLength = SNAP_TO_GROUND_CAST_LENGTH,
					collisionWorld = physicsWorld.CollisionWorld
				}.ScheduleParallel(snapToGroundQuery, state.Dependency);

				state.Dependency = new GravityCorrectionJob {
					gravity = gravity,
					dt = dt,
				}.ScheduleParallel(gravityCorrectionQuery, state.Dependency);

				state.Dependency = new AcceptInstructionJob {
					epsilon = MOVE_EPSILON,
					dt = dt,
					upward = upward,
				}.ScheduleParallel(acceptInstructionQuery, state.Dependency);

				state.Dependency = new DigestCharacterActionCueJob {
					gravity = math.length(gravity),
					upward = upward,
				}.ScheduleParallel(digestCharacterActionCueQuery, state.Dependency);

				state.Dependency = new ApplyPhysicalJob {
					dt = dt,
				}.ScheduleParallel(applyPhysicalQuery, state.Dependency);

				state.Dependency = new ResetEntityJob {
				
				}.ScheduleParallel(resetEntityQuery, state.Dependency);
			}
		}
 		private readonly EntityCommandBuffer CreateCommandBuffer(ref SystemState state) {
			return SystemAPI
				.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.World.Unmanaged);
		}

		/// <summary>
		/// 接地状態の更新
		/// </summary>
		partial struct UpdateIsGroundedJob : IJobEntity {
			[ReadOnly] public float3 upward;
			[ReadOnly] public float threshold;

			void Execute(
				RefRW<SkycladCharacterControlComponent> characterControl, 
				ref DynamicBuffer<ColliderCollisionStayEvent> colliderCollisionStayEvents
			) {
				float current = threshold, cos;
				bool isGrounded = false;
				float3 normal = upward;

				foreach(ColliderCollisionStayEvent evt in colliderCollisionStayEvents) {
					cos = math.dot(evt.Normal, upward);
					if (cos > current) {
						current = cos;
						normal = evt.Normal;
						isGrounded = true;
					}
				}
				characterControl.ValueRW.isGrounded = isGrounded;
				characterControl.ValueRW.normal = normal;
				if (isGrounded) {
					characterControl.ValueRW.snapToGround = true;
				}
			}
		}

		/// <summary>
		/// 坂道の切れ目をちゃんと走らせるために、地上のキャラクターを地面に沿って押し付けるよう移動させる
		/// </summary>
		partial struct SnapToGroundJob : IJobEntity {
			[ReadOnly] public float3 upward;
			[ReadOnly] public float groundCos;
			[ReadOnly] public float castLength;
			[ReadOnly] public CollisionWorld collisionWorld;

			void Execute(
				RefRO<LocalToWorld> localToWorld,
				RefRO<PhysicsCollider> physicsCollider,
				RefRW<SkycladCharacterControlComponent> characterControl,
				RefRW<LocalTransform> localTransform
			) {
				if (characterControl.ValueRO.ignoreSnapToGround) { return; }
				if (!characterControl.ValueRO.snapToGround) { return; }

				unsafe {
					if (physicsCollider.ValueRO.ColliderPtr->Type != ColliderType.Capsule) { return; }

					CapsuleCollider* capsule = (CapsuleCollider*)physicsCollider.ValueRO.ColliderPtr;

					float radius = capsule->Geometry.Radius;
					
					float3 start = localToWorld.ValueRO.Position + upward * radius;
					float3 end = start - upward * castLength;

					ColliderCastInput cast = new ColliderCastInput {
						Collider = physicsCollider.ValueRO.ColliderPtr,
						Orientation = quaternion.identity,
						Start = start,
						End = end,
					};

					if (collisionWorld.CastCollider(cast, out ColliderCastHit hit)) {
						float3 normal = hit.SurfaceNormal;
						if (math.dot(normal, upward) > groundCos) {
							characterControl.ValueRW.isGrounded = true;
							characterControl.ValueRW.normal = normal;
	
							float3 delta = math.lerp(start, end, hit.Fraction) - start;
							Translate(localTransform, localToWorld.ValueRO.Value, delta);
						} else {
							characterControl.ValueRW.snapToGround = false;
						}
					} else {
						characterControl.ValueRW.snapToGround = false;
					}
				}
			}
			
			/// <summary>
			/// グローバル移動量をLocalTransformに適用する
			/// </summary>
			/// <param name="localTransform"></param>
			/// <param name="matrix"></param>
			/// <param name="delta"></param>
			private void Translate(
				RefRW<LocalTransform> localTransform,
				in float4x4 matrix,
				float3 delta
			) {
				localTransform.ValueRW = localTransform.ValueRO.Translate(
					math.mul(matrix, new float4(delta, 0.0f)).xyz
				);
			}
		}

		/// <summary>
		/// 重力を坂道方向に変換することで坂道の滑りを防ぐ
		/// </summary>
		partial struct GravityCorrectionJob : IJobEntity {
			public float3 gravity;
			public float dt;

			void Execute(
				RefRO<PhysicsMass> physicsMass,
				RefRO<PhysicsGravityFactor> physicsGravityFactor,
				RefRW<SkycladCharacterControlComponent> characterControl
			) {
				if (characterControl.ValueRO.isGrounded) {
					float3 normal = characterControl.ValueRO.normal;
					float3 correction = normal * math.dot(normal, gravity) - gravity;
					float factor = physicsGravityFactor.ValueRO.Value;
					characterControl.ValueRW.force += correction * factor / physicsMass.ValueRO.InverseMass;
				}
			}
		}

		/// <summary>
		/// 移動指示を受け取る
		/// </summary>
		partial struct AcceptInstructionJob : IJobEntity {
			[ReadOnly] public float epsilon;
			[ReadOnly] public float dt;
			[ReadOnly] public float3 upward;

			private const float LOOK_DIRECTION_THRESHOLD = 0.2f;

			void Execute(
				RefRO<PhysicsVelocity> physicsVelocity,
				RefRW<CharacterControlInstruction> characterControlInstruction,
				RefRW<SkycladCharacterControlComponent> characterControl
			) {
				float3 dv = CalculateVelocityDelta(
					characterControlInstruction.ValueRO.moveCorrectionSeconds,
					new float3(1.0f, 0.0f, 0.0f),
					float3.zero,
					float3.zero,
					characterControlInstruction.ValueRO.preferMove,
					physicsVelocity.ValueRO.Linear
				);
				characterControl.ValueRW.velocityChanges += dv;

				if (math.any(characterControlInstruction.ValueRO.overrideLookDirection.value != float4.zero)) {
					characterControl.ValueRW.lookDirection = characterControlInstruction.ValueRO.overrideLookDirection;
				} else if (math.lengthsq(characterControlInstruction.ValueRO.preferMove) > LOOK_DIRECTION_THRESHOLD * LOOK_DIRECTION_THRESHOLD) {
					characterControl.ValueRW.lookDirection = quaternion.LookRotation(characterControlInstruction.ValueRO.preferMove, upward);
				}
			}

			/// <summary>
			/// フレーム内での速度の変化量の計算
			/// 各Dirにゼロ成分を設定することでその軸は無視できる。
			/// また、軸は正規化・直交していれば斜めでも可。
			/// </summary>
			/// <param name="correctionSeconds"></param>
			/// <param name="xDir"></param>
			/// <param name="yDir"></param>
			/// <param name="zDir"></param>
			/// <param name="instruction"></param>
			/// <param name="current"></param>
			/// <returns></returns>
			private float3 CalculateVelocityDelta(
				in float correctionSeconds,
				in float3 xDir,
				in float3 yDir,
				in float3 zDir,
				in float3 instruction,
				in float3 current
			) {
				// フレームレートをfとして、
				// t秒後に1がε以下になる減衰比率pを考える
				// 漸化式はステップをsとすると、sMax = ftかつ、
				// a(s+1) = (1-p)a(s), a(0) = 1であり、つまり公比1-pの等比数列。
				// よって(1-p)^ft < εとなるようにする
				// これを解くと、p > 1-ε^(1/(ft))
				// つまりp = 1 - ε^(1/(ft)) = 1 - ε^(dt/t)となるようにpを決めれば、
				// t程度の時間でεまで減衰することになる
				float alpha = 1.0f - math.pow(epsilon, dt / correctionSeconds);
				float3x3 matrix = new float3x3(xDir, yDir, zDir);
				
				// x, y, z方向それぞれの理想と現状の差分
				// xの成分はmath.dot(xDir, instruction - current)
				// yの成分はmath.dot(yDir, instruction - current)
				// zの成分はmath.dot(zDir, instruction - current)
				// 行列に変換できる
				float3 sub = math.mul(matrix, instruction - current);

				// subはxDir, yDir, zDirの成分割合を示すので、
				// 最終的な変化量はそれらにかけ合わせる。
				// xDir * x + yDir * y + zDir * z
				return math.mul(alpha * sub, math.transpose(matrix));
			}
		}

		partial struct DigestCharacterActionCueJob : IJobEntity {
			[ReadOnly] public float gravity;
			[ReadOnly] public float3 upward;

			void Execute(
				ref DynamicBuffer<CharacterActionCueBufferElement> characterActionCues,
				RefRO<PhysicsVelocity> physicsVelocity,
				RefRO<PhysicsGravityFactor> physicsGravityFactor,
				RefRW<SkycladCharacterControlComponent> characterControl
			) {
				foreach(CharacterActionCueBufferElement cue in characterActionCues) {
					switch(cue.action) {
						case CharacterAction.Jump: {
							Jump(physicsVelocity, physicsGravityFactor, characterControl, 4.0f);
							break;
						}
					}
				}
				characterActionCues.Clear();
			}

			private void Jump(
				RefRO<PhysicsVelocity> physicsVelocity,
				RefRO<PhysicsGravityFactor> physicsGravityFactor,
				RefRW<SkycladCharacterControlComponent> characterControl,
				float height
			) {
				float g = physicsGravityFactor.ValueRO.Value * gravity;
				
				// v^2 - v0^2 = 2gh
				// 高さhではv0^2 = 0とすると、必要な速度はsqrt(2gh)
				float dot = math.dot(physicsVelocity.ValueRO.Linear, upward);
				float dv = math.sqrt(2 * g * height) - dot;

				characterControl.ValueRW.velocityChanges += dv * upward;
				characterControl.ValueRW.snapToGround = false;
			}
		}

		/// <summary>
		/// キャラクターに設定された力の情報を移動に反映する
		/// キャラクターの向きを更新する
		/// </summary>
		partial struct ApplyPhysicalJob : IJobEntity {
			[ReadOnly] public float dt;

			void Execute(
				RefRO<PhysicsMass> physicsMass,
				RefRO<SkycladCharacterControlComponent> characterControl,
				RefRO<LocalToWorld> localToWorld,
				RefRW<LocalTransform> localTransform,
				RefRW<PhysicsVelocity> physicsVelocity
			) {
				float3 dv = characterControl.ValueRO.force * dt * physicsMass.ValueRO.InverseMass;
				physicsVelocity.ValueRW.Linear += dv + characterControl.ValueRO.velocityChanges;

				UpdateLocalRotation(localTransform, localToWorld, characterControl.ValueRO.lookDirection);
			}

			private void UpdateLocalRotation(
				RefRW<LocalTransform> localTransform, 
				RefRO<LocalToWorld> localToWorld,
				in quaternion direction
			) {
				if (math.any(direction.value != float4.zero)) {
					quaternion parentRot =  math.mul(localToWorld.ValueRO.Rotation, math.inverse(localTransform.ValueRO.Rotation));
					localTransform.ValueRW.Rotation = math.mul(math.inverse(parentRot), direction);
				}
			}
		}

		/// <summary>
		/// フレームパラメーターの初期化
		/// </summary>
		partial struct ResetEntityJob : IJobEntity {
			void Execute(
				RefRW<SkycladCharacterControlComponent> characterControl
			) {
				characterControl.ValueRW.force = float3.zero;
				characterControl.ValueRW.velocityChanges = float3.zero;
				characterControl.ValueRW.lookDirection = float4.zero;
			}
		}
	}
}