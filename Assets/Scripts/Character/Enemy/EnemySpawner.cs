using System;
using System.Collections.Generic;
using UnityEngine;

namespace Madbox.Character
{
    [DisallowMultipleComponent]
    public sealed class EnemySpawner : MonoBehaviour
    {
        [Header("Enemy")]
        [SerializeField] private GameObject enemyPrefab;

        [Header("Spawn")]
        [SerializeField, Min(0f)] private float spawnRadius = 8f;
        [SerializeField, Min(0.05f)] private float spawnIntervalSeconds = 1.5f;
        [SerializeField, Min(1)] private int maxAliveEnemies = 5;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private string playerTag = "Player";
        [SerializeField, Min(0f)] private float minDistanceFromPlayer = 3f;
        [SerializeField, Min(0f)] private float minDistanceBetweenEnemies = 1.5f;
        [SerializeField, Min(1)] private int maxPositionAttempts = 10;

        [Header("Random")]
        [SerializeField] private bool useSeed;
        [SerializeField] private int seed;

        private readonly List<GameObject> _pooledEnemies = new();

        private System.Random _rng;
        private float _nextSpawnTime;
        private bool _missingPlayerWarningShown;
        private bool _failedSpawnPositionWarningShown;

        private void Awake()
        {
            InitializeRandom();
            ResolvePlayerReference();
            PrewarmPool();
            _nextSpawnTime = Time.time + spawnIntervalSeconds;
        }

        private void Update()
        {
            if (enemyPrefab == null || maxAliveEnemies <= 0)
            {
                return;
            }

            if (Time.time < _nextSpawnTime)
            {
                return;
            }

            _nextSpawnTime = Time.time + spawnIntervalSeconds;

            if (GetActiveEnemyCount() >= maxAliveEnemies)
            {
                return;
            }

            TrySpawnEnemy();
        }

        private void InitializeRandom()
        {
            _rng = useSeed ? new System.Random(seed) : new System.Random();
        }

        private void PrewarmPool()
        {
            _pooledEnemies.Clear();

            if (enemyPrefab == null)
            {
                Debug.LogWarning("EnemySpawner: Enemy prefab is missing.", this);
                return;
            }

            for (int i = 0; i < maxAliveEnemies; i++)
            {
                GameObject enemyInstance = Instantiate(enemyPrefab, transform.position, Quaternion.identity, transform);
                enemyInstance.SetActive(false);
                _pooledEnemies.Add(enemyInstance);
            }
        }

        private void TrySpawnEnemy()
        {
            GameObject enemy = GetFirstInactiveEnemy();
            if (enemy == null)
            {
                return;
            }

            if (!TryFindSpawnPosition(out Vector3 spawnPosition))
            {
                if (!_failedSpawnPositionWarningShown)
                {
                    Debug.LogWarning("EnemySpawner: Could not find a valid spawn position. This warning is shown once.", this);
                    _failedSpawnPositionWarningShown = true;
                }

                return;
            }

            _failedSpawnPositionWarningShown = false;

            enemy.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
            ResetPooledEnemy(enemy);
            enemy.SetActive(true);
        }

        private GameObject GetFirstInactiveEnemy()
        {
            for (int i = 0; i < _pooledEnemies.Count; i++)
            {
                GameObject pooledEnemy = _pooledEnemies[i];
                if (pooledEnemy != null && !pooledEnemy.activeSelf)
                {
                    return pooledEnemy;
                }
            }

            return null;
        }

        private bool TryFindSpawnPosition(out Vector3 spawnPosition)
        {
            float minDistanceFromPlayerSqr = minDistanceFromPlayer * minDistanceFromPlayer;
            float minDistanceBetweenEnemiesSqr = minDistanceBetweenEnemies * minDistanceBetweenEnemies;
            Vector3 spawnerPosition = transform.position;

            for (int i = 0; i < maxPositionAttempts; i++)
            {
                Vector2 pointInCircle = RandomPointInCircle() * spawnRadius;
                Vector3 candidate = new Vector3(
                    spawnerPosition.x + pointInCircle.x,
                    spawnerPosition.y,
                    spawnerPosition.z + pointInCircle.y);

                if (!IsFarEnoughFromPlayer(candidate, minDistanceFromPlayerSqr))
                {
                    continue;
                }

                if (!IsFarEnoughFromActiveEnemies(candidate, minDistanceBetweenEnemiesSqr))
                {
                    continue;
                }

                spawnPosition = candidate;
                return true;
            }

            spawnPosition = default;
            return false;
        }

        private bool IsFarEnoughFromPlayer(Vector3 candidate, float minDistanceFromPlayerSqr)
        {
            ResolvePlayerReference();
            if (playerTransform == null)
            {
                return true;
            }

            Vector3 toPlayer = candidate - playerTransform.position;
            toPlayer.y = 0f;
            return toPlayer.sqrMagnitude >= minDistanceFromPlayerSqr;
        }

        private bool IsFarEnoughFromActiveEnemies(Vector3 candidate, float minDistanceBetweenEnemiesSqr)
        {
            for (int i = 0; i < _pooledEnemies.Count; i++)
            {
                GameObject pooledEnemy = _pooledEnemies[i];
                if (pooledEnemy == null || !pooledEnemy.activeInHierarchy)
                {
                    continue;
                }

                Vector3 offset = candidate - pooledEnemy.transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude < minDistanceBetweenEnemiesSqr)
                {
                    return false;
                }
            }

            return true;
        }

        private void ResetPooledEnemy(GameObject enemy)
        {
            if (enemy.TryGetComponent(out Health health))
            {
                health.ResetToMax();
            }
            else
            {
                Health healthInChildren = enemy.GetComponentInChildren<Health>(true);
                if (healthInChildren != null)
                {
                    healthInChildren.ResetToMax();
                }
            }

            if (enemy.TryGetComponent(out CharacterAnimationDriver animationDriver))
            {
                animationDriver.ResetToIdle();
            }
            else
            {
                CharacterAnimationDriver animationInChildren = enemy.GetComponentInChildren<CharacterAnimationDriver>(true);
                if (animationInChildren != null)
                {
                    animationInChildren.ResetToIdle();
                }
            }
        }

        private int GetActiveEnemyCount()
        {
            int count = 0;
            for (int i = 0; i < _pooledEnemies.Count; i++)
            {
                GameObject pooledEnemy = _pooledEnemies[i];
                if (pooledEnemy != null && pooledEnemy.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        private Vector2 RandomPointInCircle()
        {
            float angle = NextFloat01() * Mathf.PI * 2f;
            float radius = Mathf.Sqrt(NextFloat01());
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        private float NextFloat01()
        {
            return (float)_rng.NextDouble();
        }

        private void ResolvePlayerReference()
        {
            if (playerTransform != null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(playerTag))
            {
                return;
            }

            GameObject taggedPlayer = GameObject.FindWithTag(playerTag);
            if (taggedPlayer != null)
            {
                playerTransform = taggedPlayer.transform;
                _missingPlayerWarningShown = false;
                return;
            }

            if (!_missingPlayerWarningShown)
            {
                Debug.LogWarning($"EnemySpawner: Player transform is missing and no object was found with tag '{playerTag}'.", this);
                _missingPlayerWarningShown = true;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
    }
}
