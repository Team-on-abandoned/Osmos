using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceInViewpoint : MonoBehaviour {
	[SerializeField] Vector2 viewpointPos;

	private void Start() {
		transform.position = Camera.main.ViewportToWorldPoint(viewpointPos);
		Destroy(this);
	}
}
