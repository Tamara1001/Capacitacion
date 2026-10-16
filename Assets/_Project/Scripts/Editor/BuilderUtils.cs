using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LotoSim.Editor
{
    /// <summary>Helpers para construir escenas greybox por código (primitivas, materiales, labels, UI).</summary>
    internal static class BuilderUtils
    {
        private const string MaterialsFolder = "Assets/_Project/Art/Materials/Greybox/";
        private static readonly Dictionary<string, Material> _materials = new();

        // ---------- Materiales ----------

        public static Material Mat(string name, string hex, float smoothness = 0.25f, bool emissive = false)
        {
            if (_materials.TryGetValue(name, out var cached) && cached != null)
                return cached;

            string path = $"{MaterialsFolder}M_GB_{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                if (!AssetDatabase.IsValidFolder(MaterialsFolder.TrimEnd('/')))
                    AssetDatabase.CreateFolder("Assets/_Project/Art/Materials", "Greybox");
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            ColorUtility.TryParseHtmlString(hex, out var color);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            // Emisión siempre habilitada (negra por defecto) para que la guía pueda pulsarla con MaterialPropertyBlock.
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emissive ? color : Color.black);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
            _materials[name] = material;
            return material;
        }

        /// <summary>Material URP Unlit transparente (fantasmas de la guía).</summary>
        public static Material TransparentMat(string name, string hex, float alpha)
        {
            if (_materials.TryGetValue(name, out var cached) && cached != null)
                return cached;
            string path = $"{MaterialsFolder}M_GB_{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }
            ColorUtility.TryParseHtmlString(hex, out var color);
            color.a = alpha;
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            _materials[name] = material;
            return material;
        }

        // ---------- Modelos (FBX de Blender) ----------

        private const string ModelsFolder = "Assets/_Project/Art/Models/";

        /// <summary>
        /// Instancia SM_{asset} dentro de un contenedor. El FBX llega girado 180° en Y respecto del diseño
        /// en Blender; el contenedor lo corrige para que las coordenadas del diseño valgan directo
        /// (X derecha, Y arriba, frente hacia -Z).
        /// </summary>
        public static GameObject Model(string asset, Transform parent, Vector3 localPos, Vector3 euler = default,
            string name = null, float scale = 1f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsFolder}SM_{asset}.fbx") ??
                         throw new ArgumentException($"No existe el modelo SM_{asset}.fbx (correr Blender/scripts/build_props.py)");
            var wrapper = Empty(name ?? asset, parent, localPos, euler);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, wrapper.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            instance.transform.localScale = Vector3.one * scale;
            return wrapper;
        }

        /// <summary>Agrega un BoxCollider en root que envuelve todos sus renderers.</summary>
        public static BoxCollider FitCollider(GameObject root, float padding = 0.01f)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var bounds = new Bounds();
            bool first = true;
            foreach (var r in renderers)
            {
                if (r is not MeshRenderer || r.GetComponent<TMP_Text>() != null)
                    continue;
                var b = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? b.min.x : b.max.x,
                        (i & 2) == 0 ? b.min.y : b.max.y,
                        (i & 4) == 0 ? b.min.z : b.max.z);
                    var local = root.transform.InverseTransformPoint(corner);
                    if (first)
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        first = false;
                    }
                    else
                    {
                        bounds.Encapsulate(local);
                    }
                }
            }
            var collider = root.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size + Vector3.one * padding;
            return collider;
        }

        /// <summary>Reemplaza todos los materiales de los renderers (ej. botón verde/rojo, guantes de tela).</summary>
        public static void Recolor(GameObject root, Material material)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r.GetComponent<TMP_Text>() != null)
                    continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = material;
                r.sharedMaterials = mats;
            }
        }

        // ---------- Geometría ----------

        public static GameObject Empty(string name, Transform parent, Vector3 localPos, Vector3 euler = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localEulerAngles = euler;
            return go;
        }

        public static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat,
            bool collider = true, Vector3 euler = default) =>
            Primitive(PrimitiveType.Cube, name, parent, localPos, euler, size, mat, collider);

        /// <summary>Cilindro: scale.y es la media altura (la primitiva mide 2 de alto).</summary>
        public static GameObject Cylinder(string name, Transform parent, Vector3 localPos, Vector3 euler, Vector3 scale,
            Material mat, bool collider = true) =>
            Primitive(PrimitiveType.Cylinder, name, parent, localPos, euler, scale, mat, collider);

        public static GameObject Sphere(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat,
            bool collider = true) =>
            Primitive(PrimitiveType.Sphere, name, parent, localPos, Vector3.zero, scale, mat, collider);

        public static GameObject Capsule(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat) =>
            Primitive(PrimitiveType.Capsule, name, parent, localPos, Vector3.zero, scale, mat, true);

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPos,
            Vector3 euler, Vector3 scale, Material mat, bool collider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider)
                Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>Texto 3D. Por defecto se lee mirando hacia +Z (el texto "mira" a -Z).</summary>
        public static TextMeshPro Label(string text, Transform parent, Vector3 localPos, Vector3 euler,
            float fontSize = 0.5f, string hex = "#FFFFFF", float width = 1.5f)
        {
            var go = new GameObject($"Label_{text.Split('\n')[0]}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localEulerAngles = euler;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            ColorUtility.TryParseHtmlString(hex, out var color);
            tmp.color = color;
            tmp.rectTransform.sizeDelta = new Vector2(width, fontSize * 0.4f);
            return tmp;
        }

        // ---------- Serialización ----------

        /// <summary>Asigna campos serializados privados por nombre (con validación).</summary>
        public static void Set(Object target, params (string name, object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var (name, value) in values)
            {
                var prop = so.FindProperty(name) ??
                           throw new ArgumentException($"{target.GetType().Name} no tiene el campo '{name}'");
                switch (value)
                {
                    case null: prop.objectReferenceValue = null; break;
                    case Object o:
                        if (o == null)
                            throw new ArgumentException($"Referencia destruida asignada a '{name}' en {target.GetType().Name}");
                        prop.objectReferenceValue = o;
                        break;
                    case Object[] array:
                        prop.arraySize = array.Length;
                        for (int i = 0; i < array.Length; i++)
                            prop.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
                        break;
                    case string s: prop.stringValue = s; break;
                    case bool b: prop.boolValue = b; break;
                    case int i: prop.intValue = i; break;
                    case float f: prop.floatValue = f; break;
                    case Vector3 v: prop.vector3Value = v; break;
                    case Color c: prop.colorValue = c; break;
                    case Enum e: prop.intValue = Convert.ToInt32(e); break;
                    default: throw new ArgumentException($"Tipo no soportado para '{name}': {value.GetType()}");
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- UI ----------

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform Stretch(string name, Transform parent, float padding = 0f)
        {
            var rect = Rect(name, parent, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            return rect;
        }

        public static Image Panel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
            string hex = "#1C1F24", float alpha = 0.9f)
        {
            var image = Rect(name, parent, anchor, pivot, pos, size).gameObject.AddComponent<Image>();
            ColorUtility.TryParseHtmlString(hex, out var color);
            color.a = alpha;
            image.color = color;
            return image;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, float fontSize, TextAlignmentOptions align,
            string text = "")
        {
            var tmp = Stretch(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.text = text;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Button Button(string name, Transform parent, Vector2 pos, Vector2 size, string label,
            string hex = "#2D7FF9", float fontSize = 26f)
        {
            var image = Panel(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size, hex, 1f);
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            button.colors = colors;
            Text("Label", image.transform, fontSize, TextAlignmentOptions.Center, label);
            return button;
        }

        public static Slider Slider(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var go = DefaultControls.CreateSlider(new DefaultControls.Resources());
            go.name = name;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            TintChild(go.transform, "Background", "#2E3238");
            TintChild(go.transform, "Fill Area/Fill", "#FFC629");
            TintChild(go.transform, "Handle Slide Area/Handle", "#FFFFFF");
            return go.GetComponent<Slider>();
        }

        public static Toggle Toggle(string name, Transform parent, Vector2 pos, string label)
        {
            var go = DefaultControls.CreateToggle(new DefaultControls.Resources());
            go.name = name;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(360f, 36f);
            var background = (RectTransform)go.transform.Find("Background");
            background.sizeDelta = new Vector2(28f, 28f);
            TintChild(go.transform, "Background/Checkmark", "#FFC629");
            Object.DestroyImmediate(go.transform.Find("Label").gameObject);
            var text = Text("Label", go.transform, 22, TextAlignmentOptions.Left, label);
            text.rectTransform.offsetMin = new Vector2(40f, 0f);
            return go.GetComponent<Toggle>();
        }

        public static TMP_InputField InputField(string name, Transform parent, Vector2 pos, Vector2 size, string placeholder)
        {
            var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.name = name;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            ColorUtility.TryParseHtmlString("#2E3238", out var bg);
            go.GetComponent<Image>().color = bg;
            var input = go.GetComponent<TMP_InputField>();
            input.characterLimit = 32;
            input.textComponent.fontSize = 24;
            input.textComponent.color = Color.white;
            var placeholderText = (TextMeshProUGUI)input.placeholder;
            placeholderText.text = placeholder;
            placeholderText.fontSize = 24;
            placeholderText.color = new Color(1f, 1f, 1f, 0.4f);
            return input;
        }

        private static void TintChild(Transform root, string path, string hex)
        {
            var child = root.Find(path);
            if (child == null || !child.TryGetComponent<Image>(out var image))
                return;
            ColorUtility.TryParseHtmlString(hex, out var color);
            image.color = color;
        }
    }
}
