using Gazeus.DesafioMatch3.Enums;

namespace Gazeus.DesafioMatch3.Models
{
    public class Tile
    {
        public int Id { get; set; }
        public int Type { get; set; }
        public SpecialTileType Special { get; set; }
    }
}
