using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace Blocks
{
	[RequiresEntityConversion]
	public class PlayerCameraConvertToEntity : MonoBehaviour, IConvertGameObjectToEntity
	{
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, new CopyTransformToGameObject());

			PlayerCameraFollowData cameraFollow = dstManager.GetComponentData<PlayerCameraFollowData>(entity);
			Rotation cameraRotation = dstManager.GetComponentData<Rotation>(entity);
			
			cameraFollow.currentPivotRotation = cameraFollow.targetPivotRotation = quaternion.Euler(0, math.radians(225), 0);
			cameraFollow.currentPositionOffset = cameraFollow.targetPositionOffset;
			cameraRotation.Value = cameraFollow.currentPivotRotation;

			dstManager.SetComponentData<PlayerCameraFollowData>(entity, cameraFollow);
			dstManager.SetComponentData<Rotation>(entity, cameraRotation);

			PlayerCameraOrientationSystem cameraOrientationSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<PlayerCameraOrientationSystem>();
			cameraOrientationSystem.FollowEntity = cameraFollow.followEntity;
		}
	}
}