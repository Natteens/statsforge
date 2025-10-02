# ⚡ **StatsForge**

Sistema modular de atributos com modificadores temporais para Unity

[![Unity](https://img.shields.io/badge/Unity-2022.3+-blue.svg)](https://unity3d.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Version](https://img.shields.io/badge/Version-1.0.0-orange.svg)](CHANGELOG.md)

---

## 🎯 O que é?

StatsForge é um sistema completo de atributos para Unity que permite:

* ✅ Criar atributos customizados (Health, Mana, Strength, etc.)
* ✅ Aplicar modificadores temporários e permanentes
* ✅ Interface visual integrada ao Unity Editor
* ✅ Runtime totalmente funcional
* ✅ Performance otimizada

---

## 🚀 Instalação Rápida

### Via Package Manager

1. Window → Package Manager
2. `+` → *Add package from git URL*
3. Cole:

   ```text
   https://github.com/Natteens/statsforge.git
   ```

---

## 📖 Guia Rápido

### 1. Setup Básico

```csharp
using StatsForge;

public class Player : MonoBehaviour {
    private EntityAttributes stats;
    void Start() {
        stats = GetComponent<EntityAttributes>();
    }
}
```

### 2. Acessar Valores

```csharp
// API simplificada
float health = stats["Health"];
float mana   = stats["Mana"];

// API tradicional
float speed = stats.GetValue("Speed");

// Verificação segura
if (stats.TryGetAttribute("Strength", out var strengthAttr)) {
    float current = strengthAttr.CurrentValue;
    float baseVal = strengthAttr.BaseValue;
}
```

### 3. Modificar Valores

```csharp
// Definir valor (limpa modificadores)
stats["Health"] = 100f;

// Alterar base (mantém modificadores)
stats.SetBaseValue("Health", 100f);

// Aplicar modificadores
stats.AddFlat("Health", 50f, 10f, "Healing Potion");   // +50 por 10s
stats.AddPercentage("Speed", 20f, 5f, "Speed Boost");  // +20% por 5s
stats.AddMultiplier("Damage", 50f, 8f, "Damage Buff"); // x1.5 por 8s
```

### 4. Escutar Mudanças

```csharp
void Start() {
    stats.OnAttributeChanged += OnStatChanged;
}

private void OnStatChanged(AttributeType type, float newValue) {
    if (type.Name == "Health") {
        UpdateHealthBar(newValue);
    }
}
```

---

## 🎮 Exemplos Práticos

### Sistema de Combate

```csharp
public class Combat : MonoBehaviour {
    public void Attack(EntityAttributes target) {
        float damage = stats["Attack"];
        float defense = target["Defense"];
        float finalDamage = Mathf.Max(1f, damage - defense);
        target.AddFlat("Health", -finalDamage, 0f, "Combat");
    }
}
```

### Sistema de Poções

```csharp
public class HealthPotion : MonoBehaviour {
    [SerializeField] private float healAmount = 50f;
    public void Use(EntityAttributes target) {
        target.AddFlat("Health", healAmount, 0f, "Health Potion");
    }
}

public class StrengthPotion : MonoBehaviour {
    [SerializeField] private float bonus = 20f;
    [SerializeField] private float duration = 30f;
    public void Use(EntityAttributes target) {
        target.AddPercentage("Strength", bonus, duration, "Strength Potion");
    }
}
```

### Sistema de Buff/Debuff

```csharp
public class StatusEffects : MonoBehaviour {
    public void ApplyPoison(EntityAttributes target) {
        target.AddPercentage("Health", -10f, 8f, "Poison");
    }
    public void ApplyHaste(EntityAttributes target) {
        target.AddPercentage("Speed", 50f, 5f, "Haste");
    }
    public void ApplyWeakness(EntityAttributes target) {
        target.AddPercentage("Strength", -25f, 12f, "Weakness");
    }
}
```

### Sistema de Level Up

```csharp
public class LevelSystem : MonoBehaviour {
    public void LevelUp(EntityAttributes character) {
        character.AddFlat("Health", 20f, 0f, "Level Up");
        character.AddFlat("Mana", 15f, 0f, "Level Up");
        character.AddFlat("Strength", 5f, 0f, "Level Up");
    }
}
```

---

## 🎨 Interface Visual

### Attributes Manager

* Tools → Attributes Manager
* Criação de atributos customizados
* Configuração de *Attribute Sets*
* Interface drag-and-drop

### Runtime Inspector

* Visualização em tempo real
* Lista de modificadores ativos
* Debug detalhado de cálculos
* Botão para limpar modificadores

---

## 🛠️ API Completa

### EntityAttributes

```csharp
// Acesso
float this[string name] { get; set; }
float GetValue(string name);
float GetBaseValue(string name);
bool HasAttribute(string name);
bool TryGetAttribute(string name, out ModifiableAttributeInstance instance);

// Modificação
void SetValue(string name, float value);
void SetBaseValue(string name, float value);

// Modificadores
void AddFlat(string name, float value, float duration, string source);
void AddPercentage(string name, float percentage, float duration, string source);
void AddMultiplier(string name, float multiplier, float duration, string source);
void ApplyModifier(string name, AttributeModifier modifier);
bool RemoveModifier(string name, string modifierId);
void ClearAllModifiers(string name = null);
void ClearModifiersBySource(string source, string name = null);

// Utilitários
int GetModifierCount(string name);
IEnumerable<string> GetAttributeNames();
IReadOnlyDictionary<string, ModifiableAttributeInstance> AllAttributes;
```

### AttributeModifierHelper

```csharp
AttributeModifier CreateFlat(float value, string source, float duration, string id);
AttributeModifier CreatePercentage(float percentage, string source, float duration, string id);
AttributeModifier CreateMultiplier(float multiplier, string source, float duration, string id);
AttributeModifier CreateBuff(float value, float duration, ModifierType type, string source, string id);
AttributeModifier CreateDebuff(float value, float duration, ModifierType type, string source, string id);
```

---

## 📊 Tipos de Modificadores

| Tipo                   | Descrição                    | Exemplo       |
| ---------------------- | ---------------------------- | ------------- |
| **Flat**               | Valor fixo                   | `+50 Health`  |
| **PercentageAdd**      | Percentual do valor base     | `+20% Speed`  |
| **PercentageMultiply** | Multiplica o resultado final | `x1.5 Damage` |

### Aplicações

| Aplicação     | Descrição             | Duração        |
| ------------- | --------------------- | -------------- |
| **Instant**   | Aplica e remove       | `0s`           |
| **Temporary** | Valor temporário      | `5s, 10s, ...` |
| **Permanent** | Permanente            | `∞`            |
| **OverTime**  | Aplicado gradualmente | `X segundos`   |

---

## ❓ FAQ

<details>
<summary><strong>Como funciona o cálculo de modificadores?</strong></summary>

```
1. Valor Base: 100
2. Flat: +20 = 120
3. Percentage: +10% do base (100) = +10 → 130
4. Multiply: x1.2 = 156
```

</details>

<details>
<summary><strong>Posso usar em projetos comerciais?</strong></summary>
Sim, a licença MIT permite uso comercial.
</details>

<details>
<summary><strong>Como debugar cálculos?</strong></summary>

Use o botão `?` no inspector ou:

```csharp
Debug.Log(attributeInstance.GetCalculationBreakdown());
```

</details>

---

## 📞 Suporte

* 🐛 [Issues](https://github.com/Natteens/statsforge/issues)
* 📧 Email: `natteens.social@gmail.com`
* 📖 [Wiki](https://github.com/Natteens/statsforge/wiki)

---

<div align="center">

**⚡ Desenvolvido por [Natteens](https://github.com/Natteens) ⚡**

[![GitHub stars](https://img.shields.io/github/stars/Natteens/statsforge.svg?style=social\&label=Star)](https://github.com/Natteens/statsforge)

</div>
