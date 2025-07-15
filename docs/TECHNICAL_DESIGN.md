# Technical Design Document

**RimAI Framework - Technical Architecture & Implementation Guide**

**Author**: [@oidahdsah0](https://github.com/oidahdsah0)  
**Version**: 1.0  
**Date**: July 2025

---

## 🎯 **Core Design Philosophy: Engine vs. Content**

This project follows a fundamental principle: **we are building an "AI Narrative Engine," not a single, feature-locked story mod.**

This philosophy mirrors RimWorld's own design: RimWorld has a powerful game engine and official core content built on that engine. Our framework follows the same pattern:

### Framework (Engine) Responsibilities
- **Provide APIs**: Clear, stable, easy-to-use APIs (`JudiciaryAPI`, `ColonyChronicleAPI`, `AiToolRegistryAPI`)
- **Establish Standards**: Data structures and blueprints (`ToolDef`, `CaseDef`, `IContextProvider` interfaces)
- **Handle Universal Logic**: LLM communication, API key management, request handling, error management, data persistence
- **Provide UI Containers**: Extensible UI frameworks like terminals for content modules
- **Remain Neutral**: Framework contains no hardcoded "stories" or "rules"

### Content Responsibilities
- **Implement Specific Narratives**: Concrete `ToolDef` XML files defining AI tools
- **Connect Game Events**: Harmony patches linking game events to framework APIs
- **Define Rules**: Specific `CaseDef` XML files defining case types and severity
- **Populate UI**: Add specific interaction interfaces to framework containers

## 🏗️ **Core Technology Stack**

### 1. Harmony - Event-Driven Foundation

**Purpose**: Runtime code injection without modifying game source files

**When to Use**:
- Creating event systems for untracked game events
- Modifying game behavior for framework logic
- Cross-mod compatibility

**Implementation Examples**:
```csharp
// Colony Chronicle Events
[HarmonyPostfix]
[HarmonyPatch(typeof(InteractionWorker_SocialFight), "Interacted")]
public static void PostInteractionFight(Pawn initiator, Pawn recipient, InteractionDef intDef)
{
    var fightEvent = new SocialFightEvent(initiator, recipient, intDef);
    ColonyChronicleAPI.LogEvent(fightEvent);
}

// Judicial System Cases
[HarmonyPostfix]
[HarmonyPatch(typeof(JobDriver_Steal), "MakeNewToils")]
public static void PostStealAttempt(Pawn thief, Thing target)
{
    var theftCase = new CaseFile("theft", thief, target);
    JudiciaryAPI.FileNewCase(theftCase);
}
```

### 2. ThingComp - Component System

**Purpose**: Attach custom data and logic to any game object

**When to Use**: Adding new persistent state and continuous functionality to items

**Implementation Example**:
```csharp
public class Comp_Terminal : ThingComp
{
    private List<CaseFile> activeCases = new List<CaseFile>();
    private string aiContext = "";

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Collections.Look(ref activeCases, "activeCases", LookMode.Deep);
        Scribe_Values.Look(ref aiContext, "aiContext", "");
    }

    public override void CompTickRare()
    {
        UpdateNetworkStatus();
        ProcessPendingCases();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        yield return new Command_Action
        {
            defaultLabel = "Open AI Console",
            action = () => Find.WindowStack.Add(new TerminalWindow(this))
        };
    }
}
```

### 3. GameComponent - Global Data Management

**Purpose**: Single instance for save-wide data and system-level logic

**Implementation Example**:
```csharp
public class RimAI_GameComponent : GameComponent
{
    private List<ChronicleEvent> allEvents = new List<ChronicleEvent>();
    private List<CaseFile> allCases = new List<CaseFile>();
    private List<ToolDef> registeredTools = new List<ToolDef>();

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref allEvents, "chronicleEvents", LookMode.Deep);
        Scribe_Collections.Look(ref allCases, "caseFiles", LookMode.Deep);
        Scribe_Collections.Look(ref registeredTools, "aiTools", LookMode.Deep);
    }

    public override void GameComponentTick()
    {
        // Check for overdue cases
        ProcessOverdueCases();
        
        // Periodic AI analysis
        if (Find.TickManager.TicksGame % 60000 == 0) // Every game hour
        {
            TriggerAIAnalysis();
        }
    }
}
```

### 4. Custom Def Classes - New XML Concepts

**Purpose**: Define new XML-configurable game concepts

**Implementation Examples**:
```csharp
public class ToolDef : Def
{
    public string functionToCall;
    public List<string> parameters;
    public int energyCost;
    public string description;

    public void Execute(params object[] args)
    {
        // Reflection-based method calling
        var method = typeof(AiToolRegistry).GetMethod(functionToCall);
        method?.Invoke(null, args);
    }
}

public class CaseDef : Def
{
    public bool isViolent;
    public float baseSeverity;
    public int maxDuration; // in game hours
    public string aiPromptTemplate;
}
```

### 5. DefModExtension - Compatibility Layer

**Purpose**: Attach additional data to existing Defs without modification

**Implementation Example**:
```csharp
public class CrimeSeverityExtension : DefModExtension
{
    public float severityModifier = 1.0f;
    public string criminalityLevel = "minor";
}

// Usage in XML by other mods:
// <ThingDef ParentName="BaseWeapon">
//   <defName>SoulDagger</defName>
//   <modExtensions>
//     <li Class="RimAI.CrimeSeverityExtension">
//       <severityModifier>2.0</severityModifier>
//       <criminalityLevel>heinous</criminalityLevel>
//     </li>
//   </modExtensions>
// </ThingDef>
```

## 🔄 **Core Workflow Architecture**

### 1. Event Detection (Harmony Patches)
```csharp
[HarmonyPatch] → Game Event → Create Data Object → Call Framework API
```

### 2. Data Processing (GameComponent)
```csharp
API Call → Validate Data → Store in GameComponent → Queue for AI Processing
```

### 3. AI Integration (LLMManager)
```csharp
Queue Processing → Build Context → LLM Request → Parse Response → Apply Results
```

### 4. Result Broadcasting (Multiple Systems)
```csharp
AI Decision → Update Game State → Notify Players → Log to Chronicle
```

## 🎮 **User Interface Architecture**

### Terminal System
- **Base Window**: `TerminalWindow` - Main container
- **Tab System**: `ITerminalTab` interface for extensible functionality
- **Context Management**: Each tab maintains its own AI context
- **Settings Integration**: Direct access to `RimAISettings` for configuration

### Dossier System
- **Case UI**: Parchment-style interface for case review
- **Natural Language Input**: Free-form text for judgments and reasoning
- **Deadline Tracking**: Visual countdown timers
- **Auto-delegation**: Seamless handoff to appointed officials

## 🔧 **Development Tools**

### Debug Actions
```csharp
[DebugAction("RimAI", "Test: Create Murder Case")]
public static void CreateTestCase()
{
    var testCase = new CaseFile("murder", pawn1, pawn2);
    JudiciaryAPI.FileNewCase(testCase);
}
```

### Tweak Values
```csharp
public void DoWindowContents(Rect inRect)
{
    TweakValue.Look(ref windowWidth, "Terminal Width");
    TweakValue.Look(ref aiTemperature, "AI Temperature");
    // Real-time UI and AI parameter adjustment
}
```

## 📊 **Data Persistence Strategy**

### Core Data (GameComponent)
- Essential game state stored in save file
- Optimized for size and load performance
- Automatic cleanup of old data

### Extended Data (External Files)
- Full conversation histories
- Detailed case transcripts
- AI analysis logs
- Stored in `SaveName.rimai_data/` directory

### Settings Data (ModSettings)
- API configurations
- User preferences
- Debug options
- Persistent across saves

## 🔐 **Security & Privacy**

### API Key Management
- Encrypted storage using `ProtectedData`
- No logging of API keys
- Secure transmission headers

### Data Handling
- Optional local processing support
- Configurable data retention policies
- User consent for cloud processing

## 🚀 **Performance Optimization**

### Async Processing
- All LLM requests are asynchronous
- Non-blocking UI updates
- Background processing queues

### Memory Management
- Lazy loading of historical data
- Automatic cleanup of old contexts
- Efficient data structures

### Network Optimization
- Request batching where possible
- Intelligent retry logic
- Connection pooling

---

## 🎨 **Design Patterns Used**

- **Singleton**: `LLMManager` for global AI communication
- **Observer**: Event system for game state changes
- **Factory**: Dynamic creation of case types and tools
- **Strategy**: Pluggable AI processing algorithms
- **Facade**: Simplified APIs for complex operations

## 📈 **Future Extensibility**

### Plugin Architecture
- Standard interfaces for new modules
- Dynamic loading of content packages
- Version compatibility management

### AI Model Support
- Abstract LLM interface
- Multiple provider support
- Local model integration

### Community Integration
- Standardized data formats
- Shared context providers
- Collaborative AI training

---

**This document serves as the technical foundation for all RimAI Framework development. It should be updated as the architecture evolves.**
