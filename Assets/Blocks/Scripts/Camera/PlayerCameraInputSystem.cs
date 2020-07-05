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
	public class PlayerCameraInputSystem : SystemBase
	{
		[BurstCompile]
		public struct CameraInputJob : IJobChunk
		{
			[ReadOnly] public bool rotateLeftInput;
			[ReadOnly] public bool rotateRightInput;
			public ArchetypeChunkComponentType<PlayerCameraFollowData> cameraFollowDataType;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				NativeArray<PlayerCameraFollowData> cameraFollowDatas = chunk.GetNativeArray(this.cameraFollowDataType);

				for(int ci = 0, cn = chunk.Count; ci < cn; ci++) {
					PlayerCameraFollowData cameraFollow = cameraFollowDatas[ci];

					// Rotate left/right based on input

					if (rotateLeftInput) {
						cameraFollow.targetPivotRotation = math.mul(cameraFollow.targetPivotRotation, quaternion.Euler(0, math.PI / 2, 0));
					}
					if (rotateRightInput) {
						cameraFollow.targetPivotRotation = math.mul(cameraFollow.targetPivotRotation, quaternion.Euler(0, -(math.PI / 2), 0));
					}

					// Apply changes

					cameraFollowDatas[ci] = cameraFollow;
				}
			}
		}

		private EntityQuery cameraQuery;

		protected override void OnCreate()
		{
			// Query to find camera

			this.cameraQuery = GetEntityQuery(typeof(Tag_PlayerCamera));
		}

		protected override void OnUpdate()
		{
			CameraInputJob cameraInputJob = new CameraInputJob()
			{
				rotateLeftInput = Input.GetKeyDown(KeyCode.D),
				rotateRightInput = Input.GetKeyDown(KeyCode.A),
				cameraFollowDataType = GetArchetypeChunkComponentType<PlayerCameraFollowData>(false),
			};

			this.Dependency = cameraInputJob.ScheduleParallel(this.cameraQuery, this.Dependency);
		}
	}
}