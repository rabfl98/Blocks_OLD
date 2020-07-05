using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Blocks
{
	[UpdateInGroup(typeof(PlayerMovementGroup))]
	public class PlayerMovementSystem : SystemBase
	{
		[BurstCompile]
		public struct MovementJob : IJobChunk
		{
			public float deltaTime;

			public ArchetypeChunkComponentType<Translation> translationType;
			public ArchetypeChunkComponentType<Rotation> rotationType;
			public ArchetypeChunkComponentType<PlayerMovementData> playerMovementType;
			public ArchetypeChunkBufferType<PlayerNavigationNodeElement> pathNodeBufferType;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				NativeArray<Translation> translations = chunk.GetNativeArray(this.translationType);
				NativeArray<Rotation> rotations = chunk.GetNativeArray(this.rotationType);
				NativeArray<PlayerMovementData> playerMovements = chunk.GetNativeArray(this.playerMovementType);
				BufferAccessor<PlayerNavigationNodeElement> nodeBufferAccessor = chunk.GetBufferAccessor(this.pathNodeBufferType);

				for (int ci = 0, cn = chunk.Count; ci < cn; ci++) {
					Translation translation = translations[ci];
					Rotation rotation = rotations[ci];
					PlayerMovementData playerMovement = playerMovements[ci];
					DynamicBuffer<PlayerNavigationNodeElement> pathNodeBuffer = nodeBufferAccessor[ci];

					// Move player along path if moving
					if (playerMovement.isMoving) {
						float3 currentPosition = translation.Value;

						// Current/target nodes
						PlayerNavigationNode currentNode = playerMovement.currentNode;
						PlayerNavigationNode targetNode = pathNodeBuffer[playerMovement.movementTargetPathNodeIndex].Value;
						float3 fromPosition = currentNode.WorldPosition;
						float3 toPosition = targetNode.WorldPosition;

						// Get velocity and move
						float d = math.length(toPosition - fromPosition);
						float secs = d / playerMovement.movementVelocity;
						playerMovement.movementT += playerMovement.movementVelocity / secs;
						currentPosition = math.lerp(fromPosition, toPosition, playerMovement.movementT);

						if (playerMovement.movementT >= 1f) {
							if (playerMovement.movementTargetPathNodeIndex < pathNodeBuffer.Length - 1) {
								playerMovement.ToNextPathNode(targetNode);
							} else {
								playerMovement.FinishMovement(targetNode);
								currentPosition = toPosition;
							}
						}
						translation.Value = currentPosition;

						// Rotate toward movement direction
						quaternion targetRot = quaternion.LookRotation(math.normalize(toPosition - fromPosition), math.up());
						float3 rotationDir = math.lerp(math.forward(rotation.Value), math.normalize(toPosition - fromPosition), playerMovement.rotationSmoothFactor * deltaTime);
						rotation.Value = UnityEngine.Quaternion.Lerp(rotation.Value, targetRot, playerMovement.rotationSmoothFactor * deltaTime);

						// Apply changes
						translations[ci] = translation;
						rotations[ci] = rotation;
						playerMovements[ci] = playerMovement;
					}
				}
			}
		}

		private EntityQuery playerQuery;

		protected override void OnCreate()
		{
			// Query for player
			this.playerQuery = GetEntityQuery(typeof(Tag_Player));
		}

		protected override void OnUpdate()
		{
			float deltaTime = Time.DeltaTime;

			// Job, move player along path

			MovementJob movementJob = new MovementJob()
			{
				deltaTime = deltaTime,
				translationType = GetArchetypeChunkComponentType<Translation>(false),
				rotationType = GetArchetypeChunkComponentType<Rotation>(false),
				playerMovementType = GetArchetypeChunkComponentType<PlayerMovementData>(false),
				pathNodeBufferType = GetArchetypeChunkBufferType<PlayerNavigationNodeElement>(true),
			};
			JobHandle movementHandle = movementJob.ScheduleParallel(this.playerQuery, this.Dependency);

			this.Dependency = movementHandle;
		}
	}
}