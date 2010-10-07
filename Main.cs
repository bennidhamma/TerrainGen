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
			//for (int i = 0; i < 50; i++)
			//	g.DrawLand ();
			for (int i = 0; i < w/20; i++)
				g.DrawSpine ();
			g.SmoothMountains ();
			g.Cleanup ();
			g.DrawRivers ();
			g.DrawForest ();
			g.DrawTowns ();
			//g.SaveToDb ();
			g.Bitmap.Save ("test.png");
		}
	}
}

