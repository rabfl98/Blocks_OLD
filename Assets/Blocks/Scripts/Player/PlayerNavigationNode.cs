using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Blocks
{
	[System.Serializable]
	public struct PlayerNavigationNode
	{
		public int XCoord { get; private set; }
		public int YCoord { get; private set; }
		public int Index { get; private set; }
		public float3 WorldPosition { get; private set; }

		public PlayerNavigationNode(int xCoord, int yCoord, int index, float3 worldPosition)
		{
			this.XCoord = xCoord;
			this.YCoord = yCoord;
			this.Index = index;
			this.WorldPosition = worldPosition;
		}
	}

	public struct PlayerNavigationNodeElement : IBufferElementData
	{
		public PlayerNavigationNode Value;

		// The following implicit conversions are optional, but can be convenient.
		public static implicit operator PlayerNavigationNode(PlayerNavigationNodeElement e)
		{
			return e.Value;
		}

		public static implicit operator PlayerNavigationNodeElement(PlayerNavigationNode e)
		{
			return new PlayerNavigationNodeElement { Value = e };
		}
	}
}