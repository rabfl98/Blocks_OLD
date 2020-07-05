using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Blocks
{
	[UpdateInGroup(typeof(InputGroup))]
	public class PlayerMovementInputSystem : SystemBase
	{
		[BurstCompile]
		public struct PointerRaycastJob : IJob
		{
			[ReadOnly] public CollisionWorld collisionWorld;
			[ReadOnly] public RaycastInput input;

			public NativeArray<RaycastHit> outHit;

			public void Execute()
			{
				// Raycast
				RaycastHit hit;
				if (this.collisionWorld.CastRay(this.input, out hit)) {
					outHit[0] = hit;
				}
			}
		}

		[BurstCompile]
		public struct GetCurrentNodeJob : IJobChunk
		{
			public NativeArray<PlayerNavigationNode> outCurrentNode;

			[ReadOnly] public ArchetypeChunkComponentType<PlayerMovementData> playerMovementType;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				NativeArray<PlayerMovementData> playerMovements = chunk.GetNativeArray(this.playerMovementType);

				for (int ci = 0, cn = chunk.Count; ci < cn; ci++) {
					// Return current node as path start
					outCurrentNode[0] = playerMovements[ci].currentNode;
				}
			}
		}

		[BurstCompile]
		public struct RequestPathJob : IJobChunk
		{
			[DeallocateOnJobCompletion] // Deallocate after job finishes
			[ReadOnly]
			public NativeArray<RaycastHit> hit;

			[DeallocateOnJobCompletion] // Deallocate after job finishes
			[ReadOnly]
			public NativeArray<PlayerNavigationNode> currentNode;

			public ArchetypeChunkComponentType<NavigationGridData> navigationGridType;
			[ReadOnly] public ArchetypeChunkBufferType<AStarNodeElement> nodeBufferType;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				NativeArray<NavigationGridData> navigationGrids = chunk.GetNativeArray(this.navigationGridType);
				BufferAccessor<AStarNodeElement> nodeBufferAccessor = chunk.GetBufferAccessor(this.nodeBufferType);

				for (int ci = 0, cn = chunk.Count; ci < cn; ci++) {
					if (hit[0].Entity != Entity.Null) {
						// Set navigation if valid block was selected

						NavigationGridData navigationGrid = navigationGrids[ci];
						DynamicBuffer<AStarNodeElement> nodeBuffer = nodeBufferAccessor[ci];

						// End node grid index
						int endNodeIndex;
						float3 hitPosition = hit[0].Position;
						navigationGrid.WorldToGridPosition(hitPosition, out endNodeIndex);

						// Start node grid index
						int startNodeIndex = this.currentNode[0].Index;

						// Find start/end nodes from dynamic buffer
						AStarNode startNode = new AStarNode();
						AStarNode endNode = new AStarNode();
						bool startFound = false;
						bool endFound = false;
						for (int i = 0, n = nodeBuffer.Length; i < n; i++) {
							if (nodeBuffer[i].Value.Index == endNodeIndex) {
								endFound = true;
								endNode = nodeBuffer[i].Value;
							} else if (nodeBuffer[i].Value.Index == startNodeIndex) {
								startFound = true;
								startNode = nodeBuffer[i].Value;
							}

							if (startFound && endFound) {
								break;
							}
						}

						// Request path if start/end both found
						if (startFound && endFound) {
							navigationGrid.pathRequested = true;
							navigationGrid.pathMustBeStraightLine = true;
							navigationGrid.pathStart = startNode;
							navigationGrid.pathEnd = endNode;
						}

						// Apply changes
						navigationGrids[ci] = navigationGrid;
					}
				}
			}
		}

		private BuildPhysicsWorld buildPhysicsWorldSystem;

		private EntityQuery gridQuery;
		private EntityQuery playerQuery;

		protected override void OnCreate()
		{
			// Physics world for world collision query
			this.buildPhysicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();

			// Query for grid
			this.gridQuery = GetEntityQuery(typeof(Tag_NavigationGrid));

			// Query for player
			this.playerQuery = GetEntityQuery(typeof(Tag_Player));
		}

		protected override void OnUpdate()
		{
			// Raycast to find potential clicked block

			Pointer pointer = PointerInput.GetPointer();

			if (!pointer.Pressed) {
				return;
			}

			// Collision world
			CollisionWorld collisionWorld = this.buildPhysicsWorldSystem.PhysicsWorld.CollisionWorld;

			// Ray
			RaycastInput input = new RaycastInput() {
				Start = pointer.WorldPosition,
				End = pointer.WorldPosition + (pointer.WorldDirection * 1000f),
				Filter = new CollisionFilter() {
					BelongsTo = PhysicsCategories.POINTER,
					CollidesWith = PhysicsCategories.BLOCK_POINTER,
				}
			};

			// Output hit
			NativeArray<RaycastHit> hit = new NativeArray<RaycastHit>(1, Allocator.TempJob);

			// Raycast job
			PointerRaycastJob pointerRaycastJob = new PointerRaycastJob() {
				collisionWorld = collisionWorld,
				input = input,
				outHit = hit,
			};
			JobHandle pointerRaycastHandle = pointerRaycastJob.Schedule(this.Dependency);

			// Get player current node
			NativeArray<PlayerNavigationNode> currentNode = new NativeArray<PlayerNavigationNode>(1, Allocator.TempJob);
			GetCurrentNodeJob getCurrentNodeJob = new GetCurrentNodeJob() {
				outCurrentNode = currentNode,
				playerMovementType = GetArchetypeChunkComponentType<PlayerMovementData>(true),
			};
			JobHandle getCurrentNodeHandle = getCurrentNodeJob.ScheduleParallel(this.playerQuery, pointerRaycastHandle);
			getCurrentNodeHandle.Complete();

			// Request path job
			RequestPathJob requestPathJob = new RequestPathJob() {
				hit = hit,
				currentNode = currentNode,
				navigationGridType = GetArchetypeChunkComponentType<NavigationGridData>(false),
				nodeBufferType = GetArchetypeChunkBufferType<AStarNodeElement>(true),
			};
			JobHandle requestPathHandle = requestPathJob.ScheduleParallel(this.gridQuery, getCurrentNodeHandle);

			this.Dependency = requestPathHandle;
		}
	}
}
