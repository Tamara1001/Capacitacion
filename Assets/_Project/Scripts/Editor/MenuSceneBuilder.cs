using LotoSim.Core;
using LotoSim.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using static LotoSim.Editor.BuilderUtils;

namespace LotoSim.Editor
{
    /// <summary>Construye la escena Menu (greybox de UI) de forma idempotente.</summary>
    public static class MenuSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Menu.unity";
        private static readonly Vector2 Center = new(0.5f, 0.5f);

        [MenuItem("LotoSim/Greybox/Construir Menu", priority = 41)]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
                Object.DestroyImmediate(root);

            var cameraGo = new GameObject("MenuCamera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            ColorUtility.TryParseHtmlString("#0E1013", out var background);
            camera.backgroundColor = background;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var canvas = GreyboxSceneBuilder.CreateCanvas(null, "MenuCanvas", 0).transform;

            // Columna izquierda con la marca del simulador.
            var side = Panel("Side", canvas, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(760f, 1080f), "#1C1F24", 1f);
            side.rectTransform.anchorMin = new Vector2(0f, 0f);
            side.rectTransform.anchorMax = new Vector2(0f, 1f);
            side.rectTransform.sizeDelta = new Vector2(760f, 0f);
            var accent = Panel("Accent", side.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(8f, 0f), "#FFC629", 1f);
            accent.rectTransform.anchorMin = new Vector2(1f, 0f);
            accent.rectTransform.anchorMax = new Vector2(1f, 1f);

            Text("Eyebrow", Rect("EyebrowRect", side.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(80f, -150f), new Vector2(620f, 40f)),
                22, TextAlignmentOptions.Left, "<color=#FFC629>CAPACITACIÓN EN SEGURIDAD INDUSTRIAL</color>");
            Text("Title", Rect("TitleRect", side.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(80f, -200f), new Vector2(620f, 160f)),
                72, TextAlignmentOptions.TopLeft, $"<b>{AppInfo.ProductName}</b>");
            Text("Subtitle", Rect("SubtitleRect", side.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(80f, -300f), new Vector2(620f, 80f)),
                28, TextAlignmentOptions.TopLeft, $"<color=#C9CDD2>{AppInfo.Subtitle}</color>");
            Text("Description", Rect("DescriptionRect", side.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(80f, -400f), new Vector2(600f, 260f)),
                22, TextAlignmentOptions.TopLeft,
                "<color=#C9CDD2>Aplicá el procedimiento de control de energías peligrosas sobre una bomba centrífuga " +
                "antes de su mantenimiento: preparar, comunicar, aislar, bloquear y etiquetar, liberar la energía almacenada, " +
                "verificar y restablecer.\n\nBasado en OSHA 29 CFR 1910.147.</color>");
            Text("Footer", Rect("FooterRect", side.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(80f, 50f), new Vector2(620f, 30f)),
                18, TextAlignmentOptions.Left, "<color=#8A9099>Demo v0.2 · WebGL · Mouse y teclado</color>");

            // Columna derecha: nombre y modos.
            var form = Rect("Form", canvas, new Vector2(0.5f, 0.5f), Center, new Vector2(380f, 0f), new Vector2(640f, 700f));
            Text("NameLabel", Rect("NameLabelRect", form, Center, Center, new Vector2(0f, 290f), new Vector2(560f, 36f)),
                24, TextAlignmentOptions.Left, "Nombre del participante (opcional)");
            var nameInput = InputField("NameInput", form, new Vector2(0f, 235f), new Vector2(560f, 60f), "Ej.: Juana Pérez");

            var guided = Button("GuidedButton", form, new Vector2(0f, 120f), new Vector2(560f, 96f),
                "<b>Iniciar · Modo guiado</b>\n<size=70%>Te indica cada paso y explica los errores</size>", "#2D7FF9", 28f);
            var assessment = Button("AssessmentButton", form, new Vector2(0f, 0f), new Vector2(560f, 96f),
                "<b>Iniciar · Modo evaluación</b>\n<size=70%>Sin ayudas: el resultado se informa al final</size>", "#2E3238", 28f);
            var options = Button("OptionsButton", form, new Vector2(-142f, -110f), new Vector2(276f, 60f), "Opciones", "#2E3238");
            var credits = Button("CreditsButton", form, new Vector2(142f, -110f), new Vector2(276f, 60f), "Créditos", "#2E3238");

            // Opciones.
            var optionsPanel = Panel("OptionsPanel", canvas, Center, Center, Vector2.zero, Vector2.zero, "#000000", 0.7f);
            StretchFull(optionsPanel.rectTransform);
            var optionsBox = Panel("Box", optionsPanel.transform, Center, Center, Vector2.zero, new Vector2(560f, 440f), "#1C1F24", 1f);
            Text("Title", optionsBox.transform, 36, TextAlignmentOptions.Top, "<b>Opciones</b>").margin = new Vector4(0f, 28f, 0f, 0f);
            Text("SensLabel", Rect("SensLabelRect", optionsBox.transform, Center, Center, new Vector2(0f, 70f), new Vector2(400f, 30f)),
                22, TextAlignmentOptions.Left, "Sensibilidad del mouse");
            var slider = Slider("Sensitivity", optionsBox.transform, new Vector2(0f, 30f), new Vector2(400f, 24f));
            var invert = Toggle("InvertY", optionsBox.transform, new Vector2(20f, -20f), "Invertir eje vertical");
            Text("VolumeLabel", Rect("VolumeLabelRect", optionsBox.transform, Center, Center, new Vector2(0f, -60f), new Vector2(400f, 30f)),
                22, TextAlignmentOptions.Left, "Volumen");
            var volume = Slider("Volume", optionsBox.transform, new Vector2(0f, -95f), new Vector2(400f, 24f));
            var optionsClose = Button("Close", optionsBox.transform, new Vector2(0f, -160f), new Vector2(300f, 60f), "Listo");

            // Créditos.
            var creditsPanel = Panel("CreditsPanel", canvas, Center, Center, Vector2.zero, Vector2.zero, "#000000", 0.7f);
            StretchFull(creditsPanel.rectTransform);
            var creditsBox = Panel("Box", creditsPanel.transform, Center, Center, Vector2.zero, new Vector2(820f, 560f), "#1C1F24", 1f);
            var creditsText = Text("Text", creditsBox.transform, 22, TextAlignmentOptions.TopLeft,
                "<size=150%><b>Créditos</b></size>\n\n" +
                "<b>Contenido técnico</b>\nOSHA 29 CFR 1910.147 · The control of hazardous energy (lockout/tagout) y su tutorial interactivo.\n\n" +
                "<b>Desarrollo</b>\nDemo desarrollada en Unity 6 (URP). Modelos 3D propios generados en Blender.\n\n" +
                "<b>Recursos de terceros</b>\nTipografía Barlow · The Barlow Project Authors (SIL Open Font License).\n" +
                "Efectos de sonido · Kenney Impact Sounds e Interface Sounds (CC0, kenney.nl).");
            creditsText.margin = new Vector4(36f, 32f, 36f, 100f);
            var creditsClose = Button("Close", creditsBox.transform, new Vector2(0f, -220f), new Vector2(300f, 60f), "Cerrar");

            var controller = canvas.gameObject.AddComponent<MenuController>();
            Set(controller,
                ("_nameInput", nameInput), ("_guidedButton", guided), ("_assessmentButton", assessment),
                ("_optionsButton", options), ("_creditsButton", credits),
                ("_optionsPanel", optionsPanel.gameObject), ("_sensitivitySlider", slider), ("_invertToggle", invert), ("_volumeSlider", volume),
                ("_optionsCloseButton", optionsClose), ("_creditsPanel", creditsPanel.gameObject), ("_creditsCloseButton", creditsClose));

            GreyboxSceneBuilder.AddUiSounds(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LotoSim] Escena Menu construida");
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        [MenuItem("LotoSim/Greybox/Construir todo", priority = 39)]
        public static void BuildAll()
        {
            GreyboxSceneBuilder.Build();
            Build();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/_Project/Scenes/Menu.unity", true),
                new EditorBuildSettingsScene("Assets/_Project/Scenes/PumpRoom.unity", true)
            };
        }
    }
}
