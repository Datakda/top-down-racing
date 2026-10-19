using UnityEngine;

namespace TopDownRacing
{
    /// <summary>
    /// Спавнит препятствия и попутный транспорт перед игроком.
    /// Трасса прямая, полосы фиксированные: -3, -1, 1, 3.
    /// </summary>
    public class ObstacleSpawner : MonoBehaviour
    {
        [Header("Препятствия")]
        public Sprite[] hardObstacleSprites;
        public Sprite[] softObstacleSprites;
        public Sprite[] trafficCarSprites;

        [Header("Полосы")]
        public int laneCount = 4;
        public float laneWidth = 2f;

        [Header("Дистанции")]
        public float spawnAhead = 46f;
        public float despawnBehind = 26f;
        public float waveSpacingStart = 15f;
        public float waveSpacingMin = 8.5f;
        public float spacingDecreasePerMeter = 0.035f;

        [Header("Сложность")]
        public float trafficChance = 0.55f;
        public float trafficSpeedMin = 3.5f;
        public float trafficSpeedMax = 12f;
        public int sortingOrder = 5;

        [Header("Декор")]
        public Sprite[] decorationSprites;
        public float decorationChance = 0.55f;
        public float roadHalfWidth = 5f;
        public int decorationSortingOrder = 2;

        public Transform container;
        public CarController player;

        private float nextWaveDistance;
        private int waveCounter;

        private void Start()
        {
            nextWaveDistance = 30f;
        }

        private void Update()
        {
            if (GameManager.Instance == null || player == null)
            {
                return;
            }

            if (!GameManager.Instance.IsPlaying)
            {
                return;
            }

            float playerY = player.transform.position.y;

            while (playerY + spawnAhead >= nextWaveDistance)
            {
                SpawnWave(nextWaveDistance);
                nextWaveDistance += NextSpacing();
            }

            CleanupBehind(playerY);
        }

        private float NextSpacing()
        {
            float spacing = waveSpacingStart - GameManager.Instance.Distance * spacingDecreasePerMeter;
            return Mathf.Max(waveSpacingMin, spacing);
        }

        private void SpawnWave(float worldY)
        {
            waveCounter++;

            int lanes = Mathf.Max(2, laneCount);
            float firstLaneX = -((lanes - 1) * 0.5f) * laneWidth;

            bool[] blocked = new bool[lanes];
            int blockCount = Random.Range(1, Mathf.Max(2, lanes - 1));
            blockCount = Mathf.Min(blockCount, lanes - 1);

            for (int i = 0; i < blockCount; i++)
            {
                int lane = Random.Range(0, lanes);
                blocked[lane] = true;
            }

            for (int lane = 0; lane < lanes; lane++)
            {
                if (!blocked[lane])
                {
                    continue;
                }

                float x = firstLaneX + lane * laneWidth;

                // Иногда ставим целую стену из конусов — выглядит веселее.
                if (Random.value < 0.22f && softObstacleSprites.Length > 0)
                {
                    int count = Random.Range(2, 4);
                    for (int i = 0; i < count; i++)
                    {
                        CreateObstacle(softObstacleSprites[Random.Range(0, softObstacleSprites.Length)],
                            x, worldY + i * 1.1f, true);
                    }
                }
                else if (hardObstacleSprites.Length > 0)
                {
                    CreateObstacle(hardObstacleSprites[Random.Range(0, hardObstacleSprites.Length)],
                        x, worldY, false);
                }
            }

            // Попутный транспорт — в свободные полосы.
            if (trafficCarSprites.Length > 0)
            {
                bool[] used = new bool[lanes];
                int trafficCount = Random.value < trafficChance ? Random.Range(1, 3) : 0;

                for (int i = 0; i < trafficCount; i++)
                {
                    int free = -1;
                    for (int attempt = 0; attempt < 12; attempt++)
                    {
                        int candidate = Random.Range(0, lanes);
                        if (!blocked[candidate] && !used[candidate])
                        {
                            free = candidate;
                            break;
                        }
                    }

                    if (free < 0)
                    {
                        continue;
                    }

                    used[free] = true;

                    float x = firstLaneX + free * laneWidth;
                    float speed = Random.value < 0.35f
                        ? Random.Range(trafficSpeedMin, trafficSpeedMin + 2.5f)
                        : Random.Range(trafficSpeedMin + 2.5f, trafficSpeedMax);
                    float offsetY = Random.Range(-4f, 4f);
                    CreateTrafficCar(trafficCarSprites[Random.Range(0, trafficCarSprites.Length)], x, worldY + offsetY, speed);
                }
            }

            // Деревья и прочий декор вдоль обочины.
            if (decorationSprites.Length > 0 && Random.value < decorationChance)
            {
                Sprite sprite = decorationSprites[Random.Range(0, decorationSprites.Length)];
                float side = Random.value < 0.5f ? -1f : 1f;
                float x = side * (roadHalfWidth + Random.Range(0.8f, 3.5f));
                CreateDecoration(sprite, x, worldY + Random.Range(-2f, 2f));
            }
        }

        private void CreateDecoration(Sprite sprite, float x, float y)
        {
            GameObject go = new GameObject("Deco_" + sprite.name);
            go.transform.SetParent(container != null ? container : transform, false);
            go.transform.position = new Vector3(x, y, 0f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = decorationSortingOrder;
        }

        private void CreateObstacle(Sprite sprite, float x, float y, bool soft)
        {
            GameObject go = new GameObject(sprite.name);
            go.transform.SetParent(container != null ? container : transform, false);
            go.transform.position = new Vector3(x, y, 0f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            // Широкие препятствия (барьеры) ужимаем под ширину полосы.
            float scale = 1f;
            float maxWidth = laneWidth * 0.95f;
            if (sprite.bounds.size.x > maxWidth)
            {
                scale = maxWidth / sprite.bounds.size.x;
                go.transform.localScale = Vector3.one * scale;
            }

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = sprite.bounds.size * 0.7f * scale;
            collider.isTrigger = true;

            Obstacle obstacle = go.AddComponent<Obstacle>();
            obstacle.isSoft = soft;
            obstacle.Setup(player);
        }

        private void CreateTrafficCar(Sprite sprite, float x, float y, float speed)
        {
            GameObject go = new GameObject(sprite.name);
            go.transform.SetParent(container != null ? container : transform, false);
            go.transform.position = new Vector3(x, y, 0f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            // Сопернику нужен Rigidbody2D, иначе он не может двигаться.
            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = sprite.bounds.size * 0.75f;
            collider.isTrigger = true;

            TrafficCar traffic = go.AddComponent<TrafficCar>();
            traffic.Setup(player, speed);
        }

        private void CleanupBehind(float playerY)
        {
            Transform parent = container != null ? container : transform;
            float limit = playerY - despawnBehind;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.position.y < limit)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        public void ResetSpawner()
        {
            nextWaveDistance = 30f;
            waveCounter = 0;
        }

        public void ClearAll()
        {
            Transform parent = container != null ? container : transform;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
