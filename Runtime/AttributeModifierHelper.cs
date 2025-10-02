using UnityEngine;

namespace StatsForge
{
    public static class AttributeModifierHelper
    {
        private static int nextId;

        private static string GenerateId(string prefix = "mod")
        {
            return $"{prefix}_{nextId++}_{Time.time:F2}";
        }

        public static AttributeModifier Create(string id, ModifierType type, float value, ModifierApplication application, string source = "Unknown", float duration = 0f, int priority = 0)
        {
            return new AttributeModifier(id, source, type, application, value, duration, priority);
        }

        public static AttributeModifier CreateFlat(float value, string source = "Unknown", float duration = 0f, string customId = null)
        {
            var application = duration > 0 ? ModifierApplication.Temporary : ModifierApplication.Permanent;
            var id = customId ?? GenerateId("flat");
            return new AttributeModifier(id, source, ModifierType.Flat, application, value, duration);
        }

        public static AttributeModifier CreatePercentage(float percentage, string source = "Unknown", float duration = 0f, string customId = null)
        {
            var application = duration > 0 ? ModifierApplication.Temporary : ModifierApplication.Permanent;
            var id = customId ?? GenerateId("perc");
            return new AttributeModifier(id, source, ModifierType.PercentageAdd, application, percentage, duration);
        }

        public static AttributeModifier CreateMultiplier(float multiplierPercentage, string source = "Unknown", float duration = 0f, string customId = null)
        {
            var application = duration > 0 ? ModifierApplication.Temporary : ModifierApplication.Permanent;
            var id = customId ?? GenerateId("mult");
            return new AttributeModifier(id, source, ModifierType.PercentageMultiply, application, multiplierPercentage, duration);
        }

        public static AttributeModifier CreateOverTime(float totalValue, float duration, ModifierType type = ModifierType.Flat, string source = "Unknown", string customId = null)
        {
            var id = customId ?? GenerateId("ot");
            return new AttributeModifier(id, source, type, ModifierApplication.OverTime, totalValue, duration);
        }

        public static AttributeModifier CreateInstant(float value, ModifierType type = ModifierType.Flat, string source = "Unknown", string customId = null)
        {
            var id = customId ?? GenerateId("instant");
            return new AttributeModifier(id, source, type, ModifierApplication.Instant, value, 0f);
        }

        public static AttributeModifier CreateTemporary(float value, float duration, ModifierType type = ModifierType.Flat, string source = "Unknown", string customId = null)
        {
            var id = customId ?? GenerateId("temp");
            return new AttributeModifier(id, source, type, ModifierApplication.Temporary, value, duration);
        }

        public static AttributeModifier CreatePermanent(float value, ModifierType type = ModifierType.Flat, string source = "Unknown", string customId = null)
        {
            var id = customId ?? GenerateId("perm");
            return new AttributeModifier(id, source, type, ModifierApplication.Permanent, value, 0f);
        }

        public static AttributeModifier CreateBuff(float value, float duration, ModifierType type = ModifierType.Flat, string source = "Buff", string customId = null)
        {
            value = Mathf.Abs(value);
            var application = duration > 0 ? ModifierApplication.Temporary : ModifierApplication.Permanent;
            var id = customId ?? GenerateId("buff");
            return new AttributeModifier(id, source, type, application, value, duration);
        }

        public static AttributeModifier CreateDebuff(float value, float duration, ModifierType type = ModifierType.Flat, string source = "Debuff", string customId = null)
        {
            value = -Mathf.Abs(value);
            var application = duration > 0 ? ModifierApplication.Temporary : ModifierApplication.Permanent;
            var id = customId ?? GenerateId("debuff");
            return new AttributeModifier(id, source, type, application, value, duration);
        }
    }
}