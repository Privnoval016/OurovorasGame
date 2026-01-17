using UnityEngine;

namespace ProceduralGrammarGeneration.Spatial
{
    /// <summary>
    /// Strategy that stacks elements vertically (for buildings with floors).
    /// FloorMarker symbols define vertical position, other symbols are placed horizontally within floors.
    /// </summary>
    public class VerticalStackStrategy : BaseSpatialStrategy
    {
        public override string Name => "Vertical Stack";
        
        private float _heightPerFloor = 25f;
        private float _currentFloorY = 0f;
        private float _currentXOffset = 0f;
        
        public VerticalStackStrategy(float heightPerFloor = 25f)
        {
            _heightPerFloor = heightPerFloor;
        }
        
        public override void Initialize(SpatialContext context)
        {
            _currentFloorY = 0f;
            _currentXOffset = 0f;
        }
        
        public override void PlaceNode(SpatialNode node, SpatialContext context)
        {
            // Handle FloorMarker - sets vertical position for subsequent elements
            if (node.SymbolName == "FloorMarker")
            {
                int floorNum = node.GetParameter("floorNum", 0);
                _currentFloorY = floorNum * _heightPerFloor;
                _currentXOffset = 0f; // Reset horizontal position for new floor
                
                node.Position = new Vector3(0, _currentFloorY, 0);
                node.Rotation = Quaternion.identity;
                node.Scale = Vector3.one * 0.1f; // Small marker
                
                context.CurrentPosition = new Vector3(0, _currentFloorY, 0);
                return;
            }
            
            // Place element at current floor level
            node.Position = new Vector3(_currentXOffset, _currentFloorY, 0);
            node.Rotation = Quaternion.identity;
            
            // Apply geometry data (scale, offsets, alignment)
            ApplyGeometryData(node, context);
            
            // Advance horizontal position for next element
            float width = node.GetParameter("width", 1f);
            _currentXOffset += width;
            
            // Add spacing if defined in geometry data
            if (node.GeometryData != null)
                _currentXOffset += node.GeometryData.spacing;
            
            context.CurrentPosition = new Vector3(_currentXOffset, _currentFloorY, 0);
        }
    }
}
