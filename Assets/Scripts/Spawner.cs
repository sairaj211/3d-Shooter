﻿using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour {

	public bool devMode;


	[System.Serializable]
	public class Wave {
		public bool infinite;
		public int enemyCount;
		public float timeBetweenSpawns;

		public float moveSpeed;
		public int hitsToKillPlayer;
		public float enemyHealth;
		public Color skinColor;
	}

	public Wave[] waves;
	public Enemy enemy;

	Wave currentWave;
	int currentWaveNumber;

	int enemiesRemainingToSpawn;
	int enemiesRemainingAlive;
	float nextSpawnTime;

	LivingEntity playerEntity;
	Transform playerTransform;
	MapGenerator map;

	float timeBetweenCampingChecks = 2;
	float campThresholdDistance = 1.5f;
	float nextCampCheckTime;
	Vector3 campPositionOld;
	bool isCamping;

	bool isDisabled;



	public event System.Action<int> OnNewWave;

	void Start() {
		playerEntity = FindObjectOfType<Player> ();
		playerTransform = playerEntity.transform;

		nextCampCheckTime = timeBetweenCampingChecks + Time.time;
		campPositionOld = playerTransform.position;
		playerEntity.OnDeath += OnPlayerDeath;

		map = FindObjectOfType<MapGenerator> ();
		NextWave ();
	}

	void Update() {
		if (!isDisabled) {
			if (Time.time > nextCampCheckTime) {
				nextCampCheckTime = Time.time + timeBetweenCampingChecks;
				isCamping = (Vector3.Distance (playerTransform.position, campPositionOld) < campThresholdDistance);
				campPositionOld = playerTransform.position;
			}
			if ((enemiesRemainingToSpawn > 0 || currentWave.infinite) && Time.time > nextSpawnTime) {
				enemiesRemainingToSpawn--;
				nextSpawnTime = Time.time + currentWave.timeBetweenSpawns;
				
				StartCoroutine ("SpawnEnemy");
			}
		}

		if (devMode) {
			if(Input.GetKeyDown(KeyCode.Return)){
				StopCoroutine("SpawnEnemy");
				foreach (Enemy enemy in FindObjectsOfType<Enemy>()){
					GameObject.Destroy(enemy.gameObject);
				}
				NextWave();
			}
		}
	}

	IEnumerator SpawnEnemy(){
		float spawnDelay = 1;
		float tileFlashSpeed = 4;

		Transform spawnTile = map.GetRandomOpenTile ();
		if (isCamping) {
			spawnTile = map.GetTileFromPosition (playerTransform.position);
		}
		Material tileMat = spawnTile.GetComponent<Renderer> ().material;
		Color initialColor = Color.white;
		Color flashColor = Color.red;
		float spawnTime = 0;

		while (spawnTime < spawnDelay) {
			tileMat.color = Color.Lerp (initialColor, flashColor, Mathf.PingPong (spawnTime * tileFlashSpeed, 1));
			spawnTime += Time.deltaTime;

			yield return null;
		}

		Enemy spawnedEnemy = Instantiate(enemy, spawnTile.position + Vector3.up, Quaternion.identity) as Enemy;
		spawnedEnemy.OnDeath += OnEnemyDeath;
		spawnedEnemy.SetCharacteristic (currentWave.moveSpeed, currentWave.hitsToKillPlayer, currentWave.enemyHealth, currentWave.skinColor);
	}

	void OnPlayerDeath(){
		isDisabled = true;
	}

	void OnEnemyDeath() {
		enemiesRemainingAlive --;

		if (enemiesRemainingAlive == 0) {
			NextWave();
		}
	}

	void ResetPlayerPosition(){
		playerTransform.position = map.GetTileFromPosition (Vector3.zero).position + Vector3.up * 3;
	}

	void NextWave() {
		if (currentWaveNumber > 0) {
			AudioManager.instance.PlaySound2D ("Level Complete");
		}
		currentWaveNumber ++;

		if (currentWaveNumber - 1 < waves.Length) {
			currentWave = waves [currentWaveNumber - 1];

			enemiesRemainingToSpawn = currentWave.enemyCount;
			enemiesRemainingAlive = enemiesRemainingToSpawn;

			if (OnNewWave != null) {
				OnNewWave (currentWaveNumber);
			}
			ResetPlayerPosition ();
		}
	}


}