using System;
using System.Collections.Generic;
using DanielLochner.Assets.SimpleScrollSnap;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Extensions.UI
{
    public class ScrollMenu : MonoBehaviour
    {
        public List<ScrollItem> scrollItems;
        public int selectedItemIndex = 0;
        public ScrollItem SelectedItem => scrollItems[selectedItemIndex];

        public int itemsPerPage;
        
        public SimpleScrollSnap scrollSnap;

        public InputActionReference scroll;

        private float itemDistance;


        private void Awake()
        {
            scrollSnap = GetComponent<SimpleScrollSnap>();
            scroll.action.performed += OnScroll;
            
            scrollSnap.GoToPanel(8);
            print(scrollSnap.NumberOfPanels);
        }
        
        private void OnScroll(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            Vector2 scrollValue = context.ReadValue<Vector2>();
            float value = (scrollSnap.MovementAxis == MovementAxis.Horizontal) ? scrollValue.x : scrollValue.y;
            if (value > 0f)
            {
                scrollSnap.GoToNextPanel();
            }
            else if (value < 0f)
            {
                scrollSnap.GoToPreviousPanel();
            }
        }
    }
}