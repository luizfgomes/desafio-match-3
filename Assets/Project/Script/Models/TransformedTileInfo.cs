using Gazeus.DesafioMatch3.Enums;
using UnityEngine;

namespace Gazeus.DesafioMatch3.Models
{
    public class TransformedTileInfo : MonoBehaviour
    {
        public Vector2Int Position { get; set; }
        public SpecialTileType NewSpecialType { get; set; }
    }
}
