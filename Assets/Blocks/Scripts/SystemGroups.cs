using Unity.Entities;

namespace Blocks
{
	public class InputGroup : ComponentSystemGroup
	{

	}

	[UpdateAfter(typeof(InputGroup))]
	public class DefaultSystemGroup : ComponentSystemGroup
	{

	}

	[UpdateAfter(typeof(InputGroup))]
	public class PlayerMovementGroup : ComponentSystemGroup
	{

	}

	[UpdateAfter(typeof(PlayerMovementGroup))]
	public class CameraGroup : ComponentSystemGroup
	{

	}
}