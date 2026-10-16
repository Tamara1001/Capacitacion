using System.Collections.Generic;
using LotoSim.Core;
using LotoSim.Evaluation;
using LotoSim.Interaction;
using LotoSim.Procedure;
using UnityEditor;
using UnityEngine;
using C = LotoSim.Procedure.TaskCondition;

namespace LotoSim.Editor
{
    /// <summary>
    /// Genera (o regenera) los assets del procedimiento LOTO de la P-101 a partir del guion
    /// de .agents/PROCEDURE.md. Idempotente: conserva los GUIDs de los assets existentes.
    /// </summary>
    public static class ProcedureDataBuilder
    {
        public const string ProcedurePath = "Assets/_Project/Data/Procedures/PROC_LOTO_P101.asset";
        private const string StepsFolder = "Assets/_Project/Data/Steps/";
        private const string DialoguesFolder = "Assets/_Project/Data/Dialogues/";

        public const string DialogueWorkOrder = "DLG_WorkOrder";
        public const string DialogueNotifyStop = "DLG_NotifyStop";
        public const string DialogueNotifyRestore = "DLG_NotifyRestore";
        public const string DialogueOperatorIdle = "DLG_OperatorIdle";

        [MenuItem("LotoSim/Datos/Regenerar procedimiento P-101", priority = 20)]
        public static ProcedureDefinition Build()
        {
            BuildDialogues();
            BuildMistakes();
            var steps = new List<StepDefinition>
            {
                Step("S0_PPE", "S0", "Equipo de protección personal",
                    "Antes de intervenir, equipate en el locker con el EPP adecuado para trabajar sobre equipos con riesgo eléctrico.",
                    false,
                    Task("S0.helmet", "Colocarse el casco de seguridad", "PPE-HELMET", C.Flag("ppe.helmet")),
                    Task("S0.glasses", "Colocarse los anteojos de seguridad", "PPE-GLASSES", C.Flag("ppe.glasses")),
                    Task("S0.gloves", "Colocarse los guantes dieléctricos", "PPE-GLOVES", C.Flag("ppe.gloves"))),

                Step("S1_Prepare", "S1", "Preparar la intervención",
                    "Leé la orden de trabajo, identificá cada fuente de energía de la P-101 en su rótulo y tomá el kit LOTO.",
                    false,
                    Task("S1.order", "Leer la orden de trabajo OT-0427", "WORK-ORDER", C.Flag("ot.read")),
                    Task("S1.ds101", "Identificar el seccionador DS-101 en el tablero", "PLATE-DS-101", C.Flag("identify.DS-101")),
                    Task("S1.valves", "Identificar las válvulas V-101 y V-102", "PLATE-V-101,PLATE-V-102",
                        C.Flag("identify.V-101"), C.Flag("identify.V-102")),
                    Task("S1.bleed", "Identificar la purga V-103", "PLATE-V-103", C.Flag("identify.V-103")),
                    Task("S1.kit", "Tomar el kit LOTO de la estación de bloqueo", "LOTO-STATION", C.Flag(LotoStation.KitCollectedFlag))),

                Step("S2_Notify", "S2", "Comunicar y detener",
                    "Avisá al operador que la bomba quedará fuera de servicio y detenela con su mando normal (STOP).",
                    true,
                    Task("S2.notify", "Notificar al operador", "OPERATOR", C.Flag("notify.stop")),
                    Task("S2.stop", "Detener la bomba con STOP en la botonera CS-101", "CS-101", C.PumpStopped())),

                Step("S3_Isolate", "S3", "Aislar las fuentes de energía",
                    "Bajá el seccionador a OFF y cerrá las válvulas: primero la descarga, después la succión.",
                    false,
                    Task("S3.ds101", "Bajar el seccionador DS-101 a OFF", "DS-101", C.DeviceIs("DS-101", DeviceState.Off)),
                    Task("S3.v102", "Cerrar la válvula de descarga V-102", "V-102", C.DeviceIs("V-102", DeviceState.Closed)),
                    Task("S3.v101", "Cerrar la válvula de succión V-101", "V-101", C.DeviceIs("V-101", DeviceState.Closed))),

                Step("S4_Lockout", "S4", "Aplicar bloqueo y etiquetado",
                    "Elegí el dispositivo del kit con 1, 2 o 3 y hacé click en el punto de aislamiento. En las válvulas: bloqueo, candado y etiqueta.",
                    false,
                    Task("S4.ds101", "DS-101: candado y etiqueta", "DS-101",
                        C.SocketHas("DS-101", LotoItemType.Padlock), C.SocketHas("DS-101", LotoItemType.Tag)),
                    Task("S4.v102", "V-102: bloqueo de válvula, candado y etiqueta", "V-102",
                        C.SocketHas("V-102", LotoItemType.ValveLockout), C.SocketHas("V-102", LotoItemType.Padlock),
                        C.SocketHas("V-102", LotoItemType.Tag)),
                    Task("S4.v101", "V-101: bloqueo de válvula, candado y etiqueta", "V-101",
                        C.SocketHas("V-101", LotoItemType.ValveLockout), C.SocketHas("V-101", LotoItemType.Padlock),
                        C.SocketHas("V-101", LotoItemType.Tag))),

                Step("S5_Stored", "S5", "Controlar la energía almacenada",
                    "Quedó presión atrapada en la carcasa. Abrí la purga V-103 y esperá a que el manómetro marque 0 bar.",
                    true,
                    Task("S5.bleed", "Abrir la purga V-103", "V-103", C.DeviceIs("V-103", DeviceState.Open)),
                    Task("S5.zero", "Esperar a que la presión llegue a 0 bar", "PI-101", C.PumpDepressurized())),

                Step("S6_Verify", "S6", "Verificar el aislamiento",
                    "Intentá arrancar con START: la bomba no debe arrancar. Volvé a STOP, confirmá 0 bar y realizá el trabajo.",
                    true,
                    Task("S6.tryout", "Prueba de arranque: presionar START", "CS-101", C.Signal(ControlStationSignals.Start)),
                    Task("S6.stop", "Volver el mando a STOP", "CS-101", C.Signal(ControlStationSignals.Stop)),
                    Task("S6.gauge", "Leer el manómetro PI-101 en 0 bar", "PI-101", C.Signal("gauge.read.zero")),
                    Task("S6.work", "Realizar el cambio de sello mecánico", "P-101", C.Flag("maintenance.done"))),

                Step("S7_Restore", "S7", "Finalizar y restablecer",
                    "Dejá el área en orden, avisá al operador, retirá tus bloqueos y restablecé la bomba de forma controlada.",
                    true,
                    Task("S7.tools", "Recoger las herramientas del área", "TOOL-WRENCH,TOOL-SCREWDRIVER",
                        C.Flag("tool.wrench"), C.Flag("tool.screwdriver")),
                    Task("S7.bleed", "Cerrar la purga V-103", "V-103", C.DeviceIs("V-103", DeviceState.Closed)),
                    Task("S7.notify", "Avisar al operador que se restablece el equipo", "OPERATOR", C.Flag("notify.restore")),
                    Task("S7.remove", "Retirar etiquetas, candados y bloqueos", "DS-101",
                        C.SocketEmpty("DS-101"), C.SocketEmpty("V-101"), C.SocketEmpty("V-102")),
                    Task("S7.kit", "Devolver el kit a la estación LOTO", "LOTO-STATION", C.Flag(LotoStation.KitReturnedFlag)),
                    Task("S7.v101", "Abrir la válvula de succión V-101", "V-101", C.DeviceIs("V-101", DeviceState.Open)),
                    Task("S7.v102", "Abrir la válvula de descarga V-102", "V-102", C.DeviceIs("V-102", DeviceState.Open)),
                    Task("S7.ds101", "Subir el seccionador DS-101 a ON", "DS-101", C.DeviceIs("DS-101", DeviceState.On)),
                    Task("S7.start", "Arrancar la bomba con START", "CS-101", C.PumpRunning()))
            };

            var procedure = LoadOrCreate<ProcedureDefinition>(ProcedurePath);
            procedure.EditorInitialize("LOTO — Bomba centrífuga P-101", steps);
            EditorUtility.SetDirty(procedure);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LotoSim] Procedimiento regenerado: {steps.Count} pasos");
            return procedure;
        }

        public static DialogueDefinition LoadDialogue(string name) =>
            AssetDatabase.LoadAssetAtPath<DialogueDefinition>($"{DialoguesFolder}{name}.asset");

        private static void BuildDialogues()
        {
            Dialogue(DialogueWorkOrder, "Orden de trabajo OT-0427",
                "Equipo: bomba centrífuga P-101 (agua de proceso)\n" +
                "Trabajo: reemplazo del sello mecánico\n" +
                "Energías: eléctrica (seccionador DS-101), hidráulica (válvulas V-101 succión y V-102 descarga) " +
                "y presión residual (purga V-103, manómetro PI-101)\n" +
                "Ejecuta: técnico de mantenimiento autorizado",
                new DialogueOption("Entendido", true, "ot.read", "Orden de trabajo leída."));

            Dialogue(DialogueNotifyStop, "Operador",
                "Hola, ¿necesitás algo de la P-101?",
                new DialogueOption("Voy a revisar la bomba, vos seguí trabajando normal.", false, null,
                    "No informaste que el equipo va a quedar fuera de servicio."),
                new DialogueOption("Voy a bloquear la P-101 para cambiar el sello mecánico. Queda fuera de servicio hasta que te avise.",
                    true, "notify.stop", "Operador notificado. Ahora detené la bomba con su mando normal."),
                new DialogueOption("Apagá todo el sector, voy a trabajar.", false, null,
                    "Mensaje impreciso: hay que indicar qué equipo se interviene y por qué."));

            Dialogue(DialogueNotifyRestore, "Operador",
                "¿Terminaste con la P-101?",
                new DialogueOption("Terminé. Voy a retirar los bloqueos y arrancar la bomba: mantenete alejado del equipo.",
                    true, "notify.restore", "Operador avisado. Podés retirar los bloqueos."),
                new DialogueOption("Ya está, arrancala vos cuando quieras.", false, null,
                    "Quien colocó el bloqueo debe retirarlo y restablecer el equipo de forma controlada."),
                new DialogueOption("No hace falta avisar, ya termino.", false, null,
                    "Hay que avisar al personal afectado antes de restablecer la energía."));

            Dialogue(DialogueOperatorIdle, "Operador",
                "Todo tranquilo por acá. Avisame si vas a intervenir algún equipo.",
                new DialogueOption("Dale, gracias.", true, null, null));
        }

        private static void Dialogue(string name, string speaker, string line, params DialogueOption[] options)
        {
            var dialogue = LoadOrCreate<DialogueDefinition>($"{DialoguesFolder}{name}.asset");
            dialogue.EditorInitialize(speaker, line, new List<DialogueOption>(options));
            EditorUtility.SetDirty(dialogue);
        }

        private static StepDefinition Step(string file, string id, string title, string description, bool ordered,
            params TaskDefinition[] tasks)
        {
            var step = LoadOrCreate<StepDefinition>($"{StepsFolder}STEP_{file}.asset");
            step.EditorInitialize(id, title, description, ordered, new List<TaskDefinition>(tasks));
            EditorUtility.SetDirty(step);
            return step;
        }

        /// <param name="highlightIds">Ids a resaltar separados por coma.</param>
        private static TaskDefinition Task(string id, string description, string highlightIds, params TaskCondition[] conditions) =>
            new(id, description, conditions, highlightIds.Split(','));

        public const string MistakeCatalogPath = "Assets/_Project/Data/Mistakes/MISTAKES_P101.asset";

        [MenuItem("LotoSim/Datos/Regenerar catálogo de errores", priority = 21)]
        public static MistakeCatalog BuildMistakes()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Data/Mistakes"))
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Mistakes");

            const MistakeSeverity minor = MistakeSeverity.Minor;
            const MistakeSeverity critical = MistakeSeverity.Critical;
            var mistakes = new List<MistakeDefinition>
            {
                new("PPE_CAP", minor, "EPP inadecuado: gorra",
                    "Una gorra no protege contra impactos. El casco de seguridad es obligatorio.", "ppe.wrong.cap"),
                new("PPE_CLOTH_GLOVES", minor, "EPP inadecuado: guantes de tela",
                    "Los guantes de tela no aíslan. Frente a riesgo eléctrico se usan guantes dieléctricos.", "ppe.wrong.cloth-gloves"),
                new("PPE_HEADPHONES", minor, "Distracción: auriculares de música",
                    "Los auriculares impiden escuchar alarmas y avisos. No son elementos de protección.", "ppe.wrong.headphones"),
                new(MistakeIds.PpeMissing, minor, "Intervención sin EPP completo",
                    "Antes de intervenir hay que colocarse casco, anteojos de seguridad y guantes dieléctricos."),
                new("ID_WRONG_DS100", minor, "Fuente mal identificada (DS-100)",
                    "DS-100 alimenta al compresor C-201, no a la P-101. Verificá siempre el rótulo del equipo.", "identify.wrong.DS-100"),
                new("ID_WRONG_DS102", minor, "Fuente mal identificada (DS-102)",
                    "DS-102 alimenta a la ventilación, no a la P-101. Verificá siempre el rótulo del equipo.", "identify.wrong.DS-102"),
                new("NOTIFY_WRONG", minor, "Comunicación incompleta al operador",
                    "Hay que avisar qué equipo se interviene, por qué y que quedará fuera de servicio.", $"dialogue.wrong.{DialogueNotifyStop}"),
                new("NOTIFY_RESTORE_WRONG", minor, "Restablecimiento mal comunicado",
                    "Quien colocó el bloqueo avisa al personal y restablece el equipo de forma controlada.", $"dialogue.wrong.{DialogueNotifyRestore}"),
                new(MistakeIds.ValveCloseOrder, minor, "Orden de cierre de válvulas incorrecto",
                    "Primero se cierra la descarga (V-102) y después la succión (V-101)."),
                new(MistakeIds.TagWithoutLock, minor, "Etiqueta sin candado",
                    "La etiqueta sola no impide operar el equipo: primero se coloca el candado y después la etiqueta."),
                new(MistakeIds.RestoreNoNotify, minor, "Bloqueos retirados sin avisar",
                    "Antes de retirar los bloqueos hay que avisar al personal afectado que el equipo se va a restablecer."),
                new(MistakeIds.RestoreValveOrder, minor, "Orden de apertura de válvulas incorrecto",
                    "Al restablecer se abre primero la succión (V-101) y después la descarga (V-102)."),
                new(MistakeIds.RestoreTools, minor, "Equipo energizado con herramientas en el área",
                    "Antes de energizar, el área debe quedar libre de herramientas y materiales."),
                new(MistakeIds.RestoreBleedOpen, minor, "Equipo energizado con la purga abierta",
                    "Cerrá la purga V-103 antes de restablecer la energía."),

                new(MistakeIds.SwitchUnderLoad, critical, "Corte del seccionador bajo carga",
                    "El seccionador no se abre con el equipo en marcha: primero se detiene con su mando normal (STOP)."),
                new(MistakeIds.Deadhead, critical, "Válvula cerrada con la bomba en marcha",
                    "Cerrar válvulas con la bomba funcionando la daña y puede romper la línea. Primero detené el equipo."),
                new(MistakeIds.PressurizedBleed, critical, "Purga abierta con la línea presurizada",
                    "Se liberó presión sin aislar: con una válvula abierta o la bomba en marcha la purga descarga sin control."),
                new(MistakeIds.WorkWithoutVerify, critical, "Trabajo sin verificar el aislamiento",
                    "Nunca se interviene el equipo sin bloquear, liberar la energía y verificar con la prueba de arranque y el manómetro."),
                new(MistakeIds.LockRemovedEarly, critical, "Bloqueo retirado antes de terminar el trabajo",
                    "Los candados y bloqueos sólo se retiran cuando el trabajo terminó y el área es segura."),
            };

            var catalog = LoadOrCreate<MistakeCatalog>(MistakeCatalogPath);
            catalog.EditorInitialize(mistakes);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // Alias legibles de las señales de la botonera.
        private static class ControlStationSignals
        {
            public const string Start = Devices.ControlStation.StartSignal;
            public const string Stop = Devices.ControlStation.StopSignal;
        }
    }
}
