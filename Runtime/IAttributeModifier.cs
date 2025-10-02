namespace StatsForge
{
    public interface IAttributeModifier
    {
        string ID { get; }
        string Source { get; }
        ModifierType ModifierType { get; }
        float Value { get; }
        int Order { get; } 
        bool IsExpired { get; }
    }
}