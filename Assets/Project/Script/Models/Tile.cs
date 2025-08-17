using Gazeus.DesafioMatch3.Enums;

namespace Gazeus.DesafioMatch3.Models
{
    public class Tile
    {
        /// <summary>
        /// --- PT-BR ---
        /// O identificador único da peça durante a sessão de jogo. 
        /// Este valor será 'null' se o tile representar um espaço vazio, 
        /// um placeholder, ou uma peça que já foi removida do tabuleiro. 
        /// Uma peça jogável e ativa sempre terá um ID.
        ///
        /// --- EN ---
        /// The unique identifier of the tile during the game session.
        /// This value will be 'null' if the tile represents an empty space,
        /// a placeholder, or a piece that has already been removed from the board.
        /// An active, playable tile will always have an ID.
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// --- PT-BR ---
        /// O tipo da peça (ex: Cor, formato).
        /// O tipo 'Empty' é usado para espaços vazios no tabuleiro.
        /// 
        /// --- EN ---
        /// The type of the tile (e.g., color, shape).
        /// The 'Empty' type is used for empty spaces on the board.
        /// </summary>
        public TileType Type { get; set; }
        public SpecialTileType Special { get; set; }
    }
}