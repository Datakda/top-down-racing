using UnityEngine;

namespace TopDownRacing
{
    /// <summary>
    /// Раскидывает бонусы по трассе: монетки, нитро, ремонт и щиты.
    /// </summary>
    public class PickupSpawner : MonoBehaviour
    {
        [Header("Спрайты")]
        public Sprite coinSprite;
        public Sprite nitroSprite;
        public Sprite repairSprite;
        public Sprite shieldSprite;

        [Header("Полосы")]
        public int laneCount = 4;
        public float laneWidth = 2.5f;

        [Header("Дистанции")]
        public float spawnAhead = 46f;
        public float despawnBehind = 26f;
        public float intervalStart = 8.5f;
        public float intervalMax = 12f;
        public float intervalMin = 4f;
        public float intervalDecreasePerMeter = 0.012f;

        [Header("Шансы")]
        [Range(0f, 1f)] public float coinChance = 0.58f;
        [Range(0f, 1f)] public float nitroChance = 0.18f;
        [Range(0f, 1f)] public float repairChance = 0.1f;
        [Range(0f, 1f)] public float shieldChance = 0.14f;
        [Range(0f, 1f)] public float coinLineChance = 0.4f;

        public Transform container;
        public CarController player;

        private float nextSpawnDistance;

        private void Start()
        {
            nextSpawnDistance = 22f;
        }

        private void Update()
        {
            if (GameManager.Instance == null || player == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            float playerY = player.transform.position.y;

            while (playerY + spawnAhead >= nextSpawnDistance)
            {
                SpawnPickup(nextSpawnDistance);
                nextSpawnDistance += NextInterval();
            }

            CleanupBehind(playerY);
        }

        private float NextInterval()
        {
            float interval = Random.Range(intervalMin, intervalMax) - GameManager.Instance.Distance * intervalDecreasePerMeter;
            return Mathf.Max(intervalMin * 0.6f, interval);
        }

        private void SpawnPickup(float worldY)
        {
            PickupType type = ChooseType();

            int lanes = Mathf.Max(1, laneCount);
            float firstLaneX = -((lanes - 1) * 0.5f) * laneWidth;
            int lane = Random.Range(0, lanes);
            float x = firstLaneX + lane * laneWidth;

            if (type == PickupType.Coin && Random.value < coinLineChance)
            {
                // Цепочка монет — награда за красивый заход.
                int count = Random.Range(3, 5);
                for (int i = 0; i < count; i++)
                {
                    CreatePickup(coinSprite, PickupType.Coin, x, worldY + i * 1.15f);
                }

                return;
            }

            CreatePickup(SpriteFor(type), type, x, worldY);
        }

        private PickupType ChooseType()
        {
            float roll = Random.value;

            if (roll < coinChance)
            {
                return PickupType.Coin;
            }

            if (roll < coinChance + nitroChance)
            {
                return PickupType.Nitro;
            }

            if (roll < coinChance + nitroChance + repairChance)
            {
                return PickupType.Repair;
            }

            return PickupType.Shield;
        }

        private Sprite SpriteFor(PickupType type)
        {
            switch (type)
            {
                case PickupType.Coin:
                    return coinSprite;
                case PickupType.Nitro:
                    return nitroSprite;
                case PickupType.Repair:
                    return repairSprite;
                default:
                    return shieldSprite;
            }
        }

        private void CreatePickup(Sprite sprite, PickupType type, float x, float y)
        {
            if (sprite == null)
            {
                return;
            }

            GameObject go = new GameObject("Pickup_" + type);
            go.transform.SetParent(container != null ? container : transform, false);
            go.transform.position = new Vector3(x, y, 0f);

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = sprite.bounds.size * 1.1f;
            collider.isTrigger = true;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 8;

            Pickup pickup = go.AddComponent<Pickup>();
            pickup.type = type;
            pickup.visualRoot = visual.transform;
            pickup.spriteRenderer = renderer;
            pickup.Setup(player);
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
