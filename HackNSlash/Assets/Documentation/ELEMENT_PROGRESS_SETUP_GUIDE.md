# Element Progress Tab - Complete Setup Guide

## **Overview**
The Element Progress Tab (Tab 3) allows players to view element progression and assign attacks to controller buttons. It uses a layered navigation system for intuitive controller-based UI.

---

## **Navigation Layers**

### **Layer 1: Element Selection**
- **Location**: Left side of screen
- **Components**: 5 x `ElementSlotUI` (one for each element)
- **Behavior**: User navigates between elements, presses A to select and enter Layer 2

### **Layer 2: Attack Buttons OR Unlock Grid**
- **Location**: Center of screen
- **Components**: 
  - **Bottom**: 3 x `AttackButtonSlotUI` (X, Y, A buttons in triangle formation)
  - **Top**: 10 x `UnlockSlotUI` (horizontal progression bar)
- **Behavior**: 
  - User navigates UP/DOWN between attack buttons
  - User navigates UP from attack buttons to unlock progression bar
  - Press A on attack button → Enter Layer 3 (scroll menu)
  - Navigate into unlock grid → Can view descriptions

### **Layer 3: Scroll Menu**
- **Location**: Overlays right side (over render texture)
- **Components**: `ScrollMenu` with available attacks
- **Behavior**:
  - User scrolls through available attacks
  - Press A to assign attack
  - Press B to cancel and return to Layer 2

---

## **Unity Editor Setup**

### **1. Create Element Slots (Left Side)**

Create a container: `ElementSelectionContainer`
```
ElementSelectionContainer (Vertical Layout Group)
├─ ElementSlot_Fire (ElementSlotUI component)
│  ├─ Background (Image)
│  ├─ Border (Image)
│  ├─ Icon (Image)
│  └─ NameText (TextMeshProUGUI)
├─ ElementSlot_Water (ElementSlotUI component)
├─ ElementSlot_Earth (ElementSlotUI component)
├─ ElementSlot_Wind (ElementSlotUI component)
└─ ElementSlot_Lightning (ElementSlotUI component)
```

**ElementSlotUI Configuration**:
- `Icon Image`: Reference to Icon child
- `Background Image`: Reference to Background child
- `Border Image`: Reference to Border child
- `Name Text`: Reference to NameText child
- `Normal Color`: White
- `Selected Color`: Yellow
- `Selected Scale`: 1.1
- `On Element Selected`: Leave empty (will be hooked up in code)

**Navigation**: Set explicit navigation between slots (up/down)

---

### **2. Create Progress Display (Top Center)**

Create a container: `ProgressDisplayContainer`
```
ProgressDisplayContainer (Horizontal Layout Group)
├─ UnlockSlot_Level1 (UnlockSlotUI component)
├─ UnlockSlot_Level2 (UnlockSlotUI component)
├─ ... (8 more)
└─ UnlockSlot_Level10 (UnlockSlotUI component)
```

**UnlockSlotUI Configuration**:
- `Icon Image`: Reference to Icon child
- `Background Image`: Reference to Background child
- `Border Image`: Reference to Border child
- `Lock Overlay`: Reference to lock icon overlay
- `Level Text`: Reference to "Lv.X" text
- `Normal Color`: White
- `Selected Color`: Yellow
- `Locked Color`: Gray
- `On Unlock Hovered`: Leave empty (hooked up in code)

**Navigation**: Set explicit horizontal navigation, with down navigation going to attack buttons

---

### **3. Create Attack Buttons (Bottom Center)**

Create a container: `AttackButtonsContainer`
```
AttackButtonsContainer
├─ AttackButton_X (AttackButtonSlotUI component) ← West/X button
│  ├─ Background (Image)
│  ├─ Border (Image)
│  ├─ ButtonIcon (Image) ← X button icon
│  └─ AttackNameText (TextMeshProUGUI)
├─ AttackButton_Y (AttackButtonSlotUI component) ← North/Y button
└─ AttackButton_A (AttackButtonSlotUI component) ← South/A button
```

**Position them in triangle formation**:
- X button: Left
- Y button: Top-right
- A button: Bottom-right

**AttackButtonSlotUI Configuration**:
- `Button Icon Image`: Reference to ButtonIcon child
- `Background Image`: Reference to Background child
- `Border Image`: Reference to Border child
- `Attack Name Text`: Reference to AttackNameText child
- `Normal Color`: White
- `Hover Color`: Light Gray
- `Selected Color`: Yellow
- `Selected Scale`: 1.15
- `On Button Selected`: Leave empty (hooked up in code)
- `On Button Hovered`: Leave empty (hooked up in code)

**Navigation**: Set custom navigation between the 3 buttons to match triangle layout

---

### **4. Create Scroll Menu (Right Side Overlay)**

Create a container: `ScrollMenuContainer`
```
ScrollMenuContainer (CanvasGroup, starts inactive)
├─ ScrollMenu (ScrollMenu component)
│  └─ ScrollItemContainer
│      ├─ ScrollPanel_0 (ScrollUIPanel)
│      ├─ ScrollPanel_1 (ScrollUIPanel)
│      ├─ ScrollPanel_2 (ScrollUIPanel - CENTER/FOCUSED)
│      ├─ ScrollPanel_3 (ScrollUIPanel)
│      └─ ScrollPanel_4 (ScrollUIPanel)
└─ SelectableContainer (SelectableContainer component)
```

**ScrollMenu Configuration**: Same as Equipment tab setup

---

### **5. Create Description Display (Under Video Player)**

Create: `DescriptionDisplay`
```
DescriptionDisplay (CanvasGroup)
├─ Background (Image with padding)
├─ TitleText (TextMeshProUGUI - larger, bold)
└─ DescriptionText (TextMeshProUGUI - smaller, word wrap)
```

**DescriptionDisplay Component Configuration**:
- `Title Text`: Reference to TitleText
- `Description Text`: Reference to DescriptionText
- `Canvas Group`: Auto-assigned
- `Fade Duration`: 0.2
- `Fade Ease`: OutQuad

**Position**: Directly under video player area, overlays render texture slightly

---

### **6. Configure ElementProgressTab Component**

On the `ElementProgressTab` GameObject, assign all references:

**Data Provider**:
- `Element Data Provider Object`: Reference to `ElementProgressDataProvider` GameObject

**Element Selection**:
- `Element Slots`: Array of 5 ElementSlotUI components
- `Element Selection Container`: Reference to ElementSelectionContainer

**Progress Display**:
- `Element Name Text`: Large text showing "Fire", "Water", etc.
- `Level Displays`: Array of 10 UnlockSlotUI components
- `Unlock Grid Container`: The progression bar container

**Attack Assignment**:
- `Attack Buttons`: Array of 3 AttackButtonSlotUI components
- `Attack Buttons Container`: Container for the 3 buttons
- `Attack Scroll Menu`: Reference to ScrollMenu component
- `Scroll Menu Container`: Reference to ScrollMenuContainer GameObject (starts inactive)

**Description Display**:
- `Description Display`: Reference to DescriptionDisplay component

**Character/Video Display**:
- `Character Model Display`: Reference to RenderTextureDisplay (shared)
- `Video Player Overlay`: Optional overlay GameObject for attack videos

**Navigation**:
- `Default Button`: First ElementSlotUI (will be selected when tab opens)

---

## **Key Behaviors**

### **Element Selection → Attack Buttons**
1. User navigates element slots with D-pad up/down
2. Press A on element → `OnElementSelected()` called
3. UI transitions to attack buttons (first button selected)
4. Progress bar and attack assignments update for selected element

### **Attack Button Hover → Description**
1. User navigates attack buttons
2. On hover: `OnAttackButtonHovered()` → Shows attack description below
3. Description fades in smoothly

### **Attack Button Select → Scroll Menu**
1. User presses A on attack button
2. `ActivateScrollMenu()` → Scroll menu slides in from right
3. Attack buttons become non-interactable
4. User scrolls through available attacks
5. Press A → `OnAttackConfirmed()` → Assigns attack, closes menu
6. Press B → `DeactivateScrollMenu()` → Returns to attack buttons

### **Navigate to Unlock Grid**
1. From attack buttons, navigate UP
2. Reaches horizontal progression bar
3. Can navigate left/right through unlocks
4. Hovering shows description below
5. Locked items are visible but not selectable

---

## **Animation Details**

### **Element Slot Selection**:
- Scale up to 1.1x with `OutBack` ease (0.2s)
- Border color lerps to yellow
- Scale down on deselect with `OutQuad` ease

### **Attack Button Selection**:
- Scale up to 1.15x with `OutBack` ease (0.15s)
- Border color lerps to yellow
- Faster animations than element slots for snappier feel

### **Scroll Menu Transition**:
- Slides in from right side (uses existing scroll menu system)
- Attack buttons fade out slightly (alpha 0.5)
- On close: attack buttons fade back in, menu slides out

### **Description Display**:
- Fades in over 0.2s when content changes
- Fades out over 0.2s when empty
- Content updates immediately (no delay)

---

## **Input Handling**

All input is handled through EventSystem + Input Manager:
- **D-pad/Left Stick**: Navigate between UI elements
- **A Button**: Select/Confirm
- **B Button**: Back/Cancel (only handled when scroll menu active)
- **Right Stick**: Scroll menu navigation (when active)

No mouse support - 100% controller-driven.

---

## **Code Connections**

### **ElementSlotUI**:
```csharp
slot.Initialize(elementData, icon);
slot.onElementSelected.AddListener(OnElementSelected);
```

### **AttackButtonSlotUI**:
```csharp
button.Initialize(buttonIndex, buttonIcon);
button.SetAttack(attackData);
button.onButtonSelected.AddListener(OnAttackButtonSelected);
button.onButtonHovered.AddListener(OnAttackButtonHovered);
```

### **UnlockSlotUI**:
```csharp
unlock.Initialize(index, name, description, level, isUnlocked, icon);
unlock.onUnlockHovered.AddListener(OnUnlockHovered);
```

### **DescriptionDisplay**:
```csharp
descriptionDisplay.ShowAttack(attackData);
descriptionDisplay.ShowUnlock(name, description);
descriptionDisplay.Hide();
```

All connections are made in code - no manual UnityEvent setup required in Inspector!

---

## **Testing Checklist**

✅ Can navigate element slots with D-pad
✅ Pressing A on element loads that element's data
✅ Can navigate attack buttons after selecting element
✅ Hovering attack button shows description below
✅ Pressing A on attack button opens scroll menu
✅ Can scroll through attacks with right stick
✅ Pressing A assigns attack and closes menu
✅ Pressing B closes menu without assigning
✅ Can navigate to progression bar from attack buttons
✅ Progression bar shows correct unlocked/locked states
✅ All animations are smooth with no jank
✅ No mouse interaction possible

---

This architecture ensures clean separation, smooth animations, and intuitive controller navigation!

