using System;
using System.Drawing;

namespace terrain
{
	class MainClass
	{
		public static void Main (string[] args)
		{
//			River river = new River();
//			river.Draw ();
//			return;
			
			//for starters, let's create a 256x256 map.
			int w = int.Parse (args [0]);
			Genner g = new Genner (w,w);
			if (args.Length > 1)
			{
				g.Seed = int.Parse(args[1]);
			}
			g.Setup ();
			//for (int i = 0; i < 50; i++)
			//	g.DrawLand ();
			for (int i = 0; i < w/30; i++)
				g.DrawSpine ();
			g.SmoothMountains ();
			g.Cleanup ();
			g.DrawRivers ();
			g.DrawForest ();
			g.DrawTowns ();
			g.SaveToDb ();
			g.DrawFinal (g.Seed.ToString() + ".png");
		}
	}
}

