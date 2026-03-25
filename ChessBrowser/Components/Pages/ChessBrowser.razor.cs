using ChessBrowser.Components.Model;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Net.Quic;
using System.Security.Policy;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ChessBrowser.Components.Pages
{
    public partial class ChessBrowser
    {
        /// <summary>
        /// Bound to the Unsername form input
        /// </summary>
        private string Username = "";

        /// <summary>
        /// Bound to the Password form input
        /// </summary>
        private string Password = "";

        /// <summary>
        /// Bound to the Database form input
        /// </summary>
        private string Database = "";

        /// <summary>
        /// Represents the progress percentage of the current
        /// upload operation. Update this value to update 
        /// the progress bar.
        /// </summary>
        private int Progress = 0;

        /// <summary>
        /// This method runs when a PGN file is selected for upload.
        /// Given a list of lines from the selected file, parses the 
        /// PGN data, and uploads each chess game to the user's database.
        /// </summary>
        /// <param name="PGNFileLines">The lines from the selected file</param>
        private async Task InsertGameData(string[] PGNFileLines)
        {
            // This will build a connection string to your user's database on atr,
            // assuming you've filled in the credentials in the GUI
            string connection = GetConnectionString();

            Console.WriteLine("called");

            //   Parse the provided PGN data
            List<ChessGame> games = PgnParser.Parse(PGNFileLines);

            using (MySqlConnection conn = new MySqlConnection(connection))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    //   Iterate through data and generate appropriate insert commands
                    for (int i = 0; i < games.Count; i++)
                    {

                        ChessGame game = games[i];

                        int whiteID = InsertPlayer(conn, game.White, game.WhiteElo);
                        int blackID = InsertPlayer(conn, game.Black, game.BlackElo);
                        int eID = InsertEvent(conn, game.Event, game.Site, game.Date);

                        MySqlCommand command = conn.CreateCommand();
                        command.CommandText = "Insert Into Games(Round, Result, Moves, BlackPlayer, WhitePlayer, eID) " +
                            "Values(@round, @result, @moves, @bID, @wID, @eId); ";

                        command.Parameters.AddWithValue("@round", game.Round);
                        command.Parameters.AddWithValue("@eID", eID);
                        command.Parameters.AddWithValue("@wID", whiteID);
                        command.Parameters.AddWithValue("@bID", blackID);
                        command.Parameters.AddWithValue("@result", game.Result);
                        command.Parameters.AddWithValue("@moves", game.Moves);

                        command.ExecuteNonQuery();


                        // This tells the GUI to redraw after you update Progress (this should go inside your loop)
                        Progress = (int)(((double)(i) / games.Count) * 100);
                        await InvokeAsync(StateHasChanged);

                    }
                }   
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

        }

        /// <summary>
        /// Insert Player and get PlayerID
        /// </summary>
        /// <param name="conn">Current Connection</param>
        /// <param name="playerName">Player Name</param>
        /// <param name="elo">current Elo</param>
        /// <returns></returns>
        private int InsertPlayer(MySqlConnection conn, string playerName, int elo)
        {
            MySqlCommand command = conn.CreateCommand();
            command.CommandText = "Insert Into Players(Name, Elo) " +
                "Values(@name,@elo) " +
                "On Duplicate Key Update Elo = " +
                "If(@elo > Elo, @elo, Elo);";
            command.Parameters.AddWithValue("@name", playerName);
            command.Parameters.AddWithValue("@elo", elo);
            command.ExecuteNonQuery();

            MySqlCommand getID = conn.CreateCommand();
            getID.CommandText = "Select pID from Players Where Name = @name;";
            getID.Parameters.AddWithValue("@name", playerName);

            int pID = 0;
            using (MySqlDataReader reader = getID.ExecuteReader())
            {
                if (reader.Read())
                {
                    pID = reader.GetInt32(0);
                }
            }
            return pID;
        }

        /// <summary>
        /// Insert Event and get EventID
        /// </summary>
        /// <param name="conn"> Connection </param>
        /// <param name="EventName"> Event Name </param>
        /// <param name="Site"> Site name</param>
        /// <param name="Date"> date of the event</param>
        /// <returns></returns>
        private int InsertEvent(MySqlConnection conn, string EventName, string Site, string Date)
        {
            MySqlCommand insert = conn.CreateCommand();
            insert.CommandText = "Insert Ignore into Events(Name, Site, Date) Values (@name, @site, @date);";
            insert.Parameters.AddWithValue("@name", EventName);
            insert.Parameters.AddWithValue("@site", Site);
            insert.Parameters.AddWithValue("@date", Date);
            insert.ExecuteNonQuery();

            int eID = 0;

            MySqlCommand getID = conn.CreateCommand();
            getID.CommandText = "Select eID from Events where Name = @name and Site = @site and Date = @date";
            getID.Parameters.AddWithValue("@name", EventName);
            getID.Parameters.AddWithValue("@site", Site);
            getID.Parameters.AddWithValue("@date", Date);

            using (MySqlDataReader reader = getID.ExecuteReader())
            {
                if (reader.Read())
                {
                    eID = reader.GetInt32(0);
                }
            }
            return eID;
        }


        /// <summary>
        /// Queries the database for games that match all the given filters.
        /// The filters are taken from the various controls in the GUI.
        /// </summary>
        /// <param name="white">The white player, or "" if none</param>
        /// <param name="black">The black player, or "" if none</param>
        /// <param name="opening">The first move, e.g. "1.e4", or "" if none</param>
        /// <param name="winner">The winner as "W", "B", "D", or "" if none</param>
        /// <param name="useDate">true if the filter includes a date range, false otherwise</param>
        /// <param name="start">The start of the date range</param>
        /// <param name="end">The end of the date range</param>
        /// <param name="showMoves">true if the returned data should include the PGN moves</param>
        /// <returns>A string separated by newlines containing the filtered games</returns>
        private string PerformQuery(string white, string black, string opening,
          string winner, bool useDate, DateTime start, DateTime end, bool showMoves)
        {
            // This will build a connection string to your user's database on atr,
            // assuimg you've typed a user and password in the GUI
            string connection = GetConnectionString();

            // Build up this string containing the results from your query
            string parsedResult = "";

            // Use this to count the number of rows returned by your query
            // (see below return statement)
            int numRows = 0;

            using (MySqlConnection conn = new MySqlConnection(connection))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    //   Generate and execute an SQL command,
                    MySqlCommand command = conn.CreateCommand();

                    string query = "Select Wp.Name, Wp.Elo, Bp.Name, Bp.Elo, G.Result, G.Moves, E.Name, E.Site, E.Date " +
                        "From Games G Join Events E ON G.eID = E.eID " +
                        "Join Players Wp on G.WhitePlayer = Wp.pID " + 
                        "Join Players Bp on G.BlackPlayer = Bp.pID ";


                    if (opening != "")
                    {
                        query += "and G.Moves Like @opening ";
                        command.Parameters.AddWithValue("@opening", opening + "%");
                    }

                    if (white != "")
                    {
                        query += "and Wp.Name = @white ";
                        command.Parameters.AddWithValue("@white", white);
                    }

                    if (black != "")
                    {
                        query += "and Bp.Name = @black ";
                        command.Parameters.AddWithValue("@black", black);
                    }
                    
                    if (winner != "")
                    {
                        if (winner == "W")
                        {
                            query += "and G.Result = \"W\" ";
                        }
                        else if (winner == "B")
                        {
                            query += "and G.Result = \"B\" ";
                        }
                        else
                        {
                            query += "and G.Result = \"D\" ";
                        }
                    }

                    if (useDate)
                    {
                        query += "and E.Date >= @startDate and E.Date <= @endDate ";
                        command.Parameters.AddWithValue("@startDate", start.ToString("yyyy-MM-dd"));
                        command.Parameters.AddWithValue("@endDate", end.ToString("yyyy-MM-dd"));
                    }

                    query += ";";
                    command.CommandText = query;

                    Console.WriteLine(query);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string White = reader.GetString(0);
                            string WElo = reader.GetInt32(1).ToString();
                            string Black = reader.GetString(2);
                            string BElo = reader.GetInt32(3).ToString();
                            string Result = reader.GetString(4);
                            string Moves = reader.GetString(5);
                            string EName = reader.GetString(6);
                            string Site = reader.GetString(7);
                            string Date = reader.GetDateTime(8).ToString();

                            parsedResult += "Event: " + EName + "\n" +
                                "Site: " + Site + "\n" +
                                "Date: " + Date + "\n" +
                                "White: " + White + " (" + WElo + ")\n" +
                                "Black: " + Black + " (" + BElo + ")\n" +
                                "Result: " + Result + "\n";

                            if (showMoves)
                            {
                                parsedResult += Moves + "\n";
                            }

                            parsedResult += "\n";
                            numRows++;
                        }
                    }

                    //   then parse the results into an appropriate string and return it.
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            return numRows + " results\n" + parsedResult;
        }


        private string GetConnectionString()
        {
            return "server=atr.eng.utah.edu;database=" + Database + ";uid=" + Username + ";password=" + Password;
        }


        /// <summary>
        /// This method will run when the file chooser is used.
        /// It loads the files contents as an array of strings,
        /// then invokes the InsertGameData method.
        /// </summary>
        /// <param name="args">The event arguments, which contains the selected file name</param>
        private async void HandleFileChooser(EventArgs args)
        {
            try
            {
                string fileContent = string.Empty;

                InputFileChangeEventArgs eventArgs = args as InputFileChangeEventArgs ?? throw new Exception("unable to get file name");
                if (eventArgs.FileCount == 1)
                {
                    var file = eventArgs.File;
                    if (file is null)
                    {
                        return;
                    }

                    // load the chosen file and split it into an array of strings, one per line
                    using var stream = file.OpenReadStream(1000000); // max 1MB
                    using var reader = new StreamReader(stream);
                    fileContent = await reader.ReadToEndAsync();
                    string[] fileLines = fileContent.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                    // insert the games, and don't wait for it to finish
                    // _ = throws away the task result, since we aren't waiting for it
                    _ = InsertGameData(fileLines);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("an error occurred while loading the file..." + e);
            }
        }

    }

}
