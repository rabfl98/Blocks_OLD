using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Blocks
{
	[UpdateInGroup(typeof(CameraGroup))]
	public class PlayerCameraOrientationSystem : SystemBase
	{
		[BurstCompile]
		public struct CameraOrientationJob : IJobChunk
		{
			[ReadOnly] public float deltaTime;
			[ReadOnly] public float3 followPosition;
			public ArchetypeChunkComponentType<Translation> translationType;
			public ArchetypeChunkComponentType<Rotation> rotationType;
			public ArchetypeChunkComponentType<PlayerCameraFollowData> cameraFollowDataType;
			
			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				NativeArray<Rotation> rotations = chunk.GetNativeArray(this.rotationType);
				NativeArray<Translation> translations = chunk.GetNativeArray(this.translationType);
				NativeArray<PlayerCameraFollowData> cameraFollowDatas = chunk.GetNativeArray(this.cameraFollowDataType);

				for (int ci = 0, cn = chunk.Count; ci < cn; ci++) {
					PlayerCameraFollowData cameraFollow = cameraFollowDatas[ci];
					Translation cameraTranslation  = translations[ci];
					Rotation cameraRotation = rotations[ci];

					// Set camera orientation

					quaternion currentPivotRotation = UnityEngine.Quaternion.Lerp(cameraFollow.currentPivotRotation, cameraFollow.targetPivotRotation, cameraFollow.rotationSmoothFactor * deltaTime);
					float3 currentFollowPosition = math.lerp(cameraFollow.currentFollowPosition, this.followPosition, cameraFollow.positionSmoothFactor * deltaTime);

					float3 basePositionOffset = new float3(0f, cameraFollow.height, cameraFollow.distance);
					float3 targetPosition = currentFollowPosition + math.mul(currentPivotRotation, basePositionOffset);
					quaternion targetRotation = quaternion.LookRotation(math.normalize(currentFollowPosition - targetPosition), math.up());

					float3 currentPositionOffset = math.lerp(cameraFollow.currentPositionOffset, cameraFollow.targetPositionOffset, cameraFollow.positionSmoothFactor * deltaTime);
					float3x3 rotationMatrix = new float3x3(currentPivotRotation);
					float3 worldOffset = math.mul(rotationMatrix, currentPositionOffset);
					targetPosition += worldOffset;

					// Set data

					cameraFollow.currentFollowPosition = currentFollowPosition;
					cameraFollow.currentPivotRotation = currentPivotRotation;
					cameraFollow.currentPositionOffset = currentPositionOffset;

					cameraTranslation.Value = targetPosition;
					cameraRotation.Value = targetRotation;

					// Apply changes

					cameraFollowDatas[ci] = cameraFollow;
					translations[ci] = cameraTranslation;
					rotations[ci] = cameraRotation;
				}
			}
		}

		public Entity FollowEntity { get; set; }

		private EntityQuery cameraQuery;

		protected override void OnCreate()
		{
			// Query to find camera

			this.cameraQuery = GetEntityQuery(typeof(Tag_PlayerCamera));
		}

		protected override void OnUpdate()
		{
			// Create chunk job from query

			LocalToWorld followLtoW = this.EntityManager.GetComponentData<LocalToWorld>(this.FollowEntity);

			CameraOrientationJob job = new CameraOrientationJob {
				deltaTime = Time.DeltaTime,
				followPosition = followLtoW.Position,
				translationType = GetArchetypeChunkComponentType<Translation>(false),
				rotationType = GetArchetypeChunkComponentType<Rotation>(false),
				cameraFollowDataType = GetArchetypeChunkComponentType<PlayerCameraFollowData>(false),
			};

			this.Dependency = job.ScheduleParallel(this.cameraQuery, this.Dependency);
		}
	}
}