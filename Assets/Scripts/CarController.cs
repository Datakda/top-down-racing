using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownRacing
{
    /// <summary>
    /// Машина игрока: едет вперёд сама, руль — за курсором мыши.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class CarController : MonoBehaviour
    {
        [Header("Управление мышью")]
        [Tooltip("Насколько сильно машина тянется к курсору")]
        public float steerGain = 5.5f;
        public float maxLateralSpeed = 9f;
        public float lateralSmooth = 10f;

        [Header("Визуал")]
        public Transform visualRoot;
        public SpriteRenderer spriteRenderer;
        public DustTrail dustTrail;
        [Tooltip("Максимальный наклон кузова при повороте, градусы")]
        public float maxTilt = 22f;
        public float tiltSmooth = 9f;

        [Header("Нитро")]
        public float boostMultiplier = 1.45f;
        public float nitroDrain = 0.35f;
        public float nitroRegen = 0.1f;
        public float minNitroToBoost = 0.08f;

        [Header("Внедорожье")]
        public float roadHalfWidth = 4f;
        public float offRoadMultiplier = 0.5f;
        public float brakeMultiplier = 0.55f;
        [Tooltip("Насколько можно выехать за пределы дороги")]
        public float offRoadLimit = 1.6f;

        [Header("Урон")]
        public float invulnerabilityTime = 1.6f;
        public float stumbleTime = 0.9f;
        public float stumbleSpeedMultiplier = 0.4f;

        private Rigidbody2D body;
        private float lateralVelocity;
        private float invulnTimer;
        private float flashTimer;
        private float shieldTimer;
        private float stumbleTimer;
        private Color baseColor = Color.white;
        private Camera worldCamera;

        public bool IsInvulnerable => invulnTimer > 0f;
        public bool IsShielded => shieldTimer > 0f;
        public bool IsOffRoad => Mathf.Abs(transform.position.x) > roadHalfWidth;
        public float Speed01 { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            worldCamera = Camera.main;

            if (spriteRenderer != null)
            {
                baseColor = spriteRenderer.color;
            }
        }

        private void Update()
        {
            HandleNitro();
            HandleFlash();
            HandleDust();

            if (shieldTimer > 0f)
            {
                shieldTimer -= Time.deltaTime;
            }

            if (stumbleTimer > 0f)
            {
                stumbleTimer -= Time.deltaTime;
            }

            if (invulnTimer > 0f)
            {
                invulnTimer -= Time.deltaTime;
            }
        }

        private void HandleDust()
        {
            if (dustTrail == null)
            {
                return;
            }

            float amount = 0f;
            if (IsOffRoad)
            {
                amount = 1.2f;
            }
            else if (Mathf.Abs(lateralVelocity) > maxLateralSpeed * 0.7f)
            {
                amount = 0.6f;
            }

            if (amount > 0f)
            {
                dustTrail.Emit(amount);
            }
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            bool playing = GameManager.Instance.IsPlaying;

            float forwardSpeed = playing ? GameManager.Instance.CurrentSpeed : 0f;

            if (IsOffRoad)
            {
                forwardSpeed *= offRoadMultiplier;
            }

            if (IsBraking())
            {
                forwardSpeed *= brakeMultiplier;
            }

            if (playing && IsBoosting())
            {
                forwardSpeed *= boostMultiplier;
            }

            if (stumbleTimer > 0f)
            {
                forwardSpeed *= stumbleSpeedMultiplier;
            }

            float lateralTarget = 0f;
            if (playing)
            {
                lateralTarget = ComputeSteering();
            }

            lateralVelocity = Mathf.Lerp(lateralVelocity, lateralTarget, lateralSmooth * Time.fixedDeltaTime);

            body.linearVelocity = new Vector2(lateralVelocity, forwardSpeed);
            ClampToTrack();
            Speed01 = forwardSpeed;

            UpdateVisuals(forwardSpeed);
        }

        /// <summary>Не даёт машине уехать совсем далеко в траву.</summary>
        private void ClampToTrack()
        {
            float limit = roadHalfWidth + offRoadLimit;
            Vector3 position = body.position;

            if (position.x < -limit)
            {
                position.x = -limit;
                lateralVelocity = Mathf.Max(0f, lateralVelocity);
            }
            else if (position.x > limit)
            {
                position.x = limit;
                lateralVelocity = Mathf.Min(0f, lateralVelocity);
            }

            body.position = position;
        }

        private float ComputeSteering()
        {
            if (!ShouldAcceptPointerInput())
            {
                return 0f;
            }

            float keyboard = 0f;
            Keyboard keyboardDevice = Keyboard.current;
            if (keyboardDevice != null)
            {
                if (keyboardDevice.aKey.isPressed || keyboardDevice.leftArrowKey.isPressed)
                {
                    keyboard -= 1f;
                }

                if (keyboardDevice.dKey.isPressed || keyboardDevice.rightArrowKey.isPressed)
                {
                    keyboard += 1f;
                }
            }

            if (Mathf.Abs(keyboard) > 0.01f)
            {
                return Mathf.Clamp(keyboard * maxLateralSpeed, -maxLateralSpeed, maxLateralSpeed);
            }

            Vector3 pointerWorld = GetPointerWorldPosition();
            float dx = pointerWorld.x - transform.position.x;
            return Mathf.Clamp(dx * steerGain, -maxLateralSpeed, maxLateralSpeed);
        }

        private Vector3 GetPointerWorldPosition()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            Mouse mouse = Mouse.current;
            if (worldCamera == null || mouse == null)
            {
                return transform.position;
            }

            Vector2 screenPosition = mouse.position.ReadValue();
            Vector3 world = worldCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(worldCamera.transform.position.z)));
            return world;
        }

        private void UpdateVisuals(float forwardSpeed)
        {
            if (visualRoot == null)
            {
                return;
            }

            float angle = Mathf.Atan2(lateralVelocity, Mathf.Max(0.5f, forwardSpeed)) * Mathf.Rad2Deg;
            float targetTilt = Mathf.Clamp(-angle, -maxTilt, maxTilt);
            Quaternion target = Quaternion.Euler(0f, 0f, targetTilt);
            visualRoot.localRotation = Quaternion.Slerp(
                visualRoot.localRotation, target, tiltSmooth * Time.deltaTime);
        }

        private void HandleNitro()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            bool playing = GameManager.Instance.IsPlaying;
            bool boosting = playing && IsBoosting() && GameManager.Instance.Nitro > minNitroToBoost;

            if (boosting)
            {
                GameManager.Instance.ConsumeNitro(nitroDrain * Time.deltaTime);
                GameManager.Instance.NitroActive = true;
            }
            else
            {
                GameManager.Instance.NitroActive = false;
                if (playing)
                {
                    GameManager.Instance.RegenerateNitro(nitroRegen * Time.deltaTime);
                }
            }
        }

        /// <summary>
        /// В редакторе не рулим мышью, если окно Unity не в фокусе — иначе курсор
        /// над редактором дёргает машину в сторону.
        /// </summary>
        private static bool ShouldAcceptPointerInput()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isFocused)
            {
                return false;
            }
#endif
            return true;
        }

        private bool IsBoosting()
        {
            if (!ShouldAcceptPointerInput())
            {
                return false;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                return true;
            }

            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.leftShiftKey.isPressed;
        }

        private bool IsBraking()
        {
            if (!ShouldAcceptPointerInput())
            {
                return false;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                return true;
            }

            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.sKey.isPressed;
        }

        private void HandleFlash()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (shieldTimer > 0f)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 10f);
                spriteRenderer.color = Color.Lerp(baseColor, new Color(0.35f, 0.85f, 1f), 0.35f + 0.35f * pulse);
                return;
            }

            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                float blink = Mathf.PingPong(Time.time * 12f, 1f);
                spriteRenderer.color = Color.Lerp(baseColor, Color.red, blink);
                if (flashTimer <= 0f)
                {
                    spriteRenderer.color = baseColor;
                }
            }
            else if (spriteRenderer.color != baseColor)
            {
                spriteRenderer.color = baseColor;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            Obstacle obstacle = other.GetComponentInParent<Obstacle>();
            if (obstacle != null)
            {
                obstacle.HandleHit();
                return;
            }

            TrafficCar traffic = other.GetComponentInParent<TrafficCar>();
            if (traffic != null)
            {
                traffic.HandleHit();
            }
        }

        public void TakeDamage(int amount)
        {
            if (IsInvulnerable || IsShielded || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            invulnTimer = invulnerabilityTime;
            flashTimer = invulnerabilityTime;
            stumbleTimer = stumbleTime;
            GameManager.Instance.ApplyDamage(amount);

            if (dustTrail != null)
            {
                dustTrail.ImpactBurst(transform.position, new Color(1f, 0.55f, 0.2f), 12);
            }

            CameraFollow follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (follow != null)
            {
                follow.Shake(1.1f);
            }
        }

        public void Bump()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            // Мягкое столкновение: теряем немного скорости, но не получаем урона.
            body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * 0.45f);
            stumbleTimer = Mathf.Max(stumbleTimer, 0.35f);

            if (dustTrail != null)
            {
                dustTrail.ImpactBurst(transform.position, new Color(1f, 0.9f, 0.6f), 6);
            }
        }

        public void ActivateShield(float duration)
        {
            shieldTimer = Mathf.Max(shieldTimer, duration);
            flashTimer = 0f;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = baseColor;
            }
        }

        public void ResetState()
        {
            lateralVelocity = 0f;
            invulnTimer = 0f;
            flashTimer = 0f;
            shieldTimer = 0f;
            stumbleTimer = 0f;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = baseColor;
            }
        }
    }
}
