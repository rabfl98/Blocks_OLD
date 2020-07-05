using UnityEngine;
using Unity.Entities;

namespace Blocks
{
	[GenerateAuthoringComponent]
	public struct BlockPlayerCollisionData : IComponentData
	{
		[HideInInspector] public PhysicsTriggerState triggerState;
	}
}