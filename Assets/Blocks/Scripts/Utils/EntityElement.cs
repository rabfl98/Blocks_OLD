using Unity.Entities;

namespace Blocks
{
	public struct EntityElement : IBufferElementData
	{
		public Entity Value;

		// The following implicit conversions are optional, but can be convenient.
		public static implicit operator Entity(EntityElement e)
		{
			return e.Value;
		}

		public static implicit operator EntityElement(Entity e)
		{
			return new EntityElement { Value = e };
		}
	}
}