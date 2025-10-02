namespace StatsForge
{
    public enum ModifierType
    {
        Flat,              // +10 (adiciona valor fixo)
        PercentageAdd,     // +20% (adiciona porcentagem)
        PercentageMultiply // x1.2 (multiplica)
    }
    
    public enum ModifierApplication
    {
        Instant,           // Aplica uma vez e remove
        Temporary,         // Aplica por duração específica
        Permanent,         // Aplica permanentemente
        OverTime          // Aplica gradualmente ao longo do tempo
    }
}