# Inventory Tab - Setup Guide

## Overview

The Inventory Tab provides a complete item management system with category filtering, item usage, and discard functionality. It includes equipped item indicators and automatically unequips items before discarding them.

## Architecture

### Core Components

1. **InventoryTab (UI Controller)**
   - Manages category selection and item display
   - Handles action menu (Use/Discard)
   - Checks equipment status before discarding
   - Updates equipped indicators

2. **ItemActionMenu (Modular Component)**
   - Reusable action menu for any inventory system
   - Shows "Use" and "Discard" options
   - Controller navigation built-in
   - Events for action responses

3. **ScrollMenu**
   - Displays items in scrollable list
   - Handles navigation and selection
   - Updates item info on scroll

4. **InventoryDataProvider**
   - Interfaces with game inventory system
   - Provides item data by category
   - Handles item usage and discard operations

5. **EquipmentDataProvider**
   - Used to check if items are equipped
   - Unequips items when discarding

### Design Patterns

- **MVVM**: UI ↔ DataProvider ↔ Game Model
- **Strategy Pattern**: Item usage via UsageStrategy
- **Observer Pattern**: Action menu events
- **Modular Components**: ItemActionMenu works in any inventory

## UI Setup

### Hierarchy Structure

```
InventoryTab (GameObject)
├── CategoryPanel (Left side)
│   ├── CategoryButton_All (Button)
│   │   └── Text (TextMeshProUGUI)
│   ├── CategoryButton_Accessories (Button)
│   │   └── Text (TextMeshProUGUI)
│   ├── CategoryButton_Consumables (Button)
│   │   └── Text (TextMeshProUGUI)
│   └── ... (More category buttons as needed)
├── ItemDisplayPanel (Center)
│   ├── ScrollMenuContainer (GameObject)
│   │   └── ScrollMenu (Component: ScrollMenu)
│   │       └── Panels[] (ScrollPanel components)
│   ├── ActionMenuContainer (GameObject - initially disabled)
│   │   └── ItemActionMenu (Component: ItemActionMenu)
│   │       ├── UseButton (Button)
│   │       │   └── Text (TextMeshProUGUI) "Use"
│   │       └── DiscardButton (Button)
│   │           └── Text (TextMeshProUGUI) "Discard"
│   └── InfoPanel (Bottom display)
│       ├── ItemIcon (Image)
│       ├── EquippedIndicator (Image - shows when equipped)
│       ├── ItemNameText (TextMeshProUGUI)
│       ├── ItemDescriptionText (TextMeshProUGUI)
│       └── ItemQuantityText (TextMeshProUGUI)
└── CharacterModelDisplay (Right side - RenderTextureDisplay)
```

### Component Configuration

#### 1. InventoryTab Component

**Data Provider:**
- **Inventory Data Provider Object**: Drag InventoryDataProvider GameObject
- **Equipment Data Provider Object**: Drag EquipmentDataProvider GameObject

**UI Components - Category Selection:**
- **Category Buttons[]**: Array of category filter buttons
- **Category Button Texts[]**: Array of TextMeshProUGUI components for each button
- **Selected Category Color**: Yellow (1, 1, 0)
- **Unselected Category Color**: White (1, 1, 1)

**UI Components - Item Display:**
- **Item Scroll Menu**: ScrollMenu component
- **Scroll Menu Container**: GameObject containing scroll menu
- **Selected Item Name Text**: TextMeshProUGUI for item name
- **Selected Item Description Text**: TextMeshProUGUI for description
- **Selected Item Icon**: Image for item icon
- **Selected Item Quantity Text**: TextMeshProUGUI for "x10" etc.

**UI Components - Action Menu:**
- **Item Action Menu**: ItemActionMenu component
- **Action Menu Container**: GameObject containing action menu (starts disabled)

**UI Components - Equipped Indicator:**
- **Equipped Indicator**: Image that shows when item is equipped elsewhere

**UI Components - Character Model:**
- **Character Model Display**: RenderTextureDisplay component

#### 2. ItemActionMenu Component

**Menu Buttons:**
- **Use Button**: Button component for "Use" action
- **Discard Button**: Button component for "Discard" action

**Button Text (Optional):**
- **Use Button Text**: TextMeshProUGUI showing "Use"
- **Discard Button Text**: TextMeshProUGUI showing "Discard"

**Settings:**
- **Hide On Action**: True (menu closes after action)

#### 3. ScrollMenu Component

Follow the standard ScrollMenu setup (see ScrollMenuSetup.md).

**Important Settings:**
- **Cycle Mode**: Stop (don't wrap around)
- **Visible Panels**: 5-7 (depends on screen space)
- **Focus Index**: 2 (middle position)

#### 4. Category Buttons

Each category button should:
- Have a Button component
- Have a child TextMeshProUGUI for the category name
- Be added to the categoryButtons array in order
- Corresponding text added to categoryButtonTexts array

**Default Categories** (customize based on your inventory system):
- All
- Accessories
- Consumables
- Materials
- Key Items

#### 5. Equipped Indicator Setup

The equipped indicator should:
- Be an Image component
- Use a distinctive icon (e.g., checkmark, star, equipped badge)
- Start disabled (InventoryTab will enable when needed)
- Position near the item icon
- Use a noticeable color (e.g., green, gold)

## Usage Flow

### Player Workflow

1. **Opening Inventory**:
   - Tab to Inventory (Tab 5)
   - Default shows "All" category
   - First item is selected

2. **Filtering by Category**:
   - Navigate left to category buttons
   - Select a category button
   - Items refresh to show only that category

3. **Viewing Item Details**:
   - Use D-pad Up/Down to scroll through items
   - Item info updates in bottom panel
   - **Equipped indicator shows if item is equipped elsewhere**
   - Quantity shown for stackable items

4. **Using an Item**:
   - Press A button on an item
   - Action menu appears with "Use" and "Discard"
   - Navigate to "Use" option
   - Press A to use item
   - Item's UsageStrategy.Use() is called
   - Menu closes, display refreshes

5. **Discarding an Item**:
   - Press A button on an item
   - Action menu appears
   - Navigate to "Discard" option
   - Press A to discard
   - **System checks if item is equipped**
   - **If equipped, automatically unequips first**
   - Item removed from inventory
   - Menu closes, display refreshes

6. **Canceling**:
   - Press B button to close action menu
   - Returns to item scrolling

### Developer Workflow

#### Adding a New Item Category

1. **Define Category in Inventory System**:
   - Add category to your inventory data structure
   - Return it in `IInventoryDataProvider.GetCategories()`

2. **No UI Changes Needed**:
   - InventoryTab dynamically creates buttons for all categories
   - Just ensure you have enough categoryButtons in the array

3. **Implement Category Filtering**:
   - Implement `GetItemsByCategory(string category)` in your data provider
   - Return only items matching that category

#### Implementing Item Usage

1. **Create UsageStrategy**:
```csharp
public class HealingPotionStrategy : UsageStrategy
{
    public int healAmount = 50;
    
    public override void Use(InventoryItem item)
    {
        // Heal player
        PlayerController player = FindObjectOfType<PlayerController>();
        player.Health += healAmount;
        
        // Remove item from inventory (handled by data provider)
    }
}
```

2. **Assign to InventoryItem**:
   - In InventoryItem inspector, set `canBeUsed = true`
   - Assign your UsageStrategy to `usageStrategy` field

3. **Data Provider Handles Execution**:
   - `InventoryDataProvider.UseItem()` calls `item.usageStrategy.Use()`
   - Automatically handles inventory updates

#### Adding Equipped Check

The equipped check is automatic:
- InventoryTab checks both Accessories and Passives
- Before discarding, unequips from all slots
- Works with any item that can be equipped

**To add more equipment types**:
1. Add check in `IsItemEquipped()`
2. Add unequip in `UnequipItem()`
3. Add corresponding methods to IEquipmentDataProvider

## Key Features

### 1. Category Filtering

- **Dynamic**: Categories are loaded from data provider
- **"All" Category**: Shows all items
- **Visual Feedback**: Selected category is highlighted
- **Persistence**: Category selection maintained during tab session

### 2. Item Action Menu

- **Conditional Use Button**: Disabled if item can't be used
- **Always-Available Discard**: Can discard any item
- **Controller Navigation**: Vertical navigation between options
- **Auto-Hide**: Closes after action or cancel

### 3. Equipped Indicator

- **Automatic Detection**: Checks all equipment slots
- **Real-Time Update**: Updates when scrolling through items
- **Visual Warning**: Shows user before discarding equipped items
- **Multi-Slot Check**: Finds item in any equipment slot

### 4. Safe Discard

- **Equipment Check**: Never allows equipped item to be discarded without unequipping
- **Automatic Unequip**: Removes from all slots where equipped
- **Both Systems Updated**: Updates both inventory and equipment data
- **No Orphaned Data**: Ensures consistency between systems

### 5. Item Usage

- **Strategy Pattern**: Flexible item effects via UsageStrategy
- **Extensible**: Easy to add new item types and effects
- **Inventory Update**: Automatically consumes/removes item after use
- **Error Handling**: Graceful failure if item can't be used

## Troubleshooting

### Items Not Showing

- Check that InventoryDataProvider is assigned
- Verify `GetAllItems()` or `GetItemsByCategory()` returns data
- Check ScrollMenu is properly configured
- Ensure category name matches data provider categories

### Action Menu Not Opening

- Verify ItemActionMenu component is assigned
- Check actionMenuContainer is assigned and starts disabled
- Ensure Input Manager has onSelect action configured
- Check that scroll menu is active when pressing A

### Equipped Indicator Not Showing

- Verify EquipmentDataProvider is assigned
- Check that equipped indicator Image is assigned
- Ensure `GetEquippedAccessory()` and `GetEquippedPassive()` return valid data
- Check item names match between inventory and equipment

### Can't Discard Item

- Check if `DiscardItem()` is implemented in data provider
- Verify item exists in inventory
- Check for errors in unequip logic
- Ensure equipment provider methods are working

### Use Button Always Disabled

- Check that item has `canBeUsed = true`
- Verify `usageStrategy` is assigned to item
- Ensure `CanItemBeUsed()` is implemented correctly
- Check data provider returns correct value

## Best Practices

### 1. Data Provider Implementation

**Always cache inventory data**:
```csharp
private Dictionary<string, InventoryItem> itemCache;

public bool UseItem(string itemName)
{
    if (!itemCache.ContainsKey(itemName))
        return false;
    
    var item = itemCache[itemName];
    
    if (item.usageStrategy != null)
    {
        item.usageStrategy.Use(item);
        // Update inventory
        return true;
    }
    
    return false;
}
```

### 2. Usage Strategies

**Keep them self-contained**:
```csharp
public override void Use(InventoryItem item)
{
    // Access game systems via services or find
    // Don't depend on UI or data providers
    // Handle all effects here
}
```

### 3. Category Management

**Use enums or constants**:
```csharp
public static class ItemCategories
{
    public const string Accessories = "Accessories";
    public const string Consumables = "Consumables";
    public const string Materials = "Materials";
}
```

### 4. Equipped Checking

**Centralize in data provider**:
```csharp
public bool IsItemEquipped(string itemName)
{
    // Check all possible equipment slots
    // Return true if found in any
}
```

## Extension Points

### Adding New Categories

1. Return category from `GetCategories()`
2. Implement filtering in `GetItemsByCategory()`
3. UI updates automatically

### Custom Item Actions

1. Create new button in ItemActionMenu
2. Add event for new action
3. Subscribe in InventoryTab
4. Implement action logic

### Equipment Slot Types

1. Add new slot type check in `IsItemEquipped()`
2. Add unequip logic in `UnequipItem()`
3. Update IEquipmentDataProvider interface
4. Implement in data provider

### Visual Enhancements

- Add rarity color coding to items
- Item preview 3D models
- Category icons
- Sorting options (name, rarity, quantity)
- Search/filter functionality

## Summary

The Inventory Tab is:
- **Modular**: ItemActionMenu works in any context
- **Safe**: Auto-unequips before discarding
- **Extensible**: Easy to add categories and actions
- **User-Friendly**: Clear visual feedback
- **Controller-First**: Full gamepad support
- **Data-Driven**: Categories and items from data provider

All item management flows through proper MVVM architecture with clean separation between UI and game logic.

