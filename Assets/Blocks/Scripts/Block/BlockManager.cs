using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using System.Collections.Generic;

namespace Blocks
{
	[RequiresEntityConversion]
	public class BlockManager : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
	{
		private static BlockManager instance;
		public static BlockManager Instance
		{
			get
			{
				if (instance == null) {
					instance = FindObjectOfType<BlockManager>();
				}
				return instance;
			}
		}

		[SerializeField] private GameObject blockPrefab = default;
		private Entity blockPrefabEntity;

		public Entity BlockPrefabEntity { get { return this.blockPrefabEntity; } }

		public void DeclareReferencedPrefabs(List<GameObject> gameObjects)
		{
			// Required by IDeclareReferencedPrefabs
			gameObjects.Add(this.blockPrefab);
		}

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			// Called upon converting this GameObject to entity

			// Get entity representation of block prefab and store it
			this.blockPrefabEntity = conversionSystem.GetPrimaryEntity(this.blockPrefab);
		}

		public Entity InstantiateBlock(float3 position)
		{
			// Instantiate block entity from blockPrefabEntity

			EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

			Entity blockEntity = entityManager.Instantiate(this.blockPrefabEntity);

			Translation translation = entityManager.GetComponentData<Translation>(blockEntity);
			translation.Value = position;
			entityManager.SetComponentData<Translation>(blockEntity, translation);

			return blockEntity;
		}
	}
}