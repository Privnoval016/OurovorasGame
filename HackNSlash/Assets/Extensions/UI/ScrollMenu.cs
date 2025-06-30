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
        public int SelectedItemIndex => scrollSnap.SelectedPanel;
        public ScrollItem SelectedItem => scrollItems[SelectedItemIndex];
        
        
        public SimpleScrollSnap scrollSnap;

        public InputActionReference scroll;

        private float itemDistance;


        private void Awake()
        {
            scrollSnap = GetComponent<SimpleScrollSnap>();
            
            if (scroll != null)
            {
                scroll.action.performed += OnScroll;
            } 
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
        
        public void ScrollLeft()
        {
            scrollSnap.GoToPreviousPanel();
        }
        
        public void ScrollRight()
        {
            scrollSnap.GoToNextPanel();
        }
        
        public int GetSelectedIndex()
        {
            return scrollSnap.SelectedPanel;
        }
    }
}