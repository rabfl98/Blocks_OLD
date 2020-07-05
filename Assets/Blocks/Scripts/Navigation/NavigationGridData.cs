using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Blocks
{
	[GenerateAuthoringComponent]
	public struct NavigationGridData : IComponentData
	{
		[HideInInspector] public int lengthX;
		[HideInInspector] public int lengthY;

		public int LinearLength { get { return this.lengthX * this.lengthY; } }

		[HideInInspector] public float2 worldBoundsMin;
		[HideInInspector] public float2 worldBoundsMax;

		[HideInInspector] public bool pathRequested;
		[HideInInspector] public bool pathMustBeStraightLine;
		[HideInInspector] public AStarNode pathStart;
		[HideInInspector] public AStarNode pathEnd;

		public float3 GridToWorldPosition(int x, int y)
		{
			// Convert grid coords to world position

			return new float3(
				x * GridConstants.BLOCK_SIZE + this.worldBoundsMin.x,
				0f,
				y * GridConstants.BLOCK_SIZE + this.worldBoundsMin.y
			);
		}

		public void WorldToGridPosition(float3 worldPosition, out int x, out int y)
		{
			// Convert world position to grid coords

			float wnx = (int)((worldPosition.x - this.worldBoundsMin.x) + (GridConstants.BLOCK_SIZE / 2));
			float wnz = (int)((worldPosition.z - this.worldBoundsMin.y) + (GridConstants.BLOCK_SIZE / 2));

			x = (int)(wnx / GridConstants.BLOCK_SIZE);
			y = (int)(wnz / GridConstants.BLOCK_SIZE);
		}

		public void WorldToGridPosition(float3 worldPosition, out int i)
		{
			// Convert world position to grid coords

			float wnx = (int)((worldPosition.x - this.worldBoundsMin.x) + (GridConstants.BLOCK_SIZE / 2));
			float wnz = (int)((worldPosition.z - this.worldBoundsMin.y) + (GridConstants.BLOCK_SIZE / 2));

			int x = (int)(wnx / GridConstants.BLOCK_SIZE);
			int y = (int)(wnz / GridConstants.BLOCK_SIZE);
			i = (y * this.lengthX) + x;
		}
	}
}