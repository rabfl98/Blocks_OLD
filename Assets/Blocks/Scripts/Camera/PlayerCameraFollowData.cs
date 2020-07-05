using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Blocks
{
	[GenerateAuthoringComponent]
	public struct PlayerCameraFollowData : IComponentData
	{
		public Entity followEntity;

		public float height;
		public float distance;
		public float3 targetPositionOffset;
		public quaternion targetPivotRotation;

		public float rotationSmoothFactor;
		public float positionSmoothFactor;

		[HideInInspector] public float3 currentFollowPosition;
		[HideInInspector] public quaternion currentPivotRotation;
		[HideInInspector] public float3 currentPositionOffset;
	}
}