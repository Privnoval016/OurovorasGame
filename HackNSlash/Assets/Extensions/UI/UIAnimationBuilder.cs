using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace Extensions.UI
{
    /// <summary>
    /// Builder for UI animations. Creates animations based on UIAnimationConfig.
    /// Provides fluent API for building complex animation sequences.
    /// </summary>
    public class UIAnimationBuilder
    {
        private readonly UIAnimationConfig config;
        private readonly Transform target;
        private readonly Graphic[] graphics;
        
        private bool useUnscaledTime = true;
        
        /// <summary>
        /// Creates a new animation builder for the given target.
        /// </summary>
        /// <param name="config">The animation configuration to use.</param>
        /// <param name="target">The transform to animate.</param>
        /// <param name="graphics">Optional graphics to animate colors on.</param>
        public UIAnimationBuilder(UIAnimationConfig config, Transform target, params Graphic[] graphics)
        {
            this.config = config;
            this.target = target;
            this.graphics = graphics;
        }
        
        /// <summary>
        /// Sets whether to use unscaled time for animations.
        /// </summary>
        public UIAnimationBuilder WithUnscaledTime(bool unscaled)
        {
            useUnscaledTime = unscaled;
            return this;
        }
        
        /// <summary>
        /// Animates selection (scale up + color change).
        /// </summary>
        public UIAnimationBuilder AnimateSelection()
        {
            // Scale animation
            if (target != null)
            {
                Tween.Scale(target, config.selectedScale, 
                    duration: config.scaleAnimationDuration,
                    ease: config.selectionEase,
                    useUnscaledTime: useUnscaledTime);
            }
            
            // Color animation
            AnimateColorTo(config.selectedColor);
            
            return this;
        }
        
        /// <summary>
        /// Animates deselection (scale back to normal + color change).
        /// </summary>
        public UIAnimationBuilder AnimateDeselection()
        {
            // Scale animation
            if (target != null)
            {
                Tween.Scale(target, 1f,
                    duration: config.scaleAnimationDuration,
                    ease: config.deselectionEase,
                    useUnscaledTime: useUnscaledTime);
            }
            
            // Color animation
            AnimateColorTo(config.normalColor);
            
            return this;
        }
        
        /// <summary>
        /// Animates a punch scale effect (quick scale up and back).
        /// </summary>
        public UIAnimationBuilder AnimatePunch()
        {
            if (target != null)
            {
                Tween.PunchScale(target, config.punchScaleStrength,
                    duration: config.punchScaleDuration,
                    useUnscaledTime: useUnscaledTime);
            }
            
            return this;
        }
        
        /// <summary>
        /// Animates color transition to the specified color.
        /// </summary>
        public UIAnimationBuilder AnimateColorTo(Color targetColor)
        {
            if (graphics != null && graphics.Length > 0)
            {
                foreach (var graphic in graphics)
                {
                    if (graphic != null)
                    {
                        Tween.Color(graphic, targetColor,
                            duration: config.colorTransitionDuration,
                            ease: config.colorEase,
                            useUnscaledTime: useUnscaledTime);
                    }
                }
            }
            
            return this;
        }
        
        /// <summary>
        /// Sets color immediately without animation.
        /// </summary>
        public UIAnimationBuilder SetColor(Color color)
        {
            if (graphics != null && graphics.Length > 0)
            {
                foreach (var graphic in graphics)
                {
                    if (graphic != null)
                    {
                        graphic.color = color;
                    }
                }
            }
            
            return this;
        }
        
        /// <summary>
        /// Sets scale immediately without animation.
        /// </summary>
        public UIAnimationBuilder SetScale(float scale)
        {
            if (target != null)
            {
                target.localScale = Vector3.one * scale;
            }
            
            return this;
        }
        
        /// <summary>
        /// Animates fade in (alpha 0 to 1).
        /// </summary>
        public UIAnimationBuilder AnimateFadeIn()
        {
            if (graphics != null && graphics.Length > 0)
            {
                foreach (var graphic in graphics)
                {
                    if (graphic != null)
                    {
                        Color target = graphic.color;
                        target.a = 1f;
                        Tween.Color(graphic, target,
                            duration: config.fadeAnimationDuration,
                            ease: config.colorEase,
                            useUnscaledTime: useUnscaledTime);
                    }
                }
            }
            
            return this;
        }
        
        /// <summary>
        /// Animates fade out (alpha 1 to 0).
        /// </summary>
        public UIAnimationBuilder AnimateFadeOut()
        {
            if (graphics != null && graphics.Length > 0)
            {
                foreach (var graphic in graphics)
                {
                    if (graphic != null)
                    {
                        Color target = graphic.color;
                        target.a = 0f;
                        Tween.Color(graphic, target,
                            duration: config.fadeAnimationDuration,
                            ease: config.colorEase,
                            useUnscaledTime: useUnscaledTime);
                    }
                }
            }
            
            return this;
        }
        
        /// <summary>
        /// Animates to empty state (gray color, normal scale).
        /// </summary>
        public UIAnimationBuilder AnimateToEmpty()
        {
            AnimateColorTo(config.emptyColor);
            SetScale(1f);
            return this;
        }
        
        /// <summary>
        /// Animates to disabled state.
        /// </summary>
        public UIAnimationBuilder AnimateToDisabled()
        {
            AnimateColorTo(config.disabledColor);
            SetScale(1f);
            return this;
        }
    }
}

