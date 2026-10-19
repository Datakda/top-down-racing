using UnityEngine;

namespace TopDownRacing
{
    /// <summary>
    /// Простой след пыли: пул спрайтов, которые растут и исчезают.
    /// Используется для грунта, резкого поворота и взрывов при ударе.
    /// </summary>
    public class DustTrail : MonoBehaviour
    {
        public Sprite dustSprite;
        public int poolSize = 16;
        public float spawnInterval = 0.04f;
        public float lifetime = 0.55f;
        public Vector2 offset = new Vector2(0f, -0.85f);
        public float startSize = 0.3f;
        public float endSize = 1.1f;
        public Color color = new Color(1f, 1f, 1f, 0.55f);
        public int sortingOrder = 4;

        [Tooltip("Контейнер в мире, куда падают частицы (чтобы они не двигались вместе с машиной)")]
        public Transform fxContainer;

        private SpriteRenderer[] pool;
        private float[] timers;
        private Vector2[] velocities;
        private float spawnTimer;

        private void Awake()
        {
            BuildPool();
        }

        private void BuildPool()
        {
            if (dustSprite == null)
            {
                return;
            }

            Transform parent = fxContainer != null ? fxContainer : transform;
            pool = new SpriteRenderer[poolSize];
            timers = new float[poolSize];
            velocities = new Vector2[poolSize];

            for (int i = 0; i < poolSize; i++)
            {
                GameObject go = new GameObject("Dust_" + i);
                go.transform.SetParent(parent, false);
                SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = dustSprite;
                renderer.sortingOrder = sortingOrder;
                renderer.enabled = false;
                pool[i] = renderer;
            }
        }

        public void Emit(float amount)
        {
            if (pool == null || pool.Length == 0)
            {
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f)
            {
                return;
            }

            spawnTimer = spawnInterval / Mathf.Max(0.05f, amount);

            for (int i = 0; i < pool.Length; i++)
            {
                if (timers[i] > 0f)
                {
                    continue;
                }

                Transform t = pool[i].transform;
                t.position = transform.position + (Vector3)offset;
                t.localScale = Vector3.one * startSize;
                pool[i].color = color;
                pool[i].enabled = true;
                timers[i] = lifetime;
                velocities[i] = Vector2.zero;
                break;
            }
        }

        /// <summary>Разлёт частиц в точке удара.</summary>
        public void ImpactBurst(Vector2 position, Color burstColor, int count)
        {
            if (pool == null || pool.Length == 0)
            {
                return;
            }

            int spawned = 0;
            for (int i = 0; i < pool.Length && spawned < count; i++)
            {
                if (timers[i] > 0f)
                {
                    continue;
                }

                Transform t = pool[i].transform;
                t.position = position;
                t.localScale = Vector3.one * (startSize * 1.4f);
                pool[i].color = burstColor;
                pool[i].enabled = true;
                timers[i] = lifetime * 0.9f;
                velocities[i] = Random.insideUnitCircle * 5f + Vector2.up * 2.5f;
                spawned++;
            }
        }

        private void Update()
        {
            if (pool == null)
            {
                return;
            }

            for (int i = 0; i < pool.Length; i++)
            {
                if (timers[i] <= 0f)
                {
                    continue;
                }

                timers[i] -= Time.deltaTime;

                if (timers[i] <= 0f)
                {
                    pool[i].enabled = false;
                    continue;
                }

                float t = 1f - timers[i] / lifetime;
                pool[i].transform.localScale = Vector3.one * Mathf.Lerp(startSize, endSize, t);
                pool[i].transform.position += (Vector3)(velocities[i] * Time.deltaTime);
                velocities[i] = Vector2.Lerp(velocities[i], Vector2.zero, 3f * Time.deltaTime);

                Color c = pool[i].color;
                c.a = color.a * (1f - t);
                pool[i].color = c;
            }
        }

        public void Clear()
        {
            if (pool == null)
            {
                return;
            }

            for (int i = 0; i < pool.Length; i++)
            {
                timers[i] = 0f;
                pool[i].enabled = false;
            }
        }
    }
}
