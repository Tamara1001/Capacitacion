using LotoSim.Core;
using LotoSim.Devices;
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
            var (kit, interactor) = BuildPlayer(playerRoot);

            var runner = new GameObject("ProcedureRunner").AddComponent<ProcedureRunner>();
            runner.transform.SetParent(systems, false);
            Set(runner, ("_procedure", procedure), ("_pump", pump));

            BuildUi(ui, kit, interactor);

            MarkStatic(environment);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LotoSim] Escena PumpRoom (greybox) construida");
        }

        // ---------------------------------------------------------------- Ambiente

        private static void BuildLighting(Transform parent)
        {
            var sun = Empty("Directional Light", parent, new Vector3(0f, 6f, 0f), new Vector3(55f, -35f, 0f));
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            light.color = new Color(1f, 0.97f, 0.92f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.65f, 0.7f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.52f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.32f, 0.33f, 0.35f);
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
            var ceiling = Box("Ceiling", room, new Vector3(0f, 4.05f, 0f), new Vector3(12.4f, 0.1f, 9.4f), Wall, false);
            ceiling.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            // Zona demarcada alrededor de la bomba.
            var zone = Empty("SafetyZone", room, new Vector3(0.3f, 0.002f, 1f)).transform;
            Box("Line_N", zone, new Vector3(0f, 0f, 0.95f), new Vector3(5.4f, 0.004f, 0.08f), Yellow, false);
            Box("Line_S", zone, new Vector3(0f, 0f, -0.95f), new Vector3(5.4f, 0.004f, 0.08f), Yellow, false);
            Box("Line_W", zone, new Vector3(-2.7f, 0f, 0f), new Vector3(0.08f, 0.004f, 1.98f), Yellow, false);
            Box("Line_E", zone, new Vector3(2.7f, 0f, 0f), new Vector3(0.08f, 0.004f, 1.98f), Yellow, false);

            // Equipo fuera de servicio de fondo.
            var spare = Empty("P-102_Decor", parent, new Vector3(3.6f, 0f, 3.4f)).transform;
            Box("Base", spare, new Vector3(0f, 0.1f, 0f), new Vector3(2f, 0.2f, 0.8f), Graphite);
            Cylinder("Motor", spare, new Vector3(0.45f, 0.55f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.45f, 0.4f, 0.45f), Steel);
            Cylinder("Casing", spare, new Vector3(-0.4f, 0.55f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.55f, 0.15f, 0.55f), Steel);
            Label("P-102 · fuera de servicio", spare, new Vector3(0f, 1.15f, -0.2f), Vector3.zero, 0.35f, "#2E3238");

            Label("SALA DE BOMBAS · SECTOR AGUA DE PROCESO", parent, new Vector3(1.5f, 3.3f, 4.48f), Vector3.zero, 2.2f, "#2E3238", 8f);
        }

        // ---------------------------------------------------------------- EPP

        private static void BuildPpe(Transform parent)
        {
            var ppe = Empty("PPE", parent, Vector3.zero).transform;
            Box("Locker", ppe, new Vector3(-5.7f, 1f, -3.3f), new Vector3(0.7f, 2f, 0.9f), Worker);
            Box("Table", ppe, new Vector3(-5.55f, 0.45f, -1.9f), new Vector3(0.75f, 0.9f, 1.7f), Graphite);
            Label("LOCKER EPP", ppe, new Vector3(-5.98f, 1.7f, -1.9f), new Vector3(0f, -90f, 0f), 0.45f, "#2E3238");

            float z = -2.55f;
            PpeItem(ppe, "PPE-HELMET", "Casco de seguridad", "ppe.helmet", null, "Casco colocado.", ref z,
                t => Sphere("Mesh", t, Vector3.zero, new Vector3(0.24f, 0.15f, 0.24f), Yellow));
            PpeItem(ppe, "PPE-CAP", "Gorra", null, "ppe.wrong.cap",
                "Una gorra no protege contra impactos: no es EPP adecuado.", ref z,
                t => Sphere("Mesh", t, Vector3.zero, new Vector3(0.2f, 0.09f, 0.2f), Blue));
            PpeItem(ppe, "PPE-GLASSES", "Anteojos de seguridad", "ppe.glasses", null, "Anteojos colocados.", ref z,
                t => Box("Mesh", t, Vector3.zero, new Vector3(0.06f, 0.04f, 0.17f), Dark));
            PpeItem(ppe, "PPE-CLOTH-GLOVES", "Guantes de tela", null, "ppe.wrong.cloth-gloves",
                "Los guantes de tela no aíslan: necesitás guantes dieléctricos.", ref z,
                t => Box("Mesh", t, Vector3.zero, new Vector3(0.12f, 0.05f, 0.17f), White));
            PpeItem(ppe, "PPE-GLOVES", "Guantes dieléctricos", "ppe.gloves", null, "Guantes dieléctricos colocados.", ref z,
                t => Box("Mesh", t, Vector3.zero, new Vector3(0.12f, 0.05f, 0.17f), Orange));
            PpeItem(ppe, "PPE-HEADPHONES", "Auriculares de música", null, "ppe.wrong.headphones",
                "Los auriculares de música te aíslan de alarmas y avisos: no son EPP.", ref z,
                t => Box("Mesh", t, Vector3.zero, new Vector3(0.08f, 0.14f, 0.18f), Red));
        }

        private delegate GameObject MeshFactory(Transform parent);

        private static void PpeItem(Transform parent, string id, string name, string flag, string wrongSignal,
            string toast, ref float z, MeshFactory mesh)
        {
            var root = Empty(id, parent, new Vector3(-5.55f, 0.96f, z));
            mesh(root.transform);
            var interactable = root.AddComponent<SignalInteractable>();
            bool correct = flag != null;
            Set(interactable,
                ("_prompt", $"Tomar: {name}"),
                ("_flag", flag ?? string.Empty),
                ("_signal", wrongSignal ?? string.Empty),
                ("_toast", toast),
                ("_toastKind", correct ? ToastKind.Success : ToastKind.Warning),
                ("_deactivateOnUse", true));
            z += 0.26f;
        }

        // ---------------------------------------------------------------- Estación LOTO

        private static void BuildLotoStation(Transform parent)
        {
            var station = Empty("LOTO-STATION", parent, new Vector3(-5.96f, 1.4f, 1.4f));
            station.AddComponent<LotoStation>();
            var t = station.transform;
            Box("Board", t, Vector3.zero, new Vector3(0.06f, 1.1f, 1.3f), Orange);
            for (int i = 0; i < 3; i++)
            {
                float z = -0.35f + i * 0.35f;
                Box($"Padlock_{i}", t, new Vector3(0.05f, 0.28f, z), new Vector3(0.04f, 0.08f, 0.06f), Red, false);
                Box($"Tag_{i}", t, new Vector3(0.04f, 0.02f, z), new Vector3(0.01f, 0.16f, 0.09f), White, false);
            }
            Box("Lockout_0", t, new Vector3(0.06f, -0.3f, -0.18f), new Vector3(0.07f, 0.12f, 0.14f), Red, false);
            Box("Lockout_1", t, new Vector3(0.06f, -0.3f, 0.18f), new Vector3(0.07f, 0.12f, 0.14f), Red, false);
            Label("ESTACIÓN DE BLOQUEO", t, new Vector3(0.05f, 0.7f, 0f), new Vector3(0f, -90f, 0f), 0.4f, "#2E3238", 3f);

            var order = Empty("WORK-ORDER", parent, new Vector3(-5.97f, 1.4f, 0.35f));
            Box("Clipboard", order.transform, Vector3.zero, new Vector3(0.03f, 0.34f, 0.25f), White);
            Label("OT-0427", order.transform, new Vector3(0.02f, 0.08f, 0f), new Vector3(0f, -90f, 0f), 0.22f, PlateText, 0.3f);
            var source = order.AddComponent<DialogueSource>();
            Set(source,
                ("_prompt", "Leer la orden de trabajo OT-0427"),
                ("_fallback", ProcedureDataBuilder.LoadDialogue(ProcedureDataBuilder.DialogueWorkOrder)));
        }

        // ---------------------------------------------------------------- Tablero eléctrico

        private static DisconnectSwitch BuildElectricalPanel(Transform parent)
        {
            var panel = Empty("ELECTRICAL-PANEL", parent, new Vector3(-3f, 0f, 4.42f)).transform;
            Box("Cabinet", panel, new Vector3(0f, 1.3f, 0.05f), new Vector3(1.8f, 1.5f, 0.22f), Graphite);
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
            var root = Empty("DS-101", panel, new Vector3(x, 1.22f, -0.12f));
            var t = root.transform;
            Box("Body", t, Vector3.zero, new Vector3(0.3f, 0.42f, 0.12f), Steel);
            var pivot = Empty("LeverPivot", t, new Vector3(0f, 0f, -0.07f));
            Box("Lever", pivot.transform, new Vector3(0f, 0.11f, 0f), new Vector3(0.05f, 0.22f, 0.05f), Red, false);

            var padlock = Box("LOTO_Padlock", t, new Vector3(0.1f, -0.13f, -0.09f), new Vector3(0.05f, 0.07f, 0.04f), Red, false);
            var tag = Box("LOTO_Tag", t, new Vector3(0.1f, -0.24f, -0.09f), new Vector3(0.07f, 0.11f, 0.006f), White, false);

            var device = root.AddComponent<DisconnectSwitch>();
            Set(device,
                ("_deviceId", "DS-101"),
                ("_displayName", "el seccionador DS-101"),
                ("_handle", pivot.transform),
                ("_activeEuler", Vector3.zero),
                ("_inactiveEuler", new Vector3(0f, 0f, -150f)),
                ("_startsActive", true));
            var socket = root.AddComponent<LotoSocket>();
            Set(socket, ("_requiresLockout", false), ("_padlockVisual", padlock), ("_tagVisual", tag));
            return device;
        }

        private static void DecorSwitch(Transform panel, float x, string id, string load)
        {
            var root = Empty($"{id}_Decor", panel, new Vector3(x, 1.22f, -0.12f)).transform;
            Box("Body", root, Vector3.zero, new Vector3(0.3f, 0.42f, 0.12f), Steel);
            Box("Lever", root, new Vector3(0f, 0.11f, -0.07f), new Vector3(0.05f, 0.22f, 0.05f), Red, false);
            IdPlate(panel, $"PLATE-{id}", new Vector3(x, 1.6f, -0.07f), Vector3.zero, $"{id}\n{load}",
                null, $"identify.wrong.{id}", $"{id} alimenta a {load}, no a la P-101.");
        }

        private static void IdPlate(Transform parent, string id, Vector3 pos, Vector3 euler, string text,
            string flag, string wrongSignal, string toast)
        {
            var plate = Empty(id, parent, pos, euler);
            Box("Plate", plate.transform, Vector3.zero, new Vector3(0.42f, 0.14f, 0.015f), White);
            Label(text, plate.transform, new Vector3(0f, 0f, -0.01f), Vector3.zero, 0.34f, PlateText, 0.42f);
            var interactable = plate.AddComponent<SignalInteractable>();
            Set(interactable,
                ("_prompt", $"Identificar fuente: {text.Replace('\n', ' ')}"),
                ("_flag", flag ?? string.Empty),
                ("_signal", wrongSignal ?? string.Empty),
                ("_toast", toast),
                ("_toastKind", flag != null ? ToastKind.Success : ToastKind.Warning),
                ("_deactivateOnUse", false));
        }

        // ---------------------------------------------------------------- Bomba P-101

        private static PumpSystem BuildPump(Transform parent, DisconnectSwitch disconnect)
        {
            var group = Empty("P-101_Group", parent, Vector3.zero).transform;

            Box("Skid", group, new Vector3(1f, 0.1f, 1f), new Vector3(2.6f, 0.2f, 0.9f), Graphite);
            Cylinder("Motor", group, new Vector3(1.8f, 0.6f, 1f), new Vector3(0f, 0f, 90f), new Vector3(0.5f, 0.45f, 0.5f), PumpBlue);
            Cylinder("FanCover", group, new Vector3(2.3f, 0.6f, 1f), new Vector3(0f, 0f, 90f), new Vector3(0.44f, 0.05f, 0.44f), Graphite);
            var coupling = Empty("Coupling", group, new Vector3(1.22f, 0.6f, 1f), new Vector3(0f, 0f, 90f));
            Cylinder("Hub", coupling.transform, Vector3.zero, Vector3.zero, new Vector3(0.2f, 0.08f, 0.2f), Steel, false);
            Box("Marker", coupling.transform, new Vector3(0.09f, 0f, 0f), new Vector3(0.04f, 0.17f, 0.04f), Yellow, false);
            Label("P-101", group, new Vector3(1.8f, 1.05f, 0.75f), Vector3.zero, 1.2f, "#2E3238");

            // Carcasa: dispara el mantenimiento.
            var casing = Empty("P-101", group, new Vector3(0.6f, 0.6f, 1f));
            Cylinder("Volute", casing.transform, Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(0.62f, 0.17f, 0.62f), PumpBlue);
            Box("Pedestal", group, new Vector3(0.6f, 0.3f, 1f), new Vector3(0.3f, 0.2f, 0.4f), PumpBlue);

            // Succión (V-101) hacia el oeste y luego al techo.
            Cylinder("SuctionPipe", group, new Vector3(-0.885f, 0.6f, 1f), new Vector3(0f, 0f, 90f), new Vector3(0.16f, 1.315f, 0.16f), PipeGreen);
            Sphere("SuctionElbow", group, new Vector3(-2.2f, 0.6f, 1f), Vector3.one * 0.2f, PipeGreen);
            Cylinder("SuctionRiser", group, new Vector3(-2.2f, 2.3f, 1f), Vector3.zero, new Vector3(0.16f, 1.7f, 0.16f), PipeGreen);
            var suction = LeverValve(group, "V-101", "la válvula de succión V-101", new Vector3(-0.6f, 0.6f, 1f),
                handleOffset: new Vector3(0f, 0.16f, 0f), leverLocal: new Vector3(0.14f, 0f, 0f), leverSize: new Vector3(0.3f, 0.03f, 0.05f),
                closedEuler: new Vector3(0f, 90f, 0f),
                lockoutPos: new Vector3(0f, 0.16f, 0f), padlockPos: new Vector3(0f, 0.16f, -0.11f), tagPos: new Vector3(0f, 0.03f, -0.16f));
            IdPlate(group, "PLATE-V-101", new Vector3(-0.6f, 0.3f, 0.84f), Vector3.zero, "V-101\nSucción",
                "identify.V-101", null, "Fuente identificada: válvula de succión V-101 (hidráulica).");

            // Descarga (V-102) vertical hacia el techo.
            Cylinder("DischargePipe", group, new Vector3(0.6f, 2.45f, 1f), Vector3.zero, new Vector3(0.14f, 1.55f, 0.14f), PipeGreen);
            var discharge = LeverValve(group, "V-102", "la válvula de descarga V-102", new Vector3(0.6f, 1.5f, 1f),
                handleOffset: new Vector3(0f, 0f, -0.15f), leverLocal: new Vector3(0f, 0.14f, 0f), leverSize: new Vector3(0.05f, 0.3f, 0.03f),
                closedEuler: new Vector3(0f, 0f, 90f),
                lockoutPos: new Vector3(0f, 0f, -0.16f), padlockPos: new Vector3(0.1f, -0.07f, -0.2f), tagPos: new Vector3(0.1f, -0.18f, -0.2f));
            IdPlate(group, "PLATE-V-102", new Vector3(0.98f, 1.5f, 0.88f), Vector3.zero, "V-102\nDescarga",
                "identify.V-102", null, "Fuente identificada: válvula de descarga V-102 (hidráulica).");

            // Manómetro PI-101.
            var gaugeRoot = Empty("PI-101", group, new Vector3(0.6f, 1.12f, 0.86f));
            Cylinder("Stem", group, new Vector3(0.6f, 1.12f, 0.92f), new Vector3(90f, 0f, 0f), new Vector3(0.03f, 0.06f, 0.03f), Steel, false);
            Cylinder("Face", gaugeRoot.transform, Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(0.18f, 0.015f, 0.18f), White);
            var needlePivot = Empty("NeedlePivot", gaugeRoot.transform, new Vector3(0f, 0f, -0.02f));
            Box("Needle", needlePivot.transform, new Vector3(0f, 0.035f, 0f), new Vector3(0.008f, 0.07f, 0.004f), Red, false);
            Label("PI-101", gaugeRoot.transform, new Vector3(0f, -0.13f, 0f), Vector3.zero, 0.22f, "#2E3238", 0.3f);

            // Purga V-103 + balde.
            var bleed = Empty("V-103", group, new Vector3(0.6f, 0.24f, 0.8f));
            Box("Body", bleed.transform, Vector3.zero, new Vector3(0.1f, 0.1f, 0.12f), Steel);
            var bleedHandle = Empty("HandlePivot", bleed.transform, new Vector3(0f, 0f, -0.07f));
            Box("Handle", bleedHandle.transform, Vector3.zero, new Vector3(0.15f, 0.03f, 0.02f), Red, false);
            var bleedValve = bleed.AddComponent<Valve>();
            Set(bleedValve,
                ("_deviceId", "V-103"),
                ("_displayName", "la purga V-103"),
                ("_handle", bleedHandle.transform),
                ("_activeEuler", new Vector3(0f, 0f, 90f)),
                ("_inactiveEuler", Vector3.zero),
                ("_startsActive", false));
            Cylinder("Bucket", group, new Vector3(0.6f, 0.13f, 0.48f), Vector3.zero, new Vector3(0.26f, 0.13f, 0.26f), Steel);
            IdPlate(group, "PLATE-V-103", new Vector3(1.0f, 0.24f, 0.78f), Vector3.zero, "V-103\nPurga",
                "identify.V-103", null, "Fuente identificada: purga V-103 (presión residual).");

            // Botonera local CS-101.
            var station = BuildControlStation(parent);

            // Herramientas que aparecen tras el mantenimiento.
            var wrench = Tool(group, "TOOL-WRENCH", "la llave inglesa", "tool.wrench", new Vector3(1.75f, 0.03f, 0.25f),
                t => Box("Mesh", t, Vector3.zero, new Vector3(0.3f, 0.03f, 0.07f), Steel));
            var screwdriver = Tool(group, "TOOL-SCREWDRIVER", "el destornillador", "tool.screwdriver", new Vector3(-0.4f, 0.03f, 0.3f),
                t => Box("Mesh", t, Vector3.zero, new Vector3(0.22f, 0.03f, 0.03f), Red));

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
                ("_coupling", coupling.transform));
            var gauge = gaugeRoot.AddComponent<PressureGauge>();
            Set(gauge, ("_pump", system), ("_needle", needlePivot.transform));
            return system;
        }

        private static Valve LeverValve(Transform parent, string id, string displayName, Vector3 pos,
            Vector3 handleOffset, Vector3 leverLocal, Vector3 leverSize, Vector3 closedEuler,
            Vector3 lockoutPos, Vector3 padlockPos, Vector3 tagPos)
        {
            var root = Empty(id, parent, pos);
            var t = root.transform;
            Box("Body", t, Vector3.zero, new Vector3(0.26f, 0.26f, 0.26f), Steel);
            var pivot = Empty("HandlePivot", t, handleOffset);
            Box("Lever", pivot.transform, leverLocal, leverSize, Red, false);

            var lockout = Box("LOTO_Lockout", t, lockoutPos, new Vector3(0.14f, 0.12f, 0.14f), Red, false);
            var padlock = Box("LOTO_Padlock", t, padlockPos, new Vector3(0.05f, 0.07f, 0.04f), Red, false);
            var tag = Box("LOTO_Tag", t, tagPos, new Vector3(0.07f, 0.11f, 0.006f), White, false);

            var valve = root.AddComponent<Valve>();
            Set(valve,
                ("_deviceId", id),
                ("_displayName", displayName),
                ("_handle", pivot.transform),
                ("_activeEuler", Vector3.zero),
                ("_inactiveEuler", closedEuler),
                ("_startsActive", true));
            var socket = root.AddComponent<LotoSocket>();
            Set(socket, ("_requiresLockout", true), ("_lockoutVisual", lockout), ("_padlockVisual", padlock), ("_tagVisual", tag));
            return valve;
        }

        private static ControlStation BuildControlStation(Transform parent)
        {
            var root = Empty("CS-101", parent, new Vector3(2.7f, 0f, -0.1f));
            var t = root.transform;
            Box("Pedestal", t, new Vector3(0f, 0.6f, 0f), new Vector3(0.1f, 1.2f, 0.1f), Steel);
            Box("Enclosure", t, new Vector3(0f, 1.3f, 0f), new Vector3(0.34f, 0.36f, 0.16f), Graphite);
            var pilot = Sphere("Pilot", t, new Vector3(0f, 1.43f, -0.085f), Vector3.one * 0.05f, Pilot, false);
            Label("CS-101", t, new Vector3(0f, 1.56f, -0.085f), Vector3.zero, 0.24f, "#2E3238", 0.4f);

            var station = root.AddComponent<ControlStation>();
            Set(station, ("_pilotLight", pilot.GetComponent<Renderer>()));

            ButtonPart(t, station, true, new Vector3(-0.075f, 1.29f, -0.085f), Green, "START");
            ButtonPart(t, station, false, new Vector3(0.075f, 1.29f, -0.085f), Red, "STOP");
            return station;
        }

        private static void ButtonPart(Transform parent, ControlStation station, bool isStart, Vector3 pos, Material mat, string text)
        {
            var root = Empty(isStart ? "StartButton" : "StopButton", parent, pos);
            Cylinder("Cap", root.transform, Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(0.07f, 0.02f, 0.07f), mat);
            Label(text, root.transform, new Vector3(0f, -0.07f, -0.005f), Vector3.zero, 0.16f, "#FFFFFF", 0.15f);
            var button = root.AddComponent<ControlButton>();
            Set(button, ("_station", station), ("_isStart", isStart));
        }

        private static GameObject Tool(Transform parent, string id, string name, string flag, Vector3 pos, MeshFactory mesh)
        {
            var root = Empty(id, parent, pos);
            mesh(root.transform);
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
            var desk = Empty("OperatorDesk", parent, new Vector3(3.9f, 0f, -2.7f)).transform;
            Box("Desk", desk, new Vector3(0f, 0.4f, 0f), new Vector3(0.7f, 0.8f, 1.4f), Graphite);
            Box("Monitor", desk, new Vector3(0.15f, 1.05f, 0f), new Vector3(0.06f, 0.4f, 0.6f), Dark);
            Box("Radio", desk, new Vector3(-0.15f, 0.88f, 0.45f), new Vector3(0.08f, 0.16f, 0.05f), Dark);

            var root = Empty("OPERATOR", parent, new Vector3(4.65f, 0f, -2.7f));
            Capsule("Body", root.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.5f, 0.9f, 0.5f), Worker);
            Sphere("Helmet", root.transform, new Vector3(0f, 1.82f, 0f), new Vector3(0.3f, 0.16f, 0.3f), Yellow, false);
            Label("Operador", root.transform, new Vector3(0f, 2.15f, 0f), new Vector3(0f, 90f, 0f), 0.4f, "#2E3238");

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

        private static (LotoKit, DesktopInteractor) BuildPlayer(Transform parent)
        {
            var player = Empty("Player", parent, new Vector3(-4.3f, 0f, -3.7f));
            player.layer = 2; // Ignore Raycast: el raycast de interacción no se pega al jugador.
            var controller = player.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.3f;
            player.AddComponent<FirstPersonMotor>();
            var kit = player.AddComponent<LotoKit>();

            var cameraGo = Empty("PlayerCamera", player.transform, new Vector3(0f, 1.65f, 0f));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.fieldOfView = 70f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 60f;
            cameraGo.AddComponent<AudioListener>();

            var interactor = cameraGo.AddComponent<DesktopInteractor>();
            Set(interactor, ("_camera", camera), ("_kit", kit));

            var look = player.AddComponent<DesktopLook>();
            Set(look, ("_body", player.transform), ("_pitchPivot", cameraGo.transform));
            return (kit, interactor);
        }

        // ---------------------------------------------------------------- UI

        private static void BuildUi(Transform parent, LotoKit kit, DesktopInteractor interactor)
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(parent, false);

            var canvasGo = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(parent, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var canvas = canvasGo.transform;

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

            // Centro de pantalla.
            var crosshair = Panel("Crosshair", canvas, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f), "#FFFFFF", 0.9f);
            crosshair.raycastTarget = false;
            var promptRect = Rect("Prompt", canvas, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(1200f, 60f));
            var prompt = Text("Text", promptRect, 26, TextAlignmentOptions.Top);
            var hintRect = Rect("Hint", canvas, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1200f, 60f));
            var hint = Text("Text", hintRect, 32, TextAlignmentOptions.Center,
                "<mark=#1C1F24CC padding=\"24,24,10,10\">Hacé click para controlar la cámara</mark>");

            // Kit.
            var kitPanel = Panel("KitBar", canvas, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1300f, 56f));
            var kitText = Text("Text", kitPanel.transform, 22, TextAlignmentOptions.Center);

            // Avisos.
            var toastPanel = Panel("Toast", canvas, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(900f, 72f));
            var toastText = Text("Text", toastPanel.transform, 24, TextAlignmentOptions.Center);
            toastText.margin = new Vector4(16f, 8f, 16f, 8f);

            // Diálogo.
            var dialoguePanel = Panel("Dialogue", canvas, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(1100f, 340f));
            var dialogueText = Text("Text", dialoguePanel.transform, 24, TextAlignmentOptions.TopLeft);
            dialogueText.margin = new Vector4(28f, 24f, 28f, 24f);

            // Pausa.
            var pause = Panel("Pause", canvas, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "#000000", 0.6f);
            StretchFull(pause.rectTransform);
            var pauseBox = Panel("Box", pause.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 340f));
            var pauseTitle = Text("Title", pauseBox.transform, 40, TextAlignmentOptions.Top, "Pausa");
            pauseTitle.margin = new Vector4(0f, 30f, 0f, 0f);
            var resume = Button("Resume", pauseBox.transform, new Vector2(0f, 0f), new Vector2(320f, 64f), "Continuar");
            var restart = Button("Restart", pauseBox.transform, new Vector2(0f, -90f), new Vector2(320f, 64f), "Reiniciar");

            // Fin.
            var complete = Panel("Complete", canvas, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "#000000", 0.6f);
            StretchFull(complete.rectTransform);
            var completeBox = Panel("Box", complete.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 380f));
            var completeText = Text("Text", completeBox.transform, 28, TextAlignmentOptions.Top);
            completeText.margin = new Vector4(30f, 40f, 30f, 120f);
            var completeRestart = Button("Restart", completeBox.transform, new Vector2(0f, -120f), new Vector2(320f, 64f), "Reiniciar");

            var hud = canvasGo.AddComponent<HudView>();
            Set(hud,
                ("_interactor", interactor), ("_kit", kit),
                ("_stepTitle", stepTitle), ("_stepBody", stepBody), ("_taskList", tasks),
                ("_prompt", prompt), ("_hint", hint), ("_crosshair", crosshair),
                ("_kitBar", kitText), ("_toastPanel", toastPanel.gameObject), ("_toastText", toastText),
                ("_dialoguePanel", dialoguePanel.gameObject), ("_dialogueText", dialogueText),
                ("_pausePanel", pause.gameObject), ("_resumeButton", resume), ("_restartButton", restart),
                ("_completePanel", complete.gameObject), ("_completeText", completeText),
                ("_completeRestartButton", completeRestart));
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
