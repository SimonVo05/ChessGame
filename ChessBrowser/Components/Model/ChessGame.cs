namespace ChessBrowser.Components.Model
{

    // Represents one instance of a Chess game, such as the players, round, result, moves, event name and date
    public class ChessGame
    {
        public string Event { get; set; } = "";
        public string Site { get; set; } = "";
        public string Date { get; set; } = "";
        public string Round { get; set; } = "";
        public string White { get; set; } = "";
        public string Black { get; set; } = "";
        public string Result { get; set; } = "";
        public string Moves { get; set; } = "";
        public int WhiteElo { get; set; } = 0;
        public int BlackElo { get; set; } = 0;
    }
}
