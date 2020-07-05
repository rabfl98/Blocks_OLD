using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Blocks
{
	[UpdateInGroup(typeof(DefaultSystemGroup))]
	public class BlockCollisionUpdateSystem : SystemBase
	{
		[BurstCompile]
		public struct BlockCommonTraversalJob : IJobChunk
		{
			[ReadOnly] public ArchetypeChunkComponentType<BlockPlayerCollisionData> blockPlayerCollisionType;
			public ArchetypeChunkComponentType<BlockTraversalData> blockTraversalType;
			public ArchetypeChunkComponentType<BlockDissolveData> blockDissolveType;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				NativeArray<BlockPlayerCollisionData> blockCollisions = chunk.GetNativeArray(this.blockPlayerCollisionType);
				NativeArray<BlockTraversalData> blockTraversals = chunk.GetNativeArray(this.blockTraversalType);
				NativeArray<BlockDissolveData> blockDissolves = chunk.GetNativeArray(this.blockDissolveType);

				for (int ci = 0, cn = chunk.Count; ci < cn; ci++) {
					// Update common block traversal logic

					BlockPlayerCollisionData blockCollision = blockCollisions[ci];
					BlockTraversalData blockTraversal = blockTraversals[ci];
					BlockDissolveData blockDissolve = blockDissolves[ci];

					// Add to traversal count when exiting block
					if (blockCollision.triggerState == PhysicsTriggerState.Exit) {
						bool maxTraversalReached = blockTraversal.IncrementTraversalCount();

						// Mark block for dissolve if max traversal reached
						if (maxTraversalReached) {
							blockDissolve.isDissolving = maxTraversalReached;
						}
					}

					// Apply changes
					blockTraversals[ci] = blockTraversal;
					blockDissolves[ci] = blockDissolve;
				}
			}
		}

		private EntityQuery blockQuery;

		protected override void OnCreate()
		{
			// Query for block
			this.blockQuery = GetEntityQuery(typeof(Tag_Block));
		}

		protected override void OnUpdate()
		{
			// Job, update common traversal data
			BlockCommonTraversalJob blockTraversalJob = new BlockCommonTraversalJob() {
				blockPlayerCollisionType = GetArchetypeChunkComponentType<BlockPlayerCollisionData>(true),
				blockTraversalType = GetArchetypeChunkComponentType<BlockTraversalData>(false),
				blockDissolveType = GetArchetypeChunkComponentType<BlockDissolveData>(false),
			};
			JobHandle blockTraversalJobHandle = blockTraversalJob.ScheduleParallel(this.blockQuery, this.Dependency);

			this.Dependency = blockTraversalJobHandle;
		}
	}
}