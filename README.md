# 🎭 Story Engine — Temă Semestrială

Un motor de povești interactive cu două aplicații Windows Forms: **Editor** și **Player**.

---

## Structura proiectului

```
StoryEngine/
├── StoryEngine.Model/         ← Clasele de date (StoryDefinition, StoryBlock, etc.)
├── StoryEngine.Engine/        ← Logica motorului (GameState, ConditionEvaluator, EffectApplicator, StoryRuntime)
├── StoryEngine.Persistence/   ← Citire/scriere fișiere .story (ZIP + JSON)
├── StoryEngine.Player/        ← Aplicația Player (WinForms)
├── StoryEngine.Editor/        ← Aplicația Editor (WinForms)
├── demo_cazul_din_strada_florilor.story  ← Poveste demo completă
├── build_and_run.bat          ← Script de build Windows
└── StoryEngine.sln            ← Solution Visual Studio
```

---

## Cerințe

- **Windows 10/11**
- **.NET 8 SDK** — https://dotnet.microsoft.com/download/dotnet/8.0
- **Visual Studio 2022** (recomandat) sau orice editor cu suport C#

---

## Cum compilezi și rulezi

### Opțiunea 1 — Script automat
Rulează `build_and_run.bat` și urmează instrucțiunile.

### Opțiunea 2 — Visual Studio
1. Deschide `StoryEngine.sln`
2. Build → Build Solution (Ctrl+Shift+B)
3. Setează startup project la `StoryEngine.Player` sau `StoryEngine.Editor`
4. F5 să rulezi

### Opțiunea 3 — Linie de comandă
```bash
dotnet build StoryEngine.sln -c Release
dotnet run --project StoryEngine.Player -c Release
dotnet run --project StoryEngine.Editor -c Release
```

---

## Formatul fișierului `.story`

Un fișier `.story` este un **arhivă ZIP** care conține:
- `definition.json` — definiția completă a poveștii
- `images/` *(opțional)* — imagini de fundal referite în blocuri

### Structura `definition.json`

```json
{
  "title": "Titlul poveștii",
  "startBlock": "id_bloc_initial",
  "properties": [
    {
      "key": "sanatate",
      "hudLabel": "Sănătate",
      "min": 0, "max": 100, "initial": 100,
      "visibleInHud": true,
      "hudOrder": 1,
      "onMinBlock": "bloc_daca_ajunge_la_0",
      "onMaxBlock": null
    }
  ],
  "blocks": [
    {
      "id": "intro",
      "text": "Textul narativ al blocului...",
      "isFinal": false,
      "backgroundImage": "fundal.jpg",
      "decisions": [
        {
          "text": "Mergi spre nord",
          "targetBlock": "nord",
          "icon": "⬆️",
          "condition": {
            "type": "COMPARISON",
            "property": "sanatate",
            "operator": ">=",
            "value": 50
          },
          "effects": [
            { "type": "ADD", "property": "sanatate", "value": -10 }
          ]
        }
      ]
    }
  ]
}
```

---

## Sintaxa efectelor

Efectele modifică proprietățile stării la alegerea unei decizii:

| Tip  | Descriere          | Exemplu                                     |
|------|--------------------|---------------------------------------------|
| ADD  | Adaugă la valoare  | `{ "type": "ADD", "property": "sanatate", "value": -20 }` |
| SET  | Setează valoarea   | `{ "type": "SET", "property": "suspiciune", "value": 0 }` |

Valorile sunt **automat limitate** la [min, max] definit pentru proprietate.

---

## Sintaxa condițiilor

Condițiile controlează vizibilitatea deciziilor:

```json
// Simplu
{ "type": "COMPARISON", "property": "indicii", "operator": ">=", "value": 5 }

// AND (toate trebuie adevărate)
{ "type": "AND", "conditions": [ {...}, {...} ] }

// OR (cel puțin una adevărată)
{ "type": "OR", "conditions": [ {...}, {...} ] }
```

Operatori suportați: `<`, `<=`, `>`, `>=`, `==`, `!=`

---

## Sintaxa efectelor în Editor (câmpul „Efecte")

În câmpul de efecte din grid se folosește formatul text scurt:
```
proprietate:TIP:valoare
```

Exemple:
```
sanatate:ADD:-20
suspiciune:ADD:30
indicii:SET:0
sanatate:ADD:-10,suspiciune:ADD:15
```

---

## Aplicația Player

- **Ctrl+O** — Deschide fișier `.story`
- **F5** — Repornește povestea curentă
- HUD afișează toate proprietățile vizibile
- Deciziile cu condiții neîndeplinite **nu apar** în listă
- La finalul poveștii apare butonul „Repornește"

## Aplicația Editor

- **Tab „Blocuri"** — Creează/editează blocuri narative
  - ID bloc, text, imagine fundal, checkbox final
  - Grid decizii: text, bloc destinație, icon, efecte
  - Buton „Salvează blocul" — comite modificările în memorie
- **Tab „Poveste / Proprietăți"** — Meta-date și statistici personaj
- **Ctrl+S** — Salvează fișierul `.story`
- **Ctrl+O** — Deschide fișier existent
- **Ctrl+N** — Poveste nouă

---

## Povestea demo inclusa

**„Umbra Carpaților"** — Un mister în Transilvania, 1921. Jurnalistul Andrei Lazăr
investighează moartea suspectă a unui bătrân bancher, înfruntând un fiu lacom,
o văduvă misterioasă și o conspirație care implică poliția locală.

- **32 de blocuri narative**, cu rute multiple care converg și diverg
- **4 proprietăți**: ❤ Sănătate, 👁 Suspiciune, 🤝 Aliați, 🔍 Indicii
- **6 finaluri în total**:
  - 3 finaluri principale (alese prin decizii directe la confruntarea finală):
    - ⚖️ **Dreptatea Carpaților** — victorie completă (necesită ≥4 indicii)
    - 🕊️ **Umbra Rămâne** — compromis, dreptate parțială
    - 💸 **Prețul Tăcerii** — corupție, jucătorul acceptă mita
  - 3 finaluri alternative ("rele"), accesibile prin gestionare proastă a stării:
    - 🚔 **Prea Multă Agitație** — suspiciune ajunge la 100
    - 💀 **Drumul Fără Întoarcere** — sănătate ajunge la 0
    - ❌ **Fără Dovezi** — eșec la poliție din lipsă de probe
- Decizii condiționate (unele apar doar dacă ai suficiente indicii adunate)
- Multiple căi de a strânge aliați (Petre, Vera) care influențează finalul
- Efecte pe sănătate, suspiciune și indicii la fiecare alegere importantă

---

## Arhitectura (pentru documentație)

```
┌─────────────────────────────────────────────────────┐
│                   StoryEngine.Model                  │
│  StoryDefinition, StoryBlock, DecisionDefinition,   │
│  EffectDefinition, ConditionNode, StatePropertyDef  │
└────────────────────┬────────────────────────────────┘
                     │ referit de
        ┌────────────┴─────────────┐
        │                          │
┌───────▼───────┐       ┌──────────▼──────────┐
│ StoryEngine   │       │ StoryEngine         │
│ .Engine       │       │ .Persistence        │
│               │       │                     │
│ GameState     │       │ StoryFile           │
│ ConditionEval │       │ (ZIP load/save)     │
│ EffectAppl.   │       └──────────┬──────────┘
│ StoryRuntime  │                  │
└──────┬────────┘                  │
       │                           │
       └──────────┬────────────────┘
                  │
       ┌──────────┴──────────┐
       │                     │
┌──────▼──────┐    ┌─────────▼───────┐
│  Player     │    │    Editor       │
│ (WinForms)  │    │  (WinForms)     │
└─────────────┘    └─────────────────┘
```

---

*Temă semestrială — Programare Avansată — 2025*

