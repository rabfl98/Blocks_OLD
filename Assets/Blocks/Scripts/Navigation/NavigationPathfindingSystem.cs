using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Blocks
{
	[UpdateInGroup(typeof(DefaultSystemGroup))]
	public class NavigationPathfindingSystem : SystemBase
	{
		[BurstCompile]
		public struct SolvePathJob : IJobChunk
		{
			public NativeArray<AStarNode> outPathNodes;
			public NativeArray<int> outPathFound;
			public NativeArray<NavigationGridData> outNavigationGrid;

			public ArchetypeChunkComponentType<NavigationGridData> navigationGridType;
			[ReadOnly] public ArchetypeChunkBufferType<AStarNodeElement> nodeBufferType;
			
			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				NativeArray<NavigationGridData> navigationGrids = chunk.GetNativeArray(this.navigationGridType);
				BufferAccessor<AStarNodeElement> nodeBufferAccessor = chunk.GetBufferAccessor(this.nodeBufferType);

				for (int ci = 0, cn = chunk.Count; ci < cn; ci++) {
					NavigationGridData navigationGrid = outNavigationGrid[0] = navigationGrids[ci];
					DynamicBuffer<AStarNodeElement> nodeBuffer = nodeBufferAccessor[ci];

					// Solve path if requested

					if (navigationGrid.pathRequested) {
						// All nodes
						NativeArray<AStarNode> nodes = nodeBuffer.Reinterpret<AStarNode>().ToNativeArray(Allocator.Temp);

						// Create temp grid
						AStarGrid aStarGrid = new AStarGrid(navigationGrid.lengthX, navigationGrid.lengthY, nodes);

						// Start/end nodes
						AStarNode start = navigationGrid.pathStart;
						AStarNode end = navigationGrid.pathEnd;

						// Solve path
						NativeList<AStarNode> pathNodeList = new NativeList<AStarNode>(Allocator.Temp);
						bool pathFound = AStarSolver.SolvePath(aStarGrid, start, end, ref pathNodeList);
						int pathLength = pathNodeList.Length;
						if (pathFound && navigationGrid.pathMustBeStraightLine) {
							// Check if path is straight line if specfied as a requirement
							bool xDiverge = false;
							bool yDiverge = false;
							for (int i = 1, n = pathNodeList.Length; i < n; i++) {
								if (!xDiverge) {
									xDiverge = pathNodeList[i].XCoord != pathNodeList[i - 1].XCoord;
								}
								if (!yDiverge) {
									yDiverge = pathNodeList[i].YCoord != pathNodeList[i - 1].YCoord;
								}
								if (xDiverge && yDiverge) {
									pathFound = false;
									break;
								}
							}
						}
						

						// Copy path node list to output array
						NativeSlice<AStarNode> pathNodeListSlice = new NativeSlice<AStarNode>(pathNodeList.AsArray());
						NativeSlice<AStarNode> pathNodeSlice = new NativeSlice<AStarNode>(this.outPathNodes, 0, pathLength);
						pathNodeSlice.CopyFrom(pathNodeListSlice);

						// Dispose native containers
						pathNodeList.Dispose();
						aStarGrid.Dispose();
						nodes.Dispose();

						this.outPathFound[0] = pathFound ? 1 : 0;
						this.outPathFound[1] = pathLength;
						navigationGrid.pathRequested = false;
						navigationGrid.pathMustBeStraightLine = false;

						// Apply changes
						navigationGrids[ci] = navigationGrid;
					}
				}
			}
		}

		[BurstCompile]
		public struct SetPlayerPathJob : IJobChunk
		{
			[DeallocateOnJobCompletion] // Deallocate after job finishes
			[ReadOnly]
			public NativeArray<AStarNode> pathNodes;

			[DeallocateOnJobCompletion] // Deallocate after job finishes
			[ReadOnly]
			public NativeArray<int> pathFound;

			[DeallocateOnJobCompletion] // Deallocate after job finishes
			[ReadOnly]
			public NativeArray<NavigationGridData> navigationGrid;

			public ArchetypeChunkComponentType<PlayerMovementData> playerMovementType;
			public ArchetypeChunkBufferType<PlayerNavigationNodeElement> pathNodeBufferType;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				NativeArray<PlayerMovementData> playerMovements = chunk.GetNativeArray(this.playerMovementType);
				BufferAccessor<PlayerNavigationNodeElement> nodeBufferAccessor = chunk.GetBufferAccessor(this.pathNodeBufferType);

				for (int ci = 0, cn = chunk.Count; ci < cn; ci++) {
					PlayerMovementData playerMovement = playerMovements[ci];
					DynamicBuffer<PlayerNavigationNodeElement> pathNodeBuffer = nodeBufferAccessor[ci];

					// Set path if found
					if (pathFound[0] != 0) {

						// Copy path to player's path node buffer
						
						pathNodeBuffer.Clear();

						for (int i = 0, n = pathFound[1]; i < n; i++) {
							float3 worldPosition = navigationGrid[0].GridToWorldPosition(pathNodes[i].XCoord, pathNodes[i].YCoord);
							pathNodeBuffer.Add(new PlayerNavigationNode(pathNodes[i].XCoord, pathNodes[i].YCoord, pathNodes[i].Index, worldPosition));
						}

						// Start player movement
						playerMovement.StartMovement(pathNodeBuffer[0]);

						// Apply changes
						playerMovements[ci] = playerMovement;
					}
				}
			}
		}
		
		private EntityQuery gridQuery;
		private EntityQuery playerQuery;

		protected override void OnCreate()
		{
			// Queries for grid, player
			this.gridQuery = GetEntityQuery(typeof(Tag_NavigationGrid));
			this.playerQuery = GetEntityQuery(typeof(Tag_Player));
		}

		protected override void OnUpdate()
		{
			// "Return" values from path solve job
			// Not sure how to properly do writeable+resizeable NativeList...using preset max-length NativeArray for now...
			NativeArray<AStarNode> pathNodes = new NativeArray<AStarNode>(GridConstants.MAX_GRID_LINEAR_LENGTH, Allocator.TempJob);
			NativeArray<int> pathFound = new NativeArray<int>(2, Allocator.TempJob);
			NativeArray<NavigationGridData> navigationGrid = new NativeArray<NavigationGridData>(1, Allocator.TempJob);
			// Job, solve for path

			SolvePathJob solvePathJob = new SolvePathJob()
			{
				outPathNodes = pathNodes,
				outPathFound = pathFound,
				outNavigationGrid = navigationGrid,
				navigationGridType = GetArchetypeChunkComponentType<NavigationGridData>(false),
				nodeBufferType = GetArchetypeChunkBufferType<AStarNodeElement>(true),
			};
			JobHandle solvePathHandle = solvePathJob.ScheduleParallel(this.gridQuery, this.Dependency);

			// Set path to player

			SetPlayerPathJob setPlayerPathJob = new SetPlayerPathJob()
			{
				pathNodes = pathNodes,
				pathFound = pathFound,
				navigationGrid = navigationGrid,
				playerMovementType = GetArchetypeChunkComponentType<PlayerMovementData>(false),
				pathNodeBufferType = GetArchetypeChunkBufferType<PlayerNavigationNodeElement>(false),
			};
			JobHandle setPlayerPathHandle = setPlayerPathJob.ScheduleParallel(this.playerQuery, solvePathHandle);

			this.Dependency = setPlayerPathHandle;
		}
	}
}