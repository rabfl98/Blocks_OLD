using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Blocks
{
	[System.Serializable]
	public struct AStarNode : IEquatable<AStarNode>
	{
		public int XCoord { get; private set; }
		public int YCoord { get; private set; }
		public int Index { get; private set; }

		public bool IsTraverseable;
		public int TraversalCost;

		public bool IsBlockedPosX;
		public bool IsBlockedNegX;
		public bool IsBlockedPosY;
		public bool IsBlockedNegY;

		public int G;
		public int H;
		public int F
		{
			get
			{
				return this.G + this.H;
			}
		}

		public int parentIndex;

		public AStarNode(int xCoord, int yCoord, int index, bool isTraverseable, int walkCost, bool isBlockedPosX, bool isBlockedNegX, bool isBlockedPosY, bool isBlockedNegY)
		{
			this.XCoord = xCoord;
			this.YCoord = yCoord;
			this.Index = index;

			this.IsTraverseable = isTraverseable;
			this.TraversalCost = walkCost;

			this.IsBlockedPosX = isBlockedPosX;
			this.IsBlockedNegX = isBlockedNegX;
			this.IsBlockedPosY = isBlockedPosY;
			this.IsBlockedNegY = isBlockedNegY;

			this.G = 0;
			this.H = 0;

			this.parentIndex = -1;
		}

		public AStarNode(int xCoord, int yCoord, int index, bool isTraverseable, int walkCost = 1)
		{
			this.XCoord = xCoord;
			this.YCoord = yCoord;
			this.Index = index;

			this.IsTraverseable = isTraverseable;
			this.TraversalCost = walkCost;

			this.IsBlockedPosX = false;
			this.IsBlockedNegX = false;
			this.IsBlockedPosY = false;
			this.IsBlockedNegY = false;

			this.G = 0;
			this.H = 0;

			this.parentIndex = -1;
		}

		public AStarNode(int xCoord, int yCoord, int index, bool isTraverseable)
		{
			this.XCoord = xCoord;
			this.YCoord = yCoord;
			this.Index = index;

			this.IsTraverseable = isTraverseable;
			this.TraversalCost = 1;

			this.IsBlockedPosX = false;
			this.IsBlockedNegX = false;
			this.IsBlockedPosY = false;
			this.IsBlockedNegY = false;

			this.G = 0;
			this.H = 0;

			this.parentIndex = -1;
		}

		public void Reset()
		{
			this.G = 0;
			this.H = 0;

			this.parentIndex = -1;
		}

		public bool Equals(AStarNode node)
		{
			return node.XCoord == this.XCoord
				&& node.YCoord == this.YCoord;
		}
	}

	public struct AStarGrid
	{
		private NativeHashMap<int, AStarNode> nodeMap;

		public int LengthX { get; private set; }
		public int LengthY { get; private set; }
		public int LinearLength { get { return this.LengthX * this.LengthY; } }

		public AStarGrid(int lengthX, int lengthY, NativeArray<AStarNode> nodes)
		{
			this.LengthX = lengthX;
			this.LengthY = lengthY;

			this.nodeMap = new NativeHashMap<int, AStarNode>(nodes.Length, Allocator.Temp);
			for (int i = 0, n = nodes.Length; i < n; i++) {
				this.nodeMap.Add(nodes[i].Index, nodes[i]);
			}
		}

		public AStarNode this[int x, int y]
		{
			get
			{
				int i = (y * this.LengthX) + x;
				return this.nodeMap[i];
			}
			set
			{
				int i = (y * this.LengthX) + x;
				this.nodeMap[i] = value;
			}
		}

		public AStarNode this[int i]
		{
			get
			{
				return this.nodeMap[i];
			}
			set
			{
				this.nodeMap[i] = value;
			}
		}

		public bool HasNode(int x, int y)
		{
			int i = (y * this.LengthX) + x;

			return this.nodeMap.ContainsKey(i);
		}

		public bool HasNode(int i)
		{
			return this.nodeMap.ContainsKey(i);
		}

		public void Dispose()
		{
			this.nodeMap.Dispose();
		}
	}

	public static class AStarSolver
	{
		public static bool SolvePath(AStarGrid grid, AStarNode start, AStarNode end, ref NativeList<AStarNode> pathNodes)
		{
			NativeList<AStarNode> openNodes = new NativeList<AStarNode>(Allocator.Temp);
			NativeList<AStarNode> closedNodes = new NativeList<AStarNode>(Allocator.Temp);

			AStarNode current = start;

			openNodes.Add(start);

			bool pathFound = false;

			while (openNodes.Length > 0) {
				current = openNodes[0];
				int minF = current.F;
				for (int i = 1, n = openNodes.Length; i < n; i++) {
					if (minF > openNodes[i].F) {
						current = openNodes[i];
					}
				}
				
				closedNodes.Add(current);

				openNodes.RemoveAtSwapBack(openNodes.IndexOf(current));

				if (current.Equals(end)) {
					// Path to end found
					pathFound = true;
					break;
				}

				NativeArray<AStarNode> adjacentNodes = new NativeArray<AStarNode>(4, Allocator.Temp);
				int length = 0;
				GetAdjacentNodes(grid, current, ref adjacentNodes, out length);
				
				for (int i = 0; i < length; i++) {
					AStarNode adjacent = adjacentNodes[i];

					if (closedNodes.IndexOf(adjacent) < 0) {
						if (openNodes.IndexOf(adjacent) < 0) {
							adjacent.parentIndex = current.Index;
							adjacent.G = DistanceToNode(adjacent, end);
							adjacent.H = adjacent.TraversalCost + grid[adjacent.parentIndex].H;

							openNodes.Add(adjacent);

							grid[adjacent.XCoord, adjacent.YCoord] = adjacent;
						}
					}
				}

				adjacentNodes.Dispose();
			}

			if (pathFound) {
				while (current.parentIndex >= 0) {
					pathNodes.Add(current);
					current = grid[current.parentIndex];
				}
				pathNodes.Add(start);

				// Reverse
				for (int i = 0, i2 = pathNodes.Length - 1; i < i2; i++, i2--) {
					AStarNode t = pathNodes[i];
					pathNodes[i] = pathNodes[i2];
					pathNodes[i2] = t;
				}
			}

			openNodes.Dispose();
			closedNodes.Dispose();

			return pathFound;
		}

		private static int DistanceToNode(AStarNode from, AStarNode to)
		{
			return math.abs(to.XCoord - from.XCoord) + math.abs(to.YCoord - from.YCoord);
		}

		private static void GetAdjacentNodes(AStarGrid grid, AStarNode node, ref NativeArray<AStarNode> adjacentNodes, out int length)
		{
			length = 0;

			// +X
			if (!node.IsBlockedPosX) {
				int x = node.XCoord + 1;
				int y = node.YCoord;
				if (x < grid.LengthX && grid.HasNode(x, y) && grid[x, y].IsTraverseable && !grid[x, y].IsBlockedNegX) {
					adjacentNodes[length++] = grid[x, y];
				}
			}
			// -X
			if (!node.IsBlockedNegX) {
				int x = node.XCoord - 1;
				int y = node.YCoord;
				if (x >= 0 && grid.HasNode(x, y) && grid[x, y].IsTraverseable && !grid[x, y].IsBlockedPosX) {
					adjacentNodes[length++] = grid[x, y];
				}
			}
			// +Y
			if (!node.IsBlockedPosY) {
				int x = node.XCoord;
				int y = node.YCoord + 1;
				if (y < grid.LengthY && grid.HasNode(x, y) && grid[x, y].IsTraverseable && !grid[x, y].IsBlockedNegY) {
					adjacentNodes[length++] = grid[x, y];
				}
			}
			// -Y
			if (!node.IsBlockedNegY) {
				int x = node.XCoord;
				int y = node.YCoord - 1;
				if (y >= 0 && grid.HasNode(x, y) && grid[x, y].IsTraverseable && !grid[x, y].IsBlockedPosY) {
					adjacentNodes[length++] = grid[x, y];
				}
			}
		}
	}

	public struct AStarNodeElement : IBufferElementData
	{
		public AStarNode Value;

		// The following implicit conversions are optional, but can be convenient.
		public static implicit operator AStarNode(AStarNodeElement e)
		{
			return e.Value;
		}

		public static implicit operator AStarNodeElement(AStarNode e)
		{
			return new AStarNodeElement { Value = e };
		}
	}
}