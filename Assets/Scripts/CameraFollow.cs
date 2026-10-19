using UnityEngine;

namespace TopDownRacing
{
    /// <summary>
    /// Камера следует за машиной, слегка заглядывая вперёд, и умеет трястись при ударах.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;

        [Tooltip("Насколько камера заглядывает вперёд относительно машины")]
        public float lookAhead = 2.6f;

        [Tooltip("Дополнительный загляд при росте скорости (должен быть маленьким, иначе машина уходит вниз экрана)")]
        public float lookAheadSpeedFactor = 0.05f;

        public float horizontalFollow = 0.8f;
        public float smoothTime = 0.18f;
        public float shakeDecay = 2.6f;
        public float maxShakeOffset = 0.9f;

        private Vector3 velocity;
        private float shakeAmount;
        private float baseZ = -10f;

        private void Awake()
        {
            baseZ = transform.position.z;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            float speed = 0f;
            CarController car = target.GetComponent<CarController>();
            if (car != null)
            {
                speed = car.Speed01;
            }

            Vector3 desired = new Vector3(
                target.position.x * horizontalFollow,
                target.position.y + lookAhead + speed * lookAheadSpeedFactor,
                baseZ);

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);

            if (shakeAmount > 0.001f)
            {
                shakeAmount = Mathf.Max(0f, shakeAmount - shakeAmount * shakeDecay * Time.deltaTime);
                Vector2 offset = Random.insideUnitCircle * shakeAmount * maxShakeOffset;
                transform.position += new Vector3(offset.x, offset.y, 0f);
            }
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            velocity = Vector3.zero;
            transform.position = new Vector3(
                target.position.x * horizontalFollow,
                target.position.y + lookAhead,
                baseZ);
        }

        public void Shake(float amount)
        {
            shakeAmount = Mathf.Max(shakeAmount, amount);
        }
    }
}
