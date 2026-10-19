using UnityEngine;

namespace TopDownRacing
{
    /// <summary>
    /// Препятствие на трассе. Мягкие (конусы) не ранят, твёрдые наносят урон.
    /// Также считает "near miss" — награда за близкий пролёт мимо.
    /// </summary>
    public class Obstacle : MonoBehaviour
    {
        [Tooltip("Мягкое препятствие: конус, гибкий барьер — без урона")]
        public bool isSoft;

        [Tooltip("Радиус, в котором засчитывается близкий пролёт")]
        public float nearMissDistance = 0.95f;

        [Tooltip("Штраф очков за столкновение")]
        public int hitPenalty = 25;

        [Header("Реакция на удар")]
        public float reactDuration = 0.9f;
        public float flySpeed = 4f;
        public float spinSpeed = 400f;

        private bool passed;
        private bool hit;
        private float hitTime;
        private Vector2 hitVelocity;
        private float hitSpin;
        private CarController player;
        private SpriteRenderer spriteRenderer;

        public void Setup(CarController playerCar)
        {
            player = playerCar;
            passed = false;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (hit)
            {
                UpdateReact();
                return;
            }

            if (passed || player == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (player.transform.position.y >= transform.position.y)
            {
                passed = true;
                float dx = Mathf.Abs(player.transform.position.x - transform.position.x);
                if (dx <= nearMissDistance)
                {
                    GameManager.Instance.AddBonus(GameManager.Instance.nearMissBonus);
                }
            }
        }

        /// <summary>Препятствие отлетает и исчезает — удар сразу видно.</summary>
        private void UpdateReact()
        {
            hitTime += Time.deltaTime;

            hitVelocity.y -= 14f * Time.deltaTime;
            transform.position += (Vector3)hitVelocity * Time.deltaTime;
            transform.Rotate(0f, 0f, hitSpin * Time.deltaTime);

            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = Mathf.Clamp01(1f - hitTime / reactDuration);
                spriteRenderer.color = color;
            }

            if (hitTime >= reactDuration)
            {
                Destroy(gameObject);
            }
        }

        public void HandleHit()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || hit)
            {
                return;
            }

            hit = true;
            hitTime = 0f;
            hitVelocity = new Vector2(Random.Range(-2.2f, 2.2f), Random.Range(2.5f, 5f));
            hitSpin = Random.Range(-spinSpeed, spinSpeed);

            if (isSoft)
            {
                player.Bump();
            }
            else
            {
                player.TakeDamage(1);
            }
        }
    }
}
