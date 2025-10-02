using System;

namespace StatsForge
{
    public interface IAttributeInstance
    {
        IAttributeType Type { get; }
        float BaseValue { get; }
        float CurrentValue { get; }
        event Action<float> OnValueChanged;
        void Modify(float amount);
        void SetValue(float value);
        void Reset();
        void SetBaseValue(float newValue);
    }
}