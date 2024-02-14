﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AurumGames.CustomLayout
{
    public sealed class CustomHorizontalLayout : CustomStructureLayout
    {
        [SerializeField] private float _spacing;
        [SerializeField] private bool _fitHorizontal;
        [SerializeField] private bool _fitVertical;
        [SerializeField] private bool _sizeVertical;
        
        protected override void UpdateLayoutInternal()
        {
            var childrenGroups = GetChildren();
            if (childrenGroups.Count == 0)
                return;

            LayoutGroupCalculation layoutGroupCalculation = Calculate(childrenGroups);
            if (_fitHorizontal)
                RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, layoutGroupCalculation.ContainerSize.x);

            if (_fitVertical)
                RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, layoutGroupCalculation.ContainerSize.y);
            
            foreach (var pair in layoutGroupCalculation.Transforms)
            {
                Apply(childrenGroups[pair.Key], pair.Value);
            }
        }

        public override Vector2 GetSize(Vector2 preferredSize)
        {
            var hasPreferredWidth = float.IsInfinity(preferredSize.x) == false;
            var hasPreferredHeight = float.IsInfinity(preferredSize.y) == false;

            Rect rect = RectTransform.rect;

            try
            {
                if (hasPreferredWidth)
                    RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredSize.x);
                else
                    preferredSize.x = rect.width;
                
                if (hasPreferredHeight)
                    RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredSize.y);
                else
                    preferredSize.y = rect.height;
                
                var childrenGroups = GetChildren();
                Vector2 selfSize = GetSelfSizeExcludePadding();
                var childrenSizes = childrenGroups.Values
                    .Select(children => GetSizes(children, selfSize)).ToArray();
                var fitSizes = childrenSizes
                    .Select(GetFitSize).ToArray();
                Vector2 containerSize = GetContainerSize(FindBiggest(fitSizes));

                if (_fitHorizontal)
                    preferredSize.x = containerSize.x;
                
                if (_fitVertical)
                    preferredSize.y = containerSize.y;

                return preferredSize;
            }
            finally
            {
                RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rect.width);
                RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rect.height);
            }
        }

        public override LayoutGroupCalculation Calculate(Dictionary<int, List<RectTransform>> childrenGroups)
        {
            var result = new Dictionary<int, IReadOnlyList<Rect>>();
            Vector2 selfSize = GetSelfSizeExcludePadding();
            var childrenSizes = childrenGroups.Values
                .Select(children => GetSizes(children, selfSize)).ToArray();
            var fitSizes = childrenSizes
                .Select(GetFitSize).ToArray();

            Vector2 containerSize = GetContainerSize(FindBiggest(fitSizes));
            Vector2 containerPivot = GetContainerPivot(containerSize);
            Vector2 pivot = GetPivot(containerSize);

            var i = 0;
            foreach (var pair in childrenGroups)
            {
                result.Add(pair.Key, CalculateLayout(pair.Value, childrenSizes[i], pivot, fitSizes[i], containerPivot)); 
                i++;
            }

            return new LayoutGroupCalculation(result, containerSize);
        }
        
        public override LayoutCalculation Calculate(IReadOnlyList<RectTransform> children)
        {
            Vector2 selfSize = GetSelfSizeExcludePadding();
            var childrenSizes = GetSizes(children, selfSize);
            Vector2 fit = GetFitSize(childrenSizes);

            Vector2 containerSize = GetContainerSize(fit);
            Vector2 containerPivot = GetContainerPivot(containerSize);
            Vector2 pivot = GetPivot(containerSize);

            return new LayoutCalculation(CalculateLayout(children, childrenSizes, pivot, fit, containerPivot), containerSize);
        }

        public void Apply(IReadOnlyList<RectTransform> children, IReadOnlyList<Rect> calculations, bool applyPosition = true)
        {
            for (var i = 0; i < children.Count; i++)
            {
                var index = _reverse ? children.Count - i - 1 : i;
                RectTransform rectTransform = children[index];
                Rect rect = calculations[index];
                
                if (_sizeVertical)
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rect.height);
                
                if (_fitHorizontal == false)
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rect.width);
                
                if (applyPosition)
                    rectTransform.localPosition = new Vector3(rect.x, rect.y, rectTransform.localPosition.z);

                if (rectTransform.TryGetComponent(out CustomLayoutBase layout))
                    layout.UpdateLayout(false);
            }
        }

        private Vector2 GetFitSize(IReadOnlyList<Vector2> childrenSizes)
        {
            Rect self = RectTransform.rect;

            var fitHeight = self.height - _offset.vertical;
            if (_sizeVertical == false)
                fitHeight = GetHeightFit(childrenSizes);
            var fitWidth = GetWidthFit(childrenSizes);

            return new Vector2(fitWidth, fitHeight);
        }

        private IReadOnlyList<Vector2> GetSizes(IReadOnlyList<RectTransform> children, Vector2 selfSize)
        {
            var result = new Vector2[children.Count];
            Vector2 preferSize = Vector2.positiveInfinity;
            
            if (_sizeVertical)
                preferSize.y = selfSize.y;

            for (var i = 0; i < children.Count; i++)
            {
                RectTransform child = children[i];
                if (child.TryGetComponent(out CustomLayoutBase layout))
                {
                     result[i] = layout.GetSize(preferSize);
                }
                else
                {
                    result[i] = child.rect.size;
                }
            }
            
            if (_fitHorizontal == false)
                UpdateGrowChildrenSizes(result, children, selfSize);

            return result;
        }
        
        private void UpdateGrowChildrenSizes(Vector2[] sizes, IReadOnlyList<RectTransform> children, Vector2 selfSize)
        {
            var growChildIndexes = new List<int>();
            var growChildFactors = new List<float>();
            selfSize.x -= _spacing * (sizes.Length - 1);
            var growFactor = 0f;
            
            for (var i = 0; i < children.Count; i++)
            {
                RectTransform child = children[i];
                if (child.TryGetComponent(out CustomLayoutLayer layer) && layer.Grow > 0)
                {
                    growFactor += layer.Grow;
                    growChildIndexes.Add(i);
                    growChildFactors.Add(layer.Grow);
                }
                else
                {
                    selfSize.x -= sizes[i].x;
                }
            }

            if (selfSize.x < 0)
                selfSize.x = 0;

            selfSize.x /= growFactor;
            
            for (var i = 0; i < growChildIndexes.Count; i++)
            {
                sizes[growChildIndexes[i]].x = selfSize.x * growChildFactors[i];
            }
        }
        
        private float GetHeightFit(IReadOnlyList<Vector2> childrenSizes)
        {
            var maxChildHeight = 0f;
            
            for (var i = 0; i < childrenSizes.Count; i++)
            {
                var height = childrenSizes[i].y;

                if (maxChildHeight < height)
                    maxChildHeight = height;
            }

            return maxChildHeight + _offset.vertical;
        }

        private float GetWidthFit(IReadOnlyList<Vector2> childrenSizes)
        {
            var fitSize = _offset.horizontal + _spacing * (childrenSizes.Count - 1);

            for (var i = 0; i < childrenSizes.Count; i++)
            {
                fitSize += childrenSizes[i].x;
            }

            return fitSize;
        }

        private Vector2 GetContainerSize(Vector2 fitSize)
        {
            Rect self = RectTransform.rect;
            
            var containerWidth = self.width;
            var containerHeight = self.height;
            if (_fitHorizontal)
                containerWidth = fitSize.x;
            if (_fitVertical)
                containerHeight = fitSize.y;
            
            return new Vector2(containerWidth, containerHeight);
        }

        private IReadOnlyList<Rect> CalculateLayout(IReadOnlyList<RectTransform> children, IReadOnlyList<Vector2> childrenSizes, Vector2 pivot, Vector2 fitSize, Vector2 containerPivot)
        {
            var result = new Rect[children.Count];
            var currentX = pivot.x - HorizontalAlignTo(0, 0.5f, 1) * (fitSize.x - _offset.horizontal);

            for (var i = 0; i < children.Count; i++)
            {
                var index = _reverse ? children.Count - i - 1 : i;
                RectTransform rectTransform = children[index];
                Vector2 size = childrenSizes[index]; 
                var height = _sizeVertical ? fitSize.y : size.y;
                var width = size.x;
                var x = currentX + width / 2;
                currentX += _spacing + width;
                var y = pivot.y - VerticalAlignTo(0.5f, 0, -0.5f) * height;

                x -= containerPivot.x;
                y += containerPivot.y;
                Vector2 position = GetConvertToObjectPivotPosition(new Vector2(x, y), rectTransform);
                result[index] = new Rect(position.x, position.y, width, height);
            }

            return result;
        }
    }
}
