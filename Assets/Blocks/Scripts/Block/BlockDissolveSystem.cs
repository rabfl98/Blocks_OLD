using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Rendering;

namespace Blocks
{
	[UpdateInGroup(typeof(DefaultSystemGroup))]
	public class BlockDissolveSystem : SystemBase
	{
		[BurstCompile]
		public struct BlockDissolveJob : IJobChunk
		{
			[ReadOnly] public ArchetypeChunkEntityType entityType;
			[ReadOnly] public BufferFromEntity<Child> childrenBuffer;
			[ReadOnly] public ArchetypeChunkComponentType<BlockGridNodeData> blockGridNodeType;
			public ArchetypeChunkComponentType<BlockDissolveData> blockDissolveType;

			public NativeArray<int> outNumDissolvedNodes;
			public NativeArray<int> outDissolvedNodeIndices;

			public EntityCommandBuffer.Concurrent ecb;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				NativeArray<Entity> entities = chunk.GetNativeArray(this.entityType);
				NativeArray<BlockDissolveData> blockDissolves = chunk.GetNativeArray(this.blockDissolveType);
				NativeArray<BlockGridNodeData> blockGridNodes = chunk.GetNativeArray(this.blockGridNodeType);

				for (int ci = 0, cn = chunk.Count; ci < cn; ci++) {
					// Update block dissolve

					BlockDissolveData blockDissolve = blockDissolves[ci];
					BlockGridNodeData blockGridNode = blockGridNodes[ci];

					// Update while block is dissolving
					if (blockDissolve.isDissolving) {
						this.outDissolvedNodeIndices[this.outNumDissolvedNodes[0]++] = (blockGridNode.Index);

						// TEMP: Immediately disable on dissolve
						EntityUtils.DisableEntityHierarchy(ref this.ecb, chunkIndex, entities[ci], in this.childrenBuffer);
					}

					// Apply changes
					blockDissolves[ci] = blockDissolve;
				}
			}
		}

		[BurstCompile]
		public struct UpdateNavigationGridJob : IJobChunk
		{
			[DeallocateOnJobCompletion] // Deallocate after job finishes
			[ReadOnly]
			public NativeArray<int> numDissolvedNodes;

			[ReadOnly]
			public NativeArray<int> dissolvedNodeIndices;

			public ArchetypeChunkBufferType<AStarNodeElement> nodeBufferType;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				BufferAccessor<AStarNodeElement> nodeBufferAccessor = chunk.GetBufferAccessor(this.nodeBufferType);

				for (int ci = 0, cn = chunk.Count; ci < cn; ci++) {
					DynamicBuffer<AStarNodeElement> nodeBuffer = nodeBufferAccessor[ci];

					// Mark dissolved nodes as not traverseable

					for (int i = 0, n = this.numDissolvedNodes[0]; i < n; i++) {
						int index = this.dissolvedNodeIndices[i];
						for (int j = 0, m = nodeBuffer.Length; j < m; j++) {
							if (nodeBuffer[j].Value.Index == index) {
								AStarNode node = nodeBuffer[j];
								node.IsTraverseable = false;
								nodeBuffer[j] = node;
								break;
							}
						}
					}
				}
			}
		}

		private EntityQuery blockQuery;
		private EntityQuery gridQuery;

		private EndSimulationEntityCommandBufferSystem endSimulationEntitySystem;

		private NativeArray<int> dissolvedNodeIndices;

		protected override void OnCreate()
		{
			this.endSimulationEntitySystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

			// Query for block, grid
			this.blockQuery = GetEntityQuery(typeof(Tag_Block));
			this.gridQuery = GetEntityQuery(typeof(Tag_NavigationGrid));

			// List
			this.dissolvedNodeIndices = new NativeArray<int>(GridConstants.MAX_GRID_LINEAR_LENGTH, Allocator.Persistent);
		}

		protected override void OnDestroy()
		{
			this.dissolvedNodeIndices.Dispose();
		}

		protected override void OnUpdate()
		{
			// ECB to add components
			EntityCommandBuffer.Concurrent ecb = this.endSimulationEntitySystem.CreateCommandBuffer().ToConcurrent();

			// Job, update dissolve
			NativeArray<int> numDissolvedNodes = new NativeArray<int>(1, Allocator.TempJob);
			BlockDissolveJob blockDissolveJob = new BlockDissolveJob() {
				entityType = GetArchetypeChunkEntityType(),
				childrenBuffer = GetBufferFromEntity<Child>(true),
				blockGridNodeType = GetArchetypeChunkComponentType<BlockGridNodeData>(true),
				blockDissolveType = GetArchetypeChunkComponentType<BlockDissolveData>(false),
				outNumDissolvedNodes = numDissolvedNodes,
				outDissolvedNodeIndices = this.dissolvedNodeIndices,
				ecb = ecb,
			};
			JobHandle blockDissolveJobHandle = blockDissolveJob.ScheduleParallel(this.blockQuery, this.Dependency);

			// Job, update grid nodes
			UpdateNavigationGridJob updateNavigationGridJob = new UpdateNavigationGridJob() {
				numDissolvedNodes = numDissolvedNodes,
				dissolvedNodeIndices = this.dissolvedNodeIndices,
				nodeBufferType = GetArchetypeChunkBufferType<AStarNodeElement>(false),
			};
			JobHandle updateNavigationGridJobHandle = updateNavigationGridJob.ScheduleParallel(this.gridQuery, blockDissolveJobHandle);

			this.Dependency = updateNavigationGridJobHandle;

			this.endSimulationEntitySystem.AddJobHandleForProducer(this.Dependency);
		}
	}
}