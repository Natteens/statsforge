using System;

namespace StatsForge
{
    [System.Serializable]
    public class AttributeInstance
    {
        private IAttributeType type;
        private float baseValue;
        private float currentValue;

        public IAttributeType Type => type;
        public float BaseValue => baseValue;
        public float CurrentValue => currentValue;

        public event Action<float> OnValueChanged;

        public AttributeInstance(IAttributeType type, float baseValue)
        {
            this.type = type;
            this.baseValue = baseValue;
            this.currentValue = baseValue;
        }

        public void Modify(float amount)
        {
            float oldValue = currentValue;
            currentValue += amount;
            
            if (Math.Abs(oldValue - currentValue) > 0.0001f)
            {
                OnValueChanged?.Invoke(currentValue);
            }
        }

        public void SetValue(float newValue)
        {
            if (Math.Abs(currentValue - newValue) < 0.0001f) return;
            
            currentValue = newValue;
            OnValueChanged?.Invoke(currentValue);
        }

        public void Reset()
        {
            if (Math.Abs(currentValue - baseValue) < 0.0001f) return;
            
            currentValue = baseValue;
            OnValueChanged?.Invoke(currentValue);
        }

        public void SetBaseValue(float newValue)
        {
            baseValue = newValue;
            Reset();
        }
    }
}