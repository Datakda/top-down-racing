using UnityEngine;

namespace TopDownRacing
{
    /// <summary>
    /// Машина соперника: едет по своей полосе с фиксированной скоростью.
    /// </summary>
    public class TrafficCar : MonoBehaviour
    {
        public float speed = 6f;
        public float laneChangeSpeed = 2.5f;

        private CarController player;
        private Rigidbody2D body;
        private bool hit;
        private float reactTime;
        private Vector2 reactVelocity;
        private float reactSpin;

        public void Setup(CarController playerCar, float startSpeed)
        {
            player = playerCar;
            speed = startSpeed;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.gravityScale = 0f;
                body.freezeRotation = true;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
        }

        private void Update()
        {
            if (hit)
            {
                UpdateReact();
            }
        }

        private void FixedUpdate()
        {
            if (hit || body == null)
            {
                return;
            }

            float x = transform.position.x;
            float targetX = x;

            // Машина соперника слегка рыскает по полосе, чтобы не выглядеть статичной.
            targetX += Mathf.Sin(Time.time * 1.7f + transform.position.y * 0.13f) * 0.25f;

            float newX = Mathf.MoveTowards(x, targetX, laneChangeSpeed * Time.fixedDeltaTime);
            body.linearVelocity = new Vector2((newX - x) / Time.fixedDeltaTime, speed);
        }

        public void HandleHit()
        {
            if (player == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying || hit)
            {
                return;
            }

            hit = true;
            reactTime = 0f;
            reactVelocity = new Vector2(Random.Range(-3f, 3f), Random.Range(3f, 6f));
            reactSpin = Random.Range(-360f, 360f);

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.bodyType = RigidbodyType2D.Kinematic;
            }

            player.TakeDamage(1);
        }

        private void UpdateReact()
        {
            reactTime += Time.deltaTime;
            transform.position += (Vector3)reactVelocity * Time.deltaTime;
            reactVelocity.y -= 12f * Time.deltaTime;
            transform.Rotate(0f, 0f, reactSpin * Time.deltaTime);

            if (reactTime >= 1.2f)
            {
                Destroy(gameObject);
            }
        }
    }
}
