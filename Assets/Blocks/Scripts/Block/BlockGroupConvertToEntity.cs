using UnityEngine;
using Unity.Entities;

namespace Blocks
{
	[RequiresEntityConversion]
	public class BlockGroupConvertToEntity : MonoBehaviour, IConvertGameObjectToEntity
	{
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			// Get child objects (blocks in this group)
			GameObject[] groupBlockObjects = new GameObject[this.transform.childCount];
			int count = 0;
			foreach (Transform child in this.transform) {
				groupBlockObjects[count++] = child.gameObject;
			}

			// Store blocks in group
			DynamicBuffer<EntityElement> entityBuff = dstManager.AddBuffer<EntityElement>(entity);
			for (int i = 0, n = groupBlockObjects.Length; i < n; i++) {
				Entity blockEntity = conversionSystem.GetPrimaryEntity(groupBlockObjects[i]);
				entityBuff.Add(blockEntity);
			}
		}
	}
}