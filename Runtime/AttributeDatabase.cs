using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StatsForge
{
    public class AttributeDatabase : ScriptableObject
    {
        [SerializeField] private List<AttributeType> allAttributes = new List<AttributeType>();
        private Dictionary<string, AttributeType> lookup;
        
        private static AttributeDatabase instance;
        public static AttributeDatabase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<AttributeDatabase>("Attributes/AttributeDatabase");
                }
                return instance;
            }
        }

        public IReadOnlyList<AttributeType> AllAttributes => allAttributes;

        public IEnumerable<string> Categories
        {
            get
            {
                if (allAttributes == null) return new string[0];
                return allAttributes
                    .Where(a => a != null)
                    .Select(a => a.Category)
                    .Distinct()
                    .OrderBy(c => c);
            }
        }

        public AttributeType GetAttribute(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            
            EnsureLookup();
            lookup.TryGetValue(name, out var result);
            return result;
        }

        public IEnumerable<AttributeType> GetAttributesByCategory(string category)
        {
            if (allAttributes == null) return new AttributeType[0];
            return allAttributes.Where(a => a != null && a.Category == category);
        }

        private void EnsureLookup()
        {
            if (lookup == null || (allAttributes != null && lookup.Count != allAttributes.Count))
            {
                lookup = new Dictionary<string, AttributeType>();
                if (allAttributes != null)
                {
                    foreach (var attr in allAttributes)
                    {
                        if (attr != null && !string.IsNullOrEmpty(attr.Name) && !lookup.ContainsKey(attr.Name))
                            lookup[attr.Name] = attr;
                    }
                }
            }
        }

        public void Validate()
        {
            if (allAttributes == null) allAttributes = new List<AttributeType>();
            
            allAttributes.RemoveAll(a => a == null);
            
            var duplicates = allAttributes
                .GroupBy(a => a.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
                
            if (duplicates.Count > 0)
            {
                Debug.LogWarning($"Atributos duplicados encontrados: {string.Join(", ", duplicates)}");
            }
            
            lookup = null;
        }

        #if UNITY_EDITOR
        public AttributeType CreateAttribute(string name, string category = "Core")
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            var existing = GetAttribute(name);
            if (existing != null) return existing;

            var newAttr = CreateInstance<AttributeType>();
            newAttr.SetName(name);
            newAttr.SetCategory(category);

            UnityEditor.AssetDatabase.AddObjectToAsset(newAttr, this);
            UnityEditor.AssetDatabase.SaveAssets();

            if (allAttributes == null) allAttributes = new List<AttributeType>();
            allAttributes.Add(newAttr);
            lookup = null;

            return newAttr;
        }

        public bool RemoveAttribute(string name)
        {
            var attr = GetAttribute(name);
            if (attr == null) return false;
            
            allAttributes.Remove(attr);
            lookup = null;
            
            UnityEditor.AssetDatabase.RemoveObjectFromAsset(attr);
            UnityEditor.AssetDatabase.SaveAssets();
            
            return true;
        }
        #endif
    }
}