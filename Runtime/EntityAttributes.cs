using System;
using System.Collections.Generic;
using UnityEngine;

namespace StatsForge
{
    /// <summary>
    /// Component responsible for managing entity attributes with support for modifiers and runtime changes.
    /// Provides a clean API for accessing and modifying attribute values.
    /// </summary>
    public class EntityAttributes : MonoBehaviour
    {
        [SerializeField] private AttributeSet attributeSet;
        
        private Dictionary<string, ModifiableAttributeInstance> attributes = new Dictionary<string, ModifiableAttributeInstance>();
        
        /// <summary>
        /// Event triggered when an attribute value changes.
        /// </summary>
        public event Action<AttributeType, float> OnAttributeChanged;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeAttributes();
        }
        
        private void Update()
        {
            foreach (var attr in attributes.Values)
            {
                attr.Update();
            }
        }
        
        #endregion
        
        #region Indexer API
        
        /// <summary>
        /// Gets or sets the current value of an attribute by name.
        /// Setting a value clears all modifiers and sets the base value.
        /// </summary>
        /// <param name="attributeName">Name of the attribute</param>
        /// <returns>Current value of the attribute, or 0 if not found</returns>
        public float this[string attributeName]
        {
            get => GetValue(attributeName);
            set => SetValue(attributeName, value);
        }
        
        #endregion
        
        #region Implicit Conversions
        
        /// <summary>
        /// Implicit conversion to allow direct float assignment.
        /// Example: float speed = entityAttributes;
        /// Returns the first attribute's value or 0 if no attributes exist.
        /// </summary>
        public static implicit operator float(EntityAttributes entityAttributes)
        {
            if (entityAttributes?.attributes?.Count > 0)
            {
                foreach (var attr in entityAttributes.attributes.Values)
                {
                    return attr.CurrentValue;
                }
            }
            return 0f;
        }
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// Gets all attributes as a read-only dictionary.
        /// </summary>
        public IReadOnlyDictionary<string, ModifiableAttributeInstance> AllAttributes => attributes;
        
        /// <summary>
        /// Gets the number of attributes in this entity.
        /// </summary>
        public int AttributeCount => attributes.Count;
        
        /// <summary>
        /// Gets the current attribute set assigned to this entity.
        /// </summary>
        public AttributeSet CurrentAttributeSet => attributeSet;
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initializes attributes from the assigned AttributeSet.
        /// </summary>
        private void InitializeAttributes()
        {
            if (attributeSet == null) return;
            
            attributes.Clear();
            
            foreach (var entry in attributeSet.Attributes)
            {
                if (entry.type == null) continue;
                
                var instance = new ModifiableAttributeInstance(entry.type, entry.baseValue);
                instance.OnValueChanged += value => 
                {
                    OnAttributeChanged?.Invoke(entry.type, value);
                };
                attributes[entry.type.Name] = instance;
            }
        }
        
        /// <summary>
        /// Sets a new AttributeSet and reinitializes all attributes.
        /// </summary>
        /// <param name="newSet">The new AttributeSet to use</param>
        public void SetAttributeSet(AttributeSet newSet)
        {
            attributeSet = newSet;
            InitializeAttributes();
        }
        
        #endregion
        
        #region Value Access (Clean API)
        
        /// <summary>
        /// Gets the current value of an attribute.
        /// </summary>
        /// <param name="name">Attribute name</param>
        /// <returns>Current value or 0 if attribute doesn't exist</returns>
        public float GetValue(string name) => attributes.TryGetValue(name, out var inst) ? inst.CurrentValue : 0f;
        
        /// <summary>
        /// Gets the base value of an attribute (without modifiers).
        /// </summary>
        /// <param name="name">Attribute name</param>
        /// <returns>Base value or 0 if attribute doesn't exist</returns>
        public float GetBaseValue(string name) => attributes.TryGetValue(name, out var inst) ? inst.BaseValue : 0f;
        
        /// <summary>
        /// Sets the base value of an attribute and clears all modifiers.
        /// </summary>
        /// <param name="name">Attribute name</param>
        /// <param name="value">New base value</param>
        public void SetValue(string name, float value)
        {
            if (attributes.TryGetValue(name, out var inst))
            {
                inst.ClearAllModifiers();
                inst.SetBaseValue(value);
            }
        }
        
        /// <summary>
        /// Sets only the base value without clearing modifiers.
        /// </summary>
        /// <param name="name">Attribute name</param>
        /// <param name="value">New base value</param>
        public void SetBaseValue(string name, float value)
        {
            if (attributes.TryGetValue(name, out var inst))
                inst.SetBaseValue(value);
        }
        
        /// <summary>
        /// Tries to get the ModifiableAttributeInstance for direct manipulation.
        /// </summary>
        /// <param name="name">Attribute name</param>
        /// <param name="instance">Output instance if found</param>
        /// <returns>True if attribute exists</returns>
        public bool TryGetAttribute(string name, out ModifiableAttributeInstance instance) => attributes.TryGetValue(name, out instance);
        
        #endregion
        
        #region Modifier Management
        
        /// <summary>
        /// Applies a modifier to an attribute using the generic method.
        /// </summary>
        /// <param name="attributeName">Target attribute name</param>
        /// <param name="type">Type of modifier</param>
        /// <param name="value">Modifier value</param>
        /// <param name="application">How the modifier should be applied</param>
        /// <param name="duration">Duration in seconds (0 for permanent)</param>
        /// <param name="source">Source identifier for the modifier</param>
        /// <param name="priority">Priority for modifier calculation order</param>
        public void ApplyModifier(string attributeName, ModifierType type, float value, 
            ModifierApplication application = ModifierApplication.Permanent, float duration = 0f, 
            string source = "Unknown", int priority = 0)
        {
            if (!attributes.TryGetValue(attributeName, out var instance)) return;
            
            string modifierId = $"{source}_{type}_{Time.time:F3}";
            var modifier = new AttributeModifier(modifierId, source, type, application, value, duration, priority);
            
            instance.AddModifier(modifier);
        }
        
        /// <summary>
        /// Applies a pre-built modifier to an attribute.
        /// </summary>
        /// <param name="attributeName">Target attribute name</param>
        /// <param name="modifier">The modifier to apply</param>
        public void ApplyModifier(string attributeName, AttributeModifier modifier)
        {
            if (attributes.TryGetValue(attributeName, out var instance))
            {
                instance.AddModifier(modifier);
            }
        }
        
        /// <summary>
        /// Removes a specific modifier by its ID.
        /// </summary>
        /// <param name="attributeName">Target attribute name</param>
        /// <param name="modifierId">ID of the modifier to remove</param>
        /// <returns>True if modifier was found and removed</returns>
        public bool RemoveModifier(string attributeName, string modifierId)
        {
            if (attributes.TryGetValue(attributeName, out var instance))
            {
                return instance.RemoveModifier(modifierId);
            }
            return false;
        }
        
        #endregion
        
        #region Convenience Methods
        
        /// <summary>
        /// Adds a flat modifier to an attribute.
        /// </summary>
        /// <param name="attributeName">Target attribute</param>
        /// <param name="value">Flat value to add</param>
        /// <param name="duration">Duration in seconds (0 for permanent)</param>
        /// <param name="source">Source of the modifier</param>
        public void AddFlat(string attributeName, float value, float duration = 0f, string source = "Unknown")
        {
            var application = duration > 0 ? ModifierApplication.Temporary : ModifierApplication.Permanent;
            ApplyModifier(attributeName, ModifierType.Flat, value, application, duration, source);
        }
        
        /// <summary>
        /// Adds a percentage modifier to an attribute.
        /// </summary>
        /// <param name="attributeName">Target attribute</param>
        /// <param name="percentage">Percentage to add (e.g., 20 for +20%)</param>
        /// <param name="duration">Duration in seconds (0 for permanent)</param>
        /// <param name="source">Source of the modifier</param>
        public void AddPercentage(string attributeName, float percentage, float duration = 0f, string source = "Unknown")
        {
            var application = duration > 0 ? ModifierApplication.Temporary : ModifierApplication.Permanent;
            ApplyModifier(attributeName, ModifierType.PercentageAdd, percentage, application, duration, source);
        }
        
        /// <summary>
        /// Adds a multiplier modifier to an attribute.
        /// </summary>
        /// <param name="attributeName">Target attribute</param>
        /// <param name="multiplierPercentage">Multiplier percentage (e.g., 50 for x1.5)</param>
        /// <param name="duration">Duration in seconds (0 for permanent)</param>
        /// <param name="source">Source of the modifier</param>
        public void AddMultiplier(string attributeName, float multiplierPercentage, float duration = 0f, string source = "Unknown")
        {
            var application = duration > 0 ? ModifierApplication.Temporary : ModifierApplication.Permanent;
            ApplyModifier(attributeName, ModifierType.PercentageMultiply, multiplierPercentage, application, duration, source);
        }
        
        #endregion
        
        #region Bulk Operations
        
        /// <summary>
        /// Clears all modifiers from an attribute or all attributes.
        /// </summary>
        /// <param name="attributeName">Specific attribute name, or null for all attributes</param>
        public void ClearAllModifiers(string attributeName = null)
        {
            if (!string.IsNullOrEmpty(attributeName))
            {
                if (attributes.TryGetValue(attributeName, out var instance))
                {
                    instance.ClearAllModifiers();
                }
            }
            else
            {
                foreach (var instance in attributes.Values)
                {
                    instance.ClearAllModifiers();
                }
            }
        }
        
        /// <summary>
        /// Clears all modifiers from a specific source.
        /// </summary>
        /// <param name="source">Source identifier to clear</param>
        /// <param name="attributeName">Specific attribute name, or null for all attributes</param>
        public void ClearModifiersBySource(string source, string attributeName = null)
        {
            if (!string.IsNullOrEmpty(attributeName))
            {
                if (attributes.TryGetValue(attributeName, out var instance))
                {
                    var toRemove = new List<string>();
                    foreach (var mod in instance.Modifiers)
                    {
                        if (mod.Source == source)
                            toRemove.Add(mod.ID);
                    }
                    foreach (var id in toRemove)
                        instance.RemoveModifier(id);
                }
            }
            else
            {
                foreach (var instance in attributes.Values)
                {
                    var toRemove = new List<string>();
                    foreach (var mod in instance.Modifiers)
                    {
                        if (mod.Source == source)
                            toRemove.Add(mod.ID);
                    }
                    foreach (var id in toRemove)
                        instance.RemoveModifier(id);
                }
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Checks if an attribute exists.
        /// </summary>
        /// <param name="attributeName">Name of the attribute to check</param>
        /// <returns>True if the attribute exists</returns>
        public bool HasAttribute(string attributeName) => attributes.ContainsKey(attributeName);
        
        /// <summary>
        /// Gets all attribute names.
        /// </summary>
        /// <returns>Collection of attribute names</returns>
        public IEnumerable<string> GetAttributeNames() => attributes.Keys;
        
        /// <summary>
        /// Gets the number of active modifiers on an attribute.
        /// </summary>
        /// <param name="attributeName">Attribute name</param>
        /// <returns>Number of active modifiers</returns>
        public int GetModifierCount(string attributeName)
        {
            return attributes.TryGetValue(attributeName, out var instance) ? instance.Modifiers.Count : 0;
        }
        
        #endregion
    }
}