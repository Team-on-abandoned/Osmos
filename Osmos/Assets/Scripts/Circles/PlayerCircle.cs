using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCircle : MonoBehaviour {
	readonly Color newVelocityColor = new Color(0.3f, 0.7f, 0.0f);

	[Header("Balance")] [Space]
	[SerializeField] float forceOverDistance = 10.0f;
	[SerializeField] float forceOverMass = 0.5f;  //All circles slow down with radius increase, because of rigid body autosize. But I want to keep player faster than enemies)

	[Header("Refs")] [Space]
	[SerializeField] Circle circle = null;
	[SerializeField] Rigidbody2D rb = null;
	new Camera camera = null; //new keyword required, cuz camera already exist in MonoBehaviour, but marked obsolete

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
	}
#endif

	private void Start() {
		camera = Camera.main;
	}

	private void Update() {
		if (circle.IsDead)
			return;

		lastCursorWorldPosRelative = (Vector2)transform.position - lastCursorWorldPos;

#if UNITY_EDITOR
		if (!isPointerDown)
			return;
		Debug.DrawLine(transform.position, lastCursorWorldPos, Color.yellow);
		Debug.DrawRay(transform.position, lastCursorWorldPosRelative, Color.yellow);
		Debug.DrawRay(transform.position, GetForce() / rb.mass + rb.velocity, newVelocityColor);
#endif
	}

	public void OnPointerMove(InputAction.CallbackContext context) {
		if (!isPointerDown || circle.IsDead)
			return;

		lastCursorScreenPos = context.ReadValue<Vector2>();
		lastCursorWorldPos = camera.ScreenToWorldPoint(lastCursorScreenPos);
	}

	public void OnPointerPress(InputAction.CallbackContext context) {
		if (circle.IsDead)
			return;

		switch (context.phase) {
			case InputActionPhase.Started:
				isPointerDown = true;
				break;

			case InputActionPhase.Canceled:
				isPointerDown = false;
				circle.AddForce(GetForce());
				lastCursorWorldPosRelative = lastCursorWorldPos = lastCursorScreenPos = Vector2.zero;
				break;
		}
	}

	Vector2 GetForce() {
		return lastCursorWorldPosRelative * (forceOverDistance + forceOverMass * rb.mass);
	}
}
