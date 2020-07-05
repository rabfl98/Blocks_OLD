using System;
using Unity.Entities;

namespace Blocks
{
	[GenerateAuthoringComponent]
	public struct BlockDissolveData : IComponentData
	{
		public bool isDissolving;
	}
}