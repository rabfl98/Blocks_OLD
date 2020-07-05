using System;
using UnityEngine;
using Unity.Entities;

namespace Blocks
{
	public enum BlockType
	{
		Basic = 0,
		Start,
		Goal,
		Switch,
	}

	[GenerateAuthoringComponent]
	public struct BlockTraversalData : IComponentData
	{
		public BlockType blockType;

		public bool isSolid;
		public int maxTraversalCount;

		[HideInInspector] public int currentTraversalCount;

		public void ResetTraversalCount()
		{
			this.currentTraversalCount = this.maxTraversalCount;
		}

		public bool IncrementTraversalCount()
		{
			// Ignore if this block is solid
			if (this.isSolid) {
				return false;
			}

			return ++this.currentTraversalCount >= this.maxTraversalCount;
		}
	}
}