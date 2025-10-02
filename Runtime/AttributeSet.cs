using System;
using System.Collections.Generic;
using UnityEngine;

namespace StatsForge
{
    public class AttributeSet : ScriptableObject
    {
        [Serializable]
        public struct AttributeEntry
        {
            public AttributeType type;
            public float baseValue;
        }

        [SerializeField] private List<AttributeEntry> attributes = new List<AttributeEntry>();
        public IReadOnlyList<AttributeEntry> Attributes => attributes;

        public void AddAttribute(AttributeType type, float baseValue)
        {
            if (type == null) return;

            for (int i = 0; i < attributes.Count; i++)
            {
                if (attributes[i].type == type)
                {
                    var entry = attributes[i];
                    entry.baseValue = baseValue;
                    attributes[i] = entry;
                    return;
                }
            }

            attributes.Add(new AttributeEntry { type = type, baseValue = baseValue });
        }

        public void RemoveAttribute(AttributeType type)
        {
            if (type == null) return;
            
            for (int i = 0; i < attributes.Count; i++)
            {
                if (attributes[i].type == type)
                {
                    attributes.RemoveAt(i);
                    return;
                }
            }
        }

        public bool HasAttribute(AttributeType type)
        {
            foreach (var entry in attributes)
            {
                if (entry.type == type) return true;
            }
            return false;
        }

        public float GetBaseValue(AttributeType type)
        {
            foreach (var entry in attributes)
            {
                if (entry.type == type) return entry.baseValue;
            }
            return 0f;
        }
    }
}