using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCircle : MonoBehaviour {
	readonly Color newVelocityColor = new Color(0.3f, 0.7f, 0.0f);

	[Header("Balance")] [Space]
	[SerializeField] float forceOverDistance = 10.0f;

	[Header("Refs")] [Space]
	[SerializeField] Circle circle = null;
	[SerializeField] Rigidbody2D rb = null;
	[SerializeField] new Camera camera = null; //new keyword required, cuz camera already exist in MonoBehaviour, but marked obsolete

	bool isPointerDown = false;
	Vector2 lastCursorScreenPos = Vector2.zero;
	Vector2 lastCursorWorldPos = Vector2.zero;
	Vector2 lastCursorWorldPosRelative = Vector2.zero;

#if UNITY_EDITOR
	private void OnValidate() {
		if (circle == null)
			circle = GetComponent<Circle>();
		if (rb == null)
			rb = GetComponent<Rigidbody2D>();
		if (camera == null)
			camera = Camera.main;
	}
#endif

	private void Update() {
		lastCursorWorldPosRelative = (Vector2)transform.position - lastCursorWorldPos;

#if UNITY_EDITOR
		if (!isPointerDown)
			return;
		Debug.DrawLine(transform.position, lastCursorWorldPos, Color.yellow);
		Debug.DrawRay(transform.position, lastCursorWorldPosRelative, Color.yellow);
		Debug.DrawRay(transform.position, lastCursorWorldPosRelative * forceOverDistance / rb.mass + rb.velocity, newVelocityColor);
#endif
	}

	public void OnPointerMove(InputAction.CallbackContext context) {
		if (!isPointerDown)
			return;
		lastCursorScreenPos = context.ReadValue<Vector2>();
		lastCursorWorldPos = camera.ScreenToWorldPoint(lastCursorScreenPos);
	}

	public void OnPointerPress(InputAction.CallbackContext context) {
		switch (context.phase) {
			case InputActionPhase.Started:
				isPointerDown = true;
				break;

			case InputActionPhase.Canceled:
				isPointerDown = false;
				circle.AddForce(lastCursorWorldPosRelative * forceOverDistance);
				lastCursorWorldPosRelative = lastCursorWorldPos = lastCursorScreenPos = Vector2.zero;
				break;
		}
	}
}
