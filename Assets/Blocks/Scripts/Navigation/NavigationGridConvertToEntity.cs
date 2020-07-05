using UnityEngine;
using Unity.Entities;

namespace Blocks
{
	[RequiresEntityConversion]
	public class NavigationGridConvertToEntity : MonoBehaviour, IConvertGameObjectToEntity
	{
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddBuffer<AStarNodeElement>(entity);

			NavigationGridData grid = dstManager.GetComponentData<NavigationGridData>(entity);
			grid.pathRequested = false;
		}
	}
}