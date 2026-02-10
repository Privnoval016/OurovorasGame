using UnityEngine;
using UnityEngine.UI;

namespace Extensions.UI
{
    /// <summary>
    /// Automatically configures navigation for ItemSlotUI grids in code.
    /// Eliminates manual navigation setup in Inspector - just set grid dimensions.
    /// Place on parent GameObject containing ItemSlotUI children.
    /// </summary>
    public class SlotGridNavigator : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [Tooltip("Number of columns in the slot grid (e.g., 3 for a 3x2 grid).")]
        [SerializeField] private int columns = 3;
        
        [Tooltip("Number of rows in the slot grid (e.g., 2 for a 3x2 grid).")]
        [SerializeField] private int rows = 2;
        
        [Tooltip("Navigation above the grid (e.g., tab buttons).")]
        [SerializeField] private Selectable selectOnUpFromTop;
        
        [Tooltip("Navigation below the grid (e.g., confirm button).")]
        [SerializeField] private Selectable selectOnDownFromBottom;
        
        [Tooltip("Automatically configure navigation on Start.")]
        [SerializeField] private bool autoConfigureOnStart = true;
        
        private ItemSlotUI[] slots;
        
        #region MonoBehaviour Callbacks
        
        private void Start()
        {
            if (autoConfigureOnStart)
            {
                ConfigureNavigation();
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Configures navigation for all ItemSlotUI children based on grid layout.
        /// Call manually if autoConfigureOnStart is false.
        /// </summary>
        public void ConfigureNavigation()
        {
            // Get all ItemSlotUI children
            slots = GetComponentsInChildren<ItemSlotUI>(true);
            
            if (slots == null || slots.Length == 0)
            {
                Debug.LogWarning("SlotGridNavigator: No ItemSlotUI children found!");
                return;
            }
            
            // Configure each slot
            for (int i = 0; i < slots.Length; i++)
            {
                ConfigureSlot(slots[i], i);
            }
            
            Debug.Log($"SlotGridNavigator: Configured navigation for {slots.Length} slots in {columns}x{rows} grid");
        }
        
        #endregion
        
        #region Private Methods
        
        private void ConfigureSlot(ItemSlotUI slot, int index)
        {
            // Get or add Button component
            Button button = slot.GetComponent<Button>();
            if (button == null)
            {
                button = slot.gameObject.AddComponent<Button>();
            }
            
            // Setup navigation
            Navigation nav = new Navigation();
            nav.mode = Navigation.Mode.Explicit;
            
            // Calculate grid position
            int row = index / columns;
            int col = index % columns;
            
            // Left
            int leftIndex = index - 1;
            if (col > 0 && leftIndex >= 0 && leftIndex < slots.Length)
            {
                Button leftButton = slots[leftIndex].GetComponent<Button>();
                if (leftButton != null)
                    nav.selectOnLeft = leftButton;
            }
            
            // Right
            int rightIndex = index + 1;
            if (col < columns - 1 && rightIndex < slots.Length)
            {
                Button rightButton = slots[rightIndex].GetComponent<Button>();
                if (rightButton != null)
                    nav.selectOnRight = rightButton;
            }
            
            // Up
            int upIndex = index - columns;
            if (upIndex >= 0)
            {
                Button upButton = slots[upIndex].GetComponent<Button>();
                if (upButton != null)
                    nav.selectOnUp = upButton;
            }
            else if (selectOnUpFromTop != null)
            {
                // First row - navigate up to external element
                nav.selectOnUp = selectOnUpFromTop;
            }
            
            // Down
            int downIndex = index + columns;
            if (downIndex < slots.Length)
            {
                Button downButton = slots[downIndex].GetComponent<Button>();
                if (downButton != null)
                    nav.selectOnDown = downButton;
            }
            else if (selectOnDownFromBottom != null)
            {
                // Last row - navigate down to external element
                nav.selectOnDown = selectOnDownFromBottom;
            }
            
            button.navigation = nav;
        }
        
        #endregion
    }
}

