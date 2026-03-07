using System.Text;

namespace ChessBrowser.Components.Model
{
    static class PgnParser
    {

        public static List<ChessGame> Parse(string[] input)
        {
            List<ChessGame> games = new List<ChessGame>();
            ChessGame? game = null;
            StringBuilder move = new StringBuilder();

            for (int k = 0; k < input.Length; k++)
            {
                string line = input[k];

                // Skip blank line
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // If the line start if [ then it contains a field
                if (line.StartsWith("["))
                {
                    if (game == null)
                    {
                        game = new ChessGame();
                    }

                    int i = 0;
                    while (line[i] != ' ')
                    {
                        i++;
                    }

                    string column = line.Substring(1, i - 1);
                    int j = i + 2;

                    while (line[j] != '"')
                    {
                        j++;
                    }

                    string columnValue = line.Substring(i + 2, j - i - 2);

                    // Insert into ChessGame base on the column
                    switch (column)
                    {
                        case "Event":
                            game.Event = columnValue;
                            break;

                        case "Site":
                            game.Site = columnValue;
                            break;

                        case "Date":
                            game.Date = columnValue;
                            break;

                        case "Round":
                            game.Round = columnValue;
                            break;

                        case "White":
                            game.White = columnValue;
                            break;

                        case "Black":
                            game.Black = columnValue;
                            break;

                        case "WhiteElo":
                            game.WhiteElo = columnValue;
                            break;

                        case "BlackElo":
                            game.BlackElo = columnValue;
                            break;

                        case "Result":
                            if (columnValue == "1 - 0")
                            {
                                game.Result = "W";
                            }
                            else if (columnValue == "0 - 1")
                            {
                                game.Result = "B";
                            }
                            else
                            {
                                game.Result = "D";
                            }
                            break;
                    }
                }
                // Moves
                else
                {
                    move.Append(line);

                    if(string.IsNullOrWhiteSpace(input[k + 1]))
                    {
                        game.Moves = move.ToString();
                        games.Add(game);

                        Console.WriteLine("----- Game Parsed -----");
                        Console.WriteLine("Event: " + game.Event);
                        Console.WriteLine("Site: " + game.Site);
                        Console.WriteLine("Date: " + game.Date);
                        Console.WriteLine("Round: " + game.Round);
                        Console.WriteLine("White: " + game.White);
                        Console.WriteLine("Black: " + game.Black);
                        Console.WriteLine("WhiteElo: " + game.WhiteElo);
                        Console.WriteLine("BlackElo: " + game.BlackElo);
                        Console.WriteLine("Result: " + game.Result);
                        Console.WriteLine("Moves: " + game.Moves);
                        Console.WriteLine();

                        move.Clear();
                        game = null;
                    }
                }

            }

            return games;
        }
    }

}
