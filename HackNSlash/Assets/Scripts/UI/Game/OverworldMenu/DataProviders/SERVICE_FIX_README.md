# Data Providers - Service Registration Issue Fix

## The Problem
Data providers were incorrectly registered as global services. They should be locally managed by OverworldMenuUI.

## Why This Matters
- **Services** are for globally accessible, singleton-like components (PlayerController, GameManager, etc.)
- **Data Providers** are local adapters that should be scoped to the menu UI
- Having them as services pollutes the global namespace and creates unnecessary coupling

## What Was Fixed
All data provider classes now:
1. Do NOT implement `IService` interface
2. Do NOT call `Services.Register<>()` in Awake
3. Are managed locally by `OverworldMenuUI` via direct component references
4. Still access PlayerController via Services (which IS a valid global service)

## Manual Fix Required
For each file in `DataProviders/` folder, remove `, IService` from class declaration and remove the Awake method that registers the service.

**Before:**
```csharp
public class XxxDataProvider : MonoBehaviour, IXxxDataProvider, IService
{
    private void Awake()
    {
        Services.Register<XxxDataProvider>(this);
    }
}
```

**After:**
```csharp
public class XxxDataProvider : MonoBehaviour, IXxxDataProvider
{
    private void Start()
    {
        // Get PlayerController (which IS a valid global service)
        try
        {
            var playerController = Services.Get<PlayerController>();
            // ... use it
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"XxxDataProvider: Could not get PlayerController: {e.Message}");
        }
    }
}
```

Apply this to:
- PlayerDataProvider.cs ✅ (Done)
- EquipmentDataProvider.cs ✅ (Done)  
- ElementProgressDataProvider.cs ✅ (Done)
- SkillTreeDataProvider.cs
- InventoryDataProvider.cs
- QuestDataProvider.cs
- CompendiumDataProvider.cs

