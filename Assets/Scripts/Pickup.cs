using UnityEngine;

namespace TopDownRacing
{
    public enum PickupType
    {
        Coin,
        Nitro,
        Repair,
        Shield
    }

    /// <summary>
    /// Бонус, который нужно подобрать: монетка, нитро, ремонт или щит.
    /// </summary>
    public class Pickup : MonoBehaviour
    {
        public PickupType type = PickupType.Coin;
        public int scoreValue = 100;
        public float nitroValue = 0.45f;
        public float shieldDuration = 6f;

        [Header("Визуал")]
        public Transform visualRoot;
        public SpriteRenderer spriteRenderer;
        public float bobHeight = 0.22f;
        public float bobSpeed = 3.2f;
        public float spinSpeed = 110f;

        private Vector3 basePosition;
        private bool collected;
        private CarController player;

        public void Setup(CarController playerCar)
        {
            player = playerCar;
            basePosition = transform.position;
        }

        private void Update()
        {
            if (collected)
            {
                return;
            }

            basePosition.y += 0f;
            transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + basePosition.y) * bobHeight);

            if (visualRoot != null)
            {
                visualRoot.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || player == null)
            {
                return;
            }

            if (other.GetComponentInParent<CarController>() != player)
            {
                return;
            }

            collected = true;
            Apply(player);
            Destroy(gameObject);
        }

        private void Apply(CarController car)
        {
            GameManager game = GameManager.Instance;
            if (game == null)
            {
                return;
            }

            switch (type)
            {
                case PickupType.Coin:
                    game.AddPoints(scoreValue);
                    break;

                case PickupType.Nitro:
                    game.AddNitro(nitroValue);
                    break;

                case PickupType.Repair:
                    game.AddHealth(1);
                    break;

                case PickupType.Shield:
                    car.ActivateShield(shieldDuration);
                    break;
            }
        }
    }
}
