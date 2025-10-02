using UnityEngine;

namespace StatsForge
{
    public class AttributeType : ScriptableObject, IAttributeType
    {
        [SerializeField] private string attributeName;
        [SerializeField] private string category = "Core";

        public string Name => attributeName;
        public string Category => category;

        #if UNITY_EDITOR
        public void SetName(string newName)
        {
            attributeName = newName;
            name = $"atr_{attributeName}";
        }

        public void SetCategory(string newCategory)
        {
            category = newCategory;
        }
        #endif
    }
}