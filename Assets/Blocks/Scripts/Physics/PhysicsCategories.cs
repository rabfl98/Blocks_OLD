using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Blocks
{
	public class PhysicsCategories
	{
		public const uint EVERYTHING = ~0x0u;
		public const uint NOTHING = 0;

		public const uint BLOCK_AGENT = 1 << 0;
		public const uint BLOCK_POINTER = 1 << 1;
		public const uint POINTER = 1 << 2;
		public const uint PLAYER_AGENT = 1 << 3;

		public uint GetCategoryMask(params uint[] categories)
		{
			uint mask = 0;

			for (int i = 0, n = categories.Length; i < n; i++) {
				mask |= categories[i];
			}

			return mask;
		}
	}
}