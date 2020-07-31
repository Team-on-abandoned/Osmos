using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

using Random = UnityEngine.Random;

public class Level : MonoBehaviour {
	static new Camera camera = null;

	[Header("Streaming Assets Path")]
	[Space]
	[SerializeField] string generatorFile = "LevelGeneratorData.json";
	[SerializeField] string colorsFile = "ColorsData.json";

	[Header("Prefabs")]
	[Space]
	[SerializeField] GameObject enemyPrefab = null;
	[SerializeField] GameObject playerPrefab = null;

	//Loaded data from Streaming Assets
	int remaingLoads = 0;
	LevelGeneratorData levelGeneratorData = null;
	ColorData colorData = null;

	Circle player = null;
	List<Circle> enemies = null;

	private void Awake() {
		if (camera == null)
			camera = Camera.main;
	}

	private void Start() {
		remaingLoads = 2;
		StartCoroutine(LoadDataFromStreamingAssets<LevelGeneratorData>(generatorFile, OnLoadingDataEnd));
		StartCoroutine(LoadDataFromStreamingAssets<ColorData>(colorsFile, OnLoadingDataEnd));
	}

	public void StartGame() {
		//Can be rewritten with pooling instead of respawning
		if (player != null)
			Destroy(player.gameObject);
		foreach (var _enemy in enemies)
			if (_enemy != null)
				Destroy(_enemy.gameObject);
		enemies.Clear();

		player = Instantiate(playerPrefab, transform.position, Quaternion.identity, transform).GetComponent<Circle>();
		player.Radius = levelGeneratorData.playerRadius;
		player.SetColor(colorData.playerColor);

		Circle enemy;
		ContactFilter2D contactFilter = new ContactFilter2D() { };
		List<Collider2D> results = new List<Collider2D>(4);
		int tries;
		const int maxTries = 50;

		for (int i = 0; i < levelGeneratorData.enemiesToSpawn; ++i) {
			tries = 0;
			enemy = Instantiate(enemyPrefab, GetRandomSpawnPoint(), Quaternion.identity, transform).GetComponent<Circle>();
			enemy.name = $"{enemyPrefab.name} - {i}";

			if(i == 0)
				enemy.Radius = levelGeneratorData.enemyMaxRadius;   //Just to be sure, that there are at least 1 enemy bigger than player
			else
				enemy.Radius = Random.Range(levelGeneratorData.enemyMinRadius, levelGeneratorData.enemyMaxRadius);

			//For proper OverlapCollider work I enable Auto Sync Transforms, which is bad for performance	
			//Can be rewritten with (x – h)2 + (y – k)2 = r2
			while (enemy.collider.OverlapCollider(contactFilter, results) != 0 && tries++ <= maxTries) {
				enemy.transform.position = GetRandomSpawnPoint();
			}

			if(tries > maxTries) {
				Destroy(enemy.gameObject);
				Debug.Log($"No free space for enemy {enemy.name}. Skipping it");	
			}
			else {
				enemies.Add(enemy);
			}


		}

		//Move it on player grow event
		float minEnemySize = enemies[0].Radius;
		float maxEnemySize = enemies[0].Radius;

		foreach (var enemy1 in enemies) {
			if (minEnemySize > enemy1.Radius)
				minEnemySize = enemy1.Radius;
			if (maxEnemySize < enemy1.Radius)
				maxEnemySize = enemy1.Radius;
		}

		foreach (var enemy1 in enemies) {
			if (enemy1.Radius < player.Radius)
				enemy1.SetColor(Color.Lerp(colorData.enemyColor1, colorData.enemyColor2, (enemy1.Radius - minEnemySize) / player.Radius / 2));
			else
				enemy1.SetColor(Color.Lerp(colorData.enemyColor1, colorData.enemyColor2, enemy1.Radius / maxEnemySize / 2 + 0.5f));
		}
		

		Debug.Log($"Spawn {enemies.Count} enemies from {enemies.Capacity} desired");
	}

	IEnumerator LoadDataFromStreamingAssets<T>(string fileName, Action<T> onEndLoad) {
		UnityWebRequest www = UnityWebRequest.Get(Path.Combine(Application.streamingAssetsPath, fileName));
		yield return www.SendWebRequest();
		string json = www.downloadHandler.text;

		onEndLoad?.Invoke(JsonUtility.FromJson<T>(json));
	}

	void OnLoadingDataEnd<T>(T data) {
		if (data is LevelGeneratorData) {
			levelGeneratorData = data as LevelGeneratorData;
			enemies = new List<Circle>(levelGeneratorData.enemiesToSpawn);
		}
		else if (data is ColorData) {
			colorData = data as ColorData;
		}

		--remaingLoads;
		if (remaingLoads == 0)
			StartGame();
	}

	Vector2 GetRandomSpawnPoint() {
		Vector3 pos = camera.ViewportToWorldPoint(new Vector2(Random.Range(0.05f, 0.95f), Random.Range(0.05f, 0.95f)));
		pos.z = 0.0f;
		return pos;
	}
}
