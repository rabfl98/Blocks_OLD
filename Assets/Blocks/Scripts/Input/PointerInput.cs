using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace Blocks
{
	public struct Pointer
	{
		public bool Pressed { get; private set; }
		public bool Holding { get; private set; }
		public bool Released { get; private set; }

		public float2 ScreenPosition { get; private set; }
		public float2 PrevScreenPosition { get; private set; }
		public float2 DeltaPosition { get { return this.ScreenPosition - this.PrevScreenPosition; } }

		public float3 WorldPosition { get; private set; }
		public float3 WorldDirection { get; private set; }

		public void Update(bool pressed, bool holding, bool released, float2 position, float3 worldPosition, float3 worldDirection)
		{
			this.Pressed = pressed;
			this.Holding = holding;
			this.Released = released;

			this.PrevScreenPosition = pressed ? position : this.ScreenPosition;
			this.ScreenPosition = position;

			this.WorldPosition = worldPosition;
			this.WorldDirection = worldDirection;
		}

		public void Clear()
		{
			this.Pressed = false;
			this.Holding = false;
			this.Released = false;

			this.ScreenPosition = this.PrevScreenPosition = float2.zero;
			this.WorldPosition = this.WorldDirection = float3.zero;
		}
	}

	public class PointerInput : MonoBehaviour
	{
		// Simplistic pointer input manager made static for use in ECS

		[SerializeField] private Camera raycastCamera = default;

		private static Pointer[] pointers = new Pointer[2];

		public static Pointer GetPointer(int index)
		{
			return pointers[index];
		}

		public static Pointer GetPointer()
		{
			return pointers[0];
		}

		private void Update()
		{
			Vector3 forward = this.raycastCamera.transform.forward;

			for (int i = 0, n = pointers.Length; i < n; i++) {
#if true
				Vector2 position = Input.mousePosition;
				Ray ray = this.raycastCamera.ScreenPointToRay(new Vector3(position.x, position.y, 0.01f));
				
				pointers[i].Update(
					Input.GetMouseButtonDown(0),
					Input.GetMouseButton(0),
					Input.GetMouseButtonUp(0),
					position,
					ray.origin,
					ray.direction
				);
#else
				if (i >= Input.touchCount) {
					pointers[i].Clear();
					continue;
				}

				Touch touch = Input.touches[i];
				TouchPhase phase = touch.phase;
				Vector2 position = touch.position;
				Ray ray = this.raycastCamera.ScreenPointToRay(new Vector3(position.x, position.y, 0.01f));
				
				pointers[i].Update(
					phase == TouchPhase.Began,
					phase == TouchPhase.Stationary || phase == TouchPhase.Moved,
					phase == TouchPhase.Ended,
					position,
					ray.origin,
					ray.direction
				);
#endif
			}
		}
	}
}