using LotoSim.Audio;
using LotoSim.Core;
using LotoSim.Devices;
using LotoSim.Evaluation;
using LotoSim.Guidance;
using LotoSim.Interaction;
using LotoSim.Player;
using LotoSim.Procedure;
using LotoSim.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static LotoSim.Editor.BuilderUtils;

namespace LotoSim.Editor
{
    /// <summary>
    /// Construye la escena PumpRoom en greybox (primitivas) con todos los sistemas cableados.
    /// Idempotente: borra el contenido de la escena y lo regenera. Coordenadas en metros;
    /// la sala ocupa X [-6, 6] y Z [-4.5, 4.5]; el jugador entra por el sur mirando al norte.
    /// </summary>
    public static class GreyboxSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/PumpRoom.unity";
        private const string PlateText = "#1C1F24";

        private static Material Floor => Mat("Floor", "#B8BDC4");
        private static Material Wall => Mat("Wall", "#D9DCE0");
        private static Material Graphite => Mat("Graphite", "#2E3238");
        private static Material PumpBlue => Mat("PumpBlue", "#2F5D8C", 0.45f);
        private static Material PipeGreen => Mat("PipeGreen", "#3E8E5A", 0.4f);
        private static Material Orange => Mat("Orange", "#F28C1B");
        private static Material Red => Mat("Red", "#D3272E", 0.4f);
        private static Material Yellow => Mat("Yellow", "#FFC629");
        private static Material Green => Mat("Green", "#3BB273", 0.4f);
        private static Material White => Mat("White", "#F2F2F2");
        private static Material Steel => Mat("Steel", "#8A9099", 0.6f);
        private static Material Dark => Mat("Dark", "#1C1F24");
        private static Material Blue => Mat("Blue", "#2D7FF9");
        private static Material Worker => Mat("Worker", "#4A6B8A");
        private static Material Pilot => Mat("Pilot", "#3BB273", 0.6f, emissive: true);
        private static Material Ghost => TransparentMat("Ghost", "#FFC629", 0.45f);

        /// <summary>Crea/actualiza todos los materiales que usan los modelos de Blender (deben existir antes de importarlos).</summary>
        [MenuItem("LotoSim/Greybox/Crear materiales", priority = 38)]
        public static void EnsureMaterials()
        {
            _ = Floor; _ = Wall; _ = Graphite; _ = PumpBlue; _ = PipeGreen; _ = Orange; _ = Red; _ = Yellow;
            _ = Green; _ = White; _ = Steel; _ = Dark; _ = Blue; _ = Worker; _ = Pilot; _ = Ghost;
            Mat("Skin", "#C9A27E", 0.3f);
            Mat("Glass", "#9FC3D9", 0.85f);
            Mat("Light", "#FFF6E0", 0.1f, emissive: true).SetColor("_EmissionColor", new Color(1f, 0.96f, 0.88f) * 2.2f);
            Mat("Guide", "#FFC629", 0.2f, emissive: true);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("LotoSim/Greybox/Construir PumpRoom", priority = 40)]
        public static void Build()
        {
            ProcedureDataBuilder.Build();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            // Cargar después de abrir la escena: OpenScene descarga los assets no referenciados.
            var procedure = AssetDatabase.LoadAssetAtPath<ProcedureDefinition>(ProcedureDataBuilder.ProcedurePath);
            foreach (var root in scene.GetRootGameObjects())
                Object.DestroyImmediate(root);

            var systems = Empty("--- SYSTEMS ---", null, Vector3.zero).transform;
            var playerRoot = Empty("--- PLAYER ---", null, Vector3.zero).transform;
            var environment = Empty("--- ENVIRONMENT ---", null, Vector3.zero).transform;
            var devices = Empty("--- DEVICES ---", null, Vector3.zero).transform;
            var loto = Empty("--- LOTO ---", null, Vector3.zero).transform;
            var npc = Empty("--- NPC ---", null, Vector3.zero).transform;
            var lighting = Empty("--- LIGHTING ---", null, Vector3.zero).transform;
            var ui = Empty("--- UI ---", null, Vector3.zero).transform;

            BuildLighting(lighting);
            BuildRoom(environment);
            BuildPpe(loto);
            BuildLotoStation(loto);
            var disconnect = BuildElectricalPanel(devices);
            var pump = BuildPump(devices, disconnect);
            BuildOperator(npc);
            var player = BuildPlayer(playerRoot);

            var runner = new GameObject("ProcedureRunner").AddComponent<ProcedureRunner>();
            runner.transform.SetParent(systems, false);
            Set(runner, ("_procedure", procedure), ("_pump", pump));
            BuildEvaluationAndGuidance(systems, runner, pump, player.Kit);
            new GameObject("QualityController").AddComponent<QualityController>().transform.SetParent(systems, false);
            AddHighlightTargets(scene);
            BuildAudio(systems, pump, player);

            BuildUi(ui, player);
            AddUiSounds(ui.Find("HUD").gameObject);
            BuildTablet(player.Interactor.transform);

            MarkStatic(environment);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LotoSim] Escena PumpRoom (greybox) construida");
        }

        // ---------------------------------------------------------------- Evaluación y guía

        private static readonly string[] HighlightNames =
        {
            "PPE-HELMET", "PPE-GLASSES", "PPE-GLOVES", "WORK-ORDER", "LOTO-STATION", "OPERATOR",
            "PLATE-DS-101", "PLATE-V-101", "PLATE-V-102", "PLATE-V-103",
            "DS-101", "V-101", "V-102", "V-103", "PI-101", "P-101", "CS-101",
            "TOOL-WRENCH", "TOOL-SCREWDRIVER"
        };

        private static void BuildEvaluationAndGuidance(Transform systems, ProcedureRunner runner, PumpSystem pump, LotoKit kit)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<MistakeCatalog>(ProcedureDataBuilder.MistakeCatalogPath);

            var tracker = new GameObject("AssessmentTracker").AddComponent<AssessmentTracker>();
            tracker.transform.SetParent(systems, false);
            Set(tracker, ("_catalog", catalog), ("_runner", runner));

            var hazards = new GameObject("HazardMonitor").AddComponent<HazardMonitor>();
            hazards.transform.SetParent(systems, false);
            Set(hazards, ("_tracker", tracker), ("_pump", pump), ("_runner", runner));

            var guidance = new GameObject("GuidanceController").AddComponent<GuidanceController>();
            guidance.transform.SetParent(systems, false);
            var marker = Box("GuideMarker", guidance.transform, Vector3.zero, Vector3.one * 0.06f,
                Mat("Guide", "#FFC629", 0.2f, emissive: true), false, new Vector3(45f, 0f, 45f));
            marker.SetActive(false);
            Set(guidance, ("_runner", runner), ("_kit", kit), ("_markerTemplate", marker));
        }

        private static void AddHighlightTargets(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (System.Array.IndexOf(HighlightNames, t.name) >= 0 && !t.TryGetComponent<HighlightTarget>(out _))
                        t.gameObject.AddComponent<HighlightTarget>();
                }
            }
        }

        // ---------------------------------------------------------------- Tablet

        /// <summary>Tablet en world-space sostenida frente a la cámara (abajo y levemente inclinada).</summary>
        private static void BuildTablet(Transform camera)
        {
            var holder = Empty("Tablet", camera, new Vector3(0f, -0.065f, 0.33f), new Vector3(10f, 0f, 0f));
            var view = holder.AddComponent<TabletView>();

            var device = Empty("Device", holder.transform, Vector3.zero);
            Box("Body", device.transform, new Vector3(0f, 0f, 0.006f), new Vector3(0.31f, 0.22f, 0.012f), Dark, false);
            Box("Bumper", device.transform, new Vector3(0f, 0f, 0.008f), new Vector3(0.325f, 0.235f, 0.008f), Orange, false);

            var canvasGo = new GameObject("Screen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var rect = (RectTransform)canvasGo.transform;
            rect.SetParent(device.transform, false);
            rect.localPosition = new Vector3(0f, 0f, -0.001f);
            rect.sizeDelta = new Vector2(900f, 640f);
            rect.localScale = Vector3.one * (0.29f / 900f);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 4f;

            var background = Panel("Background", rect, Center, Center, Vector2.zero, Vector2.zero, "#16191E", 1f);
            StretchFull(background.rectTransform);
            var header = Panel("Header", rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(900f, 70f), "#1C1F24", 1f);
            header.rectTransform.anchorMin = new Vector2(0f, 1f);
            header.rectTransform.anchorMax = new Vector2(1f, 1f);
            header.rectTransform.sizeDelta = new Vector2(0f, 70f);
            var tabs = Text("Tabs", header.transform, 31, TextAlignmentOptions.Left);
            tabs.margin = new Vector4(24f, 0f, 70f, 0f);
            var status = Panel("Status", header.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(26f, 26f), "#3BB273", 1f);
            var content = Text("Content", rect, 33, TextAlignmentOptions.TopLeft);
            content.margin = new Vector4(30f, 90f, 30f, 24f);
            content.richText = true;

            Set(view, ("_root", device), ("_tabs", tabs), ("_content", content), ("_statusLight", status));
        }

        // ---------------------------------------------------------------- Audio

        private static void BuildAudio(Transform systems, PumpSystem pump, PlayerRefs player)
        {
            var library = AudioDataBuilder.Build();
            var service = new GameObject("AudioService").AddComponent<AudioService>();
            service.transform.SetParent(systems, false);
            Set(service, ("_library", library), ("_ambience", AudioDataBuilder.Clip("Ambience/AMB_RoomHVAC")));

            var bleed = GameObject.Find("V-103").GetComponent<Valve>();
            var audio = pump.gameObject.AddComponent<PumpAudio>();
            var motor = AudioSourceAt("MotorAudio", pump.transform, new Vector3(1.8f, 0.6f, 1f), AudioDataBuilder.Clip("Ambience/AMB_MotorHum"));
            var drain = AudioSourceAt("DrainAudio", pump.transform, bleed.transform.position, AudioDataBuilder.Clip("SFX/SFX_WaterBleed"));
            var spray = AudioSourceAt("SprayAudio", pump.transform, bleed.transform.position, AudioDataBuilder.Clip("SFX/SFX_WaterSpray"));
            Set(audio, ("_pump", pump), ("_bleed", bleed), ("_motor", motor), ("_drain", drain), ("_spray", spray));

            player.Kit.gameObject.AddComponent<Footsteps>();
        }

        private static AudioSource AudioSourceAt(string name, Transform parent, Vector3 worldPos, AudioClip clip)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.playOnAwake = false;
            return source;
        }

        public static void AddUiSounds(GameObject canvas)
        {
            var sounds = canvas.AddComponent<UiSounds>();
            Set(sounds, ("_click", AudioDataBuilder.Clip("UI/UI_Click")));
        }

        // ---------------------------------------------------------------- Ambiente

        private static void BuildLighting(Transform parent)
        {
            var sun = Empty("Directional Light", parent, new Vector3(0f, 6f, 0f), new Vector3(55f, -35f, 0f));
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.75f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.7f;
            light.color = new Color(1f, 0.97f, 0.92f);

            // Luz de relleno bajo cada luminaria del techo (sin sombras: barato en WebGL).
            foreach (var pos in new[] { new Vector3(-3f, 3.7f, -2f), new Vector3(2f, 3.7f, -2f), new Vector3(-3f, 3.7f, 2f), new Vector3(2f, 3.7f, 2f) })
            {
                var point = Empty("FixtureLight", parent, pos).AddComponent<Light>();
                point.type = LightType.Point;
                point.range = 7.5f;
                point.intensity = 1.6f;
                point.color = new Color(1f, 0.95f, 0.86f);
                point.shadows = LightShadows.None;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.66f, 0.69f, 0.74f);
            RenderSettings.ambientEquatorColor = new Color(0.52f, 0.54f, 0.57f);
            RenderSettings.ambientGroundColor = new Color(0.34f, 0.35f, 0.37f);

            var volume = Empty("PostProcess", parent, Vector3.zero).AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = PostProfile();
        }

        private const string PostProfilePath = "Assets/_Project/Settings/PP_PumpRoom.asset";

        private static VolumeProfile PostProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostProfilePath);
            if (profile != null)
                return profile;
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Settings"))
                AssetDatabase.CreateFolder("Assets/_Project", "Settings");
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, PostProfilePath);

            var tonemapping = profile.Add<UnityEngine.Rendering.Universal.Tonemapping>(true);
            tonemapping.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.Neutral);
            var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
            bloom.intensity.Override(0.35f);
            bloom.threshold.Override(1.05f);
            var color = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
            color.postExposure.Override(0.15f);
            color.contrast.Override(10f);
            color.saturation.Override(6f);
            var vignette = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
            vignette.intensity.Override(0.2f);
            vignette.smoothness.Override(0.45f);

            foreach (var component in profile.components)
                AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static void BuildRoom(Transform parent)
        {
            var room = Empty("Room", parent, Vector3.zero).transform;
            Box("Floor", room, new Vector3(0f, -0.05f, 0f), new Vector3(12.4f, 0.1f, 9.4f), Floor);
            Box("Wall_N", room, new Vector3(0f, 2f, 4.6f), new Vector3(12.4f, 4f, 0.2f), Wall);
            Box("Wall_W", room, new Vector3(-6.1f, 2f, 0f), new Vector3(0.2f, 4f, 9.4f), Wall);
            Box("Wall_E", room, new Vector3(6.1f, 2f, 0f), new Vector3(0.2f, 4f, 9.4f), Wall);
            Box("Wall_S_A", room, new Vector3(-5.6f, 2f, -4.6f), new Vector3(1.2f, 4f, 0.2f), Wall);
            Box("Wall_S_B", room, new Vector3(1.6f, 2f, -4.6f), new Vector3(9.2f, 4f, 0.2f), Wall);
            Box("Wall_S_Lintel", room, new Vector3(-4f, 3.25f, -4.6f), new Vector3(2f, 1.5f, 0.2f), Wall);
            // Puerta cerrada: sin ella el vano da al vacío y se puede caer.
            Box("Door_S", room, new Vector3(-4f, 1.25f, -4.55f), new Vector3(2f, 2.5f, 0.1f), Graphite);
            Label("SALIDA", room, new Vector3(-4f, 2.25f, -4.495f), new Vector3(0f, 180f, 0f), 0.6f, "#3BB273", 1.2f);
            var ceiling = Box("Ceiling", room, new Vector3(0f, 4.05f, 0f), new Vector3(12.4f, 0.1f, 9.4f), Wall, false);
            ceiling.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            // Zócalo oscuro para dar lectura a la base de las paredes.
            Box("Skirting_N", room, new Vector3(0f, 0.06f, 4.49f), new Vector3(12.2f, 0.12f, 0.02f), Graphite, false);
            Box("Skirting_W", room, new Vector3(-5.99f, 0.06f, 0f), new Vector3(0.02f, 0.12f, 9.2f), Graphite, false);
            Box("Skirting_E", room, new Vector3(5.99f, 0.06f, 0f), new Vector3(0.02f, 0.12f, 9.2f), Graphite, false);

            // Zona demarcada alrededor de la bomba.
            var zone = Empty("SafetyZone", room, new Vector3(0.3f, 0.002f, 1f)).transform;
            Box("Line_N", zone, new Vector3(0f, 0f, 0.95f), new Vector3(5.4f, 0.004f, 0.08f), Yellow, false);
            Box("Line_S", zone, new Vector3(0f, 0f, -0.95f), new Vector3(5.4f, 0.004f, 0.08f), Yellow, false);
            Box("Line_W", zone, new Vector3(-2.7f, 0f, 0f), new Vector3(0.08f, 0.004f, 1.98f), Yellow, false);
            Box("Line_E", zone, new Vector3(2.7f, 0f, 0f), new Vector3(0.08f, 0.004f, 1.98f), Yellow, false);

            // Luminarias, bandeja portacables y extintor.
            var fixtures = Empty("Fixtures", parent, Vector3.zero).transform;
            foreach (var pos in new[] { new Vector3(-3f, 3.96f, -2f), new Vector3(2f, 3.96f, -2f), new Vector3(-3f, 3.96f, 2f), new Vector3(2f, 3.96f, 2f) })
                Model("CeilingLight", fixtures, pos);
            Model("CableTray", fixtures, new Vector3(-1.5f, 3.4f, 4.2f));
            Model("CableTray", fixtures, new Vector3(1.5f, 3.4f, 4.2f));
            Model("Extinguisher", fixtures, new Vector3(-2.75f, 0f, -4.42f), new Vector3(0f, 180f, 0f));
            Sign(fixtures, "ExtinguisherSign", new Vector3(-2.75f, 1.05f, -4.48f), new Vector3(0f, 180f, 0f), "#D3272E", "EXTINTOR", "#FFFFFF");

            // Equipo fuera de servicio de fondo (mismos modelos, en gris).
            var spare = Empty("P-102_Decor", parent, new Vector3(3.6f, 0f, 3.4f)).transform;
            Recolor(Model("PumpSkid", spare, Vector3.zero), Steel);
            Recolor(Model("PumpCasing", spare, new Vector3(-0.4f, 0.6f, 0f)), Steel);
            Label("P-102 · fuera de servicio", spare, new Vector3(0f, 1.25f, -0.2f), Vector3.zero, 0.5f, "#2E3238", 2f);

            Label("SALA DE BOMBAS · SECTOR AGUA DE PROCESO", parent, new Vector3(1.5f, 3.0f, 4.48f), Vector3.zero, 2.2f, "#2E3238", 8f);
            Sign(parent, "PpeSign", new Vector3(-5.98f, 2.05f, -2.6f), new Vector3(0f, -90f, 0f), "#2D7FF9", "USO OBLIGATORIO\nDE EPP", "#FFFFFF");
            Sign(parent, "ElectricSign", new Vector3(-4.2f, 1.9f, 4.48f), Vector3.zero, "#FFC629", "RIESGO\nELÉCTRICO", "#1C1F24");
            Sign(parent, "LotoSign", new Vector3(-5.98f, 2.25f, 1.4f), new Vector3(0f, -90f, 0f), "#D3272E", "BLOQUEAR Y ETIQUETAR\nANTES DE INTERVENIR", "#FFFFFF", 1.1f);
        }

        /// <summary>Cartel de seguridad simple (placa de color + texto).</summary>
        private static void Sign(Transform parent, string name, Vector3 pos, Vector3 euler, string hex, string text, string textHex,
            float width = 0.6f)
        {
            var sign = Empty(name, parent, pos, euler);
            Box("Board", sign.transform, Vector3.zero, new Vector3(width, 0.36f, 0.015f), BuilderUtils.Mat($"Sign_{hex.TrimStart('#')}", hex), false);
            Label(text, sign.transform, new Vector3(0f, 0f, -0.01f), Vector3.zero, 0.55f, textHex, width);
        }

        // ---------------------------------------------------------------- EPP

        private static void BuildPpe(Transform parent)
        {
            var ppe = Empty("PPE", parent, Vector3.zero).transform;
            FitCollider(Model("Locker", ppe, new Vector3(-5.7f, 0f, -3.3f), new Vector3(0f, -90f, 0f)));
            FitCollider(Model("Workbench", ppe, new Vector3(-5.55f, 0f, -1.9f)));
            Label("LOCKER EPP", ppe, new Vector3(-5.98f, 1.55f, -1.9f), new Vector3(0f, -90f, 0f), 0.6f, "#2E3238");

            float z = -2.55f;
            var item = new Vector3(0f, -90f, 0f);
            PpeItem(ppe, "PPE-HELMET", "Casco de seguridad", "ppe.helmet", null, "Casco colocado.", ref z,
                t => Model("Helmet", t, Vector3.zero, item));
            // Los distractores no muestran aviso propio: el error lo explica la evaluación (sólo en modo guiado).
            PpeItem(ppe, "PPE-CAP", "Gorra", null, "ppe.wrong.cap", null, ref z,
                t => Model("Cap", t, Vector3.zero, item));
            PpeItem(ppe, "PPE-GLASSES", "Anteojos de seguridad", "ppe.glasses", null, "Anteojos colocados.", ref z,
                t => Model("SafetyGlasses", t, Vector3.zero, item));
            PpeItem(ppe, "PPE-CLOTH-GLOVES", "Guantes de tela", null, "ppe.wrong.cloth-gloves", null, ref z,
                t => { var g = Model("Gloves", t, Vector3.zero, item); Recolor(g, White); return g; });
            PpeItem(ppe, "PPE-GLOVES", "Guantes dieléctricos", "ppe.gloves", null, "Guantes dieléctricos colocados.", ref z,
                t => Model("Gloves", t, Vector3.zero, item));
            PpeItem(ppe, "PPE-HEADPHONES", "Auriculares de música", null, "ppe.wrong.headphones", null, ref z,
                t => Model("Headphones", t, new Vector3(0f, 0.04f, 0f), item));
        }

        private delegate GameObject MeshFactory(Transform parent);

        private static void PpeItem(Transform parent, string id, string name, string flag, string wrongSignal,
            string toast, ref float z, MeshFactory mesh)
        {
            var root = Empty(id, parent, new Vector3(-5.55f, 0.905f, z));
            mesh(root.transform);
            FitCollider(root, 0.04f);
            var interactable = root.AddComponent<SignalInteractable>();
            bool correct = flag != null;
            Set(interactable,
                ("_prompt", $"Tomar: {name}"),
                ("_flag", flag ?? string.Empty),
                ("_signal", wrongSignal ?? string.Empty),
                ("_toast", toast ?? string.Empty),
                ("_toastKind", correct ? ToastKind.Success : ToastKind.Warning),
                ("_deactivateOnUse", true));
            z += 0.26f;
        }

        // ---------------------------------------------------------------- Estación LOTO

        private static void BuildLotoStation(Transform parent)
        {
            var station = Model("LotoStationBoard", parent, new Vector3(-5.97f, 1.4f, 1.4f), new Vector3(0f, -90f, 0f), "LOTO-STATION");
            station.AddComponent<LotoStation>();
            var t = station.transform;
            for (int i = 0; i < 3; i++)
            {
                float x = -0.48f + i * 0.24f;
                Model("Padlock", t, new Vector3(x, 0.15f, -0.05f), Vector3.zero, $"Padlock_{i}");
                Model("LotoTag", t, new Vector3(x, -0.06f, -0.05f), Vector3.zero, $"Tag_{i}");
            }
            Model("ValveLockout", t, new Vector3(0.25f, -0.35f, -0.07f), Vector3.zero, "Lockout_0");
            Model("ValveLockout", t, new Vector3(0.45f, -0.35f, -0.07f), Vector3.zero, "Lockout_1");
            Label("ESTACIÓN DE BLOQUEO", t, new Vector3(0f, 0.465f, -0.022f), Vector3.zero, 1.3f, "#1C1F24", 1.2f);
            Label("LOTO · Candado + etiqueta por persona", t, new Vector3(0f, 0.395f, -0.022f), Vector3.zero, 0.6f, "#D3272E", 1.2f);
            FitCollider(station);

            // Lejos de la estación de bloqueo para no confundir "leer la OT" con "tomar el kit".
            var order = Empty("WORK-ORDER", parent, new Vector3(-5.97f, 1.4f, -1.2f));
            Box("Clipboard", order.transform, Vector3.zero, new Vector3(0.02f, 0.34f, 0.25f), Graphite);
            Box("Sheet", order.transform, new Vector3(0.012f, -0.01f, 0f), new Vector3(0.004f, 0.3f, 0.22f), White, false);
            Label("ORDEN DE TRABAJO\nOT-0427", order.transform, new Vector3(0.016f, 0.08f, 0f), new Vector3(0f, -90f, 0f), 0.22f, PlateText, 0.22f);
            var source = order.AddComponent<DialogueSource>();
            Set(source,
                ("_prompt", "Leer la orden de trabajo OT-0427"),
                ("_fallback", ProcedureDataBuilder.LoadDialogue(ProcedureDataBuilder.DialogueWorkOrder)));
        }

        // ---------------------------------------------------------------- Tablero eléctrico

        private static DisconnectSwitch BuildElectricalPanel(Transform parent)
        {
            var panel = Empty("ELECTRICAL-PANEL", parent, new Vector3(-3f, 0f, 4.42f)).transform;
            FitCollider(Model("ElectricalCabinet", panel, new Vector3(0f, 1.3f, 0.05f)));
            Label("TABLERO TG-1", panel, new Vector3(0f, 2.25f, -0.07f), Vector3.zero, 1.0f, "#2E3238");

            DecorSwitch(panel, -0.55f, "DS-100", "Compresor C-201");
            var disconnect = SwitchDevice(panel, 0f);
            DecorSwitch(panel, 0.55f, "DS-102", "Ventilación");
            IdPlate(panel, "PLATE-DS-101", new Vector3(0f, 1.6f, -0.07f), Vector3.zero, "DS-101\nBomba P-101",
                "identify.DS-101", null, "Fuente identificada: seccionador DS-101 (eléctrica).");
            return disconnect;
        }

        private static DisconnectSwitch SwitchDevice(Transform panel, float x)
        {
            var root = Model("DisconnectSwitch", panel, new Vector3(x, 1.22f, -0.12f), Vector3.zero, "DS-101");
            var t = root.transform;
            var pivot = Empty("LeverPivot", t, new Vector3(0f, 0f, -0.07f));
            Model("SwitchLever", pivot.transform, Vector3.zero);

            var padlock = Model("Padlock", t, new Vector3(0.107f, -0.16f, -0.085f), Vector3.zero, "LOTO_Padlock");
            var tag = Model("LotoTag", t, new Vector3(0.107f, -0.2f, -0.095f), Vector3.zero, "LOTO_Tag");
            AddTagText(tag);
            FitCollider(root);

            var device = root.AddComponent<DisconnectSwitch>();
            Set(device,
                ("_deviceId", "DS-101"),
                ("_displayName", "el seccionador DS-101"),
                ("_handle", pivot.transform),
                ("_activeEuler", Vector3.zero),
                ("_inactiveEuler", new Vector3(0f, 0f, -150f)),
                ("_startsActive", true));
            var socket = root.AddComponent<LotoSocket>();
            Set(socket, ("_requiresLockout", false), ("_padlockVisual", padlock), ("_tagVisual", tag), ("_ghostMaterial", Ghost));
            return device;
        }

        private static void DecorSwitch(Transform panel, float x, string id, string load)
        {
            var root = Model("DisconnectSwitch", panel, new Vector3(x, 1.22f, -0.12f), Vector3.zero, $"{id}_Decor");
            Model("SwitchLever", root.transform, new Vector3(0f, 0f, -0.07f));
            IdPlate(panel, $"PLATE-{id}", new Vector3(x, 1.6f, -0.07f), Vector3.zero, $"{id}\n{load}",
                null, $"identify.wrong.{id}", null);
        }

        private static void IdPlate(Transform parent, string id, Vector3 pos, Vector3 euler, string text,
            string flag, string wrongSignal, string toast)
        {
            var plate = Empty(id, parent, pos, euler);
            Box("Plate", plate.transform, Vector3.zero, new Vector3(0.42f, 0.14f, 0.015f), White, false);
            Label(text, plate.transform, new Vector3(0f, 0f, -0.01f), Vector3.zero, 0.34f, PlateText, 0.42f);
            // Collider profundo hacia el jugador: sobresale de los colliders vecinos (tablero, skid)
            // que si no taparían el rótulo al apuntar.
            var hitBox = plate.AddComponent<BoxCollider>();
            hitBox.size = new Vector3(0.42f, 0.14f, 0.15f);
            hitBox.center = new Vector3(0f, 0f, -0.065f);
            var interactable = plate.AddComponent<SignalInteractable>();
            Set(interactable,
                ("_prompt", $"Identificar fuente: {text.Replace('\n', ' ')}"),
                ("_flag", flag ?? string.Empty),
                ("_signal", wrongSignal ?? string.Empty),
                ("_toast", toast ?? string.Empty),
                ("_toastKind", flag != null ? ToastKind.Success : ToastKind.Warning),
                ("_deactivateOnUse", false));
        }

        /// <summary>Texto de la etiqueta LOTO; <see cref="TagLabel"/> completa el nombre en runtime.</summary>
        private static void AddTagText(GameObject tag)
        {
            var text = Label("PELIGRO\nNO OPERAR", tag.transform, new Vector3(0f, -0.028f, -0.003f), Vector3.zero, 0.1f, "#FFFFFF", 0.07f);
            text.fontStyle = FontStyles.Bold;
            var body = Label("Técnico\nOT-0427", tag.transform, new Vector3(0f, -0.085f, -0.003f), Vector3.zero, 0.09f, "#1C1F24", 0.07f);
            body.gameObject.AddComponent<TagLabel>();
        }

        // ---------------------------------------------------------------- Bomba P-101

        private static PumpSystem BuildPump(Transform parent, DisconnectSwitch disconnect)
        {
            var group = Empty("P-101_Group", parent, Vector3.zero).transform;

            // El skid es una sola malla (base + motor + soporte): un collider envolvente tapaba la purga V-103
            // y los rótulos. Se usan dos cajas: la base baja y el motor (la carcasa tiene su propio collider).
            var skid = Model("PumpSkid", group, new Vector3(1f, 0f, 1f));
            var baseBox = skid.AddComponent<BoxCollider>();
            baseBox.center = new Vector3(0.04f, 0.09f, 0f);
            baseBox.size = new Vector3(2.7f, 0.2f, 0.92f);
            var motorBox = skid.AddComponent<BoxCollider>();
            motorBox.center = new Vector3(0.785f, 0.56f, 0.065f);
            motorBox.size = new Vector3(1.19f, 0.82f, 0.67f);
            var coupling = Model("Coupling", group, new Vector3(1.22f, 0.6f, 1f));
            Label("P-101", group, new Vector3(1.8f, 1.25f, 1f), Vector3.zero, 1.6f, "#2E3238");

            // Carcasa: dispara el mantenimiento.
            var casing = Model("PumpCasing", group, new Vector3(0.6f, 0.6f, 1f), Vector3.zero, "P-101");
            // Se recorta la parte baja del collider: la purga V-103 queda debajo de la carcasa y,
            // apuntando desde arriba, la carcasa la tapaba.
            var casingBox = FitCollider(casing);
            float casingBottom = casingBox.center.y - casingBox.size.y / 2f;
            float casingTop = casingBox.center.y + casingBox.size.y / 2f;
            casingBottom = Mathf.Max(casingBottom, -0.1f);
            casingBox.center = new Vector3(casingBox.center.x, (casingBottom + casingTop) / 2f, casingBox.center.z);
            casingBox.size = new Vector3(casingBox.size.x, casingTop - casingBottom, casingBox.size.z);

            // Succión (V-101) hacia el oeste y luego al techo.
            Cylinder("SuctionPipe", group, new Vector3(-0.96f, 0.6f, 1f), new Vector3(0f, 0f, 90f), new Vector3(0.16f, 1.24f, 0.16f), PipeGreen);
            Sphere("SuctionElbow", group, new Vector3(-2.2f, 0.6f, 1f), Vector3.one * 0.2f, PipeGreen);
            Cylinder("SuctionRiser", group, new Vector3(-2.2f, 2.3f, 1f), Vector3.zero, new Vector3(0.16f, 1.7f, 0.16f), PipeGreen);
            Model("Flange", group, new Vector3(-2.2f, 1.6f, 1f));
            var suction = BallValve(group, "V-101", "la válvula de succión V-101", new Vector3(-0.6f, 0.6f, 1f), Quaternion.identity);
            IdPlate(group, "PLATE-V-101", new Vector3(-0.85f, 0.25f, 0.7f), Vector3.zero, "V-101\nSucción",
                "identify.V-101", null, "Fuente identificada: válvula de succión V-101 (hidráulica).");

            // Descarga (V-102) vertical hacia el techo: el cuerpo se gira para que la cañería quede vertical
            // y el vástago mire al frente (-Z).
            Cylinder("DischargePipe", group, new Vector3(0.6f, 2.53f, 1f), Vector3.zero, new Vector3(0.14f, 1.47f, 0.14f), PipeGreen);
            Model("Flange", group, new Vector3(0.6f, 2.6f, 1f), new Vector3(90f, 0f, 0f));
            var dischargeRotation = Quaternion.LookRotation(Vector3.left, Vector3.back);
            var discharge = BallValve(group, "V-102", "la válvula de descarga V-102", new Vector3(0.6f, 1.5f, 1f), dischargeRotation);
            IdPlate(group, "PLATE-V-102", new Vector3(1.15f, 1.55f, 0.85f), Vector3.zero, "V-102\nDescarga",
                "identify.V-102", null, "Fuente identificada: válvula de descarga V-102 (hidráulica).");

            // Manómetro PI-101.
            var gaugeRoot = Model("Gauge", group, new Vector3(0.6f, 1.15f, 0.86f), Vector3.zero, "PI-101");
            var needlePivot = Empty("NeedlePivot", gaugeRoot.transform, new Vector3(0f, 0f, -0.021f));
            Model("GaugeNeedle", needlePivot.transform, Vector3.zero);
            Label("PI-101", gaugeRoot.transform, new Vector3(0f, -0.13f, 0f), Vector3.zero, 0.3f, "#2E3238", 0.3f);
            FitCollider(gaugeRoot);

            // Purga V-103 + balde.
            var bleed = Model("BleedValve", group, new Vector3(0.6f, 0.33f, 0.74f), Vector3.zero, "V-103");
            var bleedHandle = Empty("HandlePivot", bleed.transform, new Vector3(0f, 0f, -0.04f), new Vector3(90f, 0f, 0f));
            Model("ValveHandle", bleedHandle.transform, Vector3.zero, Vector3.zero, null, 0.45f);
            FitCollider(bleed, 0.04f);
            var bleedValve = bleed.AddComponent<Valve>();
            Set(bleedValve,
                ("_deviceId", "V-103"),
                ("_displayName", "la purga V-103"),
                ("_handle", bleedHandle.transform),
                ("_activeEuler", new Vector3(90f, 0f, 90f)),
                ("_inactiveEuler", new Vector3(90f, 0f, 0f)),
                ("_startsActive", false));
            FitCollider(Model("Bucket", group, new Vector3(0.6f, 0f, 0.55f)));
            IdPlate(group, "PLATE-V-103", new Vector3(1.05f, 0.6f, 0.5f), Vector3.zero, "V-103\nPurga",
                "identify.V-103", null, "Fuente identificada: purga V-103 (presión residual).");

            // Botonera local CS-101.
            var station = BuildControlStation(parent);

            // Herramientas que aparecen tras el mantenimiento.
            var wrench = Tool(group, "TOOL-WRENCH", "la llave inglesa", "tool.wrench", new Vector3(1.75f, 0f, 0.25f), "Wrench");
            var screwdriver = Tool(group, "TOOL-SCREWDRIVER", "el destornillador", "tool.screwdriver", new Vector3(-0.4f, 0f, 0.3f), "Screwdriver");

            var pumpCasing = casing.AddComponent<PumpCasing>();
            Set(pumpCasing, ("_spawnOnDone", new Object[] { wrench, screwdriver }));
            wrench.SetActive(false);
            screwdriver.SetActive(false);

            // Simulación.
            var system = new GameObject("PumpSystem").AddComponent<PumpSystem>();
            system.transform.SetParent(group, false);
            Set(system,
                ("_disconnect", disconnect),
                ("_controlStation", station),
                ("_suction", suction),
                ("_discharge", discharge),
                ("_bleed", bleedValve),
                ("_coupling", coupling.transform),
                ("_couplingAxis", Vector3.right));
            var gauge = gaugeRoot.AddComponent<PressureGauge>();
            Set(gauge, ("_pump", system), ("_needle", needlePivot.transform));
            return system;
        }

        /// <summary>
        /// Válvula esférica con palanca. En coordenadas locales: cañería en X, vástago en +Y.
        /// Abierta = palanca paralela a la cañería; cerrada = palanca hacia -Z (90° en Y).
        /// </summary>
        private static Valve BallValve(Transform parent, string id, string displayName, Vector3 pos, Quaternion rotation)
        {
            var root = Model("BallValve", parent, pos, Vector3.zero, id);
            root.transform.localRotation = rotation;
            var t = root.transform;
            var pivot = Empty("HandlePivot", t, new Vector3(0f, 0.16f, 0f));
            Model("ValveHandle", pivot.transform, Vector3.zero);

            // Dispositivos LOTO colocados sobre la palanca cerrada (apuntando a -Z local).
            var lockout = Model("ValveLockout", t, new Vector3(0f, 0.2f, -0.07f), new Vector3(0f, 90f, 0f), "LOTO_Lockout");
            var padlock = Model("Padlock", t, new Vector3(0.05f, 0.155f, -0.15f), new Vector3(0f, 90f, 0f), "LOTO_Padlock");
            var tag = Model("LotoTag", t, new Vector3(0.055f, 0.12f, -0.17f), new Vector3(0f, 0f, 0f), "LOTO_Tag");
            AddTagText(tag);
            FitCollider(root);

            var valve = root.AddComponent<Valve>();
            Set(valve,
                ("_deviceId", id),
                ("_displayName", displayName),
                ("_handle", pivot.transform),
                ("_activeEuler", Vector3.zero),
                ("_inactiveEuler", new Vector3(0f, 90f, 0f)),
                ("_startsActive", true));
            var socket = root.AddComponent<LotoSocket>();
            Set(socket, ("_requiresLockout", true), ("_lockoutVisual", lockout), ("_padlockVisual", padlock), ("_tagVisual", tag), ("_ghostMaterial", Ghost));
            return valve;
        }

        private static ControlStation BuildControlStation(Transform parent)
        {
            var root = Model("ControlStation", parent, new Vector3(2.7f, 0f, -0.1f), Vector3.zero, "CS-101");
            var t = root.transform;
            var pilot = Model("PilotLight", t, new Vector3(0f, 1.43f, -0.09f), Vector3.zero, "Pilot");
            Label("CS-101", t, new Vector3(0f, 1.56f, -0.085f), Vector3.zero, 0.3f, "#2E3238", 0.4f);
            FitCollider(root);

            var station = root.AddComponent<ControlStation>();
            Set(station, ("_pilotLight", pilot.GetComponentInChildren<Renderer>()));

            ButtonPart(t, station, true, new Vector3(-0.075f, 1.29f, -0.095f), Green, "START");
            ButtonPart(t, station, false, new Vector3(0.075f, 1.29f, -0.095f), Red, "STOP");
            return station;
        }

        private static void ButtonPart(Transform parent, ControlStation station, bool isStart, Vector3 pos, Material mat, string text)
        {
            var root = Model("PushButton", parent, pos, Vector3.zero, isStart ? "StartButton" : "StopButton");
            Recolor(root, mat);
            // Collider profundo hacia el jugador: el de la botonera (con la tapa saliente) los taparía.
            var hitBox = root.AddComponent<BoxCollider>();
            hitBox.size = new Vector3(0.12f, 0.12f, 0.3f);
            hitBox.center = new Vector3(0f, 0f, -0.1f);
            Label(text, parent, new Vector3(pos.x, 1.225f, -0.086f), Vector3.zero, 0.13f, "#1C1F24", 0.07f);
            var button = root.AddComponent<ControlButton>();
            Set(button, ("_station", station), ("_isStart", isStart));
        }

        private static GameObject Tool(Transform parent, string id, string name, string flag, Vector3 pos, string model)
        {
            var root = Model(model, parent, pos, new Vector3(0f, 30f, 0f), id);
            FitCollider(root, 0.06f);
            var interactable = root.AddComponent<SignalInteractable>();
            Set(interactable,
                ("_prompt", $"Recoger {name}"),
                ("_flag", flag),
                ("_toast", "Herramienta recogida."),
                ("_toastKind", ToastKind.Success),
                ("_deactivateOnUse", true));
            return root;
        }

        // ---------------------------------------------------------------- Operador

        private static void BuildOperator(Transform parent)
        {
            var desk = Model("Desk", parent, new Vector3(3.9f, 0f, -2.7f), Vector3.zero, "OperatorDesk");
            FitCollider(desk);
            Model("Monitor", desk.transform, new Vector3(0.1f, 0.78f, 0f), new Vector3(0f, -90f, 0f));
            Model("Radio", desk.transform, new Vector3(-0.15f, 0.78f, 0.45f), new Vector3(0f, -90f, 0f));

            var root = Model("Operator", parent, new Vector3(4.65f, 0f, -2.7f), new Vector3(0f, 90f, 0f), "OPERATOR");
            FitCollider(root);
            Label("Operador", root.transform, new Vector3(0f, 2.05f, 0f), Vector3.zero, 0.5f, "#2E3238");

            var source = root.AddComponent<DialogueSource>();
            Set(source,
                ("_prompt", "Hablar con el operador"),
                ("_fallback", ProcedureDataBuilder.LoadDialogue(ProcedureDataBuilder.DialogueOperatorIdle)));
            var so = new SerializedObject(source);
            var list = so.FindProperty("_stepDialogues");
            list.arraySize = 2;
            SetStepDialogue(list.GetArrayElementAtIndex(0), "S2", ProcedureDataBuilder.DialogueNotifyStop);
            SetStepDialogue(list.GetArrayElementAtIndex(1), "S7", ProcedureDataBuilder.DialogueNotifyRestore);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetStepDialogue(SerializedProperty element, string stepId, string dialogue)
        {
            element.FindPropertyRelative("_stepId").stringValue = stepId;
            element.FindPropertyRelative("_dialogue").objectReferenceValue = ProcedureDataBuilder.LoadDialogue(dialogue);
        }

        // ---------------------------------------------------------------- Jugador

        private sealed class PlayerRefs
        {
            public LotoKit Kit;
            public DesktopInteractor Interactor;
            public DesktopLook Look;
            public StationTeleport Stations;
            public FirstPersonMotor Motor;
        }

        private static PlayerRefs BuildPlayer(Transform parent)
        {
            var player = Empty("Player", parent, new Vector3(-4.3f, 0f, -3.7f));
            player.layer = 2; // Ignore Raycast: el raycast de interacción no se pega al jugador.
            var controller = player.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.3f;
            var motor = player.AddComponent<FirstPersonMotor>();
            var kit = player.AddComponent<LotoKit>();

            var cameraGo = Empty("PlayerCamera", player.transform, new Vector3(0f, 1.65f, 0f));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.fieldOfView = 70f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 60f;
            var cameraData = cameraGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.FastApproximateAntialiasing;
            cameraGo.AddComponent<AudioListener>();

            var interactor = cameraGo.AddComponent<DesktopInteractor>();
            cameraGo.AddComponent<HoverOutline>();
            Set(interactor, ("_camera", camera), ("_kit", kit));

            var look = player.AddComponent<DesktopLook>();
            Set(look, ("_body", player.transform), ("_pitchPivot", cameraGo.transform));

            var stations = player.AddComponent<StationTeleport>();
            var points = Empty("StationPoints", parent, Vector3.zero).transform;
            var list = new (string name, Vector3 pos, float yaw)[]
            {
                ("Locker de EPP", new Vector3(-4.6f, 0f, -2.1f), -90f),
                ("Estación de bloqueo", new Vector3(-4.9f, 0f, 1.0f), -90f),
                ("Tablero eléctrico TG-1", new Vector3(-3f, 0f, 3.1f), 0f),
                ("Bomba P-101", new Vector3(0.4f, 0f, -0.5f), 0f),
                ("Puesto del operador", new Vector3(3.0f, 0f, -2.7f), 90f)
            };
            var so = new SerializedObject(stations);
            var array = so.FindProperty("_stations");
            array.arraySize = list.Length;
            for (int i = 0; i < list.Length; i++)
            {
                var point = Empty($"Station_{i + 1}", points, list[i].pos, new Vector3(0f, list[i].yaw, 0f));
                var element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_name").stringValue = list[i].name;
                element.FindPropertyRelative("_point").objectReferenceValue = point.transform;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            return new PlayerRefs { Kit = kit, Interactor = interactor, Look = look, Stations = stations, Motor = motor };
        }

        // ---------------------------------------------------------------- UI

        private static readonly Vector2 Center = new(0.5f, 0.5f);

        private static void BuildUi(Transform parent, PlayerRefs player)
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(parent, false);
            var canvas = CreateCanvas(parent, "HUD", 0).transform;

            // Paso actual.
            var stepPanel = Panel("StepPanel", canvas, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(600f, 0f));
            var layout = stepPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 20, 20);
            layout.spacing = 12f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            stepPanel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var stepTitle = Text("Title", stepPanel.transform, 30, TextAlignmentOptions.TopLeft);
            stepTitle.fontStyle = FontStyles.Bold;
            var stepBody = Text("Body", stepPanel.transform, 21, TextAlignmentOptions.TopLeft);
            stepBody.color = new Color(0.85f, 0.87f, 0.9f);
            var tasks = Text("Tasks", stepPanel.transform, 21, TextAlignmentOptions.TopLeft);

            // Viñeta de error (debajo del resto).
            var vignette = Panel("ErrorVignette", canvas, Center, Center, Vector2.zero, Vector2.zero, "#E5484D", 0f);
            StretchFull(vignette.rectTransform);
            vignette.raycastTarget = false;
            vignette.transform.SetAsFirstSibling();

            // Centro de pantalla.
            var crosshair = Panel("Crosshair", canvas, Center, Center, Vector2.zero, new Vector2(8f, 8f), "#FFFFFF", 0.9f);
            crosshair.raycastTarget = false;
            var promptRect = Rect("Prompt", canvas, Center, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(1200f, 60f));
            var prompt = Text("Text", promptRect, 26, TextAlignmentOptions.Top);
            var hintRect = Rect("Hint", canvas, Center, Center, new Vector2(0f, 90f), new Vector2(1200f, 60f));
            var hint = Text("Text", hintRect, 32, TextAlignmentOptions.Center,
                "<mark=#1C1F24CC padding=\"24,24,10,10\">Hacé click para controlar la cámara\n<size=70%>WASD para moverte · Esc para pausar</size></mark>");

            // Kit.
            var kitPanel = Panel("KitBar", canvas, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1300f, 56f));
            var kitText = Text("Text", kitPanel.transform, 22, TextAlignmentOptions.Center);

            // Avisos (a la derecha del panel de pasos para no superponerse).
            var toastPanel = Panel("Toast", canvas, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(760f, 0f));
            var toastLayout = toastPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            toastLayout.padding = new RectOffset(24, 24, 16, 16);
            toastLayout.childControlHeight = true;
            toastLayout.childControlWidth = true;
            toastPanel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var toastText = Text("Text", toastPanel.transform, 23, TextAlignmentOptions.TopLeft);

            // Diálogo.
            var dialoguePanel = Panel("Dialogue", canvas, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(1100f, 340f));
            var dialogueText = Text("Text", dialoguePanel.transform, 24, TextAlignmentOptions.TopLeft);
            dialogueText.margin = new Vector4(28f, 24f, 28f, 24f);

            // Mapa de estaciones.
            var stationPanel = Panel("StationMap", canvas, Center, Center, Vector2.zero, new Vector2(560f, 360f));
            var stationText = Text("Text", stationPanel.transform, 26, TextAlignmentOptions.TopLeft);
            stationText.margin = new Vector4(32f, 28f, 32f, 28f);

            // Pausa + opciones.
            var pause = Panel("Pause", canvas, Center, Center, Vector2.zero, Vector2.zero, "#000000", 0.6f);
            StretchFull(pause.rectTransform);
            var pauseBox = Panel("Box", pause.transform, Center, Center, Vector2.zero, new Vector2(560f, 730f));
            var pauseTitle = Text("Title", pauseBox.transform, 40, TextAlignmentOptions.Top, "<b>Pausa</b>");
            pauseTitle.margin = new Vector4(0f, 28f, 0f, 0f);
            var resume = Button("Resume", pauseBox.transform, new Vector2(0f, 150f), new Vector2(360f, 60f), "Continuar");
            var restart = Button("Restart", pauseBox.transform, new Vector2(0f, 75f), new Vector2(360f, 60f), "Reiniciar", "#2E3238");
            var menu = Button("Menu", pauseBox.transform, new Vector2(0f, 0f), new Vector2(360f, 60f), "Salir al menú", "#2E3238");
            Text("SensLabel", Rect("SensLabelRect", pauseBox.transform, Center, Center, new Vector2(0f, -80f), new Vector2(360f, 30f)),
                22, TextAlignmentOptions.Left, "Sensibilidad del mouse");
            var slider = Slider("Sensitivity", pauseBox.transform, new Vector2(0f, -120f), new Vector2(360f, 24f));
            var invert = Toggle("InvertY", pauseBox.transform, new Vector2(0f, -165f), "Invertir eje vertical");
            Text("VolumeLabel", Rect("VolumeLabelRect", pauseBox.transform, Center, Center, new Vector2(0f, -205f), new Vector2(360f, 30f)),
                22, TextAlignmentOptions.Left, "Volumen");
            var volume = Slider("Volume", pauseBox.transform, new Vector2(0f, -240f), new Vector2(360f, 24f));
            var qualityToggle = Toggle("LowQuality", pauseBox.transform, new Vector2(20f, -295f), "Calidad baja (más fluido)");
            var diagToggle = Toggle("InputDiag", pauseBox.transform, new Vector2(20f, -340f), "Mostrar diagnóstico de entrada");

            BuildInputDebug(canvas, player, diagToggle);
            BuildBriefing(canvas);
            BuildReport(canvas);
            BuildFade(canvas);

            var hud = canvas.gameObject.AddComponent<HudView>();
            Set(hud,
                ("_interactor", player.Interactor), ("_kit", player.Kit), ("_look", player.Look), ("_stations", player.Stations),
                ("_stepPanel", stepPanel.gameObject), ("_stepTitle", stepTitle), ("_stepBody", stepBody), ("_taskList", tasks),
                ("_prompt", prompt), ("_hint", hint), ("_crosshair", crosshair),
                ("_kitBar", kitText), ("_toastPanel", toastPanel.gameObject), ("_toastText", toastText), ("_vignette", vignette),
                ("_dialoguePanel", dialoguePanel.gameObject), ("_dialogueText", dialogueText),
                ("_stationPanel", stationPanel.gameObject), ("_stationText", stationText),
                ("_pausePanel", pause.gameObject), ("_resumeButton", resume), ("_restartButton", restart), ("_menuButton", menu),
                ("_sensitivitySlider", slider), ("_invertToggle", invert), ("_volumeSlider", volume), ("_lowQualityToggle", qualityToggle));
        }

        public static Canvas CreateCanvas(Transform parent, string name, int sortingOrder)
        {
            var canvasGo = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void BuildInputDebug(Transform canvas, PlayerRefs player, Toggle pauseToggle)
        {
            // Línea compacta siempre visible, abajo a la izquierda.
            var compact = Panel("InputDebugCompact", canvas, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 96f), new Vector2(980f, 34f), "#0E1013", 0.78f);
            var compactText = Text("Text", compact.transform, 19, TextAlignmentOptions.Left);
            compactText.margin = new Vector4(14f, 0f, 14f, 0f);
            compact.raycastTarget = false;

            // Panel completo (F3, o automático si se aprietan teclas y no hay movimiento).
            var full = Panel("InputDebugFull", canvas, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 96f), new Vector2(860f, 390f), "#0E1013", 0.9f);
            var fullText = Text("Text", full.transform, 20, TextAlignmentOptions.TopLeft);
            fullText.margin = new Vector4(18f, 14f, 18f, 12f);
            full.raycastTarget = false;

            var overlay = canvas.gameObject.AddComponent<InputDebugOverlay>();
            Set(overlay,
                ("_compactPanel", compact.gameObject), ("_compactText", compactText),
                ("_fullPanel", full.gameObject), ("_fullText", fullText),
                ("_motor", player.Motor), ("_pauseToggle", pauseToggle));
        }

        private static void BuildBriefing(Transform canvas)
        {
            var root = Panel("Briefing", canvas, Center, Center, Vector2.zero, Vector2.zero, "#0E1013", 0.92f);
            StretchFull(root.rectTransform);

            Text("Title", Rect("TitleRect", root.transform, Center, Center, new Vector2(0f, 330f), new Vector2(1500f, 80f)),
                52, TextAlignmentOptions.Center, "<b>Bloqueo y etiquetado · Bomba P-101</b>");
            var mode = Text("Mode", Rect("ModeRect", root.transform, Center, Center, new Vector2(0f, 262f), new Vector2(1500f, 40f)),
                24, TextAlignmentOptions.Center);

            Card(root.transform, new Vector2(-500f, 20f), "¿Qué es LOTO?",
                "Lockout/Tagout es el procedimiento que asegura que un equipo <b>no pueda energizarse</b> mientras alguien lo interviene.\n\n" +
                "Cada fuente de energía se <b>aísla</b>, se <b>bloquea</b> con un candado personal y se <b>identifica</b> con una etiqueta.");
            Card(root.transform, new Vector2(0f, 20f), "Tu tarea",
                "<b>Orden OT-0427:</b> reemplazar el sello mecánico de la bomba centrífuga P-101.\n\n" +
                "Antes de intervenirla aplicá el procedimiento LOTO completo. Al terminar, restablecé el equipo de forma segura.");
            Card(root.transform, new Vector2(500f, 20f), "Controles",
                "<b>WASD</b> moverse · <b>Shift</b> rápido\n<b>Mouse</b> mirar · <b>Click</b> interactuar\n" +
                "<b>1 · 2 · 3</b> candado, etiqueta, bloqueo\n<b>R</b> / click der. guardar dispositivo\n" +
                "<b>T</b> mapa de estaciones\n<b>Esc</b> pausa");

            var start = Button("Start", root.transform, new Vector2(0f, -300f), new Vector2(380f, 72f), "<b>Comenzar</b>", "#2D7FF9", 30f);
            var view = canvas.gameObject.AddComponent<BriefingView>(); // en el canvas: el panel se desactiva
            Set(view, ("_panel", root.gameObject), ("_modeLabel", mode), ("_startButton", start));
        }

        private static void Card(Transform parent, Vector2 pos, string title, string body)
        {
            var card = Panel($"Card_{title}", parent, Center, Center, pos, new Vector2(460f, 400f), "#1C1F24", 1f);
            Panel("Accent", card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(460f, 6f), "#FFC629", 1f);
            var text = Text("Text", card.transform, 23, TextAlignmentOptions.TopLeft,
                $"<size=125%><b>{title}</b></size>\n\n{body}");
            text.margin = new Vector4(28f, 30f, 28f, 24f);
        }

        private static void BuildReport(Transform canvas)
        {
            var root = Panel("Report", canvas, Center, Center, Vector2.zero, Vector2.zero, "#0E1013", 0.94f);
            StretchFull(root.rectTransform);
            var box = Panel("Box", root.transform, Center, Center, Vector2.zero, new Vector2(1500f, 900f), "#1C1F24", 1f);
            Panel("Accent", box.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1500f, 6f), "#FFC629", 1f);

            var summary = Text("Summary", Rect("SummaryRect", box.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -30f), new Vector2(1400f, 190f)), 30, TextAlignmentOptions.Top);
            var steps = Text("Steps", Rect("StepsRect", box.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(50f, -240f), new Vector2(520f, 520f)), 22, TextAlignmentOptions.TopLeft);
            var mistakes = Text("Mistakes", Rect("MistakesRect", box.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-50f, -240f), new Vector2(820f, 520f)), 21, TextAlignmentOptions.TopLeft);
            mistakes.overflowMode = TextOverflowModes.Ellipsis;

            var retry = Button("Retry", box.transform, new Vector2(-200f, -390f), new Vector2(340f, 64f), "Reintentar");
            var menu = Button("Menu", box.transform, new Vector2(200f, -390f), new Vector2(340f, 64f), "Menú principal", "#2E3238");

            var view = canvas.gameObject.AddComponent<ReportView>(); // en el canvas: el panel se desactiva
            Set(view, ("_panel", root.gameObject), ("_summary", summary), ("_steps", steps), ("_mistakes", mistakes),
                ("_retryButton", retry), ("_menuButton", menu));
        }

        private static void BuildFade(Transform canvas)
        {
            var overlay = Panel("Fade", canvas, Center, Center, Vector2.zero, Vector2.zero, "#000000", 0f);
            StretchFull(overlay.rectTransform);
            overlay.transform.SetAsLastSibling();
            var view = overlay.gameObject.AddComponent<FadeView>();
            Set(view, ("_overlay", overlay));
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void MarkStatic(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }
    }
}
