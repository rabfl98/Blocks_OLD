using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Blocks
{
	[GenerateAuthoringComponent]
	public struct PlayerMovementData : IComponentData
	{
		public float movementVelocity;
		public float rotationSmoothFactor;
		[HideInInspector] public PlayerNavigationNode currentNode;

		[HideInInspector] public bool isMoving;
		[HideInInspector] public float movementT;
		[HideInInspector] public int movementTargetPathNodeIndex;
		
		public void StartMovement(PlayerNavigationNode startNode)
		{
			this.currentNode = startNode;
			this.isMoving = true;
			this.movementT = 0f;
			this.movementTargetPathNodeIndex = 1;
		}

		public void FinishMovement(PlayerNavigationNode endNode)
		{
			this.currentNode = endNode;
			this.isMoving = false;
		}

		public void ToNextPathNode(PlayerNavigationNode currentNode)
		{
			this.currentNode = currentNode;
			
			this.movementTargetPathNodeIndex++;
			this.movementT -= 1f;
		}
	}
}