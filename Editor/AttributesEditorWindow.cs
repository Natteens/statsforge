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
        
        private Vector2 leftScrollPos;
        private Vector2 rightScrollPos;
        
        private string attributeName = "";
        private string attributeCategory = "Core";
        private string attributeSearch = "";
        private string selectedCategory = "All";
        
        private string setName = "";
        private string setSearch = "";
        private AttributeSet editingSet;
        private AttributeSet creatingSet;
        private Dictionary<string, float> setValues = new Dictionary<string, float>();
        private bool isCreatingNewSet = false;

        [MenuItem("Tools/Attributes Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<AttributesEditorWindow>("Attributes Manager");
            window.minSize = new Vector2(700, 500);
        }

        private void OnEnable()
        {
            LoadDatabase();
        }

        private void LoadDatabase()
        {
            database = Resources.Load<AttributeDatabase>("Attributes/AttributeDatabase");
            
            if (database == null)
            {
                CreateDatabase();
            }
            
            if (database != null)
            {
                database.Validate();
            }
        }

        private void CreateDatabase()
        {
            if (!Directory.Exists("Assets/Resources/Attributes"))
            {
                Directory.CreateDirectory("Assets/Resources/Attributes");
            }
            
            database = CreateInstance<AttributeDatabase>();
            AssetDatabase.CreateAsset(database, "Assets/Resources/Attributes/AttributeDatabase.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void OnGUI()
        {
            if (database == null)
            {
                EditorGUILayout.Space(20);
                EditorGUILayout.HelpBox("Database not found!", MessageType.Error);
                if (GUILayout.Button("Create New Database", GUILayout.Height(30)))
                {
                    CreateDatabase();
                }
                return;
            }

            DrawHeader();
            DrawTabs();
            DrawContent();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Attributes Manager", EditorStyles.largeLabel);
            EditorGUILayout.LabelField("Create and manage attributes and sets", EditorStyles.miniLabel);
            GUILayout.Space(10);
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = currentTab == Tab.Attributes ? Color.cyan : Color.white;
            if (GUILayout.Button("Attributes", EditorStyles.toolbarButton))
            {
                currentTab = Tab.Attributes;
                ClearFields();
            }
            
            GUI.backgroundColor = currentTab == Tab.Sets ? Color.cyan : Color.white;
            if (GUILayout.Button("Attribute Sets", EditorStyles.toolbarButton))
            {
                currentTab = Tab.Sets;
                ClearFields();
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
        }

        private void DrawContent()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (currentTab == Tab.Attributes)
            {
                DrawAttributesTab();
            }
            else
            {
                DrawSetsTab();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAttributesTab()
        {
            DrawAttributesLeftPanel();
            GUILayout.Space(10);
            DrawAttributesRightPanel();
        }

        private void DrawSetsTab()
        {
            DrawSetsLeftPanel();
            GUILayout.Space(10);
            DrawSetsRightPanel();
        }

        private void DrawAttributesLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(350));
            
            EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
            
            attributeSearch = EditorGUILayout.TextField("Search:", attributeSearch);
            
            var categories = new List<string> { "All" };
            if (database != null && database.Categories != null)
            {
                categories.AddRange(database.Categories);
            }
            
            int catIndex = categories.IndexOf(selectedCategory);
            if (catIndex == -1) catIndex = 0;
            catIndex = EditorGUILayout.Popup("Category:", catIndex, categories.ToArray());
            selectedCategory = categories[catIndex];
            
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Available Attributes", EditorStyles.boldLabel);
            
            var filteredAttrs = GetFilteredAttributes();
            
            if (filteredAttrs.Count == 0)
            {
                EditorGUILayout.HelpBox("No attributes found.", MessageType.Info);
            }
            else
            {
                leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);
                
                foreach (var attr in filteredAttrs)
                {
                    DrawAttributeCard(attr);
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawAttributeCard(AttributeType attr)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(attr.Name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Category: {attr.Category}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog("Confirm", $"Delete '{attr.Name}'?", "Yes", "No"))
                {
                    database.RemoveAttribute(attr.Name);
                    EditorUtility.SetDirty(database);
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawAttributesRightPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(330));
            
            EditorGUILayout.LabelField("Create Attribute", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            attributeName = EditorGUILayout.TextField("Name:", attributeName);
            attributeCategory = EditorGUILayout.TextField("Category:", attributeCategory);
            
            GUILayout.Space(10);
            
            GUI.enabled = !string.IsNullOrWhiteSpace(attributeName);
            if (GUILayout.Button("Create Attribute", GUILayout.Height(30)))
            {
                CreateAttribute();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(20);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total: {database.AllAttributes.Count}");
            EditorGUILayout.LabelField($"Categories: {database.Categories.Count()}");
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawSetsLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(350));
            
            EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
            setSearch = EditorGUILayout.TextField("Search:", setSearch);
            
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Attribute Sets", EditorStyles.boldLabel);
            
            var sets = GetFilteredSets();
            
            if (sets.Count == 0)
            {
                EditorGUILayout.HelpBox("No sets found.", MessageType.Info);
            }
            else
            {
                leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);
                
                foreach (var set in sets)
                {
                    DrawSetCard(set);
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawSetCard(AttributeSet set)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(set.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{set.Attributes.Count} attributes", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Edit", GUILayout.Width(40)))
            {
                EditSet(set);
            }
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                if (EditorUtility.DisplayDialog("Confirm", $"Delete '{set.name}'?", "Yes", "No"))
                {
                    DeleteSet(set);
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawSetsRightPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(330));
            
            if (editingSet != null || isCreatingNewSet)
            {
                DrawSetEditor();
            }
            else
            {
                DrawSetCreator();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawSetCreator()
        {
            EditorGUILayout.LabelField("Create Attribute Set", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            setName = EditorGUILayout.TextField("Set Name:", setName);
            
            GUILayout.Space(10);
            
            GUI.enabled = !string.IsNullOrWhiteSpace(setName);
            if (GUILayout.Button("Create and Configure", GUILayout.Height(30)))
            {
                CreateSetAndStartEditing();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndVertical();
        }

        private void DrawSetEditor()
        {
            var targetSet = editingSet ?? creatingSet;
            string title = editingSet != null ? "Editing Set" : "Configuring Set";
            
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Set: {targetSet.name}", EditorStyles.miniLabel);
            
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("Configure Attributes:", EditorStyles.boldLabel);
            
            rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos, GUILayout.Height(250));
            
            foreach (var attr in database.AllAttributes)
            {
                if (attr == null) continue;
                
                DrawAttributeToggle(attr, targetSet);
            }
            
            EditorGUILayout.EndScrollView();
            
            GUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save"))
            {
                SaveSetConfiguration();
            }
            if (GUILayout.Button("Cancel"))
            {
                CancelSetEdit();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAttributeToggle(AttributeType attr, AttributeSet targetSet)
        {
            bool hasAttr = targetSet.HasAttribute(attr);
            
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            bool newHasAttr = EditorGUILayout.Toggle(hasAttr, GUILayout.Width(20));
            EditorGUILayout.LabelField(attr.Name, GUILayout.Width(120));
            EditorGUILayout.LabelField($"({attr.Category})", EditorStyles.miniLabel, GUILayout.Width(60));
            
            if (newHasAttr)
            {
                if (!setValues.ContainsKey(attr.Name))
                {
                    setValues[attr.Name] = hasAttr ? targetSet.GetBaseValue(attr) : 0f;
                }
                
                setValues[attr.Name] = EditorGUILayout.FloatField(setValues[attr.Name], GUILayout.Width(50));
            }
            
            if (newHasAttr != hasAttr)
            {
                if (newHasAttr)
                {
                    targetSet.AddAttribute(attr, setValues.GetValueOrDefault(attr.Name, 0f));
                }
                else
                {
                    targetSet.RemoveAttribute(attr);
                    setValues.Remove(attr.Name);
                }
                EditorUtility.SetDirty(targetSet);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private List<AttributeType> GetFilteredAttributes()
        {
            if (database?.AllAttributes == null) return new List<AttributeType>();
            
            var attrs = database.AllAttributes.Where(a => a != null);
            
            if (!string.IsNullOrEmpty(attributeSearch))
            {
                attrs = attrs.Where(a => a.Name.ToLower().Contains(attributeSearch.ToLower()));
            }
            
            if (selectedCategory != "All")
            {
                attrs = attrs.Where(a => a.Category == selectedCategory);
            }
            
            return attrs.OrderBy(a => a.Name).ToList();
        }

        private List<AttributeSet> GetFilteredSets()
        {
            var guids = AssetDatabase.FindAssets("t:AttributeSet");
            var sets = guids.Select(guid => AssetDatabase.LoadAssetAtPath<AttributeSet>(AssetDatabase.GUIDToAssetPath(guid)))
                           .Where(s => s != null);
            
            if (!string.IsNullOrEmpty(setSearch))
            {
                sets = sets.Where(s => s.name.ToLower().Contains(setSearch.ToLower()));
            }
            
            return sets.OrderBy(s => s.name).ToList();
        }

        private void CreateAttribute()
        {
            var attr = database.CreateAttribute(attributeName.Trim(), attributeCategory.Trim());
            if (attr != null)
            {
                EditorUtility.SetDirty(database);
                attributeName = "";
                attributeCategory = "Core";
            }
        }

        private void CreateSetAndStartEditing()
        {
            if (!Directory.Exists("Assets/Resources/Attributes/Sets"))
            {
                Directory.CreateDirectory("Assets/Resources/Attributes/Sets");
            }
            
            creatingSet = CreateInstance<AttributeSet>();
            string path = $"Assets/Resources/Attributes/Sets/so_{setName.Trim()}.asset";
            AssetDatabase.CreateAsset(creatingSet, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            isCreatingNewSet = true;
            setValues.Clear();
        }

        private void EditSet(AttributeSet set)
        {
            editingSet = set;
            setValues.Clear();
            
            foreach (var entry in set.Attributes)
            {
                if (entry.type != null)
                {
                    setValues[entry.type.Name] = entry.baseValue;
                }
            }
        }

        private void SaveSetConfiguration()
        {
            var targetSet = editingSet ?? creatingSet;
            
            foreach (var kvp in setValues)
            {
                var attr = database.GetAttribute(kvp.Key);
                if (attr != null && targetSet.HasAttribute(attr))
                {
                    targetSet.AddAttribute(attr, kvp.Value);
                }
            }
            
            EditorUtility.SetDirty(targetSet);
            AssetDatabase.SaveAssets();
            CancelSetEdit();
        }

        private void CancelSetEdit()
        {
            if (isCreatingNewSet && creatingSet != null)
            {
                string path = AssetDatabase.GetAssetPath(creatingSet);
                AssetDatabase.DeleteAsset(path);
            }
            
            editingSet = null;
            creatingSet = null;
            isCreatingNewSet = false;
            setName = "";
            setValues.Clear();
        }

        private void DeleteSet(AttributeSet set)
        {
            if (set == editingSet)
            {
                CancelSetEdit();
            }
            
            string path = AssetDatabase.GetAssetPath(set);
            AssetDatabase.DeleteAsset(path);
        }

        private void ClearFields()
        {
            attributeName = "";
            attributeCategory = "Core";
            attributeSearch = "";
            selectedCategory = "All";
            setName = "";
            setSearch = "";
            CancelSetEdit();
        }
    }

    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }
    }
}
#endif