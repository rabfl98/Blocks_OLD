using UnityEngine;
using Unity.Entities;

namespace Blocks
{
	[GenerateAuthoringComponent]
	public struct SwitchGroupAppearData : IComponentData
	{
		public Entity groupRoot;
	}
}