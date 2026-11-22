using NegativeCarver.Core;
using UnityEngine;

namespace NegativeCarver.Unity
{
    public sealed class BlockTile : MonoBehaviour
    {
        [SerializeField] private GridManager grid;

        public GridManager Grid => grid;

        public int X { get; private set; }

        public int Y { get; private set; }

        public BlockType Type { get; private set; }

        public void Bind(GridManager owner, int x, int y, BlockType type)
        {
            grid = owner;
            X = x;
            Y = y;
            Type = type;
            var renderer = GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            if (type != BlockType.HardRock)
            {
                renderer.SetPropertyBlock(null);
                return;
            }

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            var color = new Color(
                PilotRules.HardRockRed,
                PilotRules.HardRockGreen,
                PilotRules.HardRockBlue,
                1f);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            block.SetFloat("_Metallic", PilotRules.HardRockMetallic);
            block.SetFloat("_Smoothness", PilotRules.HardRockSmoothness);
            renderer.SetPropertyBlock(block);
        }

        public void Remove()
        {
            if (grid == null)
            {
                return;
            }

            grid.RemoveTile(this);
        }
    }
}
