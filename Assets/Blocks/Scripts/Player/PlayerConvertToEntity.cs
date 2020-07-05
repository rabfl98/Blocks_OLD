using UnityEngine;
using Unity.Entities;

namespace Blocks
{
	[RequiresEntityConversion]
	public class PlayerConvertToEntity : MonoBehaviour, IConvertGameObjectToEntity
	{
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddBuffer<PlayerNavigationNodeElement>(entity);
		}
	}
}