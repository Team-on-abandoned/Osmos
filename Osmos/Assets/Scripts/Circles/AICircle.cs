using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICircle : MonoBehaviour {
	enum AIStateEnum : byte { Wander, Evade, Attack }

	[Header("Balance")] [Space]
	[SerializeField] [MinMaxSlider(0, 10, false)] Vector2 movingForce = new Vector2(1, 3);
	[SerializeField] [MinMaxSlider(0, 10, false)] Vector2 movingCooldown = new Vector2(1, 3);
	[SerializeField] float softStartTime = 4.0f;
	[SerializeField] float softStartSpeed = 0.2f;
	[SerializeField] float softStartCooldown = 0.5f;
	float currMovingCooldown;
	float currSoftStartTime;

	[Header("Refs")] [Space]
	[SerializeField] Circle circle = null;
	[SerializeField] Rigidbody2D rb = null;

	List<Circle> enemiesInRange = null;
	AIStateEnum state = AIStateEnum.Wander;

#if UNITY_EDITOR
	private void OnValidate() {
		if (circle == null)
			circle = GetComponent<Circle>();
		if (rb == null)
			rb = GetComponent<Rigidbody2D>();
	}
#endif

	private void Awake() {
		enemiesInRange = new List<Circle>(4);
		currMovingCooldown = 0;
		currSoftStartTime = softStartTime * 0.9f;
	}

	private void OnTriggerEnter2D(Collider2D collision) {
		if (!collision.isTrigger && circle.isEnemy(collision.gameObject)) {
			Circle other = collision.gameObject.GetComponent<Circle>();
			enemiesInRange.Add(other);

			if (other.Radius > circle.Radius) {
				state = AIStateEnum.Evade;
			}
			else if (other.Radius < circle.Radius && state != AIStateEnum.Evade) {
				state = AIStateEnum.Attack;
			}
			else {
				state = AIStateEnum.Wander;
			}
		}
	}

	private void OnTriggerExit2D(Collider2D collision) {
		if (!collision.isTrigger && circle.isEnemy(collision.gameObject)) {
			Circle other = collision.gameObject.GetComponent<Circle>();
			enemiesInRange.Remove(other);

			if(enemiesInRange.Count == 0) {
				state = AIStateEnum.Wander;
			}
			else {
				bool isAnyBiggerEnemy = false;

				foreach (var enemy in enemiesInRange) {
					if (enemy.Radius > circle.Radius) {
						isAnyBiggerEnemy = true;
						break;
					}
				}

				if (isAnyBiggerEnemy) 
					state = AIStateEnum.Evade;
				else 
					state = AIStateEnum.Attack;
			}
		}
	}

	private void Update() {
		if (circle.IsDead)
			return;

		currMovingCooldown -= Time.deltaTime;
		softStartTime -= Time.deltaTime;

		if (currMovingCooldown <= 0) {
			currMovingCooldown = Random.Range(movingCooldown.x, movingCooldown.y) * Mathf.Lerp(1, softStartCooldown, currSoftStartTime / softStartTime);
			ProcessAI();
		}
	}

	void ProcessAI() {
		switch (state) {
			case AIStateEnum.Wander:
				circle.AddForce((rb.velocity.normalized + Random.insideUnitCircle).normalized * GetRandomMovingForce());
				break;

			case AIStateEnum.Evade:
				Vector2 escapeVector = Vector2.zero;
				for (int i = 0; i < enemiesInRange.Count; ++i) {
					if (enemiesInRange[i].Radius > circle.Radius) {
						escapeVector += (Vector2)(transform.position - enemiesInRange[i].transform.position);
					}
				}

				circle.AddForce(escapeVector.normalized * GetRandomMovingForce());
				break;

			case AIStateEnum.Attack:
				Circle closestFood = enemiesInRange[0];
				float dist = (enemiesInRange[0].transform.position - transform.position).sqrMagnitude;
				for (int i = 1; i < enemiesInRange.Count; ++i) {
					float newDist = (enemiesInRange[i].transform.position - transform.position).sqrMagnitude;
					if (dist > newDist) {
						dist = newDist;
						closestFood = enemiesInRange[i];
					}
				}

				circle.AddForce((closestFood.transform.position - transform.position).normalized * GetRandomMovingForce());
				break;
		}
	}

	float GetRandomMovingForce() {
		return Random.Range(movingForce.x, movingForce.y) * Mathf.Lerp(1, softStartSpeed, currSoftStartTime / softStartTime);
	}
}
