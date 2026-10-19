using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TopDownRacing.EditorTools
{
    /// <summary>
    /// Собирает готовую сцену гонки: камера, трасса, машина, спавнер и весь UI.
    /// Меню: Tools / Racing / Собрать сцену гонки.
    /// </summary>
    public static class RacingSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Racing.unity";
        private const string ArtRoot = "Assets/Art/";
        private const float RoadHalfWidth = 5f;
        private const float CameraSize = 6f;

        [MenuItem("Tools/Racing/Собрать сцену гонки")]
        public static void BuildScene()
        {
            ReimportArt();
            EnsureFolder("Assets/Scenes");

            // Чтобы гонка не замирала, когда окно редактора не в фокусе.
            PlayerSettings.runInBackground = true;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ------------------------------------------------ Камера
            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = CameraSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.13f, 0.15f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            cameraGo.transform.position = new Vector3(0f, 3.2f, -10f);

            // ------------------------------------------------ Спрайты
            Sprite roadSprite = LoadSprite("Generated/road.png");
            Sprite dustSprite = LoadSprite("Generated/dust.png");
            Sprite heartSprite = LoadSprite("Generated/heart.png");
            Sprite playerSprite = LoadSprite("Cars/car_red_1.png");

            Sprite[] grass = new[]
            {
                LoadSprite("Generated/grass1.png"),
                LoadSprite("Generated/grass2.png"),
                LoadSprite("Generated/grass3.png"),
                LoadSprite("Generated/grass4.png"),
                LoadSprite("Generated/grass5.png"),
                LoadSprite("Generated/grass6.png")
            };

            Sprite[] hardObstacles =
            {
                LoadSprite("Objects/barrier_white.png"),
                LoadSprite("Objects/barrier_red.png"),
                LoadSprite("Objects/barrel_red.png"),
                LoadSprite("Objects/barrel_blue.png"),
                LoadSprite("Objects/rock1.png"),
                LoadSprite("Objects/rock2.png"),
                LoadSprite("Objects/rock3.png"),
                LoadSprite("Objects/tires_white.png")
            };

            Sprite[] softObstacles =
            {
                LoadSprite("Objects/cone_straight.png")
            };

            Sprite[] decorations =
            {
                LoadSprite("Objects/tree_small.png"),
                LoadSprite("Objects/tree_large.png")
            };

            Sprite[] traffic =
            {
                LoadSprite("Cars/car_blue_3.png"),
                LoadSprite("Cars/car_yellow_2.png"),
                LoadSprite("Cars/car_green_1.png"),
                LoadSprite("Cars/car_black_5.png"),
                LoadSprite("Cars/car_red_2.png")
            };

            // ------------------------------------------------ Мир
            GameObject world = new GameObject("World");
            GameObject groundGo = new GameObject("Ground");
            groundGo.transform.SetParent(world.transform, false);
            InfiniteGround ground = groundGo.AddComponent<InfiniteGround>();
            ground.grassTiles = grass;
            ground.roadSprite = roadSprite;
            ground.roadWidth = RoadHalfWidth * 2f;
            ground.gridColumns = 16;
            ground.gridRows = 12;
            ground.grassTileSize = 2f;
            ground.roadSegmentHeight = 10f;
            ground.roadSegmentCount = 7;

            BuildGroundVisuals(groundGo.transform, ground, grass, roadSprite);

            GameObject obstaclesRoot = new GameObject("Obstacles");
            obstaclesRoot.transform.SetParent(world.transform, false);

            GameObject pickupsRoot = new GameObject("Pickups");
            pickupsRoot.transform.SetParent(world.transform, false);

            GameObject fxRoot = new GameObject("FX");
            fxRoot.transform.SetParent(world.transform, false);

            // ------------------------------------------------ Машина
            GameObject playerRoot = new GameObject("Player");
            playerRoot.transform.SetParent(world.transform, false);

            GameObject carGo = new GameObject("PlayerCar");
            carGo.transform.SetParent(playerRoot.transform, false);

            // Спрайт машины живёт только на дочернем объекте Visual — он наклоняется при
            // повороте. Второй спрайт на самой машине давал бы "раздвоение" кузова.
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(carGo.transform, false);
            SpriteRenderer visualRenderer = visual.AddComponent<SpriteRenderer>();
            visualRenderer.sprite = playerSprite;
            visualRenderer.sortingOrder = 11;

            Rigidbody2D carBody = carGo.AddComponent<Rigidbody2D>();
            carBody.gravityScale = 0f;
            carBody.freezeRotation = true;
            carBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            carBody.interpolation = RigidbodyInterpolation2D.Interpolate;

            BoxCollider2D carCollider = carGo.AddComponent<BoxCollider2D>();
            carCollider.size = new Vector2(playerSprite.bounds.size.x * 0.75f, playerSprite.bounds.size.y * 0.7f);

            DustTrail dust = carGo.AddComponent<DustTrail>();
            dust.dustSprite = dustSprite;
            dust.fxContainer = fxRoot.transform;

            CarController car = carGo.AddComponent<CarController>();
            car.visualRoot = visual.transform;
            car.spriteRenderer = visualRenderer;
            car.dustTrail = dust;
            car.roadHalfWidth = RoadHalfWidth;

            // ------------------------------------------------ Системы
            GameObject systems = new GameObject("Systems");
            GameManager gameManager = systems.AddComponent<GameManager>();
            gameManager.maxHealth = 3;

            ObstacleSpawner spawner = systems.AddComponent<ObstacleSpawner>();
            spawner.hardObstacleSprites = hardObstacles;
            spawner.softObstacleSprites = softObstacles;
            spawner.trafficCarSprites = traffic;
            spawner.container = obstaclesRoot.transform;
            spawner.player = car;
            spawner.laneCount = 4;
            spawner.laneWidth = 2.5f;
            spawner.roadHalfWidth = RoadHalfWidth;
            spawner.decorationSprites = decorations;

            CameraFollow follow = cameraGo.AddComponent<CameraFollow>();
            follow.target = carGo.transform;
            follow.lookAhead = 2.6f;
            follow.lookAheadSpeedFactor = 0.05f;
            follow.horizontalFollow = 0.8f;

            // ------------------------------------------------ Бонусы
            PickupSpawner pickupSpawner = systems.AddComponent<PickupSpawner>();
            pickupSpawner.coinSprite = LoadSprite("Generated/coin.png");
            pickupSpawner.nitroSprite = LoadSprite("Generated/nitro.png");
            pickupSpawner.repairSprite = heartSprite;
            pickupSpawner.shieldSprite = LoadSprite("Generated/shield.png");
            pickupSpawner.container = pickupsRoot.transform;
            pickupSpawner.player = car;
            pickupSpawner.laneCount = 4;
            pickupSpawner.laneWidth = 2.5f;

            // ------------------------------------------------ UI
            Font font = AssetDatabase.LoadAssetAtPath<Font>(ArtRoot + "UI/Inter-SemiBold.ttf");

            // EventSystem обязателен, иначе uGUI-кнопки не нажимаются.
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            InputSystemUIInputModule uiInputModule = eventSystem.AddComponent<InputSystemUIInputModule>();
            uiInputModule.AssignDefaultActions();

            GameObject canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameUI ui = canvasGo.AddComponent<GameUI>();
            ui.gameManager = gameManager;
            ui.player = car;
            ui.spawner = spawner;
            ui.pickupSpawner = pickupSpawner;
            ui.cameraFollow = follow;

            BuildHud(canvasGo.transform, font, heartSprite, ui);
            BuildStartPanel(canvasGo.transform, font, ui);
            BuildPausePanel(canvasGo.transform, font, ui);
            BuildGameOverPanel(canvasGo.transform, font, ui);
            BuildFlashOverlay(canvasGo.transform, ui);

            // ------------------------------------------------ Сохранение
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            foreach (EditorBuildSettingsScene entry in scenes)
            {
                if (entry.path == ScenePath)
                {
                    found = true;
                }
            }

            if (!found)
            {
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Racing] Сцена гонки собрана: " + ScenePath);
        }

        // ------------------------------------------------------------------ UI

        private static void BuildHud(Transform canvas, Font font, Sprite heart, GameUI ui)
        {
            GameObject hud = new GameObject("HUD", typeof(RectTransform));
            hud.transform.SetParent(canvas, false);
            RectTransform hudRect = (RectTransform)hud.transform;
            Stretch(hudRect);
            ui.hudPanel = hud;

            ui.scoreText = CreateText(hud.transform, "ScoreText", "ОЧКИ: 0", 52, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(40f, -50f), new Vector2(520f, 70f), font);

            ui.bestText = CreateText(hud.transform, "BestText", "РЕКОРД: 0", 30, new Color(1f, 0.85f, 0.35f),
                TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(42f, -105f), new Vector2(520f, 44f), font);

            ui.comboText = CreateText(hud.transform, "ComboText", "", 54, new Color(1f, 0.9f, 0.3f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(600f, 70f), font);
            ui.comboText.enabled = false;

            ui.speedText = CreateText(hud.transform, "SpeedText", "0 КМ/Ч", 44, Color.white, TextAnchor.MiddleRight,
                new Vector2(1f, 0f), new Vector2(-40f, 55f), new Vector2(360f, 60f), font);

            // Сердечки
            for (int i = 0; i < 3; i++)
            {
                Image image = CreateImage(hud.transform, "Heart" + i, heart, new Vector2(0f, 0f),
                    new Vector2(50f + i * 78f, 55f), new Vector2(64f, 64f), Color.white);
                ui.hearts = AddToArray(ui.hearts, image);
            }

            // Нитро
            GameObject nitroRoot = new GameObject("Nitro", typeof(RectTransform));
            nitroRoot.transform.SetParent(hud.transform, false);
            RectTransform nitroRect = (RectTransform)nitroRoot.transform;
            nitroRect.anchorMin = new Vector2(0.5f, 1f);
            nitroRect.anchorMax = new Vector2(0.5f, 1f);
            nitroRect.pivot = new Vector2(0.5f, 1f);
            nitroRect.anchoredPosition = new Vector2(0f, -40f);
            nitroRect.sizeDelta = new Vector2(460f, 30f);

            Image background = nitroRoot.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.45f);
            background.raycastTarget = false;

            GameObject fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(nitroRoot.transform, false);
            RectTransform fillRect = (RectTransform)fillGo.transform;
            Stretch(fillRect);
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);

            Image fill = fillGo.AddComponent<Image>();
            fill.color = new Color(0.35f, 0.8f, 1f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;
            ui.nitroFill = fill;

            // Пауза
            Button pause = CreateButton(hud.transform, "PauseButton", "II", new Vector2(1f, 1f),
                new Vector2(-50f, -50f), new Vector2(70f, 70f), font, new Color(0f, 0f, 0f, 0.45f), Color.white);
            ui.pauseButton = pause;
        }

        private static void BuildStartPanel(Transform canvas, Font font, GameUI ui)
        {
            GameObject panel = CreateOverlayPanel(canvas, "StartPanel", out RectTransform panelRect);
            ui.startPanel = panel;

            CreateText(panel.transform, "Title", "ГОНОЧКИ", 96, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(900f, 140f), font);

            CreateText(panel.transform, "Hint",
                "Машина едет сама — рули мышью\nЛКМ — нитро, ПКМ — тормоз, WASD — тоже можно\nМонетки = очки · синяя банка = нитро · сердце = ремонт · щит = неуязвимость",
                30, new Color(0.9f, 0.95f, 1f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1200f, 170f), font);

            ui.startButton = CreateButton(panel.transform, "StartButton", "ПОЕХАЛИ!", new Vector2(0.5f, 0.5f),
                new Vector2(0f, -130f), new Vector2(460f, 110f), font, new Color(0.2f, 0.75f, 0.35f), Color.white);
        }

        private static void BuildPausePanel(Transform canvas, Font font, GameUI ui)
        {
            GameObject panel = CreateOverlayPanel(canvas, "PausePanel", out RectTransform panelRect);
            panel.SetActive(false);
            ui.pausePanel = panel;

            CreateText(panel.transform, "Title", "ПАУЗА", 84, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(800f, 120f), font);

            Button resume = CreateButton(panel.transform, "ResumeButton", "ПРОДОЛЖИТЬ", new Vector2(0.5f, 0.5f),
                new Vector2(0f, 20f), new Vector2(460f, 100f), font, new Color(0.2f, 0.45f, 0.8f), Color.white);
            resume.onClick.AddListener(ui.TogglePause);

            Button quit = CreateButton(panel.transform, "QuitButton", "ВЫХОД", new Vector2(0.5f, 0.5f),
                new Vector2(0f, -110f), new Vector2(300f, 80f), font, new Color(0.7f, 0.25f, 0.25f), Color.white);
            quit.onClick.AddListener(ui.QuitGame);
            ui.quitButton = quit;
        }

        private static void BuildGameOverPanel(Transform canvas, Font font, GameUI ui)
        {
            GameObject panel = CreateOverlayPanel(canvas, "GameOverPanel", out RectTransform panelRect);
            panel.SetActive(false);
            ui.gameOverPanel = panel;

            CreateText(panel.transform, "Title", "КОНЕЦ ИГРЫ", 84, new Color(1f, 0.4f, 0.35f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(900f, 120f), font);

            ui.gameOverScoreText = CreateText(panel.transform, "Score", "Очки: 0", 46, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(700f, 70f), font);

            ui.gameOverBestText = CreateText(panel.transform, "Best", "Рекорд: 0", 34, new Color(1f, 0.85f, 0.35f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(700f, 60f), font);

            ui.restartButton = CreateButton(panel.transform, "RestartButton", "ЗАНОВО", new Vector2(0.5f, 0.5f),
                new Vector2(0f, -120f), new Vector2(420f, 100f), font, new Color(0.2f, 0.75f, 0.35f), Color.white);
        }

        private static void BuildFlashOverlay(Transform canvas, GameUI ui)
        {
            GameObject overlay = new GameObject("DamageFlash", typeof(RectTransform));
            overlay.transform.SetParent(canvas, false);
            RectTransform rect = (RectTransform)overlay.transform;
            Stretch(rect);

            Image image = overlay.AddComponent<Image>();
            image.color = new Color(0.9f, 0.1f, 0.1f, 0f);
            image.raycastTarget = false;
            overlay.transform.SetAsLastSibling();

            ui.flashOverlay = image;
        }

        [MenuItem("Tools/Racing/Собрать для WebGL")]
        public static void BuildWebGL()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string buildDir = Path.Combine(root, "Build", "WebGL");
            Directory.CreateDirectory(buildDir);

            // Настройки WebGL: gzip + Decompression Fallback. Fallback нужен, потому что
            // обычные статические хостинги (GitHub Pages) не отдают заголовок
            // Content-Encoding: gzip, и без него загрузчик не смог бы распаковать файлы.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.memorySize = 512;
            PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            PlayerSettings.runInBackground = true;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Racing.unity" },
                locationPathName = buildDir,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                PatchWebGLIndex(buildDir);
                Debug.Log($"[Racing] WebGL сборка готова: {buildDir} ({summary.totalSize / 1024 / 1024} МБ, {summary.totalTime})");
            }
            else
            {
                Debug.LogError($"[Racing] Ошибка WebGL сборки: {summary.result} — {summary.totalErrors} ошибок");
            }
        }

        /// <summary>Делает страницу адаптивной: канвас на всё окно браузера.</summary>
        private static void PatchWebGLIndex(string buildDir)
        {
            string indexPath = Path.Combine(buildDir, "index.html");
            if (!File.Exists(indexPath))
            {
                return;
            }

            string html = File.ReadAllText(indexPath);

            html = html.Replace(
                "        fullscreenContainer.style.width = \"960px\";\n        fullscreenContainer.style.height = \"600px\";",
                "        fullscreenContainer.style.width = \"100%\";\n        fullscreenContainer.style.height = \"100%\";");

            if (!html.Contains("unity-container { width:100%"))
            {
                html = html.Replace(
                    "  </head>",
                    "    <style>\n" +
                    "      html, body { width:100%; height:100%; margin:0; padding:0; overflow:hidden; background:#101418; }\n" +
                    "      #unity-container { width:100%; height:100%; }\n" +
                    "      #unity-canvas { width:100%; height:100%; display:block; }\n" +
                    "    </style>\n" +
                    "  </head>");
            }

            File.WriteAllText(indexPath, html);
        }

        [MenuItem("Tools/Racing/Собрать один .exe")]
        public static void BuildSingleExe()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string buildDir = Path.Combine(root, "Build", "Windows");
            string outputPath = Path.Combine(root, "Build", "TopDownRacing.exe");

            if (!BuildWindowsPlayer())
            {
                return;
            }

            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            if (PackSingleExe(buildDir, outputPath))
            {
                long size = new FileInfo(outputPath).Length;
                Debug.Log($"[Racing] Готов один файл: {outputPath} ({size / 1024 / 1024} МБ). " +
                          "При запуске он распакуется в текущую папку и запустит игру.");
            }
            else
            {
                Debug.LogError("[Racing] Не удалось собрать SFX. Обычная сборка лежит в " + buildDir);
            }
        }

        /// <summary>Упаковывает сборку в один self-extracting .exe через модуль SFX от WinRAR.</summary>
        private static bool PackSingleExe(string buildDir, string outputPath)
        {
            string rar = @"C:\Program Files\WinRAR\Rar.exe";
            if (!File.Exists(rar))
            {
                Debug.LogError("[Racing] WinRAR не найден: " + rar);
                return false;
            }

            string commentPath = Path.Combine(Path.GetTempPath(), "topdownracing_sfx.txt");
            File.WriteAllText(commentPath,
                ";@echo off\n" +
                "Title=Top Down Racing\n" +
                "Setup=TopDownRacing.exe\n" +
                "Overwrite=1\n" +
                "Silent=1\n");

            string[] items =
            {
                "TopDownRacing.exe",
                "TopDownRacing_Data",
                "UnityPlayer.dll",
                "MonoBleedingEdge",
                "DirectML.dll",
                "dstorage.dll",
                "dstoragecore.dll",
                "UnityCrashHandler64.exe",
                "D3D12"
            };

            string args = "a -r -sfx -z\"" + commentPath + "\" \"" + outputPath + "\"";
            foreach (string item in items)
            {
                args += " \"" + item + "\"";
            }

            // Важно: stdout/stderr НЕ перенаправляем — иначе RAR упрётся в буфер
            // и процесс сборки зависнет намертво.
            System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo(rar, args)
            {
                WorkingDirectory = buildDir,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(info))
            {
                process.WaitForExit(180000);
            }

            return File.Exists(outputPath);
        }

        [MenuItem("Tools/Racing/Собрать EXE для Windows")]
        public static void BuildWindowsPlayerMenu()
        {
            BuildWindowsPlayer();
        }

        public static bool BuildWindowsPlayer()
        {
            ConfigurePlayerSettings();

            string root = Directory.GetParent(Application.dataPath).FullName;
            string buildDir = Path.Combine(root, "Build", "Windows");
            Directory.CreateDirectory(buildDir);

            string exePath = Path.Combine(buildDir, "TopDownRacing.exe");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Racing.unity" },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Racing] Сборка готова: {exePath} ({summary.totalSize / 1024 / 1024} МБ, {summary.totalTime})");
                return true;
            }

            Debug.LogError($"[Racing] Ошибка сборки: {summary.result} — {summary.totalErrors} ошибок");
            return false;
        }

        /// <summary>Настройки окна для готовой игры: окно 1280×720, а не фуллскрин.</summary>
        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.productName = "Top Down Racing";
            PlayerSettings.companyName = "My project";
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.allowFullscreenSwitch = true;
        }

        // ------------------------------------------------------------------ Хелперы

        private static void BuildGroundVisuals(Transform parent, InfiniteGround ground, Sprite[] grass, Sprite roadSprite)
        {
            int columns = ground.gridColumns;
            int rows = ground.gridRows;
            float size = ground.grassTileSize;

            Vector2Int baseCell = new Vector2Int(0, 2);

            for (int j = 0; j < rows; j++)
            {
                for (int i = 0; i < columns; i++)
                {
                    GameObject go = new GameObject("Grass_" + j + "_" + i);
                    go.transform.SetParent(parent, false);

                    Vector2Int cell = new Vector2Int(
                        baseCell.x + i - columns / 2,
                        baseCell.y + j - rows / 2);
                    go.transform.position = new Vector3(
                        (cell.x + 0.5f) * size,
                        (cell.y + 0.5f) * size,
                        0f);

                    SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
                    renderer.sprite = grass[0];
                    renderer.sortingOrder = ground.grassSortingOrder;
                }
            }

            for (int i = 0; i < ground.roadSegmentCount; i++)
            {
                GameObject go = new GameObject("RoadSegment_" + i);
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(
                    0f,
                    (i - ground.roadSegmentCount / 2) * ground.roadSegmentHeight,
                    0f);

                SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = roadSprite;
                renderer.sortingOrder = ground.roadSortingOrder;
            }
        }

        private static GameObject CreateOverlayPanel(Transform canvas, string name, out RectTransform rect)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(canvas, false);
            rect = (RectTransform)panel.transform;
            Stretch(rect);

            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.05f, 0.06f, 0.08f, 0.78f);

            return panel;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, Color color,
            TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 size, Font font)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = PivotFor(alignment);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text text = go.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Vector2 PivotFor(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.MiddleLeft:
                case TextAnchor.LowerLeft:
                    return new Vector2(0f, 0.5f);
                case TextAnchor.UpperRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.LowerRight:
                    return new Vector2(1f, 0.5f);
                default:
                    return new Vector2(0.5f, 0.5f);
            }
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 position,
            Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position,
            Vector2 size, Font font, Color background, Color textColor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = go.AddComponent<Image>();
            image.color = background;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;

            CreateText(go.transform, "Label", label, 44, textColor, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x, size.y), font);

            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Image[] AddToArray(Image[] array, Image image)
        {
            if (array == null)
            {
                return new[] { image };
            }

            Image[] result = new Image[array.Length + 1];
            System.Array.Copy(array, result, array.Length);
            result[array.Length] = image;
            return result;
        }

        private static Sprite LoadSprite(string relativePath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + relativePath);
            if (sprite == null)
            {
                Debug.LogError("[Racing] Не найден спрайт: " + ArtRoot + relativePath);
            }

            return sprite;
        }

        private static void ReimportArt()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
