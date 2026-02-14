# Menu UI System - Architecture & Design

## Table of Contents
1. [Overview](#overview)
2. [Architecture Principles](#architecture-principles)
3. [System Components](#system-components)
4. [Controller Navigation](#controller-navigation)
5. [Audio Integration](#audio-integration)
6. [Animation System](#animation-system)
7. [Data Flow](#data-flow)

---

## Overview

The Menu UI system is a complete, controller-first interface for the action RPG game, featuring 8 main tabs with full Xbox/PlayStation controller support. The architecture follows MVVM principles with complete separation between UI and game logic.

### Key Features
- **8 Tabs**: Character stats, equipment, element progress, skill tree, inventory, missions, compendium, settings
- **Controller-Only Navigation**: No mouse support - designed for console/gamepad input
- **MVVM Architecture**: Complete UI/logic separation via data provider interfaces
- **EventBus Communication**: No singletons - all communication via EventBus pattern
- **FMOD Audio Integration**: Routes through AudioSystem via EventBus
- **PrimeTween Animations**: Smooth transitions with unscaled time (works when game is paused)
- **Modular Components**: All reusable components in Extensions.UI namespace

---

## Architecture Principles

### 1. MVVM (Model-View-ViewModel) Pattern

The UI is completely decoupled from game logic. The game should work exactly as it does if you remove the UI from the scene.

```
┌─────────────────┐
│  Model          │  Game Backend (PlayerStats, PlayerInventory, etc.)
│  (Backend)      │  • Contains game state and logic
└────────┬────────┘  • No knowledge of UI
         │
         │ Implements interfaces
         │
┌────────▼────────┐
│  ViewModel      │  Data Providers (implements IMenuDataProvider interfaces)
│  (Adapter)      │  • Converts backend data to DTOs
└────────┬────────┘  • Raises events when data changes
         │
         │ Provides DTOs
         │
┌────────▼────────┐
│  View           │  UI Components (Tabs, Menus, Displays)
│  (UI)           │  • Displays data from DTOs
└─────────────────┘  • Only knows about interfaces, not concrete classes
```

**Benefits**:
- Game logic works independently of UI
- Easy to test each layer separately
- UI can be completely removed without affecting gameplay
- Clean separation of concerns
- Easy to swap implementations (e.g., different data sources)

### 2. Controller-Only Navigation (No Mouse)

**Critical Design Choice**: This UI system is built for controllers ONLY. There is NO mouse support.

- All navigation uses Unity's EventSystem with explicit navigation chains
- Tabs are switched via bumper/trigger buttons (LB/RB) ONLY, not by selecting them
- All interactions use controller buttons (A = Select, B = Back, D-Pad = Navigate)
- No `IPointerEnter`, `IPointerExit`, or `IPointerClick` interfaces used
- Only `ISelectHandler`, `IDeselectHandler`, `ISubmitHandler`, `ICancelHandler`

**Navigation Hierarchy**:
```
Tab Buttons (Non-Interactive - Bumper Switching Only)
    ↓ (Down navigation)
Tab Content (First Selectable)
    ↓ (Explicit navigation chains)
Interactive Elements (Buttons, Slots, etc.)
    → Drill-Down Navigation (for nested content)
        → Child Elements (with back button to return)
```

### 3. EventBus Communication (No Singletons)

Components communicate via EventBus instead of singletons or direct references:

```csharp
// UI raises event (no direct coupling)
UIAudio.PlaySelect();

// EventBus broadcasts to all listeners
EventBus<PlayUIAudioEvent>.Raise(new PlayUIAudioEvent(UIAudioType.Select));

// AudioSystem listens and handles
private void OnPlayUIAudio(PlayUIAudioEvent evt) { 
    // Route to FMOD
    AudioSystem.PlayUISound(evt.audioType);
}
```

**Why EventBus?**
- ✅ No singleton dependencies
- ✅ Decoupled components
- ✅ Multiple listeners possible
- ✅ Easy to test
- ✅ No global state issues

### 4. Code-First Configuration (Minimal Inspector Setup)

**Philosophy**: Everything that CAN be done in code SHOULD be done in code.

The system automatically handles:
- ✅ **Button Listeners**: Added at runtime via code
- ✅ **Navigation Chains**: Configured by MenuNavigationConfigurator
- ✅ **Grid Navigation**: Calculated by SlotGridNavigator based on columns/rows
- ✅ **First Selection**: Automatically selected when tabs open
- ✅ **Drill-Down Navigation**: Handled by DrillDownNavigator component
- ✅ **Visual Transitions**: Colors and animations set in code

**Why?**
- Reduces human error (no forgetting to wire buttons)
- Consistent behavior across all instances
- Easy to change navigation logic globally
- Faster iteration (no manual Inspector clicking)
- Easier to maintain and debug

**What You Still Set in Inspector**:
- Tab content panel references (required for tab linking)
- Data provider script references (required for data flow)
- Grid dimensions (columns/rows for SlotGridNavigator)
- UI element references (icons, text fields, images)
- Render texture and video clip assignments

### 5. Modularity & Reusability

All reusable UI components live in `Extensions.UI` namespace:
- Can be copied to other projects as-is
- No dependencies on game-specific classes
- Input handling separated and assignable
- Data-driven via exposed Inspector parameters
- Follow standard Unity component patterns

Examples of modular components:
- `ScrollMenu` - Generic scrolling menu (works with any data type)
- `ItemSlotUI` - Generic item/equipment slot display
- `SlotGridNavigator` - Grid navigation setup for any button grid
- `DrillDownNavigator` - Nested navigation for any parent/child structure
- `SharedRenderTextureController` - Animated render texture that moves between tabs

---

## System Components

### File Structure

```
Assets/
├── Extensions/
│   └── UI/                                    # Modular, reusable (copy to other projects)
│       ├── Interfaces/
│       │   ├── IMenuDataProvider.cs          # Base interface for all data providers
│       │   └── IMenuDataProviders.cs         # Specific provider interfaces
│       ├── Data/
│       │   ├── MenuDisplayData.cs            # DTOs for player, stats, equipment
│       │   ├── MenuDisplayDataExtended.cs    # DTOs for attacks, quests, compendium
│       │   ├── ItemUIInfo.cs                 # Generic item display info
│       │   └── UIDisplayConfigs.cs           # Render texture, video configs
│       ├── Components/
│       │   ├── ScrollMenu.cs                 # Carousel-style scrolling menu
│       │   ├── ScrollUIPanel.cs              # Base class for scroll panels
│       │   ├── ItemScrollPanel.cs            # Concrete scroll panel implementation
│       │   ├── ItemSlotUI.cs                 # Equipment/item slot component
│       │   ├── PlayerStatsDisplay.cs         # Stats visualization
│       │   ├── RenderTextureDisplay.cs       # 3D model preview display
│       │   ├── VideoDisplay.cs               # Video playback component
│       │   └── SharedRenderTextureController.cs  # Shared animated render texture
│       ├── Navigation/
│       │   ├── MenuNavigationConfigurator.cs # Auto-configures ALL navigation
│       │   ├── SlotGridNavigator.cs          # Auto-configures slot grid navigation
│       │   └── DrillDownNavigator.cs         # Nested drill-down navigation
│       ├── Tabs/
│       │   ├── TabGroup.cs                   # Tab navigation manager (bumper input)
│       │   ├── TabButton.cs                  # Individual tab button (visual only)
│       │   └── TabSelection.cs               # Base class for tab content
│       └── Audio/
│           └── UIAudio.cs                    # EventBus audio helper (static methods)
│
└── Scripts/
    └── UI/
        └── Game/
            └── OverworldMenu/
                ├── OverworldMenuUI.cs        # Main menu controller
                ├── DataProviders/            # Backend adapters (implement interfaces)
                │   ├── PlayerDataProvider.cs
                │   ├── EquipmentDataProvider.cs
                │   ├── ElementProgressDataProvider.cs
                │   ├── SkillTreeDataProvider.cs
                │   ├── InventoryDataProvider.cs
                │   ├── QuestDataProvider.cs
                │   └── CompendiumDataProvider.cs
                └── Tabs/                     # Tab implementations (extend TabSelection)
                    ├── CharacterStatsTab.cs      # Tab 1: Character/Stats
                    ├── EquipmentTab.cs           # Tab 2: Equipment selection
                    ├── ElementProgressTab.cs     # Tab 3: Element progress/attacks
                    ├── SkillTreeTab.cs           # Tab 4: Skill tree visualization
                    ├── InventoryTab.cs           # Tab 5: Inventory management
                    ├── MissionsTab.cs            # Tab 6: Quest tracking
                    ├── CompendiumTab.cs          # Tab 7: Lore/compendium
                    └── SettingsTab.cs            # Tab 8: Audio/video/controls
```

---

## Controller Navigation

### Input Flow

```
Controller Input
    ↓
InputManager (Unity Input System)
    ↓
TabGroup (LB/RB for tab switching)
    ↓
Unity EventSystem (A/B/D-Pad for content navigation)
    ↓
Individual UI Components (ISelectHandler, ISubmitHandler, etc.)
```

### Navigation Types

#### 1. Tab Switching (Bumper-Only)
- **Input**: LB/RB (Left/Right Bumper or Trigger)
- **Handled By**: TabGroup component
- **Behavior**: Switches between tabs, tabs themselves are NOT selectable
- **Visual**: Selected tab scales up and changes color

```csharp
// TabGroup listens to InputManager events
InputManager.Instance.onTabLeft += OnTabLeft;
InputManager.Instance.onTabRight += OnTabRight;
```

#### 2. Content Navigation (D-Pad/Analog Stick)
- **Input**: D-Pad or Left Analog Stick
- **Handled By**: Unity EventSystem + explicit Navigation setup
- **Behavior**: Move between interactive elements within tab content
- **Setup**: Automatically configured by MenuNavigationConfigurator

#### 3. Drill-Down Navigation (A to enter, B to exit)
- **Input**: A button to drill in, B button to back out
- **Handled By**: DrillDownNavigator component
- **Behavior**: Select container → Press A → Navigate children → Press B → Return to container
- **Use Case**: Equipment unlock grids, nested menus

```csharp
// Example: Equipment unlock container
// 1. User navigates to unlock container (selected)
// 2. User presses A (OnSubmit) → DrillDown()
// 3. User navigates individual unlock slots
// 4. User presses B (OnCancel) → DrillUp()
// 5. Back to container selection
```

#### 4. Scroll Navigation (D-Pad Up/Down)
- **Input**: D-Pad Up/Down (or analog stick)
- **Handled By**: ScrollMenu component via InputManager.onScroll
- **Behavior**: Scroll through item lists with carousel animation
- **Visual**: Center item scales up, smooth scrolling with PrimeTween

### Navigation Setup Process

1. **MenuNavigationConfigurator.Start()** runs automatically
2. Finds all TabButtons and their content panels
3. For each tab:
   - Configures horizontal navigation between tabs (even though they're non-interactive)
   - Finds first Selectable in tab content
   - Links tab down navigation to first content element
   - Links first content element up navigation back to tab
4. Finds all SlotGridNavigators and calls ConfigureNavigation()
5. Finds all DrillDownNavigators (they self-configure)
6. Selects initial tab and first element

**Result**: Entire menu navigation is set up automatically with minimal Inspector work.

---

## Audio Integration

### Audio Event Types

```csharp
public enum UIAudioType
{
    Navigation,     // D-Pad movement between buttons
    Select,         // A button press (confirm)
    Back,           // B button press (cancel)
    TabSwitch,      // LB/RB tab switching
    Error,          // Invalid action
    Hover,          // Element selected/focused (same as Navigation)
    Unlock,         // Unlocking skill/achievement
    SlotSelect,     // Selecting equipment slot
    ItemEquip       // Equipping an item
}
```

### Audio Flow (EventBus Pattern)

```
UI Component
    ↓ (calls static method)
UIAudio.PlaySelect()
    ↓ (raises EventBus event)
EventBus<PlayUIAudioEvent>
    ↓ (broadcasts to listeners)
AudioSystem
    ↓ (routes to FMOD)
RuntimeManager.PlayOneShot(eventRef)
```

### Usage in UI Code

```csharp
// In any UI component
public void OnSelect(BaseEventData eventData)
{
    UIAudio.PlayHover();  // Raises EventBus event
    // ... rest of select logic
}

public void OnSubmit(BaseEventData eventData)
{
    UIAudio.PlaySelect();  // Raises EventBus event
    // ... rest of submit logic
}
```

### AudioSystem Integration

```csharp
// In AudioSystem class (or separate AudioSystemUIIntegration)
private EventBinding<PlayUIAudioEvent> uiAudioBinding;

private void Awake()
{
    uiAudioBinding = new EventBinding<PlayUIAudioEvent>(OnPlayUIAudio);
    EventBus<PlayUIAudioEvent>.Register(uiAudioBinding);
}

private void OnDestroy()
{
    EventBus<PlayUIAudioEvent>.Deregister(uiAudioBinding);
}

private void OnPlayUIAudio(PlayUIAudioEvent evt)
{
    // Route to your FMOD events based on UIAudioType
    PlayUISound(evt.audioType);
}
```

**Why This Approach?**
- UI components don't need reference to AudioSystem (decoupled)
- Easy to test UI without audio system
- Multiple listeners can respond to same event
- No singleton/global state issues
- Follows game's EventBus architecture

---

## Animation System

All animations use **PrimeTween** with **unscaled time** (menu pauses game with `timeScale = 0`).

### Animation Philosophy
- **Snappy, not slow**: Durations 0.1-0.4s
- **Subtle overshoot**: Adds polish (Ease.OutBack for entrances)
- **Clear feedback**: Every interaction has visual response
- **Motion with purpose**: Animations enhance usability, not distract

### Standard Animation Timings

| Element | Duration | Ease | Use Case |
|---------|----------|------|----------|
| Button selection | 0.15s | OutBack | Quick pulse with overshoot |
| Tab switch scale | 0.2s | OutBack | Noticeable tab change |
| Tab switch color | 0.2s | OutQuad | Smooth color transition |
| Tab content fade in | 0.25s | OutQuad | Smooth entrance |
| Tab content fade out | 0.15s | InQuad | Quick exit |
| Render texture slide | 0.4s | OutCubic | Stylish, smooth movement |
| Scroll animation | 0.1s | Linear | Fast, responsive scrolling |
| Scale focus | 0.08s | OutQuad | Instant feedback |

### PrimeTween Usage Examples

**Tab Button:**
```csharp
// Scale up with overshoot (snappy feel)
Tween.Scale(rectTransform, 1.1f, duration: 0.2f, 
    ease: Ease.OutBack, useUnscaledTime: true);

// Color transition
Tween.Color(buttonImage, selectedColor, duration: 0.2f, 
    ease: Ease.OutQuad, useUnscaledTime: true);
```

**Tab Content:**
```csharp
// Fade in content
CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
Tween.Alpha(canvasGroup, 1f, duration: 0.25f, 
    ease: Ease.OutQuad, useUnscaledTime: true);
```

**Shared Render Texture:**
```csharp
// Smoothly move to new tab's position
Tween.LocalPosition(rectTransform, targetPosition, duration: 0.4f, 
    ease: Ease.OutCubic, useUnscaledTime: true);

// Scale during transition
Tween.Scale(rectTransform, targetScale, duration: 0.4f, 
    ease: Ease.OutCubic, useUnscaledTime: true);
```

**ScrollMenu:**
```csharp
// Animate container position (carousel effect)
Tween.LocalPosition(scrollItemContainer, Vector3.zero, duration: 0.1f, 
    ease: Ease.Linear, useUnscaledTime: true);

// Scale focused panel
Tween.Scale(rectTransform, focusedScale, duration: 0.08f, 
    ease: Ease.OutQuad, useUnscaledTime: true);
```

### Why PrimeTween?
- ✅ Faster and more efficient than Unity's built-in animation
- ✅ Clean, readable API
- ✅ Unscaled time support (critical for paused menus)
- ✅ Easy chaining and callbacks
- ✅ Memory efficient (no allocations)

---

## Data Flow

### Data Transfer Objects (DTOs)

DTOs are simple data containers with no logic, used to pass data from backend to UI:

```csharp
// Example DTO
public class PlayerStatsDisplayData
{
    public string playerName;
    public int level;
    public int health;
    public int maxHealth;
    public int stamina;
    public int maxStamina;
    // ... etc
}
```

### Data Provider Interfaces

UI components depend on interfaces, not concrete classes:

```csharp
public interface IPlayerDataProvider : IMenuDataProvider
{
    PlayerStatsDisplayData GetPlayerStats();
    EquippedItemsDisplayData GetEquippedItems();
    event System.Action OnPlayerDataChanged;
}
```

### Implementation Example

```csharp
// Backend adapter (implements interface)
public class PlayerDataProvider : IPlayerDataProvider
{
    public event System.Action OnPlayerDataChanged;
    
    public PlayerStatsDisplayData GetPlayerStats()
    {
        // Access backend PlayerStats system
        var stats = PlayerStats.Instance;
        
        // Convert to DTO
        return new PlayerStatsDisplayData
        {
            playerName = stats.PlayerName,
            level = stats.Level,
            health = stats.CurrentHealth,
            maxHealth = stats.MaxHealth,
            // ... etc
        };
    }
    
    // Called when backend data changes
    private void OnStatsChanged()
    {
        OnPlayerDataChanged?.Invoke();
    }
}
```

### UI Usage

```csharp
// UI component (depends only on interface)
public class CharacterStatsTab : TabSelection
{
    [SerializeField] private PlayerStatsDisplay statsDisplay;
    private IPlayerDataProvider playerDataProvider;
    
    protected override void Initialize(params IMenuDataProvider[] providers)
    {
        // Get provider via interface
        playerDataProvider = providers.OfType<IPlayerDataProvider>().FirstOrDefault();
        
        if (playerDataProvider != null)
        {
            // Subscribe to data changes
            playerDataProvider.OnPlayerDataChanged += RefreshDisplay;
            RefreshDisplay();
        }
    }
    
    private void RefreshDisplay()
    {
        // Get DTO and update display
        var data = playerDataProvider.GetPlayerStats();
        statsDisplay.UpdateDisplay(data);
    }
}
```

**Benefits of This Pattern**:
- UI has zero knowledge of backend implementation
- Easy to swap data sources (testing, different game modes, etc.)
- Clear contracts via interfaces
- Type-safe data transfer

---

## Summary

The Menu UI system is a production-ready, controller-first interface built with:
- **MVVM architecture** for complete UI/logic separation
- **EventBus communication** for decoupled, testable components
- **Code-first configuration** for consistency and maintainability
- **Controller-only navigation** with automatic setup
- **PrimeTween animations** for polish and responsiveness
- **Modular design** for reusability across projects

All components follow best practices and are fully documented with XML comments. The system is designed to be extended and customized while maintaining its core architectural principles.

