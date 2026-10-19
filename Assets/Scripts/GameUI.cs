using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TopDownRacing
{
    /// <summary>
    /// Интерфейс: счёт, рекорд, скорость, здоровье, нитро, экраны старта/паузы/поражения.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("Ссылки")]
        public GameManager gameManager;
        public CarController player;
        public ObstacleSpawner spawner;
        public PickupSpawner pickupSpawner;
        public CameraFollow cameraFollow;

        [Header("Элементы")]
        public Text scoreText;
        public Text bestText;
        public Text speedText;
        public Text comboText;
        public Text gameOverScoreText;
        public Text gameOverBestText;
        public Image[] hearts;
        public Image nitroFill;
        public Image flashOverlay;
        public GameObject startPanel;
        public GameObject pausePanel;
        public GameObject gameOverPanel;
        public GameObject hudPanel;
        public Button startButton;
        public Button restartButton;
        public Button pauseButton;
        public Button quitButton;

        [Header("Настройки")]
        public float flashDuration = 0.45f;

        private float flashTimer;
        private float gameOverDelay;

        private bool mouseWasPressed;
        private bool enterWasPressed;
        private bool spaceWasPressed;
        private bool escapeWasPressed;
        private bool rWasPressed;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (startButton != null)
            {
                startButton.onClick.AddListener(StartNewGame);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(StartNewGame);
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(TogglePause);
            }

            if (flashOverlay != null)
            {
                Color color = flashOverlay.color;
                color.a = 0f;
                flashOverlay.color = color;
            }
        }

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged += OnStateChanged;
                gameManager.HealthChanged += OnHealthChanged;
            }

            OnStateChanged(gameManager != null ? gameManager.State : GameState.Ready);
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged -= OnStateChanged;
                gameManager.HealthChanged -= OnHealthChanged;
            }
        }

        private void OnHealthChanged(int health)
        {
            if (health < gameManager.maxHealth)
            {
                FlashDamage();
            }
        }

        private void Update()
        {
            if (gameManager == null)
            {
                return;
            }

            UpdateHud();
            UpdateFlash();
            HandleFallbackInput();

            if (gameManager.State == GameState.GameOver)
            {
                gameOverDelay -= Time.unscaledDeltaTime;
            }
        }

        /// <summary>
        /// Запасное управление: клик в любом месте экрана, Enter/Space, Esc, R.
        /// Нужно на случай, если мышь не попала точно в кнопку.
        /// </summary>
        private void HandleFallbackInput()
        {
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;

            bool mouseNow = mouse != null && mouse.leftButton.isPressed;
            bool mouseClick = mouseNow && !mouseWasPressed;
            mouseWasPressed = mouseNow;

            bool enterNow = keyboard != null && keyboard.enterKey.isPressed;
            bool spaceNow = keyboard != null && keyboard.spaceKey.isPressed;
            bool confirm = (enterNow && !enterWasPressed) || (spaceNow && !spaceWasPressed);
            enterWasPressed = enterNow;
            spaceWasPressed = spaceNow;

            bool escapeNow = keyboard != null && keyboard.escapeKey.isPressed;
            bool escape = escapeNow && !escapeWasPressed;
            escapeWasPressed = escapeNow;

            bool rNow = keyboard != null && keyboard.rKey.isPressed;
            bool restart = rNow && !rWasPressed;
            rWasPressed = rNow;

            switch (gameManager.State)
            {
                case GameState.Ready:
                    if (mouseClick || confirm)
                    {
                        StartNewGame();
                    }

                    break;

                case GameState.Playing:
                    if (escape)
                    {
                        TogglePause();
                    }

                    break;

                case GameState.Paused:
                    if (escape)
                    {
                        TogglePause();
                    }

                    break;

                case GameState.GameOver:
                    if (gameOverDelay <= 0f && (mouseClick || confirm || restart))
                    {
                        StartNewGame();
                    }

                    break;
            }
        }

        private void UpdateHud()
        {
            if (scoreText != null)
            {
                scoreText.text = "ОЧКИ: " + gameManager.Score;
            }

            if (bestText != null)
            {
                bestText.text = "РЕКОРД: " + Mathf.Max(gameManager.Best, gameManager.Score);
            }

            if (speedText != null)
            {
                int kmh = Mathf.RoundToInt(gameManager.CurrentSpeed * 3.6f);
                speedText.text = kmh + " КМ/Ч";
            }

            if (comboText != null)
            {
                if (gameManager.Combo > 1)
                {
                    comboText.text = "КОМБО x" + gameManager.Combo;
                    comboText.enabled = true;
                }
                else
                {
                    comboText.enabled = false;
                }
            }

            if (hearts != null)
            {
                for (int i = 0; i < hearts.Length; i++)
                {
                    if (hearts[i] != null)
                    {
                        hearts[i].enabled = i < gameManager.Health;
                    }
                }
            }

            if (nitroFill != null)
            {
                nitroFill.fillAmount = gameManager.Nitro;
                Color color = gameManager.NitroActive ? Color.yellow : new Color(0.35f, 0.8f, 1f);
                nitroFill.color = color;
            }

            if (gameOverScoreText != null)
            {
                gameOverScoreText.text = "Очки: " + gameManager.Score;
            }

            if (gameOverBestText != null)
            {
                gameOverBestText.text = "Рекорд: " + Mathf.Max(gameManager.Best, gameManager.Score);
            }
        }

        private void UpdateFlash()
        {
            if (flashOverlay == null)
            {
                return;
            }

            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                Color color = flashOverlay.color;
                color.a = Mathf.Clamp01(flashTimer / flashDuration) * 0.45f;
                flashOverlay.color = color;
            }
            else
            {
                Color color = flashOverlay.color;
                if (color.a > 0f)
                {
                    color.a = 0f;
                    flashOverlay.color = color;
                }
            }
        }

        public void FlashDamage()
        {
            flashTimer = flashDuration;
        }

        private void OnStateChanged(GameState state)
        {
            if (startPanel != null)
            {
                startPanel.SetActive(state == GameState.Ready);
            }

            if (pausePanel != null)
            {
                pausePanel.SetActive(state == GameState.Paused);
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(state == GameState.GameOver);
            }

            if (state == GameState.GameOver)
            {
                gameOverDelay = 0.6f;
            }

            if (hudPanel != null)
            {
                hudPanel.SetActive(state != GameState.Ready);
            }
        }

        public void StartNewGame()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (player != null)
            {
                player.ResetState();
                player.transform.position = new Vector3(0f, 0f, 0f);
            }

            if (spawner != null)
            {
                spawner.ClearAll();
                spawner.ResetSpawner();
            }

            if (pickupSpawner != null)
            {
                pickupSpawner.ClearAll();
            }

            if (cameraFollow != null)
            {
                cameraFollow.SnapToTarget();
            }

            gameManager.StartGame();
        }

        public void TogglePause()
        {
            if (gameManager != null)
            {
                gameManager.TogglePause();
            }
        }

        /// <summary>Выход из игры (в редакторе — просто останавливает Play Mode).</summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
