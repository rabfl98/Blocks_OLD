using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Blocks
{
	[UpdateInGroup(typeof(InputGroup))]
	public class NavigationDebugInputSystem : SystemBase
	{
		private EntityQuery blocksQuery;
		private EntityQuery navigationGridQuery;
		EndSimulationEntityCommandBufferSystem endSimulationEntitySystem;

		protected override void OnCreate()
		{
			this.blocksQuery = GetEntityQuery(typeof(Tag_Block));
			this.navigationGridQuery = GetEntityQuery(typeof(Tag_NavigationGrid));

			this.endSimulationEntitySystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		protected override void OnUpdate()
		{
			// TEMPORARY for testing

			bool gatherBlocks = Input.GetKeyDown(KeyCode.F);

			if (gatherBlocks) {
				NativeArray<Entity> currentBlocks = this.blocksQuery.ToEntityArray(Allocator.TempJob);
				NativeArray<Translation> translations = new NativeArray<Translation>(currentBlocks.Length, Allocator.TempJob);
				NativeArray<Rotation> rotations = new NativeArray<Rotation>(currentBlocks.Length, Allocator.TempJob);
				NativeArray<BlockTraversalData> blockTraversals = new NativeArray<BlockTraversalData>(currentBlocks.Length, Allocator.TempJob);
				NativeArray<BlockGridNodeData> blockGridNodes = new NativeArray<BlockGridNodeData>(currentBlocks.Length, Allocator.TempJob);

				Translation playerStartTranslation = new Translation();
				Rotation playerStartRotation = new Rotation();
				PlayerNavigationNode playerStartNode = new PlayerNavigationNode();

				for (int i = 0, n = currentBlocks.Length; i < n; i++) {
					translations[i] = this.EntityManager.GetComponentData<Translation>(currentBlocks[i]);
					rotations[i] = this.EntityManager.GetComponentData<Rotation>(currentBlocks[i]);
					blockTraversals[i] = this.EntityManager.GetComponentData<BlockTraversalData>(currentBlocks[i]);
					blockGridNodes[i] = this.EntityManager.GetComponentData<BlockGridNodeData>(currentBlocks[i]);
				}

				Entities.WithAll<Tag_NavigationGrid>().WithoutBurst().ForEach(
					(int entityInQueryIndex, DynamicBuffer<AStarNodeElement> nodeBuf, ref NavigationGridData navigationGrid) => {
						nodeBuf.Clear();

						float minX, maxX;
						minX = maxX = translations[0].Value.x;
						float minZ, maxZ;
						minZ = maxZ = translations[0].Value.z;
						for (int ti = 0, tn = translations.Length; ti < tn; ti++) {
							if (minX > translations[ti].Value.x) {
								minX = translations[ti].Value.x;
							}
							if (maxX < translations[ti].Value.x) {
								maxX = translations[ti].Value.x;
							}
							if (minZ > translations[ti].Value.z) {
								minZ = translations[ti].Value.z;
							}
							if (maxZ < translations[ti].Value.z) {
								maxZ = translations[ti].Value.z;
							}
						}
						int lengthX = (int)(math.abs(maxX - minX) / GridConstants.BLOCK_SIZE) + 1;
						int lengthY = (int)(math.abs(maxZ - minZ) / GridConstants.BLOCK_SIZE) + 1;
						
						navigationGrid.worldBoundsMin = new float2(minX, minZ);
						navigationGrid.worldBoundsMax = new float2(maxX, maxZ);
						navigationGrid.lengthX = lengthX;
						navigationGrid.lengthY = lengthY;

						for (int ti = 0, tn = translations.Length; ti < tn; ti++) {
							int x, y;
							navigationGrid.WorldToGridPosition(translations[ti].Value, out x, out y);
							int i = (y * lengthX) + x;
							AStarNode node = new AStarNode(x, y, i, true);
							nodeBuf.Add(node);

							BlockGridNodeData gridNode = new BlockGridNodeData() {
								XCoord = x,
								YCoord = y,
								Index = i,
							};
							this.EntityManager.SetComponentData<BlockGridNodeData>(currentBlocks[ti], gridNode);

							if (blockTraversals[ti].blockType == BlockType.Start) {
								playerStartTranslation = translations[ti];
								playerStartRotation = rotations[ti];
								playerStartNode = new PlayerNavigationNode(x, y, i, translations[ti].Value);
							}
						};


					})
				.Run();

				translations.Dispose();
				rotations.Dispose();
				blockTraversals.Dispose();
				blockGridNodes.Dispose();


				currentBlocks.Dispose();

				EntityCommandBuffer.Concurrent ecb = this.endSimulationEntitySystem.CreateCommandBuffer().ToConcurrent();

				BufferFromEntity<Child> childrenBuffer = GetBufferFromEntity<Child>(true);
				//Entities.WithAll<Tag_Block>().ForEach(
				//	(int entityInQueryIndex, Entity entity) => {
				//		EntityUtils.EnableEntityHierarchy(ref ecb, entityInQueryIndex, entity, childrenBuffer);
				//	})
				//.Schedule();

				Entities.WithAll<Tag_BlockGroup>().ForEach(
					(int entityInQueryIndex, ref DynamicBuffer<EntityElement> entityBuff) => {
						for (int i = 0, n = entityBuff.Length; i < n; i++) {
							EntityUtils.DisableEntityHierarchy(ref ecb, entityInQueryIndex, entityBuff[i], childrenBuffer);
						}
					})
				.Schedule();

				this.endSimulationEntitySystem.AddJobHandleForProducer(this.Dependency);

				//ecb = this.endSimulationEntitySystem.CreateCommandBuffer().ToConcurrent();

				//Entities.WithAll<Tag_Block>().ForEach(
				//	(int entityInQueryIndex, ref BlockSwitchData blockSwitch) => {
				//		blockSwitch.Reset();
				//		blockSwitch.RefreshView(ref ecb, entityInQueryIndex);
				//	})
				//.Schedule();

				//this.endSimulationEntitySystem.AddJobHandleForProducer(this.Dependency);

				Entities.WithAll<Tag_Player>().WithoutBurst().ForEach(
					(ref PlayerMovementData playerMovement, ref Translation translation, ref Rotation rotation) => {
						translation = playerStartTranslation;
						rotation = playerStartRotation;
						playerMovement.currentNode = playerStartNode;
					})
				.Run();
			}

			/*
			bool generateBlocks = Input.GetKeyDown(KeyCode.Space);

			if (!generateBlocks) {
				return;
			}

			EntityCommandBuffer.Concurrent ecb = this.endSimulationEntitySystem.CreateCommandBuffer().ToConcurrent();

			Entity prefab = BlockManager.Instance.BlockPrefabEntity;

			NativeArray<Entity> currentBlocks = this.blocksQuery.ToEntityArray(Allocator.TempJob);

			Entities.WithAll<Tag_NavigationGrid>().WithoutBurst().ForEach(
				(int entityInQueryIndex, DynamicBuffer<AStarNodeElement> nodeBuf, ref NavigationGridData navigationGrid) => 
			{
				for (int i = 0, n = currentBlocks.Length; i < n; i++) {
					ecb.DestroyEntity(entityInQueryIndex, currentBlocks[i]);
				}

				Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Millisecond);
				int rand = 0;

				bool pathFound = false;

				NativeArray<AStarNode> nodes = new NativeArray<AStarNode>(navigationGrid.LinearLength, Allocator.Temp);

				while (!pathFound) {

					for (int y = 0; y < navigationGrid.lengthY; y++) {
						for (int x = 0; x < navigationGrid.lengthX; x++) {
							int i = (y * navigationGrid.lengthX) + x;
							AStarNode node;
							bool forceWalkable = (x == 0 && y == 0) || (x == navigationGrid.lengthX - 1 && y == navigationGrid.lengthY - 1);
							if (!forceWalkable && random.NextInt(0, navigationGrid.emptyRandFactor) < ++rand) {
								rand = 0;
								node = new AStarNode(x, y, i, false);

							} else {
								node = new AStarNode(x, y, i, true);
							}

							nodes[i] = node;
						}
					}

					// Create temp grid
					AStarGrid aStarGrid = new AStarGrid(navigationGrid.lengthX, navigationGrid.lengthY, nodes);

					// Start/end nodes
					AStarNode start = nodes[0];
					AStarNode end = nodes[nodes.Length - 1];

					// Solve path
					NativeList<AStarNode> pathNodeList = new NativeList<AStarNode>(Allocator.Temp);
					pathFound = AStarSolver.SolvePath(aStarGrid, start, end, ref pathNodeList);

					pathNodeList.Dispose();
					aStarGrid.Dispose();
				}

				nodeBuf.Clear();

				for (int i = 0, n = nodes.Length; i < n; i++) {
					if (nodes[i].IsTraverseable) {
						float3 position = new float3(nodes[i].XCoord * GridConstants.BLOCK_SIZE, 0, nodes[i].YCoord * GridConstants.BLOCK_SIZE);

						Entity block = ecb.Instantiate(entityInQueryIndex, prefab);

						Translation translation = new Translation();
						translation.Value = position;
						ecb.SetComponent<Translation>(entityInQueryIndex, block, translation);
					}

					nodeBuf.Add(nodes[i]);
				}

				navigationGrid.pathRequested = true;
				navigationGrid.pathStart = nodeBuf[0];
				navigationGrid.pathEnd = nodeBuf[nodeBuf.Length - 1];

			}).WithDeallocateOnJobCompletion(currentBlocks).ScheduleParallel();

			this.endSimulationEntitySystem.AddJobHandleForProducer(this.Dependency);
			*/
		}
	}
}
