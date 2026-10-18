using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LegionRush.EditorTools
{
    // Encontra textos nas cenas abertas e nos prefabs, sugere um estilo tipográfico e aplica após revisão.
    // Seguro por padrão: em TMP só adiciona LegionStyledText (com Undo). Converter Text/TextMesh legado é opcional,
    // pois troca o componente e quebra referências serializadas a ele.
    public class TypographyMigrationWindow : EditorWindow
    {
        private const string LogPath = "Logs/typography-migration.log";

        private class Entry
        {
            public string Location;
            public string ObjectPath;
            public string Kind;
            public string Preview;
            public LegionTextStyle Suggested;
            public bool Safe;
            public bool Selected;
            public Component Target;   // cenas abertas
            public string PrefabPath;  // prefabs (editados via LoadPrefabContents)
        }

        private readonly List<Entry> entries = new List<Entry>();
        private Vector2 scroll;
        private bool includePrefabs = true;
        private bool allowLegacyConversion;

        [MenuItem("Legion Rush/Typography/Migration Tool")]
        public static void Open() => GetWindow<TypographyMigrationWindow>("Tipografia");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "1. Escanear  2. Revisar estilo sugerido e marcar  3. Aplicar. Textos com LegionStyledText são ignorados. " +
                "A interface do jogo é criada por código e já usa o sistema; esta ferramenta é para cenas e prefabs.",
                MessageType.Info);
            includePrefabs = EditorGUILayout.Toggle("Incluir prefabs", includePrefabs);
            allowLegacyConversion = EditorGUILayout.Toggle(new GUIContent("Converter Text/TextMesh legado",
                "Troca o componente por TextMeshPro. Referências de scripts ao componente antigo precisarão ser refeitas."),
                allowLegacyConversion);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Escanear")) Scan();
                if (GUILayout.Button("Marcar seguros")) foreach (Entry e in entries) e.Selected = e.Safe;
                if (GUILayout.Button("Desmarcar")) foreach (Entry e in entries) e.Selected = false;
                GUI.enabled = entries.Exists(e => e.Selected);
                if (GUILayout.Button("Aplicar marcados")) Apply();
                GUI.enabled = true;
            }

            EditorGUILayout.LabelField($"{entries.Count} textos encontrados");
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (Entry entry in entries)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    entry.Selected = EditorGUILayout.Toggle(entry.Selected, GUILayout.Width(18));
                    EditorGUILayout.LabelField(entry.Kind, GUILayout.Width(110));
                    EditorGUILayout.LabelField(new GUIContent(entry.ObjectPath, entry.Location), GUILayout.MinWidth(160));
                    EditorGUILayout.LabelField("\"" + entry.Preview + "\"", GUILayout.Width(150));
                    entry.Suggested = (LegionTextStyle)EditorGUILayout.EnumPopup(entry.Suggested, GUILayout.Width(110));
                    EditorGUILayout.LabelField(entry.Safe ? "seguro" : "revisar", GUILayout.Width(50));
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void Scan()
        {
            entries.Clear();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects()) Collect(root, scene.path, null);
            }
            if (includePrefabs)
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null) Collect(prefab, path, path);
                }
            }
            Repaint();
        }

        private void Collect(GameObject root, string location, string prefabPath)
        {
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null || component.GetComponent<LegionStyledText>() != null) continue;
                string kind;
                string text;
                float size;
                bool safe;
                switch (component)
                {
                    case TMP_Text tmp:
                        kind = "TextMeshPro";
                        text = tmp.text;
                        size = tmp.fontSize;
                        safe = true;
                        break;
                    case Text legacy:
                        kind = "Text (legado)";
                        text = legacy.text;
                        size = legacy.fontSize;
                        safe = false;
                        break;
                    case TextMesh mesh:
                        kind = "TextMesh (legado)";
                        text = mesh.text;
                        size = mesh.fontSize * Mathf.Max(0.01f, mesh.characterSize) * 10f;
                        safe = false;
                        break;
                    default:
                        continue;
                }
                entries.Add(new Entry
                {
                    Location = location,
                    ObjectPath = PathOf(component.transform, root.transform),
                    Kind = kind,
                    Preview = Shorten(text),
                    Suggested = Suggest(component, text, size),
                    Safe = safe,
                    Selected = safe,
                    Target = prefabPath == null ? component : null,
                    PrefabPath = prefabPath
                });
            }
        }

        // Heurística: nome do objeto, conteúdo e tamanho indicam a função do texto.
        private static LegionTextStyle Suggest(Component component, string text, float size)
        {
            string name = component.gameObject.name.ToLowerInvariant();
            string content = (text ?? string.Empty).Trim();
            if (component.GetComponentInParent<Button>() != null) return LegionTextStyle.PrimaryButton;
            if (name.Contains("dano") || name.Contains("damage") || content.StartsWith("-")) return LegionTextStyle.Negative;
            if (name.Contains("bonus") || name.Contains("reward") || content.StartsWith("+")) return LegionTextStyle.Positive;
            if (IsNumeric(content) || name.Contains("count") || name.Contains("gold") || name.Contains("gem") || name.Contains("hud"))
                return LegionTextStyle.Hud;
            if (content.Contains("VITÓRIA") || content.Contains("VICTORY")) return LegionTextStyle.Epic;
            if (name.Contains("title") || name.Contains("titulo") || name.Contains("header") || size >= 64f) return LegionTextStyle.Title;
            if (name.Contains("desc") || content.Length > 40) return LegionTextStyle.Body;
            return LegionTextStyle.Menu;
        }

        private static bool IsNumeric(string content)
        {
            if (content.Length == 0) return false;
            foreach (char c in content)
            {
                if (!char.IsDigit(c) && "+-x×%/.,: KkMm".IndexOf(c) < 0) return false;
            }
            return true;
        }

        private void Apply()
        {
            var log = new StringBuilder();
            log.AppendLine($"== Migração tipográfica {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==");
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Aplicar estilos tipográficos");
            int group = Undo.GetCurrentGroup();

            var byPrefab = new Dictionary<string, List<Entry>>();
            foreach (Entry entry in entries)
            {
                if (!entry.Selected) continue;
                if (!entry.Safe && !allowLegacyConversion)
                {
                    log.AppendLine($"IGNORADO (legado, conversão desligada): {entry.Location} :: {entry.ObjectPath}");
                    continue;
                }
                if (entry.PrefabPath != null)
                {
                    if (!byPrefab.TryGetValue(entry.PrefabPath, out List<Entry> list)) byPrefab[entry.PrefabPath] = list = new List<Entry>();
                    list.Add(entry);
                }
                else if (entry.Target != null)
                {
                    ApplyTo(entry.Target, entry, true, log);
                    EditorSceneManager.MarkSceneDirty(entry.Target.gameObject.scene);
                }
            }

            foreach (KeyValuePair<string, List<Entry>> pair in byPrefab)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(pair.Key);
                try
                {
                    foreach (Entry entry in pair.Value)
                    {
                        Transform found = string.IsNullOrEmpty(entry.ObjectPath) ? contents.transform : contents.transform.Find(entry.ObjectPath);
                        Component target = found != null ? FindText(found.gameObject) : null;
                        if (target == null)
                        {
                            log.AppendLine($"NÃO ENCONTRADO: {pair.Key} :: {entry.ObjectPath}");
                            continue;
                        }
                        ApplyTo(target, entry, false, log);
                    }
                    PrefabUtility.SaveAsPrefabAsset(contents, pair.Key);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            Undo.CollapseUndoOperations(group);
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
            File.AppendAllText(LogPath, log.ToString());
            Debug.Log("[LegionRush] Migração tipográfica aplicada. Registro: " + LogPath + "\n" + log);
            Scan();
        }

        private static Component FindText(GameObject go) =>
            (Component)go.GetComponent<TMP_Text>() ?? (Component)go.GetComponent<Text>() ?? go.GetComponent<TextMesh>();

        private static void ApplyTo(Component target, Entry entry, bool undo, StringBuilder log)
        {
            TMP_Text tmp = target as TMP_Text;
            if (tmp == null) tmp = ConvertLegacy(target, undo);
            if (tmp == null) return;
            LegionStyledText styled = undo ? Undo.AddComponent<LegionStyledText>(tmp.gameObject) : tmp.gameObject.AddComponent<LegionStyledText>();
            // Preserva a cor existente; o estilo aplica fonte e material.
            styled.ApplyColor = false;
            styled.Style = entry.Suggested;
            log.AppendLine($"OK {entry.Kind} -> {entry.Suggested}: {entry.Location} :: {entry.ObjectPath} \"{entry.Preview}\"");
        }

        // Troca Text/TextMesh por TextMeshPro preservando texto, cor, tamanho e alinhamento.
        private static TMP_Text ConvertLegacy(Component target, bool undo)
        {
            GameObject go = target.gameObject;
            if (target is Text legacy)
            {
                string text = legacy.text;
                Color color = legacy.color;
                int size = legacy.fontSize;
                TextAnchor anchor = legacy.alignment;
                bool bestFit = legacy.resizeTextForBestFit;
                bool raycast = legacy.raycastTarget;
                if (undo) Undo.DestroyObjectImmediate(legacy);
                else DestroyImmediate(legacy, true);
                TextMeshProUGUI tmp = undo ? Undo.AddComponent<TextMeshProUGUI>(go) : go.AddComponent<TextMeshProUGUI>();
                tmp.text = text;
                tmp.color = color;
                tmp.fontSize = size;
                tmp.fontSizeMax = size;
                tmp.enableAutoSizing = bestFit;
                tmp.alignment = LegionTypography.Alignment(anchor);
                tmp.raycastTarget = raycast;
                return tmp;
            }
            if (target is TextMesh mesh)
            {
                string text = mesh.text;
                Color color = mesh.color;
                float size = mesh.fontSize * mesh.characterSize;
                if (undo) Undo.DestroyObjectImmediate(mesh);
                else DestroyImmediate(mesh, true);
                TextMeshPro tmp = undo ? Undo.AddComponent<TextMeshPro>(go) : go.AddComponent<TextMeshPro>();
                tmp.text = text;
                tmp.color = color;
                tmp.fontSize = size;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                return tmp;
            }
            return null;
        }

        private static string PathOf(Transform transform, Transform root)
        {
            if (transform == root) return string.Empty;
            var parts = new List<string>();
            for (Transform t = transform; t != null && t != root; t = t.parent) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }

        private static string Shorten(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            text = text.Replace('\n', ' ');
            return text.Length > 24 ? text.Substring(0, 24) + "…" : text;
        }
    }
}
