using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class Circle : MonoBehaviour {
	public float Radius {	//This is real radius, that uses for visual and calculations
		get => collider.radius;
		set {
			collider.radius = value;
			sr.size = new Vector2(collider.radius * 2, collider.radius * 2);
		}
	}
	public float DesiredRadius { get; private set; }    //This radius used for animations. Radius become DesiredRadius in growTime seconds
	public bool IsDead { get; private set; } = false;

	public Action<Circle> onDieAction;
	public Action<Circle> onGrowAction;

	[Header("Balance")] [Space]
	[SerializeField] [Tag] string enemyTag = "Circle";
	[SerializeField] float growTime = 0.5f;

	[Header("Refs")] [Space]
	public new CircleCollider2D collider = null;  //new keyword required, cuz collider already exist in MonoBehaviour, but marked obsolete
	[SerializeField] SpriteRenderer sr = null;
	[SerializeField] Rigidbody2D rb = null;

	Vector2 force = Vector2.zero;
	LTDescr colorTween;

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

	private void OnCollisionEnter2D(Collision2D collision) {
		if (isEnemy(collision.gameObject)) {
			Circle other = collision.gameObject.GetComponent<Circle>();
			if(other.Radius < Radius) {
				DesiredRadius = Mathf.Sqrt((DesiredRadius * DesiredRadius + other.Radius * other.Radius));

				other.Die();
				other.transform.SetParent(transform);
				LeanTween.moveLocal(other.gameObject, Vector3.zero, growTime * 0.9f);
				LeanTween.scale(other.gameObject, Vector3.zero, growTime * 0.9f);
				Destroy(other.gameObject, growTime);

				LeanTween.value(gameObject, Radius, DesiredRadius, growTime)
				.setOnUpdate((float r) => {
					Radius = r;
				});
				onGrowAction?.Invoke(this);
			}
		}
	}

	public void Init() {
		DesiredRadius = Radius;
	}

	public void AddForce(Vector2 _force) {
		force += _force;
	}

	public void SetColor(Color color, bool isForce) {
		if (colorTween != null)
			LeanTween.cancel(colorTween.uniqueId, false);

		if (isForce) {
			sr.color = color;
		}
		else {
			colorTween = LeanTween.value(gameObject, sr.color, color, growTime)
			.setOnUpdate((Color c) => {
				sr.color = c;
			});
		}
	}

	public bool isEnemy(GameObject go) {
		return go.CompareTag(enemyTag);
	}

	public void Die() {
		IsDead = true;
		rb.isKinematic = true;
		rb.velocity = Vector2.zero;
		collider.enabled = false;
		onDieAction?.Invoke(this);
	}
}
