using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICircle : MonoBehaviour {
	[Header("Refs")] [Space]
	[SerializeField] Circle circle = null;

#if UNITY_EDITOR
	private void OnValidate() {
		if (circle == null)
			circle = GetComponent<Circle>();
	}
#endif
}
