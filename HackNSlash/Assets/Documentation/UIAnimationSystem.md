# UI Animation System - Architecture Guide

## Overview

The UI Animation System provides centralized, consistent, and customizable animations for all menu UI elements. Built using the **Builder Pattern**, it eliminates hardcoded colors and animation values while providing a fluent API for composing animations.

## Architecture

### Components

#### 1. UIAnimationConfig (ScriptableObject)
**Purpose**: Central storage for all UI animation settings

**Location**: `Assets/Extensions/UI/UIAnimationConfig.cs`

**Contains**:
- **Colors**: normal, selected, disabled, empty, equipped, text colors
- **Durations**: color transitions, scale animations, fades, punch
- **Scale Values**: selected scale multiplier, punch strength
- **Easing**: ease types for different animation types

**Create**: `Right Click → Create → UI → Animation Config`

**Benefits**:
- Change all menu colors from one place
- Adjust all animation speeds globally
- Easy A/B testing of different styles
- Share configs across different menus

#### 2. UIAnimationManager (MonoBehaviour)
**Purpose**: Central manager providing animation builders

**Location**: `Assets/Extensions/UI/UIAnimationManager.cs`

**Responsibilities**:
- Holds reference to UIAnimationConfig
- Provides `CreateBuilder()` method for creating animations
- Offers convenience methods for getting colors
- Singleton-like pattern (local to menu, not global)

**Placement**: Attach to root menu GameObject (OverworldMenuUI)

**Usage**:
```csharp
UIAnimationManager manager = UIAnimationManager.Instance;
manager.CreateBuilder(transform, graphic1, graphic2)
    .AnimateSelection()
    .AnimatePunch();
```

#### 3. UIAnimationBuilder (Builder Class)
**Purpose**: Fluent API for composing animations

**Location**: `Assets/Extensions/UI/UIAnimationBuilder.cs`

**Methods**:
- `AnimateSelection()` - Scale up + color to selected
- `AnimateDeselection()` - Scale down + color to normal
- `AnimatePunch()` - Quick punch scale effect
- `AnimateColorTo(Color)` - Smooth color transition
- `SetColor(Color)` - Instant color change
- `SetScale(float)` - Instant scale change
- `AnimateFadeIn()` - Fade alpha 0 → 1
- `AnimateFadeOut()` - Fade alpha 1 → 0
- `AnimateToEmpty()` - Transition to empty state
- `AnimateToDisabled()` - Transition to disabled state
- `WithUnscaledTime(bool)` - Set time mode

**Builder Pattern Benefits**:
- Fluent, chainable API
- Compose complex animations easily
- Reusable animation sequences
- Self-documenting code

## Integration

### Updated Components

All UI components now use UIAnimationManager instead of hardcoded values:

#### ItemSlotUI
**Before**:
```csharp
[SerializeField] private Color normalColor = Color.white;
[SerializeField] private Color selectedColor = Color.yellow;
// ... hardcoded animations with PrimeTween directly
```

**After**:
```csharp
private UIAnimationManager animationManager;

void Select() {
    animationManager.CreateBuilder(borderImage.transform, borderImage)
        .AnimateSelection();
}
```

#### AttackButtonSlotUI
**Before**:
```csharp
[SerializeField] private Color normalColor = Color.white;
[SerializeField] private float selectedScale = 1.15f;
Tween.Scale(transform, originalScale * selectedScale, ...);
```

**After**:
```csharp
animationManager.CreateBuilder(transform, borderImage, backgroundImage)
    .AnimateSelection();
```

#### UnlockSlotUI
Similar refactoring - uses builders for all animations.

#### SkillTreeNodeUI
Uses builders for focus animations and hold progress.

### Category Buttons (Inventory Tab)
Instead of regular Unity Buttons, use custom component with animations:

**Before**: Plain Button with no visual feedback
**After**: Custom component using UIAnimationManager for hover/selection

## Usage Examples

### Basic Selection Animation
```csharp
void OnSelect() {
    var manager = UIAnimationManager.Instance;
    manager.CreateBuilder(transform, borderImage)
        .AnimateSelection();
}
```

### Custom Color Animation
```csharp
void OnHover() {
    var manager = UIAnimationManager.Instance;
    manager.CreateBuilder(transform, backgroundImage, borderImage)
        .AnimateColorTo(manager.Config.selectedColor);
}
```

### Complex Animation Sequence
```csharp
void OnSubmit() {
    var manager = UIAnimationManager.Instance;
    manager.CreateBuilder(transform, graphic)
        .AnimatePunch()
        .AnimateColorTo(Color.green)
        .WithUnscaledTime(true);
}
```

### Empty State Transition
```csharp
void SetEmpty() {
    var manager = UIAnimationManager.Instance;
    manager.CreateBuilder(transform, iconImage, nameText)
        .AnimateToEmpty();
}
```

## Setup Guide

### 1. Create Animation Config

1. In Unity: `Right Click → Create → UI → Animation Config`
2. Name it: `MenuAnimationConfig`
3. Configure settings:
   - **Normal Color**: White (1, 1, 1)
   - **Selected Color**: Yellow (1, 1, 0)
   - **Disabled Color**: Gray (0.5, 0.5, 0.5)
   - **Empty Color**: Gray (0.5, 0.5, 0.5)
   - **Equipped Color**: Green (0, 1, 0)
   - **Color Transition Duration**: 0.15s
   - **Scale Animation Duration**: 0.15s
   - **Selected Scale**: 1.05
   - **Punch Scale Strength**: (0.2, 0.2, 0.2)
   - **Selection Ease**: OutBack
   - **Deselection Ease**: OutQuad

### 2. Add UIAnimationManager

1. Select root menu GameObject (OverworldMenuUI)
2. `Add Component → UI Animation Manager`
3. Assign Animation Config to manager
4. Set "Use Unscaled Time By Default": True (for menus)

### 3. Update UI Components

All existing UI components will automatically find UIAnimationManager via `Instance`.

**Important**: UIAnimationManager must be active before UI components initialize.

### 4. Remove Old Color Fields (Optional)

You can remove old `[SerializeField] Color` fields from components since they're now unused. However, leaving them won't cause issues - they're just ignored.

## Benefits

### For Designers
- **Visual Consistency**: All UI uses same colors/animations
- **Easy Tweaking**: Change all menus from one config
- **A/B Testing**: Swap configs to try different styles
- **No Code Changes**: Adjust values in inspector

### For Programmers
- **DRY Principle**: No repeated color definitions
- **Clean Code**: Animations use self-documenting builder API
- **Maintainable**: Centralized configuration
- **Extensible**: Easy to add new animation types

### For Players
- **Polish**: Consistent, smooth animations throughout
- **Responsive**: Immediate visual feedback
- **Professional**: Cohesive visual language

## Extension Points

### Adding New Animation Types

1. Add settings to `UIAnimationConfig`:
```csharp
[Header("My Custom Animation")]
public float customDuration = 0.3f;
public Color customColor = Color.blue;
```

2. Add method to `UIAnimationBuilder`:
```csharp
public UIAnimationBuilder AnimateCustom() {
    // Use config.customDuration and config.customColor
    return this;
}
```

3. Use in components:
```csharp
manager.CreateBuilder(transform).AnimateCustom();
```

### Creating Animation Presets

Create multiple UIAnimationConfig assets for different styles:
- `MenuAnimationConfig` - Main menu style
- `CombatUIConfig` - Combat HUD style  
- `DialogueConfig` - Dialogue boxes style

Swap configs to change entire UI theme.

### Custom Easing Functions

Modify `UIAnimationConfig` to expose more PrimeTween ease types:
```csharp
public Ease customEase = Ease.InOutElastic;
```

### Animation Events

Add callbacks to builder:
```csharp
public UIAnimationBuilder OnComplete(Action callback) {
    // Store callback, invoke after animation
    return this;
}
```

## Best Practices

### 1. Always Use Builder
❌ **Don't**:
```csharp
Tween.Scale(transform, 1.05f, 0.15f, Ease.OutBack, useUnscaledTime: true);
borderImage.color = Color.yellow;
```

✅ **Do**:
```csharp
animationManager.CreateBuilder(transform, borderImage)
    .AnimateSelection();
```

### 2. Cache Manager Reference
❌ **Don't** call `Instance` every frame:
```csharp
void Update() {
    var manager = UIAnimationManager.Instance; // Bad!
}
```

✅ **Do** cache in Awake:
```csharp
private UIAnimationManager animationManager;
void Awake() {
    animationManager = UIAnimationManager.Instance;
}
```

### 3. Use Fluent API
✅ **Chain multiple animations**:
```csharp
manager.CreateBuilder(transform, graphic1, graphic2)
    .AnimateSelection()
    .AnimatePunch()
    .AnimateColorTo(Color.green);
```

### 4. Get Colors from Config
❌ **Don't** hardcode:
```csharp
image.color = Color.yellow;
```

✅ **Do** use config:
```csharp
image.color = animationManager.GetSelectedColor();
```

## Troubleshooting

### Animations Not Playing
- Check UIAnimationManager is on root menu GameObject
- Verify UIAnimationConfig is assigned
- Ensure UIAnimationManager.Instance is not null
- Check that `useUnscaledTime` matches your needs

### Colors Not Changing
- Verify Graphic components are passed to builder
- Check that Graphic is not null
- Ensure color is being applied to correct component

### Scale Not Working
- Verify Transform is passed to builder
- Check that transform is not null
- Ensure no other scripts are modifying scale

### Wrong Colors Showing
- Check UIAnimationConfig values
- Verify correct config is assigned to manager
- Ensure GetColor methods are being used

## Migration Guide

### From Old System to New

**Step 1**: Keep existing `[SerializeField]` color fields (for editor references)

**Step 2**: Add animation manager caching:
```csharp
private UIAnimationManager animationManager;
void Awake() {
    animationManager = UIAnimationManager.Instance;
}
```

**Step 3**: Replace manual Tween calls:
```csharp
// Old
Tween.Scale(transform, 1.05f, 0.15f, Ease.OutBack, useUnscaledTime: true);

// New
animationManager.CreateBuilder(transform).AnimateSelection();
```

**Step 4**: Replace color assignments:
```csharp
// Old
borderImage.color = selectedColor;

// New
borderImage.color = animationManager.GetSelectedColor();
```

**Step 5**: Test and remove old fields once working

## Summary

The UI Animation System provides:
- ✅ **Centralized Configuration**: All settings in one place
- ✅ **Builder Pattern**: Fluent, composable animations
- ✅ **Consistency**: Same animations across all UI
- ✅ **Customizable**: Easy to modify without code changes
- ✅ **Maintainable**: Clean, self-documenting code
- ✅ **Extensible**: Easy to add new animation types
- ✅ **Professional**: Polished, cohesive visual experience

The system is now ready for production use across all menu UI!

