# UI Architecture Improvements - EventBus, FMOD & PrimeTween

## Overview
The menu UI has been updated with three major architectural improvements:
1. **EventBus** pattern replaces singletons for audio
2. **FMOD** integration through AudioSystem (no Unity AudioMixer)
3. **PrimeTween** animations with unscaled time for stylish, smooth transitions

## 1. Audio System - EventBus Pattern

### The Problem with Singletons
```csharp
// OLD: Direct singleton access (bad)
UIAudioManager.PlaySelect(); // Global state, tight coupling
```

### The EventBus Solution
```csharp
// NEW: EventBus pattern (good)
UIAudio.PlaySelect(); // Raises event, AudioSystem handles it
```

### How It Works

1. **UI Component** raises audio event:
```csharp
UIAudio.PlayTabSwitch();
```

2. **EventBus** broadcasts to all listeners:
```csharp
EventBus<PlayUIAudioEvent>.Raise(new PlayUIAudioEvent(UIAudioType.TabSwitch));
```

3. **AudioSystem** (your existing system) listens and plays FMOD sound:
```csharp
// In AudioSystem.cs
private void Awake()
{
    var binding = new EventBinding<PlayUIAudioEvent>(OnUIAudioRequested);
    EventBus<PlayUIAudioEvent>.Register(binding);
}

private void OnUIAudioRequested(PlayUIAudioEvent evt)
{
    // Play appropriate FMOD event based on evt.audioType
    switch (evt.audioType)
    {
        case UIAudioType.Select:
            PlayFMODEvent("event:/UI/Select");
            break;
        // ... etc
    }
}
```

### Benefits
- ✅ No global singletons
- ✅ Decoupled components
- ✅ Easy to test
- ✅ Can have multiple listeners
- ✅ Works with your existing AudioSystem

### Available Audio Calls
```csharp
UIAudio.PlayNavigation();  // D-Pad movement
UIAudio.PlaySelect();      // Confirming/clicking
UIAudio.PlayBack();        // Canceling/closing
UIAudio.PlayTabSwitch();   // Switching tabs
UIAudio.PlayError();       // Invalid action
UIAudio.PlayHover();       // Hovering over button
UIAudio.PlayUnlock();      // Unlocking skill
UIAudio.PlaySlotSelect();  // Selecting equipment slot
UIAudio.PlayItemEquip();   // Equipping item
UIAudio.PlayCustom(EventReference); // Custom FMOD event
```

## 2. FMOD Integration

### Settings Tab Audio
The SettingsTab now directly controls FMOD buses instead of Unity AudioMixer:

```csharp
private void SetMasterVolume(float value)
{
    // Direct FMOD bus control
    FMOD.Studio.Bus masterBus = RuntimeManager.GetBus("bus:/");
    if (masterBus.isValid())
    {
        masterBus.setVolume(value);
    }
}

private void SetMusicVolume(float value)
{
    FMOD.Studio.Bus musicBus = RuntimeManager.GetBus("bus:/Music");
    if (musicBus.isValid())
    {
        musicBus.setVolume(value);
    }
}

private void SetSFXVolume(float value)
{
    FMOD.Studio.Bus sfxBus = RuntimeManager.GetBus("bus:/SFX");
    if (sfxBus.isValid())
    {
        sfxBus.setVolume(value);
    }
}
```

### FMOD Bus Structure
Ensure your FMOD project has these buses:
```
bus:/                  (Master)
├── bus:/Music        (Music)
├── bus:/SFX          (Sound Effects)
└── bus:/UI           (UI Sounds)
```

## 3. PrimeTween Animations

### Why Unscaled Time?
The menu pauses the game (`Time.timeScale = 0`), so all animations must use **unscaled time**:

```csharp
// Always set useUnscaledTime: true for menu animations!
Tween.Scale(transform, 1.1f, duration: 0.2f, useUnscaledTime: true);
```

### Animation Features

#### 1. Tab Switching (TabButton)
```csharp
public void Select()
{
    // Smooth scale up with overshoot
    Tween.Scale(rectTransform, 1.1f, duration: 0.2f, 
        ease: Ease.OutBack, useUnscaledTime: true);
}

public void Deselect()
{
    // Quick scale down
    Tween.Scale(rectTransform, 1f, duration: 0.15f, 
        ease: Ease.OutQuad, useUnscaledTime: true);
}
```

#### 2. Tab Content Fade (TabSelection)
```csharp
protected virtual void AnimateIn()
{
    // Fade in with slight scale
    canvasGroup.alpha = 0f;
    rectTransform.localScale = Vector3.one * 0.95f;
    
    Tween.Alpha(canvasGroup, 1f, duration: 0.25f, 
        ease: Ease.OutQuad, useUnscaledTime: true);
    
    Tween.Scale(rectTransform, 1f, duration: 0.25f, 
        ease: Ease.OutBack, useUnscaledTime: true);
}
```

#### 3. Item Slot Selection (ItemSlotUI)
```csharp
private void Select()
{
    // Slight scale pulse on selection
    Tween.Scale(borderImage.transform, 1.05f, duration: 0.15f, 
        ease: Ease.OutBack, useUnscaledTime: true);
}
```

#### 4. Save Status Message (SettingsTab)
```csharp
Sequence.Create(useUnscaledTime: true)
    .Group(Tween.Scale(saveStatusText.transform, 1f, 0.3f, Ease.OutBack))
    .Group(Tween.Alpha(saveStatusText, 1f, 0.2f, Ease.OutQuad))
    .ChainDelay(1.5f)
    .Chain(Tween.Alpha(saveStatusText, 0f, 0.3f, Ease.InQuad))
    .OnComplete(ClearSaveStatus);
```

#### 5. Shared Render Texture (New!)
Instead of each tab having its own render texture, one moves between tabs:

```csharp
public void MoveToTab(int tabIndex)
{
    RectTransform targetPosition = tabPositions[tabIndex];
    
    // Smooth position transition with overshoot
    Sequence.Create()
        .Group(Tween.Position(rectTransform, targetPosition.position, 
            0.4f, Ease.OutCubic, useUnscaledTime: true))
        .Group(Tween.Scale(rectTransform, 1.05f, 
            0.12f, Ease.OutQuad, useUnscaledTime: true))
        .Chain(Tween.Scale(rectTransform, 1f, 
            0.08f, Ease.InQuad, useUnscaledTime: true));
}
```

### PrimeTween Best Practices

1. **Always use unscaled time in menus**:
```csharp
Tween.Alpha(target, 1f, duration: 0.3f, useUnscaledTime: true);
```

2. **Use sequences for complex animations**:
```csharp
Sequence.Create(useUnscaledTime: true)
    .Group(...)  // Play simultaneously
    .Chain(...)  // Play after previous
    .ChainDelay(1f)  // Wait
    .OnComplete(() => {});  // Callback
```

3. **Ease curves for personality**:
- `Ease.OutBack` - Overshoot (snappy, stylish)
- `Ease.OutCubic` - Smooth deceleration
- `Ease.OutQuad` - Quick deceleration
- `Ease.InOutQuad` - Smooth both ends

4. **Keep durations short** (menu should feel snappy):
- Select: 0.15-0.2s
- Fade: 0.2-0.3s
- Movement: 0.3-0.5s

5. **Add subtle overshoot** for polish:
```csharp
// Scale to 1.05, then back to 1.0
Sequence.Create(useUnscaledTime: true)
    .Group(Tween.Scale(transform, 1.05f, 0.12f))
    .Chain(Tween.Scale(transform, 1f, 0.08f));
```

## 4. Shared Render Texture System

### The Problem
Each tab had its own render texture → wasteful, no smooth transitions.

### The Solution
One `SharedRenderTextureController` that moves between tabs:

```csharp
// Setup in Unity:
// 1. Create one RenderTexture + Camera
// 2. Add SharedRenderTextureController component
// 3. Assign tab position transforms (empty GameObjects at each tab's RT position)

// Usage in tabs:
public override void OnTabSelect()
{
    base.OnTabSelect();
    
    // Move shared render texture to this tab's position
    sharedRenderTexture.MoveToTab(tabIndex);
    
    // Update camera to render what this tab needs
    sharedRenderTexture.SetCullingMask(characterModelLayer);
}
```

### Setup Instructions

1. **Create Position Markers**:
```
OverworldMenuUI/
├── RenderTexturePositions/
│   ├── Tab1_CharacterPos (Empty RectTransform at desired position)
│   ├── Tab2_EquipmentPos
│   ├── Tab3_ElementPos
│   └── ...
```

2. **Configure SharedRenderTextureController**:
- Assign Camera
- Assign RenderTexture
- Assign RawImage
- Drag all position transforms into `tabPositions` array

3. **Call from Tabs**:
```csharp
[SerializeField] private SharedRenderTextureController sharedRenderTexture;

public override void OnTabSelect()
{
    base.OnTabSelect();
    sharedRenderTexture.MoveToTab(0); // Tab index
}
```

## 5. Updated Components

### Modified Files
1. **UIAudioManager.cs** → EventBus pattern (no singleton)
2. **TabGroup.cs** → EventBus audio calls
3. **TabButton.cs** → PrimeTween animations
4. **TabSelection.cs** → Fade in/out animations
5. **ItemSlotUI.cs** → Selection animations
6. **ItemScrollPanel.cs** → EventBus audio
7. **SettingsTab.cs** → FMOD buses, PrimeTween save status

### New Files
1. **SharedRenderTextureController.cs** - Animated shared render texture

## 6. AudioSystem Integration

Your AudioSystem needs to listen for UI audio events:

```csharp
// In AudioSystem.cs (or wherever you handle audio)
using Extensions.EventBus;
using Extensions.UI;

public class AudioSystem : MonoBehaviour
{
    [Header("UI Audio Events")]
    [SerializeField] private EventReference uiNavigationEvent;
    [SerializeField] private EventReference uiSelectEvent;
    [SerializeField] private EventReference uiBackEvent;
    [SerializeField] private EventReference uiTabSwitchEvent;
    [SerializeField] private EventReference uiErrorEvent;
    [SerializeField] private EventReference uiHoverEvent;
    [SerializeField] private EventReference uiUnlockEvent;
    
    private EventBinding<PlayUIAudioEvent> uiAudioBinding;
    
    private void Awake()
    {
        // Subscribe to UI audio events
        uiAudioBinding = new EventBinding<PlayUIAudioEvent>(OnPlayUIAudio);
        EventBus<PlayUIAudioEvent>.Register(uiAudioBinding);
    }
    
    private void OnDestroy()
    {
        EventBus<PlayUIAudioEvent>.Deregister(uiAudioBinding);
    }
    
    private void OnPlayUIAudio(PlayUIAudioEvent evt)
    {
        // If custom event specified, use it
        if (!evt.customEvent.IsNull)
        {
            RuntimeManager.PlayOneShot(evt.customEvent);
            return;
        }
        
        // Otherwise use mapped event
        EventReference eventRef = evt.audioType switch
        {
            UIAudioType.Navigation => uiNavigationEvent,
            UIAudioType.Select => uiSelectEvent,
            UIAudioType.Back => uiBackEvent,
            UIAudioType.TabSwitch => uiTabSwitchEvent,
            UIAudioType.Error => uiErrorEvent,
            UIAudioType.Hover => uiHoverEvent,
            UIAudioType.Unlock => uiUnlockEvent,
            UIAudioType.SlotSelect => uiSelectEvent, // Reuse select
            UIAudioType.ItemEquip => uiSelectEvent,  // Reuse select
            _ => default
        };
        
        if (!eventRef.IsNull)
            RuntimeManager.PlayOneShot(eventRef);
    }
}
```

## 7. Animation Timing Guide

For the best feel, use these timing guidelines:

| Action | Duration | Ease | Notes |
|--------|----------|------|-------|
| Button hover | 0.1s | OutQuad | Very quick response |
| Button select | 0.15s | OutBack | Snappy with overshoot |
| Tab switch | 0.2s | OutBack | Noticeable but fast |
| Tab content fade in | 0.25s | OutQuad | Smooth appearance |
| Tab content fade out | 0.15s | InQuad | Quick exit |
| Render texture move | 0.4s | OutCubic | Smooth, stylish |
| Save status | 0.3s in, 1.5s wait, 0.3s out | OutBack, InQuad | Clear feedback |

## 8. Testing Checklist

- [ ] All UI sounds play through AudioSystem (check EventBus)
- [ ] Tab switching has smooth scale animation
- [ ] Tab content fades in/out smoothly
- [ ] Slot selection has subtle scale pulse
- [ ] Render texture moves between tabs (if implemented)
- [ ] Animations work when `Time.timeScale = 0` (unscaled time)
- [ ] FMOD volume sliders control correct buses
- [ ] Save status animates with fade and scale
- [ ] No singleton dependencies (only EventBus)

## Summary

The UI now:
✅ Uses EventBus instead of singletons (decoupled)
✅ Routes all audio through AudioSystem (centralized FMOD)
✅ Has smooth PrimeTween animations with unscaled time
✅ Supports shared render texture with animated transitions
✅ Feels stylish and polished with subtle motion
✅ Works correctly when game is paused (`timeScale = 0`)

Next steps:
1. Integrate EventBus listener in AudioSystem
2. Create FMOD events for UI sounds
3. Set up SharedRenderTextureController positions
4. Test all animations with menu open (timeScale = 0)

