using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Circle : MonoBehaviour {
	[Header("Refs")] [Space]
	[SerializeField] SpriteRenderer sr = null;
	[SerializeField] Rigidbody2D rb = null;
	[SerializeField] new CircleCollider2D collider = null;  //new keyword required, cuz collider already exist in MonoBehaviour, but marked obsolete

	Vector2 force = Vector2.zero;

#if UNITY_EDITOR
	private void OnValidate() {
		if (sr == null)
			sr = GetComponent<SpriteRenderer>();
		if (rb == null)
			rb = GetComponent<Rigidbody2D>();
		if (collider == null)
			collider = GetComponent<CircleCollider2D>();
	}
#endif

#if UNITY_EDITOR
	private void Update() {
		if(rb.velocity.sqrMagnitude >= 0.0025f)
			Debug.DrawRay(transform.position, rb.velocity, Color.green);
	}
#endif

	private void FixedUpdate() {
		if (force == Vector2.zero)
			return;
		rb.AddForce(force, ForceMode2D.Impulse);
		force = Vector2.zero;
	}

	public void AddForce(Vector2 _force) {
		force += _force;
	}
}
