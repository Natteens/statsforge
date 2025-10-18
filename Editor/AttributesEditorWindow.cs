#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatsForge.Editor
{
    public class AttributesEditorWindow : EditorWindow
    {
        private enum Tab { Attributes, Sets }
        private Tab currentTab = Tab.Attributes;
        
        private AttributeDatabase database;
        private Vector2 leftScroll;
        private Vector2 rightScroll;
        
        private string attributeName = "";
        private string attributeCategory = "Core";
        private string attributeSearch = "";
        private string selectedCategory = "All";
        
        private string setName = "";
        private string setSearch = "";
        private AttributeSet editingSet;
        private Dictionary<string, float> setValues = new Dictionary<string, float>();
        private bool isCreatingNewSet = false;

        [MenuItem("Tools/Attributes Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<AttributesEditorWindow>("Attributes Manager");
            window.minSize = new Vector2(1000, 600);
        }

        private void OnEnable()
        {
            LoadDatabase();
        }

        private void LoadDatabase()
        {
            database = Resources.Load<AttributeDatabase>("Attributes/AttributeDatabase");
            if (database == null) CreateDatabase();
            database?.Validate();
        }

        private void CreateDatabase()
        {
            if (!Directory.Exists("Assets/Resources/Attributes"))
                Directory.CreateDirectory("Assets/Resources/Attributes");
            
            database = CreateInstance<AttributeDatabase>();
            AssetDatabase.CreateAsset(database, "Assets/Resources/Attributes/AttributeDatabase.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void OnGUI()
        {
            if (database == null)
            {
                DrawEmptyState();
                return;
            }

            EditorGUILayout.BeginVertical();
            
            DrawHeader();
            DrawTabs();
            
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            
            if (currentTab == Tab.Attributes)
                DrawAttributesLayout();
            else
                DrawSetsLayout();
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("No Database", EditorStyles.largeLabel);
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Create a database to start", EditorStyles.miniLabel);
            GUILayout.Space(20);
            
            if (GUILayout.Button("Create Database", GUILayout.Height(35)))
                CreateDatabase();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Attributes Manager", EditorStyles.largeLabel);
            EditorGUILayout.Space(5);
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(40));
            GUILayout.Space(5);
            
            if (DrawTabButton("Attributes", currentTab == Tab.Attributes))
                currentTab = Tab.Attributes;
            
            if (DrawTabButton("Sets", currentTab == Tab.Sets))
                currentTab = Tab.Sets;
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            var divider = EditorGUILayout.GetControlRect(GUILayout.Height(1));
            EditorGUI.DrawRect(divider, new Color(0.2f, 0.2f, 0.2f));
            GUILayout.Space(5);
        }

        private bool DrawTabButton(string label, bool isActive)
        {
            Color prevColor = GUI.color;
            GUI.color = isActive ? new Color(0.3f, 0.65f, 1f) : new Color(0.6f, 0.6f, 0.6f);
            
            bool clicked = GUILayout.Button(label, GUILayout.Width(100), GUILayout.Height(28));
            
            GUI.color = prevColor;
            return clicked;
        }

        private void DrawAttributesLayout()
        {
            DrawLeftPanel_Attributes();
            GUILayout.Space(10);
            DrawRightPanel_Attributes();
        }

        private void DrawLeftPanel_Attributes()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(380), GUILayout.ExpandHeight(true));
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("Browse Attributes", EditorStyles.boldLabel);
            GUILayout.Space(8);
            
            attributeSearch = EditorGUILayout.TextField("Search", attributeSearch, GUILayout.Height(22));
            
            var categories = new List<string> { "All" };
            categories.AddRange(database.Categories);
            int catIndex = categories.IndexOf(selectedCategory);
            if (catIndex == -1) catIndex = 0;
            catIndex = EditorGUILayout.Popup("Category", catIndex, categories.ToArray());
            selectedCategory = categories[catIndex];
            
            GUILayout.Space(10);
            
            var filtered = GetFilteredAttributes();
            EditorGUILayout.LabelField($"{filtered.Count} attributes", EditorStyles.miniLabel);
            GUILayout.Space(5);
            
            leftScroll = EditorGUILayout.BeginScrollView(leftScroll, GUILayout.ExpandHeight(true));
            
            if (filtered.Count == 0)
            {
                EditorGUILayout.Space(30);
                var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
                EditorGUILayout.LabelField("No attributes", style, GUILayout.Height(30));
            }
            else
            {
                foreach (var attr in filtered)
                {
                    DrawAttributeItem(attr);
                }
            }
            
            EditorGUILayout.EndScrollView();
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        private void DrawAttributeItem(AttributeType attr)
        {
            EditorGUILayout.BeginHorizontal("box", GUILayout.Height(50));
            GUILayout.Space(8);
            
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(attr.Name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(attr.Category, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            
            if (GUILayout.Button("Delete", GUILayout.Width(60), GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Delete", $"Delete '{attr.Name}'?", "Delete", "Cancel"))
                {
                    database.RemoveAttribute(attr.Name);
                    EditorUtility.SetDirty(database);
                }
            }
            
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRightPanel_Attributes()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("Create New Attribute", EditorStyles.boldLabel);
            GUILayout.Space(8);
            
            EditorGUILayout.BeginVertical("box");
            GUILayout.Space(5);
            
            EditorGUILayout.LabelField("Name", EditorStyles.miniLabel);
            attributeName = EditorGUILayout.TextField(attributeName, GUILayout.Height(22));
            
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("Category", EditorStyles.miniLabel);
            attributeCategory = EditorGUILayout.TextField(attributeCategory, GUILayout.Height(22));
            
            GUILayout.Space(15);
            
            GUI.enabled = !string.IsNullOrWhiteSpace(attributeName);
            if (GUILayout.Button("Create Attribute", GUILayout.Height(35)))
            {
                var attr = database.CreateAttribute(attributeName.Trim(), attributeCategory.Trim());
                if (attr != null)
                {
                    EditorUtility.SetDirty(database);
                    attributeName = "";
                    attributeCategory = "Core";
                    attributeSearch = "";
                }
            }
            GUI.enabled = true;
            
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        private void DrawSetsLayout()
        {
            if (editingSet != null || isCreatingNewSet)
            {
                DrawLeftPanel_SetsList();
                GUILayout.Space(10);
                DrawRightPanel_SetEditor();
            }
            else
            {
                DrawLeftPanel_SetsList();
                GUILayout.Space(10);
                DrawRightPanel_SetCreate();
            }
        }

        private void DrawLeftPanel_SetsList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(380), GUILayout.ExpandHeight(true));
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("Attribute Sets", EditorStyles.boldLabel);
            GUILayout.Space(8);
            
            setSearch = EditorGUILayout.TextField("Search", setSearch, GUILayout.Height(22));
            
            GUILayout.Space(10);
            
            var sets = GetFilteredSets();
            EditorGUILayout.LabelField($"{sets.Count} sets", EditorStyles.miniLabel);
            GUILayout.Space(5);
            
            leftScroll = EditorGUILayout.BeginScrollView(leftScroll, GUILayout.ExpandHeight(true));
            
            if (sets.Count == 0)
            {
                EditorGUILayout.Space(30);
                var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
                EditorGUILayout.LabelField("No sets", style, GUILayout.Height(30));
            }
            else
            {
                foreach (var set in sets)
                {
                    DrawSetItem(set);
                }
            }
            
            EditorGUILayout.EndScrollView();
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        private void DrawSetItem(AttributeSet set)
        {
            EditorGUILayout.BeginHorizontal("box", GUILayout.Height(55));
            GUILayout.Space(8);
            
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(set.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{set.Attributes.Count} attributes", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            
            if (GUILayout.Button("Edit", GUILayout.Width(50), GUILayout.Height(45)))
            {
                editingSet = set;
                isCreatingNewSet = false;
                setValues.Clear();
                foreach (var entry in set.Attributes)
                    if (entry.type != null)
                        setValues[entry.type.Name] = entry.baseValue;
            }
            
            if (GUILayout.Button("Delete", GUILayout.Width(60), GUILayout.Height(45)))
            {
                if (EditorUtility.DisplayDialog("Delete", $"Delete '{set.name}'?", "Delete", "Cancel"))
                {
                    string path = AssetDatabase.GetAssetPath(set);
                    AssetDatabase.DeleteAsset(path);
                    if (set == editingSet)
                        editingSet = null;
                }
            }
            
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRightPanel_SetCreate()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("Create New Set", EditorStyles.boldLabel);
            GUILayout.Space(8);
            
            EditorGUILayout.BeginVertical("box");
            GUILayout.Space(5);
            
            EditorGUILayout.LabelField("Set Name", EditorStyles.miniLabel);
            setName = EditorGUILayout.TextField(setName, GUILayout.Height(22));
            
            GUILayout.Space(15);
            
            GUI.enabled = !string.IsNullOrWhiteSpace(setName);
            if (GUILayout.Button("Create and Edit", GUILayout.Height(35)))
            {
                if (!Directory.Exists("Assets/Resources/Attributes/Sets"))
                    Directory.CreateDirectory("Assets/Resources/Attributes/Sets");
                
                var newSet = CreateInstance<AttributeSet>();
                string path = $"Assets/Resources/Attributes/Sets/so_{setName.Trim()}.asset";
                AssetDatabase.CreateAsset(newSet, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                isCreatingNewSet = true;
                setValues.Clear();
                setName = "";
            }
            GUI.enabled = true;
            
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        private void DrawRightPanel_SetEditor()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.Space(10);
            
            var targetSet = editingSet ?? GetCreatingSet();
            string title = editingSet != null ? "Editing: " + editingSet.name : "Configuring New Set";
            
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Select attributes", EditorStyles.miniLabel);
            GUILayout.Space(5);
            
            rightScroll = EditorGUILayout.BeginScrollView(rightScroll, GUILayout.ExpandHeight(true));
            
            if (database?.AllAttributes != null && database.AllAttributes.Count > 0)
            {
                foreach (var attr in database.AllAttributes)
                {
                    if (attr == null) continue;
                    DrawSetAttributeToggle(attr, targetSet);
                }
            }
            else
            {
                EditorGUILayout.Space(30);
                var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
                EditorGUILayout.LabelField("No attributes available", style, GUILayout.Height(30));
            }
            
            EditorGUILayout.EndScrollView();
            
            GUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Save", GUILayout.Height(35)))
            {
                if (targetSet != null)
                {
                    foreach (var kvp in setValues)
                    {
                        var attr = database.GetAttribute(kvp.Key);
                        if (attr != null && targetSet.HasAttribute(attr))
                            targetSet.AddAttribute(attr, kvp.Value);
                    }
                    EditorUtility.SetDirty(targetSet);
                    AssetDatabase.SaveAssets();
                }
                
                editingSet = null;
                isCreatingNewSet = false;
                setValues.Clear();
            }
            
            if (GUILayout.Button("Cancel", GUILayout.Height(35)))
            {
                if (isCreatingNewSet)
                {
                    var creatingSet = GetCreatingSet();
                    if (creatingSet != null)
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(creatingSet));
                }
                
                editingSet = null;
                isCreatingNewSet = false;
                setValues.Clear();
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        private void DrawSetAttributeToggle(AttributeType attr, AttributeSet targetSet)
        {
            bool hasAttr = targetSet.HasAttribute(attr);
            
            EditorGUILayout.BeginHorizontal(GUILayout.Height(30));
            GUILayout.Space(5);
            
            bool newHasAttr = EditorGUILayout.Toggle(hasAttr, GUILayout.Width(20));
            
            EditorGUILayout.LabelField(attr.Name, GUILayout.Width(110));
            EditorGUILayout.LabelField(attr.Category, EditorStyles.miniLabel, GUILayout.Width(80));
            
            GUILayout.FlexibleSpace();
            
            if (newHasAttr)
            {
                if (!setValues.ContainsKey(attr.Name))
                    setValues[attr.Name] = hasAttr ? targetSet.GetBaseValue(attr) : 0f;
                
                setValues[attr.Name] = EditorGUILayout.FloatField(setValues[attr.Name], GUILayout.Width(60));
            }
            
            if (newHasAttr != hasAttr)
            {
                if (newHasAttr)
                    targetSet.AddAttribute(attr, setValues.GetValueOrDefault(attr.Name, 0f));
                else
                {
                    targetSet.RemoveAttribute(attr);
                    setValues.Remove(attr.Name);
                }
                EditorUtility.SetDirty(targetSet);
            }
            
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();
        }

        private List<AttributeType> GetFilteredAttributes()
        {
            if (database?.AllAttributes == null) return new List<AttributeType>();
            
            var attrs = database.AllAttributes.Where(a => a != null);
            
            if (!string.IsNullOrEmpty(attributeSearch))
                attrs = attrs.Where(a => a.Name.ToLower().Contains(attributeSearch.ToLower()));
            
            if (selectedCategory != "All")
                attrs = attrs.Where(a => a.Category == selectedCategory);
            
            return attrs.OrderBy(a => a.Name).ToList();
        }

        private List<AttributeSet> GetFilteredSets()
        {
            var guids = AssetDatabase.FindAssets("t:AttributeSet");
            var sets = guids.Select(guid => AssetDatabase.LoadAssetAtPath<AttributeSet>(AssetDatabase.GUIDToAssetPath(guid)))
                           .Where(s => s != null);
            
            if (!string.IsNullOrEmpty(setSearch))
                sets = sets.Where(s => s.name.ToLower().Contains(setSearch.ToLower()));
            
            return sets.OrderBy(s => s.name).ToList();
        }

        private AttributeSet GetCreatingSet()
        {
            if (!isCreatingNewSet) return null;
            
            var guids = AssetDatabase.FindAssets("t:AttributeSet");
            var sets = guids.Select(guid => AssetDatabase.LoadAssetAtPath<AttributeSet>(AssetDatabase.GUIDToAssetPath(guid)))
                           .Where(s => s != null);
            
            return sets.OrderByDescending(s => System.IO.File.GetLastWriteTime(AssetDatabase.GetAssetPath(s))).FirstOrDefault();
        }
    }

    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }
    }
}
#endif