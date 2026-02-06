using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Extensions.UI
{
    /// <summary>
    /// Modular component for displaying player stats in the UI.
    /// Can be reused in different contexts (character tab, pause menu, etc.).
    /// </summary>
    public class PlayerStatsDisplay : MonoBehaviour
    {
        [Header("Level & Experience")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Slider experienceBar;
        [SerializeField] private TextMeshProUGUI experienceText;
        
        [Header("Health")]
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Slider healthBar;
        
        [Header("Charge/Energy")]
        [SerializeField] private TextMeshProUGUI chargeText;
        [SerializeField] private Slider chargeBar;
        
        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI strengthText;
        [SerializeField] private TextMeshProUGUI defenseText;
        
        [Header("Formatting")]
        [SerializeField] private string levelFormat = "Lv. {0}";
        [SerializeField] private string experienceFormat = "{0} / {1} XP";
        [SerializeField] private string healthFormat = "{0:F0} / {1:F0}";
        [SerializeField] private string chargeFormat = "{0:F0} / {1:F0}";
        [SerializeField] private string statFormat = "{0}";
        
        private PlayerStatsDisplayData currentData;
        
        #region Public Methods
        
        /// <summary>
        /// Updates the stats display with new data.
        /// </summary>
        /// <param name="data">The stats data to display.</param>
        public void UpdateDisplay(PlayerStatsDisplayData data)
        {
            if (data == null)
            {
                Debug.LogWarning("PlayerStatsDisplay: Received null data!");
                return;
            }
            
            currentData = data;
            RefreshDisplay();
        }
        
        /// <summary>
        /// Refreshes the display with the current data.
        /// </summary>
        public void RefreshDisplay()
        {
            if (currentData == null)
                return;
            
            UpdateLevel();
            UpdateExperience();
            UpdateHealth();
            UpdateCharge();
            UpdateStats();
        }
        
        /// <summary>
        /// Clears the display and shows default/empty values.
        /// </summary>
        public void ClearDisplay()
        {
            currentData = PlayerStatsDisplayData.Default();
            RefreshDisplay();
        }
        
        #endregion
        
        #region Private Update Methods
        
        private void UpdateLevel()
        {
            if (levelText != null)
                levelText.text = string.Format(levelFormat, currentData.level);
        }
        
        private void UpdateExperience()
        {
            if (experienceBar != null)
            {
                float expPercent = currentData.experienceToNextLevel > 0
                    ? (float)currentData.currentExperience / currentData.experienceToNextLevel
                    : 0f;
                experienceBar.value = expPercent;
            }
            
            if (experienceText != null)
            {
                experienceText.text = string.Format(experienceFormat,
                    currentData.currentExperience,
                    currentData.experienceToNextLevel);
            }
        }
        
        private void UpdateHealth()
        {
            if (healthBar != null)
            {
                float healthPercent = currentData.maxHealth > 0
                    ? currentData.currentHealth / currentData.maxHealth
                    : 0f;
                healthBar.value = healthPercent;
            }
            
            if (healthText != null)
            {
                healthText.text = string.Format(healthFormat,
                    currentData.currentHealth,
                    currentData.maxHealth);
            }
        }
        
        private void UpdateCharge()
        {
            if (chargeBar != null)
            {
                float chargePercent = currentData.maxCharge > 0
                    ? currentData.currentCharge / currentData.maxCharge
                    : 0f;
                chargeBar.value = chargePercent;
            }
            
            if (chargeText != null)
            {
                chargeText.text = string.Format(chargeFormat,
                    currentData.currentCharge,
                    currentData.maxCharge);
            }
        }
        
        private void UpdateStats()
        {
            if (strengthText != null)
                strengthText.text = string.Format(statFormat, currentData.strength);
            
            if (defenseText != null)
                defenseText.text = string.Format(statFormat, currentData.defense);
        }
        
        #endregion
    }
}

