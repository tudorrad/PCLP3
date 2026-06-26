# StoryEngine

Un motor de povesti interactive cu arhitectura modulara, construit in C# pe .NET 8 si Windows Forms. Proiectul include doua aplicatii desktop distincte — **Editor** si **Player** — impreuna cu un layer de model, un motor de executie si un sistem de persistenta bazat pe arhive ZIP.

---

## Cuprins

- [Prezentare generala](#prezentare-generala)
- [Arhitectura proiectului](#arhitectura-proiectului)
- [Cerinte de sistem](#cerinte-de-sistem)
- [Instalare si pornire](#instalare-si-pornire)
- [Formatul fisierului .story](#formatul-fisierului-story)
- [Sistemul de efecte](#sistemul-de-efecte)
- [Sistemul de conditii](#sistemul-de-conditii)
- [Aplicatia Player](#aplicatia-player)
- [Aplicatia Editor](#aplicatia-editor)
- [Povestea demo inclusa](#povestea-demo-inclusa)
- [Depanare](#depanare)

---

## Prezentare generala

StoryEngine este un motor de naratiune ramificata (Choose Your Own Adventure) care permite crearea si redarea de povesti interactive cu stare dinamica. Fiecare poveste este definita printr-un graf de blocuri narative conectate prin decizii conditionare, iar starea jucatorului este modelata prin proprietati numerice cu valori minime si maxime configurabile.

Proiectul demonstreaza separarea clara a responsabilitatilor intr-o aplicatie desktop C#: modelul de date este complet independent de logica de executie, iar ambele sunt separate de stratul de persistenta si de interfetele grafice.

---

## Arhitectura proiectului

```
StoryEngine/
├── StoryEngine.Model/         -- Clasele de date (entitati pure, fara logica)
├── StoryEngine.Engine/        -- Logica motorului de executie
├── StoryEngine.Persistence/   -- Citire si scriere fisiere .story
├── StoryEngine.Player/        -- Aplicatia Player (WinForms)
├── StoryEngine.Editor/        -- Aplicatia Editor (WinForms)
├── demo_umbra_carpatilor.story -- Poveste demo completa (1.85 MB)
├── PORNESTE_TOTUL.bat         -- Script de build si lansare automata
├── PORNESTE_EDITOR.bat        -- Script de build si lansare a editorului
├── StoryEngine.sln            -- Solutie Visual Studio 2022
└── build_log.txt              -- Log de build generat automat
```

### StoryEngine.Model

Contine exclusiv clasele de date serializabile, fara nicio logica de executie:

- `StoryDefinition` — radacina unui fisier `.story`: titlu, bloc de start, lista de proprietati si lista de blocuri
- `StoryBlock` — un nod al grafului narativ: ID unic, text, imagine de fundal, flag `isFinal` si lista de decizii
- `DecisionDefinition` — o tranzitie intre blocuri: text afisat, bloc tinta, icon, conditie optionala si lista de efecte
- `EffectDefinition` — o modificare de stare: tip (`ADD` sau `SET`), proprietate tinta si valoare
- `ConditionNode` — un nod al arborelui de conditii: tip (`COMPARISON`, `AND`, `OR`), operand si sub-conditii
- `StatePropertyDef` — definitia unei proprietati de stare: cheie, eticheta HUD, valori min/max/initial, vizibilitate si blocuri de overflow

### StoryEngine.Engine

Contine toata logica de executie a motorului:

- `GameState` — starea curenta a jucatorului: valorile proprietatilor si blocul activ
- `ConditionEvaluator` — evalueaza arbori de conditii (`COMPARISON`, `AND`, `OR`) cu operatorii `<`, `<=`, `>`, `>=`, `==`, `!=`
- `EffectApplicator` — aplica efecte de tip `ADD` si `SET` pe starea jucatorului, cu clampare automata la [min, max]
- `StoryRuntime` — orchestreaza executia: incarca definitia, initializeaza starea, filtreaza deciziile disponibile, aplica efectele la alegere si detecteaza finalurile si blocurile de overflow

### StoryEngine.Persistence

Gestioneaza serializarea si deserializarea fisierelor `.story`:

- `StoryFile` — clasa principala de I/O: deschide arhiva ZIP, deserializeaza `definition.json` prin `System.Text.Json` si extrage imaginile din directorul `images/` al arhivei

### StoryEngine.Player si StoryEngine.Editor

Doua aplicatii WinForms independente care referencieaza aceleasi proiecte de infrastructura (`Model`, `Engine`, `Persistence`) fara a se depinde una de cealalta.

Diagrama dependentelor:

```
StoryEngine.Model
       |
       +-- StoryEngine.Engine
       |          |
       +-- StoryEngine.Persistence
                  |
         +--------+--------+
         |                 |
  StoryEngine.Player  StoryEngine.Editor
```

---

## Cerinte de sistem

- **Sistem de operare:** Windows 10 sau Windows 11
- **.NET 8 SDK** — [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** (optional, recomandat pentru dezvoltare)

Verificare instalare .NET:

```cmd
dotnet --version
```

Trebuie sa afiseze o versiune `8.x.x`.

---

## Instalare si pornire

### Metoda 1 — Script automat (recomandata)

Dublu-click pe `PORNESTE_TOTUL.bat`.

Scriptul executa urmatorii pasi in ordine:

1. Sterge toate directoarele `bin/` si `obj/` pentru o compilare curata
2. Compileaza pe rand: `Model`, `Engine`, `Persistence`, `Player`
3. Verifica exit code-ul la fiecare pas si se opreste la prima eroare
4. Scrie output-ul complet de compilare in `build_log.txt`
5. Lanseaza executabilul `StoryEngine.Player.exe` intr-un proces separat

Pentru a lansa editorul, folositi `PORNESTE_EDITOR.bat`.

### Metoda 2 — Visual Studio 2022

1. Deschideti `StoryEngine.sln`
2. Click dreapta pe `StoryEngine.Player` in Solution Explorer si selectati `Set as Startup Project`
3. Apasati `F5` pentru a compila si lansa aplicatia

### Metoda 3 — Linie de comanda

```cmd
dotnet build StoryEngine.sln -c Release
dotnet run --project StoryEngine.Player -c Release
dotnet run --project StoryEngine.Editor -c Release
```

---

## Formatul fisierului .story

Un fisier `.story` este o **arhiva ZIP** cu urmatoarea structura interna:

```
my_story.story  (arhiva ZIP)
├── definition.json       -- Definitia completa a poveștii
└── images/               -- (optional) Imagini de fundal referentiate in blocuri
    ├── fundal_intro.jpg
    └── ...
```

### Structura definition.json

```json
{
  "title": "Titlul poveștii",
  "startBlock": "id_bloc_initial",
  "properties": [
    {
      "key": "sanatate",
      "hudLabel": "Sanatate",
      "min": 0,
      "max": 100,
      "initial": 100,
      "visibleInHud": true,
      "hudOrder": 1,
      "onMinBlock": "bloc_game_over",
      "onMaxBlock": null
    }
  ],
  "blocks": [
    {
      "id": "intro",
      "text": "Textul narativ al blocului.",
      "isFinal": false,
      "backgroundImage": "fundal_intro.jpg",
      "decisions": [
        {
          "text": "Mergi spre nord",
          "targetBlock": "nord",
          "icon": "",
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

## Sistemul de efecte

Efectele sunt aplicate pe starea jucatorului in momentul alegerii unei decizii. Valorile rezultate sunt intotdeauna clampate la intervalul [min, max] definit per proprietate.

| Tip | Comportament | Exemplu |
|-----|-------------|---------|
| `ADD` | Adauga valoarea la cea curenta (poate fi negativa) | `{ "type": "ADD", "property": "sanatate", "value": -20 }` |
| `SET` | Seteaza proprietatea la valoarea exacta | `{ "type": "SET", "property": "suspiciune", "value": 0 }` |

In interfata Editor, efectele se introduc in format text scurt, cu virgula ca separator intre efecte multiple:

```
sanatate:ADD:-20
suspiciune:ADD:30
indicii:SET:0
sanatate:ADD:-10,suspiciune:ADD:15
```

---

## Sistemul de conditii

Conditiile controleaza vizibilitatea deciziilor: o decizie cu o conditie neindeplinita nu apare in lista jucatorului.

**Comparatie simpla:**

```json
{ "type": "COMPARISON", "property": "indicii", "operator": ">=", "value": 5 }
```

**AND — toate sub-conditiile trebuie sa fie adevarate:**

```json
{
  "type": "AND",
  "conditions": [
    { "type": "COMPARISON", "property": "indicii", "operator": ">=", "value": 3 },
    { "type": "COMPARISON", "property": "sanatate", "operator": ">", "value": 0 }
  ]
}
```

**OR — cel putin o sub-conditie trebuie sa fie adevarata:**

```json
{
  "type": "OR",
  "conditions": [
    { "type": "COMPARISON", "property": "aliati", "operator": ">=", "value": 2 },
    { "type": "COMPARISON", "property": "indicii", "operator": ">=", "value": 5 }
  ]
}
```

Operatori suportati: `<`, `<=`, `>`, `>=`, `==`, `!=`

Conditiile pot fi imbricate la orice adancime.

---

## Aplicatia Player

Interfata de redare a poveștilor.

**Scurtaturi de tastatura:**

| Scurtatura | Actiune |
|------------|---------|
| `Ctrl+O` | Deschide un fisier `.story` |
| `F5` | Reporneste povestea curenta de la bloc initial |

**Comportament:**

- HUD-ul lateral afiseaza toate proprietatile cu `visibleInHud: true`, ordonate dupa `hudOrder`
- Deciziile cu conditii neindeplinite sunt ascunse complet din lista
- La atingerea valorii minime a unei proprietati cu `onMinBlock` configurat, jucatorul este redirectionat automat catre blocul respectiv
- La finalul poveștii (`isFinal: true`), apare butonul de restartare
- La incarcare, aplicatia scrie un log de diagnostic la `%TEMP%\StoryEngine_Player_diagnostic.log` cu detalii despre incarcarea imaginilor

---

## Aplicatia Editor

Interfata de creare si editare a poveștilor.

**Scurtaturi de tastatura:**

| Scurtatura | Actiune |
|------------|---------|
| `Ctrl+N` | Poveste noua |
| `Ctrl+O` | Deschide un fisier `.story` existent |
| `Ctrl+S` | Salveaza fisierul `.story` |

**Tab "Blocuri":**

- Creare si editare blocuri narative prin ID, text narativ, imagine de fundal si flag de final
- Grid de decizii: text afisat, bloc destinatie, icon, conditie JSON si efecte in format scurt
- Butonul `Salveaza blocul` comite modificarile curente in memoria interna

**Tab "Poveste / Proprietati":**

- Metadate ale poveștii: titlu si bloc de start
- Editarea proprietatilor de stare: cheie, eticheta, valori min/max/initial, vizibilitate HUD, blocuri de overflow

---

## Povestea demo inclusa

**"Umbra Carpatilor"** — Un mister in Transilvania, 1921.

Jurnalistul Andrei Lazar investighează moartea suspectă a unui batran bancher, infrunand un fiu lacom, o vaduva misterioasa si o conspiratie care implica politia locala.

**Statistici:**

- 32 de blocuri narative cu rute multiple convergente si divergente
- 4 proprietati de stare: Sanatate, Suspiciune, Aliati, Indicii
- 6 finaluri distincte

**Finaluri principale** (alese prin decizii directe la confruntarea finala):

| Final | Conditie de acces |
|-------|-------------------|
| Dreptatea Carpatilor — victorie completa | Minim 4 indicii acumulate |
| Umbra Ramane — dreptate partiala | Disponibil implicit |
| Pretul Tacerii — coruptie, acceptarea mitei | Disponibil implicit |

**Finaluri alternative** (declansate de gestionarea precara a proprietatilor):

| Final | Declansator |
|-------|-------------|
| Prea Multa Agitatie | Suspiciune atinge 100 |
| Drumul Fara Intoarcere | Sanatate atinge 0 |
| Fara Dovezi | Lipsa de probe la confruntarea finala |

Povestea demonstreaza toate capabilitatile motorului: decizii conditionate, efecte multiple per decizie, aliati colectabili (Petre, Vera) cu impact asupra finalului si blocuri de overflow per proprietate.

---

## Depanare

**Aplicatia nu porneste:**

1. Verificati ca aveti .NET 8 SDK instalat: `dotnet --version`
2. Deschideti `build_log.txt` din radacina proiectului pentru detalii despre erori de compilare
3. Rulati scriptul `.bat` din File Explorer (dublu-click), nu din alt director de lucru

**Imaginile de fundal nu apar:**

Aplicatia Player genereaza un fisier de diagnostic la:

```
%TEMP%\StoryEngine_Player_diagnostic.log
```

Fisierul contine numarul de imagini gasite in arhiva, statusul incarcarii fiecareia si dimensiunile imaginii incarcate.

**Crash la pornire:**

Verificati fisierul:

```
%TEMP%\StoryEngine_Player_crash.log
```

---
