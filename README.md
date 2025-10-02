# 🔥 StatsForge

<div align="center">

[![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-blue.svg)](https://unity3d.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Mac%20%7C%20Linux-lightgrey.svg)](https://unity3d.com)
[![Version](https://img.shields.io/badge/Version-1.0.0-green.svg)](https://github.com/Natteens/statsforge/releases)

</div>

---

<div align="center">

> 🎮 **Sistema modular de atributos com modificadores temporais, suporte a runtime e interface visual completa para Unity.**

</div>

---

## ✨ Características Principais

<table>
<tr>
<td align="center">🧩</td>
<td><strong>Sistema Modular</strong><br/>Arquitetura flexível e expansível</td>
<td align="center">⚡</td>
<td><strong>Performance Otimizada</strong><br/>Processamento eficiente em runtime</td>
</tr>
<tr>
<td align="center">🎨</td>
<td><strong>Interface Visual</strong><br/>Editor completo integrado ao Unity</td>
<td align="center">🔄</td>
<td><strong>Modificadores Temporais</strong><br/>Sistema avançado de buffs e debuffs</td>
</tr>
<tr>
<td align="center">🏗️</td>
<td><strong>Runtime Support</strong><br/>Funciona perfeitamente durante execução</td>
<td align="center">📊</td>
<td><strong>Attribute Sets</strong><br/>Configurações predefinidas reutilizáveis</td>
</tr>
<tr>
<td align="center">🎯</td>
<td><strong>Type Safe</strong><br/>Sistema fortemente tipado</td>
<td align="center">📈</td>
<td><strong>Escalável</strong><br/>Suporta desde projetos simples até complexos</td>
</tr>
</table>

---

## 📥 Instalação

### 🚀 Via Package Manager (Recomendado)

1. Abra o **Package Manager** (`Window > Package Manager`)
2. Clique no botão **`+`** no canto superior esquerdo
3. Selecione **`Add package from git URL...`**
4. Digite a URL:
   ```
   https://github.com/Natteens/statsforge.git
   ```
5. Clique em **`Add`**

### 📝 Via manifest.json

Adicione a seguinte linha ao arquivo `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.natteens.statsforge": "https://github.com/Natteens/statsforge.git"
  }
}
```

---

## 🚀 Guia de Uso Completo

### 1️⃣ Configuração Inicial

<details>
<summary><strong>📊 Criar Database de Atributos</strong></summary>

```csharp
// O sistema cria automaticamente o database em:
// Assets/Resources/Attributes/AttributeDatabase.asset

// Acesse via: Tools > Attributes Manager
```

</details>

<details>
<summary><strong>⚙️ Criar Seus Primeiros Atributos</strong></summary>

No Attributes Manager, crie atributos como:
- **Health** (Categoria: Combat)
- **Mana** (Categoria: Combat) 
- **Strength** (Categoria: Core)
- **Defense** (Categoria: Core)
- **Speed** (Categoria: Movement)

</details>

### 2️⃣ Configurar Attribute Sets

Crie sets para diferentes tipos de personagens:
- `PlayerBaseStats`
- `EnemyWarriorStats` 
- `EnemyMageStats`
- `BossStats`

### 3️⃣ Implementação Básica

#### 🎯 Setup do Personagem

```csharp
using StatsForge;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private AttributeSet playerStats;
    private EntityAttributes entityAttributes;
    
    void Start()
    {
        // Obter o componente EntityAttributes
        entityAttributes = GetComponent<EntityAttributes>();
        
        // O sistema inicializa automaticamente com o AttributeSet configurado
        
        // Escutar mudanças nos atributos
        entityAttributes.OnAttributeChanged += OnStatChanged;
    }
    
    private void OnStatChanged(AttributeType attributeType, float newValue)
    {
        Debug.Log($"{attributeType.Name} mudou para: {newValue}");
    }
}
```

---

## 📖 API Reference

### 🎯 Acessando Valores de Atributos

<table>
<tr>
<th>Método</th>
<th>Descrição</th>
<th>Exemplo</th>
</tr>
<tr>
<td><code>GetValue(string)</code></td>
<td>Obtém o valor atual de um atributo</td>
<td><code>float health = stats.GetValue("Health");</code></td>
</tr>
<tr>
<td><code>TryGet(string, out instance)</code></td>
<td>Tenta obter a instância do atributo</td>
<td><code>if (stats.TryGet("Mana", out var mana))</code></td>
</tr>
</table>

#### 💡 Exemplo Prático

```csharp
public class PlayerController : MonoBehaviour
{
    private EntityAttributes stats;
    
    void Start()
    {
        stats = GetComponent<EntityAttributes>();
    }
    
    void Update()
    {
        // Obter valor atual de um atributo
        float currentHealth = stats.GetValue("Health");
        
        // Verificar se um atributo existe
        if (stats.TryGet("Mana", out ModifiableAttributeInstance manaInstance))
        {
            float currentMana = manaInstance.CurrentValue;
            float maxMana = manaInstance.BaseValue;
            
            Debug.Log($"Mana: {currentMana}/{maxMana}");
        }
        
        // Exemplo prático: movimento baseado em velocidade
        float speed = stats.GetValue("Speed");
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}
```

---

## 🔄 Sistema de Modificadores

### ⚙️ Tipos de Modificadores

```csharp
public enum ModifierType
{
    Additive,      // +10 pontos
    Multiplicative, // x1.5 multiplicador
    Override       // Define valor específico
}
```

### 💊 Aplicando Buffs e Debuffs

<details>
<summary><strong>🟢 Poção de Força (Buff)</strong></summary>

```csharp
public void ApplyStrengthPotion()
{
    if (playerStats.TryGet("Strength", out var strength))
    {
        var strengthBuff = new AttributeModifier
        {
            Type = ModifierType.Additive,
            Value = 10f,
            Duration = 30f, // 30 segundos
            Source = "Strength Potion"
        };
        
        strength.AddModifier(strengthBuff);
    }
}
```

</details>

<details>
<summary><strong>🟡 Veneno (Debuff)</strong></summary>

```csharp
public void ApplyPoison()
{
    if (playerStats.TryGet("Health", out var health))
    {
        var poisonDebuff = new AttributeModifier
        {
            Type = ModifierType.Multiplicative,
            Value = -0.1f, // -10% de vida
            Duration = 10f,
            Source = "Poison"
        };
        
        health.AddModifier(poisonDebuff);
    }
}
```

</details>

---

## 🎮 Exemplos Práticos

### ⚔️ Sistema de Combate

```csharp
public class CombatSystem : MonoBehaviour
{
    public void PerformAttack(EntityAttributes attacker, EntityAttributes target)
    {
        // Calcular dano baseado na força do atacante
        float attackPower = attacker.GetValue("Strength");
        float defense = target.GetValue("Defense");
        
        // Fórmula simples de dano
        float damage = Mathf.Max(1f, attackPower - defense);
        
        // Aplicar dano à vida do alvo
        if (target.TryGet("Health", out var healthInstance))
        {
            var damageModifier = new AttributeModifier
            {
                Type = ModifierType.Additive,
                Value = -damage,
                Duration = 0f, // Instantâneo
                Source = "Combat Damage"
            };
            
            healthInstance.AddModifier(damageModifier);
            
            Debug.Log($"Dano aplicado: {damage}. Vida restante: {healthInstance.CurrentValue}");
        }
    }
}
```

### 📈 Sistema de Progressão

```csharp
public class LevelSystem : MonoBehaviour
{
    private EntityAttributes playerStats;
    
    void Start()
    {
        playerStats = GetComponent<EntityAttributes>();
    }
    
    public void LevelUp()
    {
        // Aumentar atributos permanentemente
        ApplyPermanentBonus("Health", 20f);
        ApplyPermanentBonus("Mana", 15f);
        ApplyPermanentBonus("Strength", 5f);
        ApplyPermanentBonus("Defense", 3f);
    }
    
    private void ApplyPermanentBonus(string attributeName, float bonus)
    {
        if (playerStats.TryGet(attributeName, out var attribute))
        {
            var levelBonus = new AttributeModifier
            {
                Type = ModifierType.Additive,
                Value = bonus,
                Duration = -1f, // Permanente
                Source = "Level Up"
            };
            
            attribute.AddModifier(levelBonus);
        }
    }
}
```

### 🖥️ Integração com UI

```csharp
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider manaBar;
    [SerializeField] private Text strengthText;
    
    private EntityAttributes playerStats;
    
    void Start()
    {
        playerStats = FindObjectOfType<Player>().GetComponent<EntityAttributes>();
        playerStats.OnAttributeChanged += UpdateUI;
        
        // Atualização inicial
        UpdateUI(null, 0f);
    }
    
    private void UpdateUI(AttributeType changedAttribute, float newValue)
    {
        // Atualizar barra de vida
        if (playerStats.TryGet("Health", out var health))
        {
            healthBar.value = health.CurrentValue / health.BaseValue;
        }
        
        // Atualizar barra de mana
        if (playerStats.TryGet("Mana", out var mana))
        {
            manaBar.value = mana.CurrentValue / mana.BaseValue;
        }
        
        // Atualizar texto de força
        float strength = playerStats.GetValue("Strength");
        strengthText.text = $"STR: {strength:F0}";
    }
}
```

---

## 📚 Conceitos Principais

<table>
<tr>
<th>Conceito</th>
<th>Descrição</th>
<th>Exemplo</th>
</tr>
<tr>
<td><strong>AttributeType</strong></td>
<td>Definição base de um atributo</td>
<td>Health, Mana, Strength</td>
</tr>
<tr>
<td><strong>AttributeSet</strong></td>
<td>Coleção de atributos com valores iniciais</td>
<td>PlayerBaseStats.asset</td>
</tr>
<tr>
<td><strong>EntityAttributes</strong></td>
<td>Componente que gerencia atributos de uma entidade</td>
<td>MonoBehaviour no Player</td>
</tr>
<tr>
<td><strong>ModifiableAttributeInstance</strong></td>
<td>Instância de atributo que pode receber modificadores</td>
<td>health.AddModifier(buff)</td>
</tr>
<tr>
<td><strong>AttributeModifier</strong></td>
<td>Modificação temporária ou permanente</td>
<td>Poção, Buff, Debuff</td>
</tr>
</table>

---

## 🎨 Exemplo Completo: RPG Character

<details>
<summary><strong>🎭 Clique para ver o exemplo completo</strong></summary>

```csharp
using StatsForge;
using UnityEngine;

public class RPGCharacter : MonoBehaviour
{
    [Header("Stats Configuration")]
    [SerializeField] private AttributeSet characterStats;
    
    [Header("UI References")]
    [SerializeField] private CharacterUI characterUI;
    
    private EntityAttributes stats;
    
    void Start()
    {
        InitializeCharacter();
        SetupEventListeners();
    }
    
    private void InitializeCharacter()
    {
        stats = GetComponent<EntityAttributes>();
        
        // Aplicar modificadores iniciais baseados na classe
        ApplyClassModifiers();
    }
    
    private void ApplyClassModifiers()
    {
        // Exemplo: Guerreiro tem mais vida e força
        if (characterStats.name.Contains("Warrior"))
        {
            ApplyModifier("Health", ModifierType.Multiplicative, 1.5f, -1f, "Warrior Class");
            ApplyModifier("Strength", ModifierType.Additive, 10f, -1f, "Warrior Class");
        }
        // Exemplo: Mago tem mais mana
        else if (characterStats.name.Contains("Mage"))
        {
            ApplyModifier("Mana", ModifierType.Multiplicative, 2f, -1f, "Mage Class");
            ApplyModifier("Strength", ModifierType.Additive, -5f, -1f, "Mage Class");
        }
    }
    
    private void ApplyModifier(string attributeName, ModifierType type, float value, float duration, string source)
    {
        if (stats.TryGet(attributeName, out var attribute))
        {
            var modifier = new AttributeModifier
            {
                Type = type,
                Value = value,
                Duration = duration,
                Source = source
            };
            
            attribute.AddModifier(modifier);
        }
    }
    
    private void SetupEventListeners()
    {
        stats.OnAttributeChanged += (attr, value) => 
        {
            characterUI?.UpdateAttribute(attr.Name, value);
            
            // Verificar se morreu
            if (attr.Name == "Health" && value <= 0)
            {
                OnCharacterDeath();
            }
        };
    }
    
    private void OnCharacterDeath()
    {
        Debug.Log($"{gameObject.name} morreu!");
        // Implementar lógica de morte
    }
    
    // Métodos públicos para interação
    public void Heal(float amount)
    {
        ApplyModifier("Health", ModifierType.Additive, amount, 0f, "Healing");
    }
    
    public void TakeDamage(float damage)
    {
        ApplyModifier("Health", ModifierType.Additive, -damage, 0f, "Damage");
    }
    
    public void DrinkManaPotion(float amount)
    {
        ApplyModifier("Mana", ModifierType.Additive, amount, 0f, "Mana Potion");
    }
    
    public void ApplyTemporaryBuff(string attributeName, float value, float duration)
    {
        ApplyModifier(attributeName, ModifierType.Additive, value, duration, "Temporary Buff");
    }
}
```

</details>

---

## 🛠️ Ferramentas Incluídas

- 🎨 **Attributes Manager**: Interface visual para criação de atributos
- 📊 **Attribute Set Creator**: Criador de conjuntos predefinidos
- 👁️ **Runtime Inspector**: Visualização em tempo real dos valores
- 🔧 **Editor Integration**: Inspectors customizados

---

## ❓ FAQ

<details>
<summary>❓ <strong>Como obter o valor de um atributo?</strong></summary>

```csharp
EntityAttributes stats = GetComponent<EntityAttributes>();
float health = stats.GetValue("Health");
```

</details>

<details>
<summary>❓ <strong>Como aplicar um buff temporário?</strong></summary>

```csharp
if (stats.TryGet("Strength", out var strength))
{
    var buff = new AttributeModifier
    {
        Type = ModifierType.Additive,
        Value = 10f,
        Duration = 30f,
        Source = "Strength Potion"
    };
    strength.AddModifier(buff);
}
```

</details>

<details>
<summary>❓ <strong>Como escutar mudanças em atributos?</strong></summary>

```csharp
entityAttributes.OnAttributeChanged += (attributeType, newValue) => 
{
    Debug.Log($"{attributeType.Name} mudou para: {newValue}");
};
```

</details>

<details>
<summary>❓ <strong>Posso usar em projetos comerciais?</strong></summary>

Sim! Este projeto usa a licença MIT, permitindo uso comercial.

</details>

---

## 📊 Roadmap

- [x] 🎨 Sistema básico de atributos
- [x] 🔧 Editor visual integrado
- [ ] 🌐 Sistema de Networking
- [ ] 💾 Salvamento/Carregamento automático
- [ ] 🎯 Attribute Conditions e Requirements
- [ ] 📈 Analytics e Debugging tools
- [ ] 🔧 Visual Scripting Support
- [ ] 📱 Mobile Optimization

---

## 🤝 Contribuindo

Contribuições são bem-vindas! Por favor:

1. 🍴 Fork o projeto
2. 🌿 Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. 💾 Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. 📤 Push para a branch (`git push origin feature/AmazingFeature`)
5. 🔄 Abra um Pull Request

---

## 🆘 Suporte

- 📧 **Email**: [seu-email@exemplo.com]
- 💬 **Discord**: [Link do servidor]
- 🐛 **Issues**: [GitHub Issues](https://github.com/Natteens/statsforge/issues)
- 📖 **Wiki**: [GitHub Wiki](https://github.com/Natteens/statsforge/wiki)

---

## 📝 Changelog

Veja o [CHANGELOG.md](CHANGELOG.md) para detalhes sobre mudanças e atualizações.

## 📄 Licença

Este projeto está licenciado sob a **Licença MIT** - veja o arquivo [LICENSE.md](LICENSE.md) para detalhes.

---

<div align="center">

**⭐ Se este projeto te ajudou, considere dar uma estrela! ⭐**

**Feito com ❤️ por [Natteens](https://github.com/Natteens)**

[![GitHub stars](https://img.shields.io/github/stars/Natteens/statsforge.svg?style=social&label=Star)](https://github.com/Natteens/statsforge)
[![GitHub forks](https://img.shields.io/github/forks/Natteens/statsforge.svg?style=social&label=Fork)](https://github.com/Natteens/statsforge/fork)

</div>
