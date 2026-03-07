using System;
using System.Globalization;
using System.Text;

namespace ChessBrowser.Components;

public static class PgnParser
{
	public static List<ChessGame> PgnParser(string[] pgnFile)
	{
		List<ChessGame> game = new();
		Dictionary<string, string> tag = new();
		stringBuilder moves = new();
		bool moveReading = false; 

        foreach (string pgn in pgnFile.Append(""))
		{
			string line = pgn.Trim();

			if (!moveReading)
			{
				if (string.IsNullOrEmpty(line))
				{
					if (tag.Count > 0)
					{
						moveReading = true;
					}
				}
				ParseTag(line, tag);

			}
			else { 
				if (string.IsNullOrEmpty(line))
				{
					if(tag.Count > 0)
					{

					}
				}

			}
			

		}

	}

	private static void ParseTag(string line, Dictionary<string, string> tag)
	{
		if (!line.StartsWith('[') || !line.EndsWith(']'))
			{ return; }
		int firstSpace = line.IndexOf(' ');
		int firstQuote = line.IndexOf('"');
		int lastQuote = line.LastIndexOf('"');

		if(firstSpace < 0 || firstQuote < 0 || lastQuote < 0 ) 
			{ return; }

        // For example:  [Event "4. IIFL Wealth Mumbai Op"] 
        // name : Event , value = "4. IIFL Wealth Mumbai Op"

        string name = line.Substring(1, firstSpace - 1).Trim();
		string value = line.Substring(firstQuote + 1, lastQuote - firstQuote - 1);

		tag[name] = value;
	}



}
