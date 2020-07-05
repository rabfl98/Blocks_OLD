using UnityEngine;
using Unity.Entities;

namespace Blocks
{
	public enum BlockSwitchState
	{
		Off = 0,
		On = 1,
	}

	[GenerateAuthoringComponent]
	public struct BlockSwitchData : IComponentData
	{
		public BlockSwitchState startState;
		public bool isSingleActivation;

		public Entity onView;
		public Entity offView;

		[HideInInspector] public BlockSwitchState currentState;
		[HideInInspector] public bool hasActivated;
		[HideInInspector] public bool wasJustActivated;

		public void Reset()
		{
			this.currentState = startState;
			this.hasActivated = false;
		}

		public void Activate()
		{
			if (this.hasActivated && this.isSingleActivation) {
				// Can only activate this switch once
				return;
			}

			this.currentState = this.currentState == BlockSwitchState.Off
				? BlockSwitchState.On
				: BlockSwitchState.Off;

			this.hasActivated = true;
			this.wasJustActivated = true;
		}

		public void RefreshView(ref EntityManager em)
		{
			em.AddComponent<Disabled>(this.currentState == BlockSwitchState.On ? this.offView : this.onView);
			em.RemoveComponent<Disabled>(this.currentState == BlockSwitchState.On ? this.onView : this.offView);
		}

		public void RefreshView(ref EntityCommandBuffer.Concurrent ecb, int jobIndex)
		{
			ecb.AddComponent<Disabled>(jobIndex, this.currentState == BlockSwitchState.On ? this.offView : this.onView);
			ecb.RemoveComponent<Disabled>(jobIndex, this.currentState == BlockSwitchState.On ? this.onView : this.offView);
		}
	}
}