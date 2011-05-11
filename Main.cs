using System;
using System.Drawing;
using Mono.Options;
using System.Collections.Generic;
using SuperFunCon;

namespace terrain
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			int length = 256;
			int spineFactor = 25;
			string gameName = null;
			int seed = (int)DateTime.Now.Ticks;
			bool saveGame = false;
			bool showHelp = false;
			
			var options = new OptionSet () {
				{"l|length:", "Side length of map (default 256)", s => length = int.Parse(s)},
				{"n|name:", "The name of the game to create.  Required if persisting.", s => gameName = s},
				{"s|seed:", "Random seed", s => seed = int.Parse(s)},
				{"f|spineFactor:", "Spine Factor (lower is more)", s => spineFactor = int.Parse(s)},
				{"p|persist", "Persist to db (default false)", s => saveGame = true},
				{"h|help", "Show help", s => showHelp = true}
			};
			
			List<string> extra;
			try
			{
				extra = options.Parse(args);
			}
			catch (OptionException e)
			{
				Console.WriteLine (e.Message);
				showHelp = true;
				return;
			}
			
			if (showHelp)
			{
				ShowHelp (options);
				return;
			}
			
			//for starters, let's create a 256x256 map.
			Genner g = new Genner (length, length);
			g.Seed = seed;
			g.Setup ();
			//for (int i = 0; i < 50; i++)
			//	g.DrawLand ();
			for (int i = 0; i < length/spineFactor; i++)
				g.DrawSpine ();
			g.SmoothMountains ();
			g.Cleanup ();
			g.DrawRivers ();
			g.DrawForest ();
			//g.DrawTowns ();
			if (saveGame)
			{
				Startup.InitializeServices ();
				g.SaveToDb (gameName);
			}
			g.DrawFinal (g.Seed.ToString() + ".png");
			
			System.Environment.Exit (0);
		}
		
		private static void ShowHelp (OptionSet options)
		{
			Console.WriteLine ("Usage: TerrainGen.exe [OPTIONS]");
			Console.WriteLine ("Generate a random map, and optionally save it to a database");
			Console.WriteLine ();
			Console.WriteLine ("Options:");
			options.WriteOptionDescriptions (Console.Out);
		}
	}
}

