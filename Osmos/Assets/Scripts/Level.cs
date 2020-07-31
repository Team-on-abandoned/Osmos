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

	[Header("Scene Refs")] [Space]
	[SerializeField] MenuManager menuManager;

	//Loaded data from Streaming Assets
	int remaingLoads = 0;
	LevelGeneratorData levelGeneratorData = null;
	ColorData colorData = null;

	Circle player = null;
	List<Circle> enemies = null;
	Circle minEnemy, maxEnemy;

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
		LeanTween.cancel(gameObject, false);
		ClearLevel();
		SpawnPlayer();
		SpawnEnemies();

		RecalEnemySizes();
		RepaintEnemies(true);
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

	void ClearLevel() {
		//Can be rewritten with pooling instead of respawning
		if (player != null)
			Destroy(player.gameObject);
		foreach (var _enemy in enemies)
			if (_enemy != null)
				Destroy(_enemy.gameObject);
		enemies.Clear();
	}

	void SpawnPlayer() {
		player = Instantiate(playerPrefab, transform.position, Quaternion.identity, transform).GetComponent<Circle>();
		player.Radius = levelGeneratorData.playerRadius;
		player.SetColor(colorData.playerColor, true);
		player.Init();

		player.onGrowAction += OnPlayerGrow;
		player.onDieAction += OnPlayerDie;
	}

	void SpawnEnemies() {
		Circle enemy;
		ContactFilter2D contactFilter = new ContactFilter2D() { };
		List<Collider2D> results = new List<Collider2D>(4);
		int tries;
		const int maxTries = 50;

		for (int i = 0; i < levelGeneratorData.enemiesToSpawn; ++i) {
			tries = 0;
			enemy = Instantiate(enemyPrefab, GetRandomSpawnPoint(), Quaternion.identity, transform).GetComponent<Circle>();
			enemy.name = $"{enemyPrefab.name} - {i}";
			enemy.Radius = levelGeneratorData.enemyMaxRadius * 1.2f;	//Temp radius, to spawn enemies away from each other

			//For proper OverlapCollider work I enable Auto Sync Transforms, which is bad for performance	
			//Can be rewritten with (x – h)2 + (y – k)2 = r2
			while (enemy.collider.OverlapCollider(contactFilter, results) != 0 && (player.transform.position - enemy.transform.position).sqrMagnitude <= 6.25f && tries++ <= maxTries) {
				enemy.transform.position = GetRandomSpawnPoint();
			}

			if (tries > maxTries) {
				Destroy(enemy.gameObject);
				Debug.Log($"No free space for enemy {enemy.name}. Skipping it");
			}
			else {
				enemy.onGrowAction += OnEnemyGrow;
				enemy.onDieAction += OnEnemyDie;

				enemies.Add(enemy);
			}
		}

		for (int i = 0; i < enemies.Count; ++i) {
			if (i == 0)
				enemies[i].Radius = levelGeneratorData.enemyMaxRadius;   //Just to be sure, that there are at least 1 enemy bigger than player
			else
				enemies[i].Radius = Random.Range(levelGeneratorData.enemyMinRadius, levelGeneratorData.enemyMaxRadius);
			enemies[i].Init();
		}

		Debug.Log($"Spawn {enemies.Count} enemies from {enemies.Capacity} desired");
	}

	void OnPlayerGrow(Circle c) {
		RepaintEnemies(false);

		if (maxEnemy == null || maxEnemy.DesiredRadius <= player.DesiredRadius) {
			Debug.Log("Win game");
			LeanTween.delayedCall(gameObject, menuManager.Show("WinScreen"), ClearLevel);
		}
	}

	void OnPlayerDie(Circle c) {
		Debug.Log("Lose game");
		LeanTween.delayedCall(gameObject, menuManager.Show("LoseScreen"), ClearLevel);
	}

	void OnEnemyGrow(Circle c) {
		if(c == minEnemy || c == maxEnemy || c.DesiredRadius < minEnemy.DesiredRadius || c.DesiredRadius > maxEnemy.DesiredRadius) {
			RecalEnemySizes();
			RepaintEnemies(false);
		}
		else {
			RepaintEnemy(c, false);
		}
	}

	void OnEnemyDie(Circle c) {
		enemies.Remove(c);

		if(c == minEnemy || c == maxEnemy) {
			RecalEnemySizes();
			RepaintEnemies(false);
		}
	}

	void RecalEnemySizes() {
		if (enemies.Count != 0) {
			minEnemy = enemies[0];
			maxEnemy = enemies[0];

			foreach (var enemy in enemies) {
				if (minEnemy.DesiredRadius > enemy.DesiredRadius)
					minEnemy = enemy;
				if (maxEnemy.DesiredRadius < enemy.DesiredRadius)
					maxEnemy = enemy;
			}
		}
	}

	void RepaintEnemies(bool isForce) {
		foreach (var enemy in enemies) {
			RepaintEnemy(enemy, isForce);
		}
	}

	void RepaintEnemy(Circle enemy, bool isForce) {
		if (enemy.DesiredRadius < player.DesiredRadius)
			enemy.SetColor(Color.Lerp(colorData.enemyColor1, colorData.enemyColor2, (enemy.DesiredRadius - minEnemy.DesiredRadius) / player.DesiredRadius / 2), isForce);
		else
			enemy.SetColor(Color.Lerp(colorData.enemyColor1, colorData.enemyColor2, enemy.DesiredRadius / maxEnemy.DesiredRadius / 2 + 0.5f), isForce);
	}
}
