using System;
using UnityEngine;
using Unity.Entities;

namespace Blocks
{
	[GenerateAuthoringComponent]
	public struct BlockGridNodeData : IComponentData
	{
		[HideInInspector] public int XCoord;
		[HideInInspector] public int YCoord;
		[HideInInspector] public int Index;
	}
}