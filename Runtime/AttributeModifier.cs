using System;
using UnityEngine;

namespace StatsForge
{
    [System.Serializable]
    public class AttributeModifier
    {
        [SerializeField] private string id;
        [SerializeField] private string source;
        [SerializeField] private ModifierType modifierType;
        [SerializeField] private ModifierApplication application;
        [SerializeField] private float value;
        [SerializeField] private float duration;
        [SerializeField] private int priority;
        
        private float startTime;
        private float appliedAmount;
        private bool isActive;
        private bool isExpired;

        public string ID => id;
        public string Source => source;
        public ModifierType ModifierType => modifierType;
        public ModifierApplication Application => application;
        public float Value => value;
        public float Duration => duration;
        public int Priority => priority;
        public bool IsActive => isActive;
        public bool IsExpired => isExpired;
        public float RemainingTime => duration > 0 ? Mathf.Max(0, duration - (Time.time - startTime)) : 0;
        public float AppliedAmount => appliedAmount;

        public event Action<AttributeModifier> OnExpired;

        public AttributeModifier(string id, string source, ModifierType type, ModifierApplication app, float value, float duration = 0f, int priority = 0)
        {
            this.id = id;
            this.source = source;
            this.modifierType = type;
            this.application = app;
            this.value = value;
            this.duration = duration;
            this.priority = priority;
            this.appliedAmount = 0f;
            this.isActive = false;
            this.isExpired = false;
        }

        public void Activate()
        {
            if (isActive) return;
            
            isActive = true;
            startTime = Time.time;
            
            if (application == ModifierApplication.Instant)
            {
                appliedAmount = value;
                isExpired = true;
            }
        }

        public float GetCurrentValue()
        {
            if (!isActive || isExpired) return 0f;

            switch (application)
            {
                case ModifierApplication.Instant:
                    return value;
                    
                case ModifierApplication.Temporary:
                case ModifierApplication.Permanent:
                    return value;
                    
                case ModifierApplication.OverTime:
                    float elapsed = Time.time - startTime;
                    if (duration > 0)
                    {
                        float progress = Mathf.Clamp01(elapsed / duration);
                        return value * progress;
                    }
                    return value;
                    
                default:
                    return value;
            }
        }

        public void Update()
        {
            if (!isActive || isExpired) return;

            if (application == ModifierApplication.Temporary && duration > 0)
            {
                if (Time.time >= startTime + duration)
                {
                    Expire();
                }
            }
            else if (application == ModifierApplication.OverTime && duration > 0)
            {
                if (Time.time >= startTime + duration)
                {
                    appliedAmount = value;
                    Expire();
                }
            }
        }

        private void Expire()
        {
            isExpired = true;
            OnExpired?.Invoke(this);
        }

        public void ForceExpire()
        {
            Expire();
        }
    }
}