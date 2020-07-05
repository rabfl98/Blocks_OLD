using Unity.Entities;
using Unity.Transforms;

namespace Blocks
{
	public sealed class EntityUtils
	{
		public static void DisableEntityHierarchy(ref EntityManager em, Entity currentEntity, in BufferFromEntity<Child> childrenBuffer)
		{
			// Recursively disable entity hierarchy

			em.AddComponent<Disabled>(currentEntity);

			if (childrenBuffer.Exists(currentEntity)) {
				DynamicBuffer<Child> children = childrenBuffer[currentEntity];

				for (int i = 0, n = children.Length; i < n; i++) {
					DisableEntityHierarchy(ref em, children[i].Value, in childrenBuffer);
				}
			}
		}

		public static void DisableEntityHierarchy(ref EntityCommandBuffer.Concurrent ecb, int jobIndex, Entity currentEntity, in BufferFromEntity<Child> childrenBuffer)
		{
			// Recursively disable entity hierarchy
			
			ecb.AddComponent<Disabled>(jobIndex, currentEntity);

			if (childrenBuffer.Exists(currentEntity)) {
				DynamicBuffer<Child> children = childrenBuffer[currentEntity];

				for (int i = 0, n = children.Length; i < n; i++) {
					DisableEntityHierarchy(ref ecb, jobIndex, children[i].Value, in childrenBuffer);
				}
			}
		}

		public static void EnableEntityHierarchy(ref EntityManager em, Entity currentEntity, in BufferFromEntity<Child> childrenBuffer)
		{
			// Recursively disable entity hierarchy

			em.RemoveComponent<Disabled>(currentEntity);

			if (childrenBuffer.Exists(currentEntity)) {
				DynamicBuffer<Child> children = childrenBuffer[currentEntity];

				for (int i = 0, n = children.Length; i < n; i++) {
					EnableEntityHierarchy(ref em, children[i].Value, in childrenBuffer);
				}
			}
		}

		public static void EnableEntityHierarchy(ref EntityCommandBuffer.Concurrent ecb, int jobIndex, Entity currentEntity, in BufferFromEntity<Child> childrenBuffer)
		{
			// Recursively enable entity hierarchy
			
			ecb.RemoveComponent<Disabled>(jobIndex, currentEntity);

			if (childrenBuffer.Exists(currentEntity)) {
				DynamicBuffer<Child> children = childrenBuffer[currentEntity];

				for (int i = 0, n = children.Length; i < n; i++) {
					EnableEntityHierarchy(ref ecb, jobIndex, children[i].Value, in childrenBuffer);
				}
			}
		}
	}
}