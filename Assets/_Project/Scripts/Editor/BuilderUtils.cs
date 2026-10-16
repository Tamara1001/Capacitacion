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
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(material);
            _materials[name] = material;
            return material;
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

        public static Button Button(string name, Transform parent, Vector2 pos, Vector2 size, string label)
        {
            var image = Panel(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size, "#2D7FF9", 1f);
            var button = image.gameObject.AddComponent<Button>();
            Text("Label", image.transform, 26, TextAlignmentOptions.Center, label);
            return button;
        }
    }
}
