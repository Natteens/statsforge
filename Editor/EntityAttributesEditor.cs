#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StatsForge.Editor
{
    [CustomEditor(typeof(EntityAttributes))]
    public class EntityAttributesEditor : UnityEditor.Editor
    {
        private bool showAttributes = true;
        private Dictionary<string, bool> categoryFoldouts = new();
        private bool allCategoriesExpanded = true;
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var entityAttributes = (EntityAttributes)target;
            
            DrawAttributeSetField(entityAttributes);
            EditorGUILayout.Space(5);
            DrawAttributesSection(entityAttributes);
            
            serializedObject.ApplyModifiedProperties();
            
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
        
        private void DrawAttributeSetField(EntityAttributes entityAttributes)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(3);
            
            var setProperty = serializedObject.FindProperty("attributeSet");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(setProperty, new GUIContent("Attribute Set"));
            
            if (EditorGUI.EndChangeCheck() && Application.isPlaying)
            {
                entityAttributes.SetAttributeSet((AttributeSet)setProperty.objectReferenceValue);
            }
            
            GUILayout.Space(3);
            EditorGUILayout.EndVertical();
        }
        
        private void DrawAttributesSection(EntityAttributes entityAttributes)
        {
            var attributes = GetAttributesForDisplay(entityAttributes);
            
            if (attributes.Count == 0)
            {
                EditorGUILayout.HelpBox("No attributes configured.", MessageType.Info);
                return;
            }
            
            var totalCount = GetTotalAttributeCount(attributes);
            
            EditorGUILayout.BeginHorizontal();
            showAttributes = EditorGUILayout.Foldout(showAttributes, $"Attributes ({totalCount})", true, EditorStyles.foldoutHeader);
            
            if (showAttributes)
            {
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button(allCategoriesExpanded ? "Collapse All" : "Expand All", EditorStyles.miniButton, GUILayout.Width(80)))
                {
                    ToggleAllCategories(!allCategoriesExpanded);
                }
                
                if (Application.isPlaying && GUILayout.Button("Clear Mods", EditorStyles.miniButton, GUILayout.Width(70)))
                {
                    entityAttributes.ClearAllModifiers();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (!showAttributes) return;
            
            DrawCompactTable(attributes, entityAttributes);
        }
        
        private void DrawCompactTable(Dictionary<string, List<ModifiableAttributeInstance>> attributes, EntityAttributes entityAttributes)
        {
            var categorizedAttributes = GroupAttributesByCategory(attributes);
            
            foreach (var category in categorizedAttributes.Keys.OrderBy(k => k))
            {
                DrawCategorySection(category, categorizedAttributes[category], entityAttributes);
            }
        }
        
        private void DrawCategorySection(string category, List<ModifiableAttributeInstance> attributes, EntityAttributes entityAttributes)
        {
            if (!categoryFoldouts.ContainsKey(category))
                categoryFoldouts[category] = true;
            
            var categoryStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.7f, 0.8f, 1f) }
            };
            
            categoryFoldouts[category] = EditorGUILayout.Foldout(
                categoryFoldouts[category], 
                $"{category} ({attributes.Count})", 
                true, 
                categoryStyle
            );
            
            if (!categoryFoldouts[category]) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            DrawTableHeader();
            
            for (int i = 0; i < attributes.Count; i++)
            {
                DrawAttributeRow(attributes[i], entityAttributes, i % 2 == 0);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }
        
        private void DrawTableHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            EditorGUILayout.LabelField("Attribute", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField("Current", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Base", EditorStyles.boldLabel, GUILayout.Width(50));
            EditorGUILayout.LabelField("Modifiers", EditorStyles.boldLabel, GUILayout.MinWidth(80));
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawAttributeRow(ModifiableAttributeInstance instance, EntityAttributes entityAttributes, bool isEvenRow)
        {
            var attributeType = (AttributeType)instance.Type;
            var attributeName = attributeType.Name;
            
            var bgColor = isEvenRow ? new Color(0, 0, 0, 0.1f) : Color.clear;
            
            EditorGUILayout.BeginHorizontal();
            
            var rect = EditorGUILayout.GetControlRect(false, 18);
            if (isEvenRow)
            {
                EditorGUI.DrawRect(rect, bgColor);
            }
            
            var x = rect.x;
            var remainingWidth = rect.width;
            
            var nameRect = new Rect(x, rect.y, 100, rect.height);
            GUI.Label(nameRect, attributeName, EditorStyles.label);
            x += 100;
            remainingWidth -= 100;
            
            var currentRect = new Rect(x, rect.y, 60, rect.height);
            if (Application.isPlaying)
            {
                EditorGUI.BeginChangeCheck();
                var newValue = EditorGUI.FloatField(currentRect, instance.CurrentValue);
                if (EditorGUI.EndChangeCheck())
                {
                    entityAttributes.SetValue(attributeName, newValue);
                }
            }
            else
            {
                GUI.Label(currentRect, instance.CurrentValue.ToString("F1"), EditorStyles.textField);
            }
            x += 60;
            remainingWidth -= 60;
            
            var baseRect = new Rect(x, rect.y, 50, rect.height);
            GUI.Label(baseRect, instance.BaseValue.ToString("F1"));
            x += 50;
            remainingWidth -= 50;
            
            var modifiersRect = new Rect(x, rect.y, remainingWidth - 25, rect.height);
            var modifierText = GetModifierSummary(instance);
            GUI.Label(modifiersRect, modifierText, EditorStyles.miniLabel);
            
            if (Application.isPlaying && instance.Modifiers.Count > 0)
            {
                var debugRect = new Rect(rect.width - 20, rect.y, 20, rect.height);
                if (GUI.Button(debugRect, "?", EditorStyles.miniButton))
                {
                    Debug.Log($"Calculation for {attributeName}:\n{instance.GetCalculationBreakdown()}");
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private string GetModifierSummary(ModifiableAttributeInstance instance)
        {
            if (instance.Modifiers.Count == 0)
                return "—";
            
            if (instance.Modifiers.Count == 1)
            {
                var mod = instance.Modifiers[0];
                var text = $"{mod.Value:+0.#;-0.#}";
                if (mod.ModifierType != ModifierType.Flat) text += "%";
                if (mod.RemainingTime > 0) text += $" ({mod.RemainingTime:F0}s)";
                return text;
            }
            
            return $"{instance.Modifiers.Count} mods";
        }
        
        private int GetTotalAttributeCount(Dictionary<string, List<ModifiableAttributeInstance>> attributes)
        {
            return attributes.Values.Sum(list => list.Count);
        }
        
        private void ToggleAllCategories(bool expand)
        {
            allCategoriesExpanded = expand;
            var keys = categoryFoldouts.Keys.ToList();
            foreach (var key in keys)
            {
                categoryFoldouts[key] = expand;
            }
        }
        
        private Dictionary<string, List<ModifiableAttributeInstance>> GetAttributesForDisplay(EntityAttributes entityAttributes)
        {
            var result = new Dictionary<string, List<ModifiableAttributeInstance>>();
            
            if (Application.isPlaying)
            {
                var runtimeAttributes = entityAttributes.AllAttributes;
                foreach (var pair in runtimeAttributes)
                {
                    if (!result.ContainsKey(pair.Key))
                        result[pair.Key] = new List<ModifiableAttributeInstance>();
                    
                    result[pair.Key].Add(pair.Value);
                }
            }
            else
            {
                var setProperty = serializedObject.FindProperty("attributeSet");
                var attributeSet = setProperty.objectReferenceValue as AttributeSet;
                
                if (attributeSet != null)
                {
                    foreach (var entry in attributeSet.Attributes)
                    {
                        if (entry.type != null)
                        {
                            var tempInstance = new ModifiableAttributeInstance(entry.type, entry.baseValue);
                            if (!result.ContainsKey(entry.type.Name))
                                result[entry.type.Name] = new List<ModifiableAttributeInstance>();
                            
                            result[entry.type.Name].Add(tempInstance);
                        }
                    }
                }
            }
            
            return result;
        }
        
        private Dictionary<string, List<ModifiableAttributeInstance>> GroupAttributesByCategory(Dictionary<string, List<ModifiableAttributeInstance>> attributes)
        {
            var categorized = new Dictionary<string, List<ModifiableAttributeInstance>>();
            
            foreach (var kvp in attributes)
            {
                foreach (var instance in kvp.Value)
                {
                    var category = "Core";
                    if (instance.Type is AttributeType attrType)
                    {
                        category = attrType.Category;
                    }
                    
                    if (!categorized.TryGetValue(category, out var list))
                    {
                        list = new List<ModifiableAttributeInstance>();
                        categorized[category] = list;
                    }
                    
                    list.Add(instance);
                }
            }
            
            return categorized;
        }
    }
}
#endif