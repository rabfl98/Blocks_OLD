using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Blocks
{
	[UpdateAfter(typeof(EndFramePhysicsSystem))]
	public class PlayerBlockCollisionSystem : SystemBase
	{
		public struct TriggerEventJob : ITriggerEventsJob
		{
			[ReadOnly] public ComponentDataFromEntity<Tag_Player> playerGroup;
			[ReadOnly] public ComponentDataFromEntity<BlockPlayerCollisionData> blockGroup;

			public NativeHashMap<Entity, bool> outTriggerEntities;

			public void Execute(TriggerEvent triggerEvent)
			{
				// Return blocks which are colliding with player

				if (this.blockGroup.Exists(triggerEvent.Entities.EntityA)
					&& this.playerGroup.Exists(triggerEvent.Entities.EntityB)) {
					this.outTriggerEntities.Add(triggerEvent.Entities.EntityA, true);
				}
				if (this.blockGroup.Exists(triggerEvent.Entities.EntityB)
					&& this.playerGroup.Exists(triggerEvent.Entities.EntityA)) {
					this.outTriggerEntities.Add(triggerEvent.Entities.EntityB, true);
				}
			}
		}

		[BurstCompile]
		public struct UpdateBlockTriggerStateJob : IJobChunk
		{
			[ReadOnly] public NativeHashMap<Entity, bool> triggerEntities;

			public ArchetypeChunkComponentType<BlockPlayerCollisionData> blockPlayerCollisionType;
			[ReadOnly] public ArchetypeChunkEntityType entityType;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				NativeArray<BlockPlayerCollisionData> blockCollisions = chunk.GetNativeArray(this.blockPlayerCollisionType);
				NativeArray<Entity> entities = chunk.GetNativeArray(this.entityType);
				
				for (int ci = 0, cn = chunk.Count; ci < cn; ci++) {
					// Update trigger event type for block collision

					Entity entity = entities[ci];
					BlockPlayerCollisionData blockCollision = blockCollisions[ci];

					// Check if this block entity currently is colliding (is in passed triggerEntities dic)
					if (this.triggerEntities.ContainsKey(entity)) {
						// If colliding, update none/exit to enter, enter to stay

						switch (blockCollision.triggerState) {
						case PhysicsTriggerState.None:
						case PhysicsTriggerState.Exit:
							blockCollision.triggerState = PhysicsTriggerState.Enter;
							break;

						case PhysicsTriggerState.Enter:
						case PhysicsTriggerState.Stay:
							blockCollision.triggerState = PhysicsTriggerState.Stay;
							break;
						}

					} else {
						// If not colliding, update enter/stay to exit, and exit to none

						switch (blockCollision.triggerState) {
						case PhysicsTriggerState.Enter:
						case PhysicsTriggerState.Stay:
							blockCollision.triggerState = PhysicsTriggerState.Exit;
							break;

						case PhysicsTriggerState.Exit:
						case PhysicsTriggerState.None:
							blockCollision.triggerState = PhysicsTriggerState.None;
							break;
						}
					}
					
					// Apply changes
					blockCollisions[ci] = blockCollision;
				}
			}
		}

		private BuildPhysicsWorld buildPhysicsWorldSystem;
		private StepPhysicsWorld stepPhysicsWorldSystem;

		private EntityQuery blockQuery;
		
		private NativeHashMap<Entity, bool> triggerEntities;

		protected override void OnCreate()
		{
			// Physics worlds for collision
			this.buildPhysicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
			this.stepPhysicsWorldSystem = World.GetExistingSystem<StepPhysicsWorld>();

			// Query for block
			this.blockQuery = GetEntityQuery(typeof(Tag_Block));

			// Trigger entities map
			this.triggerEntities = new NativeHashMap<Entity, bool>(4, Allocator.Persistent);
		}

		protected override void OnDestroy()
		{
			this.triggerEntities.Dispose();
		}

		protected override void OnUpdate()
		{
			// Job, get currently colliding blocks
			this.triggerEntities.Clear();
			TriggerEventJob triggerEventJob = new TriggerEventJob() {
				playerGroup = GetComponentDataFromEntity<Tag_Player>(true),
				blockGroup = GetComponentDataFromEntity<BlockPlayerCollisionData>(true),
				outTriggerEntities = triggerEntities,
			};
			JobHandle triggerEventJobHandle = triggerEventJob.Schedule(this.stepPhysicsWorldSystem.Simulation, ref this.buildPhysicsWorldSystem.PhysicsWorld, this.Dependency);

			// Job, update block trigger state
			UpdateBlockTriggerStateJob updateBlockTriggerStateJob = new UpdateBlockTriggerStateJob() {
				triggerEntities = triggerEntities,
				blockPlayerCollisionType = GetArchetypeChunkComponentType<BlockPlayerCollisionData>(false),
				entityType = GetArchetypeChunkEntityType(),
			};
			JobHandle updateBlockTriggerStateJobHandle = updateBlockTriggerStateJob.ScheduleParallel(this.blockQuery, triggerEventJobHandle);

			this.Dependency = updateBlockTriggerStateJobHandle;
		}
	}
}