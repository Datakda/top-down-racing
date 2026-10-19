using System;
using UnityEngine;

namespace TopDownRacing
{
    public enum GameState
    {
        Ready,
        Playing,
        Paused,
        GameOver
    }

    /// <summary>
    /// Глобальное состояние гонки: скорость, очки, здоровье, жизненный цикл игры.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Скорость")]
        [Tooltip("Стартовая скорость машины, м/с")]
        public float baseSpeed = 9f;
        [Tooltip("Максимальная скорость, м/с")]
        public float maxSpeed = 19f;
        [Tooltip("Насколько скорость растёт за секунду игры")]
        public float speedRampPerSecond = 0.3f;

        [Header("Правила")]
        public int maxHealth = 3;
        public float scorePerMeter = 1f;
        public int nearMissBonus = 50;

        [Header("Сохранение")]
        public string bestScoreKey = "TopDownRacing.BestScore";

        public GameState State { get; private set; } = GameState.Ready;
        public float CurrentSpeed { get; private set; }
        public float Distance { get; private set; }
        public int Score { get; private set; }
        public int BonusScore { get; private set; }
        public int Health { get; private set; }
        public int Best { get; private set; }
        public int Combo { get; private set; }
        public float ComboTimer { get; private set; }
        public float Nitro { get; private set; } = 1f;
        public bool NitroActive { get; set; }

        public bool IsPlaying => State == GameState.Playing;

        public event Action<GameState> StateChanged;
        public event Action<int> ScoreChanged;
        public event Action<int> HealthChanged;
        public event Action<int> ComboChanged;

        private void Awake()
        {
            Instance = this;
            Health = maxHealth;
            Best = PlayerPrefs.GetInt(bestScoreKey, 0);
        }

        private float playTime;
        private void Update()
        {
            if (State == GameState.Playing)
            {
                playTime += Time.deltaTime;
                CurrentSpeed = Mathf.Min(maxSpeed, baseSpeed + playTime * speedRampPerSecond);
                Distance += CurrentSpeed * Time.deltaTime;

                if (ComboTimer > 0f)
                {
                    ComboTimer -= Time.deltaTime;
                    if (ComboTimer <= 0f && Combo != 0)
                    {
                        Combo = 0;
                        ComboChanged?.Invoke(Combo);
                    }
                }
            }

            if (State != GameState.Playing)
            {
                NitroActive = false;
            }
        }

        public void StartGame()
        {
            Distance = 0f;
            BonusScore = 0;
            Score = 0;
            Combo = 0;
            playTime = 0f;
            Health = maxHealth;
            Nitro = 1f;
            CurrentSpeed = baseSpeed;
            SetState(GameState.Playing);
            ScoreChanged?.Invoke(Score);
            HealthChanged?.Invoke(Health);
            ComboChanged?.Invoke(Combo);
        }

        public void GameOver()
        {
            if (State != GameState.Playing)
            {
                return;
            }

            if (Score > Best)
            {
                Best = Score;
                PlayerPrefs.SetInt(bestScoreKey, Best);
                PlayerPrefs.Save();
            }

            SetState(GameState.GameOver);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing)
            {
                SetState(GameState.Paused);
            }
            else if (State == GameState.Paused)
            {
                SetState(GameState.Playing);
            }
        }

        public void ApplyDamage(int amount)
        {
            if (!IsPlaying)
            {
                return;
            }

            Health = Mathf.Max(0, Health - amount);
            HealthChanged?.Invoke(Health);

            if (Combo != 0)
            {
                Combo = 0;
                ComboChanged?.Invoke(Combo);
            }

            if (Health <= 0)
            {
                GameOver();
            }
        }

        public void AddBonus(int amount)
        {
            BonusScore += amount;
            Combo++;
            ComboTimer = 3f;
            UpdateScore();
            ComboChanged?.Invoke(Combo);
        }

        /// <summary>Очки за подбор монет (без комбо).</summary>
        public void AddPoints(int amount)
        {
            BonusScore += amount;
            UpdateScore();
        }

        public void AddNitro(float amount)
        {
            Nitro = Mathf.Clamp01(Nitro + amount);
        }

        public void AddHealth(int amount)
        {
            if (Health >= maxHealth)
            {
                return;
            }

            Health = Mathf.Min(maxHealth, Health + amount);
            HealthChanged?.Invoke(Health);
        }

        public void ConsumeNitro(float amount)
        {
            Nitro = Mathf.Clamp01(Nitro - amount);
        }

        public void RegenerateNitro(float amount)
        {
            Nitro = Mathf.Clamp01(Nitro + amount);
        }

        private void UpdateScore()
        {
            int newScore = Mathf.FloorToInt(Distance * scorePerMeter) + BonusScore;
            if (newScore != Score)
            {
                Score = newScore;
                ScoreChanged?.Invoke(Score);
            }
        }
        private void SetState(GameState newState)
        {
            if (State == newState)
            {
                return;
            }

            State = newState;
            StateChanged?.Invoke(newState);
        }

        private void LateUpdate()
        {
            if (State == GameState.Playing)
            {
                UpdateScore();
            }
        }
    }
}
