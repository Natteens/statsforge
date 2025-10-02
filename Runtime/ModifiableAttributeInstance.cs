using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StatsForge
{
    [Serializable]
    public class ModifiableAttributeInstance
    {
        private IAttributeType type;
        private float baseValue;
        private List<AttributeModifier> modifiers = new();

        public IAttributeType Type => type;
        public float BaseValue => baseValue;
        public float CurrentValue => CalculateCurrentValue();
        public IReadOnlyList<AttributeModifier> Modifiers => modifiers;

        public event Action<float> OnValueChanged;

        public ModifiableAttributeInstance(IAttributeType type, float baseValue)
        {
            this.type = type;
            this.baseValue = baseValue;
        }

        public void Update()
        {
            float previousValue = CurrentValue;
            
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                modifiers[i].Update();
                
                if (modifiers[i].IsExpired)
                {
                    modifiers.RemoveAt(i);
                }
            }
            
            float currentValue = CurrentValue;
            if (Mathf.Abs(previousValue - currentValue) > 0.0001f)
            {
                OnValueChanged?.Invoke(currentValue);
            }
        }

        private float CalculateCurrentValue()
        {
            var activeModifiers = modifiers.Where(m => m.IsActive && !m.IsExpired)
                                          .OrderBy(m => m.Priority)
                                          .ToList();
            
            float result = baseValue;
            
            foreach (var modifier in activeModifiers.Where(m => m.ModifierType == ModifierType.Flat))
            {
                result += modifier.GetCurrentValue();
            }
            
            float totalPercentageAdd = 0f;
            foreach (var modifier in activeModifiers.Where(m => m.ModifierType == ModifierType.PercentageAdd))
            {
                totalPercentageAdd += modifier.GetCurrentValue();
            }
            if (totalPercentageAdd != 0f)
            {
                result += baseValue * (totalPercentageAdd / 100f);
            }
            
            foreach (var modifier in activeModifiers.Where(m => m.ModifierType == ModifierType.PercentageMultiply))
            {
                float multiplier = 1f + (modifier.GetCurrentValue() / 100f);
                result *= multiplier;
            }
            
            return result;
        }

        public void AddModifier(AttributeModifier modifier)
        {
            if (modifier == null) return;
            
            RemoveModifier(modifier.ID);
            
            modifier.OnExpired += OnModifierExpired;
            modifiers.Add(modifier);
            modifier.Activate();
            
            OnValueChanged?.Invoke(CurrentValue);
        }

        public bool RemoveModifier(string modifierId)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].ID == modifierId)
                {
                    modifiers[i].OnExpired -= OnModifierExpired;
                    modifiers.RemoveAt(i);
                    OnValueChanged?.Invoke(CurrentValue);
                    return true;
                }
            }
            return false;
        }

        public bool HasModifier(string modifierId)
        {
            return modifiers.Any(m => m.ID == modifierId && m.IsActive && !m.IsExpired);
        }

        public AttributeModifier GetModifier(string modifierId)
        {
            return modifiers.FirstOrDefault(m => m.ID == modifierId && m.IsActive && !m.IsExpired);
        }

        public void ClearAllModifiers()
        {
            foreach (var modifier in modifiers)
            {
                modifier.OnExpired -= OnModifierExpired;
            }
            modifiers.Clear();
            OnValueChanged?.Invoke(CurrentValue);
        }

        public void SetBaseValue(float newValue)
        {
            baseValue = newValue;
            OnValueChanged?.Invoke(CurrentValue);
        }

        private void OnModifierExpired(AttributeModifier modifier)
        {
            modifier.OnExpired -= OnModifierExpired;
            OnValueChanged?.Invoke(CurrentValue);
        }

        public string GetCalculationBreakdown()
        {
            var breakdown = new System.Text.StringBuilder();
            breakdown.AppendLine($"Base: {baseValue:F1}");
            
            var activeModifiers = modifiers.Where(m => m.IsActive && !m.IsExpired).OrderBy(m => m.Priority).ToList();
            
            float step1 = baseValue;
            var flatMods = activeModifiers.Where(m => m.ModifierType == ModifierType.Flat).ToList();
            if (flatMods.Any())
            {
                float flatTotal = flatMods.Sum(m => m.GetCurrentValue());
                step1 += flatTotal;
                breakdown.AppendLine($"+ Flat: {flatTotal:+0.0;-0.0} = {step1:F1}");
            }
            
            var percentMods = activeModifiers.Where(m => m.ModifierType == ModifierType.PercentageAdd).ToList();
            if (percentMods.Any())
            {
                float percentTotal = percentMods.Sum(m => m.GetCurrentValue());
                float percentValue = baseValue * (percentTotal / 100f);
                step1 += percentValue;
                breakdown.AppendLine($"+ {percentTotal:F1}% of base ({baseValue:F1}): {percentValue:+0.0;-0.0} = {step1:F1}");
            }
            
            foreach (var multMod in activeModifiers.Where(m => m.ModifierType == ModifierType.PercentageMultiply))
            {
                float multiplier = 1f + (multMod.GetCurrentValue() / 100f);
                float oldValue = step1;
                step1 *= multiplier;
                breakdown.AppendLine($"x {multiplier:F2}: {oldValue:F1} → {step1:F1}");
            }
            
            return breakdown.ToString();
        }
    }
}