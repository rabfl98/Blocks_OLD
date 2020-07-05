using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace Blocks
{
	[UpdateInGroup(typeof(DefaultSystemGroup))]
	public class SwitchBlockSystem : SystemBase
	{
		[BurstCompile]
		public struct ActivateSwitchJob : IJobChunk
		{
			[ReadOnly] public ArchetypeChunkComponentType<BlockPlayerCollisionData> blockPlayerCollisionType;
			public ArchetypeChunkComponentType<BlockSwitchData> blockSwitchType;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				NativeArray<BlockPlayerCollisionData> blockCollisions = chunk.GetNativeArray(this.blockPlayerCollisionType);
				NativeArray<BlockSwitchData> blockSwitches = chunk.GetNativeArray(this.blockSwitchType);
				
				for (int ci = 0, cn = chunk.Count; ci < cn; ci++) {
					// Activate switch if entered block

					BlockPlayerCollisionData blockCollision = blockCollisions[ci];
					BlockSwitchData blockSwitch = blockSwitches[ci];

					if (blockCollision.triggerState == PhysicsTriggerState.Enter) {
						blockSwitch.Activate();
					}

					// Apply changes
					blockSwitches[ci] = blockSwitch;
				}
			}
		}

		[BurstCompile]
		public struct CleanupSwitchJob : IJobChunk
		{
			public ArchetypeChunkComponentType<BlockSwitchData> blockSwitchType;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				NativeArray<BlockSwitchData> blockSwitches = chunk.GetNativeArray(this.blockSwitchType);

				for (int ci = 0, cn = chunk.Count; ci < cn; ci++) {
					// Reset activate flag

					BlockSwitchData blockSwitch = blockSwitches[ci];
					if (blockSwitch.wasJustActivated) {
						blockSwitch.wasJustActivated = false;

						// Apply changes
						blockSwitches[ci] = blockSwitch;
					}
				}
			}
		}

		[BurstCompile]
		public struct GroupAppearJob : IJobChunk
		{
			[ReadOnly] public ArchetypeChunkComponentType<BlockSwitchData> blockSwitchType;
			[ReadOnly] public ArchetypeChunkComponentType<SwitchGroupAppearData> switchGroupAppearType;

			[ReadOnly] public BufferFromEntity<EntityElement> entityBuffer;
			[ReadOnly] public BufferFromEntity<Child> childrenBuffer;

			public EntityCommandBuffer.Concurrent ecb;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				NativeArray<BlockSwitchData> blockSwitches = chunk.GetNativeArray(this.blockSwitchType);
				NativeArray<SwitchGroupAppearData> switchGroupAppears = chunk.GetNativeArray(this.switchGroupAppearType);

				for (int ci = 0, cn = chunk.Count; ci < cn; ci++) {
					// Show blocks in group if switch activated

					BlockSwitchData blockSwitch = blockSwitches[ci];
					SwitchGroupAppearData switchGroupAppear = switchGroupAppears[ci];

					if (blockSwitch.wasJustActivated && blockSwitch.currentState == BlockSwitchState.On) {
						// Show group blocks
						
						if (this.entityBuffer.Exists(switchGroupAppear.groupRoot)) {
							DynamicBuffer<EntityElement> entityBuff = this.entityBuffer[switchGroupAppear.groupRoot];

							for (int i = 0, n = entityBuff.Length; i < n; i++) {
								EntityUtils.EnableEntityHierarchy(ref ecb, chunkIndex, entityBuff[i], in this.childrenBuffer);
							}
						}
					}
				}
			}
		}

		private EndSimulationEntityCommandBufferSystem endSimulationEntitySystem;

		private EntityQuery switchBlockQuery;
		private EntityQuery switchGroupAppearBlockQuery;

		protected override void OnCreate()
		{
			this.endSimulationEntitySystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

			// Query for block
			this.switchBlockQuery = GetEntityQuery(typeof(Tag_SwitchBlock));
			this.switchGroupAppearBlockQuery = GetEntityQuery(typeof(Tag_SwitchGroupAppearBlock));
		}

		protected override void OnUpdate()
		{
			// Job, activate switches
			ActivateSwitchJob activateSwitchJob = new ActivateSwitchJob() { 
				blockPlayerCollisionType = GetArchetypeChunkComponentType<BlockPlayerCollisionData>(true),
				blockSwitchType = GetArchetypeChunkComponentType<BlockSwitchData>(false),
			};
			JobHandle activateSwitchJobHandle = activateSwitchJob.ScheduleParallel(this.switchBlockQuery, this.Dependency);

			// Job, group appear switch
			EntityCommandBuffer.Concurrent ecb = this.endSimulationEntitySystem.CreateCommandBuffer().ToConcurrent();
			GroupAppearJob groupAppearJob = new GroupAppearJob() {
				blockSwitchType = GetArchetypeChunkComponentType<BlockSwitchData>(true),
				switchGroupAppearType = GetArchetypeChunkComponentType<SwitchGroupAppearData>(true),
				entityBuffer = GetBufferFromEntity<EntityElement>(true),
				childrenBuffer = GetBufferFromEntity<Child>(true),
				ecb = ecb,
			};
			JobHandle groupAppearJobHandle = groupAppearJob.ScheduleParallel(this.switchGroupAppearBlockQuery, activateSwitchJobHandle);

			JobHandle cleanupDependency = groupAppearJobHandle; //JobHandle.CombineDependencies(groupAppearJobHandle);

			// Job, cleanup switches
			CleanupSwitchJob cleanupSwitchJob = new CleanupSwitchJob() {
				blockSwitchType = GetArchetypeChunkComponentType<BlockSwitchData>(false),
			};
			JobHandle cleanupSwitchJobHandle = cleanupSwitchJob.ScheduleParallel(this.switchBlockQuery, cleanupDependency);

			this.Dependency = cleanupSwitchJobHandle;

			this.endSimulationEntitySystem.AddJobHandleForProducer(this.Dependency);
		}
	}
}