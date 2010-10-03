using System;
using System.Drawing;
using System.Collections.Generic;
using OpenTK;

namespace terrain
{
	public class River
	{
		public River ()
		{
		}
		
		int w = 512;
		int h = 512;
		Bitmap b;
		public void Draw ()
		{
			b = new Bitmap(512,512);
			
			for (int x = 0; x < w; x++)
				for (int y = 0; y < h; y++)
					b.SetPixel(x,y,Color.Black);
			
			DrawRiver (new Vector2(10,10), new Vector2 (1,1));
			b.Save ("river.png");
		}
		
		public void DrawRiver(Vector2 p, Vector2 direction)
		{
			//keep going until we are off the screen.
			bool going = true;
			Vector2 currentDirection = direction;
			Random r = new Random ();
			int length = 4000;
			int xt = 0;
			int yt = 0;
			while (length-- > 0)
			{
				float xFactor = ((float)r.NextDouble () - 0.5f);
				float yFactor = ((float)r.NextDouble () - 0.5f);
				
				if ( r.Next(8) == 0 )
					currentDirection = Approximate (currentDirection, direction, 0.2f);
				else
					currentDirection = Vector2.Add (currentDirection, new Vector2 (xFactor,yFactor));			
				currentDirection.Normalize ();
				
				for (int i = 0; i < 5; i++)
				{
					p = Vector2.Add(p,currentDirection);
					if (!(p.X > w || p.Y > h || p.X < 0 || p.Y < 0))
						b.SetPixel((int)p.X,(int)p.Y,Color.White);
				}
			}
		}
		
		public Vector2 Approximate (Vector2 v, Vector2 target, float dist)
		{
			if (v.X < target.X) v.X += dist;
			else v.X -= dist;
			
			if (v.Y < target.Y) v.Y += dist;
			else v.Y -= dist;
			
			return v;
		}
	}
}

