using System;
using System.Linq;
using System.Drawing;
using OpenTK;
using System.Collections.Generic;

namespace terrain
{
	public class Genner
	{
		int w,h;
		Bitmap b;
		Graphics g;
		int seed = (int)DateTime.Now.Ticks;
		Random r; 
		Color blue = Color.FromArgb(0,0,255);
		Color river = Color.FromArgb(25,255,255);
		Color green = Color.FromArgb(0,200,0);
		Color darkGreen = Color.FromArgb(0,100,0);
		Color white = Color.FromArgb(255,255,255);
		Color town = Color.FromArgb(255,255,0);
		Color[] colorsToPreserve;
		Vector2 min, max;
		
		List<KeyValuePair<Vector2, Vector2>> rivers = new List<KeyValuePair<Vector2, Vector2>>();
		
		Point[] directions = new Point[] { 
			new Point(-1,-1), new Point(-1,0), new Point(-1,1),
			new Point(0,-1), /*new Point(0,0),*/ new Point(0,1),
			new Point(1,-1), new Point(1,0), new Point(1,1) };
		
		public Bitmap Bitmap {
			get {
				return this.b;
			}
			set {
				b = value;
			}
		}

		public Genner (int w, int h)
		{
			Console.WriteLine ("Random seed: " + seed.ToString() );
			r = new Random(seed);
			colorsToPreserve = new Color[]{white, river};
			this.w = w; 
			this.h = h;
			min = new Vector2(0,0);
			max = new Vector2(w-1,h-1);
			b = new Bitmap(w,h);
			g = Graphics.FromImage (b);
			for (int x = 0; x < w; x++)
			{
				for (int y = 0; y < h; y++)
				{
					b.SetPixel (x,y,blue);
				}
			}
		}
		
		public void DrawSpine ()
		{
			Console.WriteLine ("Drawing spine");
			int x1 = r.Next (50, w - 50);
			int y1 = r.Next (50, h - 50);
			
			int x2 = r.Next (x1 - 150, x1 + 150);
			int y2 = r.Next (y1 - 150, y1 + 150);
			
			Vector2 v = new Vector2(x2-x1, y2-y1);
			Vector2 perpRight = Vector2.Normalize (v.PerpendicularRight);
			Vector2 perpLeft = Vector2.Normalize(v.PerpendicularLeft);
			
			while (x1 != x2 && y1 != y2)
			{
				//75% chance for each axis moving closer to 2s
				double d = r.NextDouble ();
				x1 += (x2 > x1 ? 1 : -1) * (d > 0.45d ? 1 : -1);
				d = r.NextDouble ();
				y1 += (y2 > y1 ? 1 : -1) * (d > 0.45d ? 1 : -1);
				x1 = Clamp(0, x1, w-5);
				y1 = Clamp(0, y1, h-5);
				//Console.WriteLine ("{0},{1} -> {2}.{3}", x1, y1, x2, y2);
				b.SetPixel (x1, y1, white);
				
				d = r.NextDouble ();
				if ( d < 0.1d )
				{
					DrawLand (x1, y1, r.Next(400,1000), green);
				}
				
				//1 / 200 chance of left river
				//1 / 200 chance of right river
				d = r.NextDouble ();
				if ( d < 0.025 )
					rivers.Add ( new KeyValuePair<Vector2, Vector2>(new Vector2 (x1, y1), perpLeft));
				
				d = r.NextDouble ();
				if ( d < 0.025 )
					rivers.Add ( new KeyValuePair<Vector2, Vector2>(new Vector2 (x1, y1), perpRight));
			}
		}		
		
		public int Clamp (int min, int a, int max)
		{
			if ( a <= min ) a = min;
			else if ( a >= max ) a = max;
			return a;
		}
		
		public void DrawRivers ()
		{
			Console.WriteLine ("Drawing Rivers");
			foreach (var kvp in rivers)
				DrawRiver(kvp.Key, kvp.Value);
		}
		
		public void DrawTowns ()
		{
			Console.WriteLine ("Drawing towns");
			int townSpacing = r.Next(5,20);
			for (int x = 0; x < w; x++)
			{
				for (int y = 0; y < h; y++)
				{
					//if square is green, at least 1 river tile adj. and no towns within townSpacing, and 1/3 rand
					Color c = b.GetPixel(x,y);
					if (c == green
					    && NumPixelsInSquareAreColor(x-1,y-1, 3, river) > 0
					    && NumPixelsInSquareAreColor(x-townSpacing, y-townSpacing, townSpacing * 2, town ) == 0 
					    && r.Next(2) == 0)
					{
						b.SetPixel (x,y,town);
					}
				}
			}
		}
		
		void DrawRiver (Vector2 p, Vector2 direction)
		{
			Vector2 currentDirection = direction;
			List<Vector2> points = new List<Vector2>();
			bool foundSea = false;			
			while(!foundSea)
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
					p = Vector2.Clamp(Vector2.Add(p,currentDirection), min, max);
					if( points.Contains (p))
					{
						break;	
					}
					Color colorAt = b.GetPixel((int)p.X, (int)p.Y);
					if (colorAt == blue || colorAt == river)
					{
						foundSea = true;
						break;
					}
					else if (colorAt != white )
					{
						b.SetPixel ((int)p.X, (int)p.Y, river);
						points.Add (p);
					}
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

		public void DrawLand()
		{
			int cx = r.Next (w);
			int cy = r.Next (h);
			int numTimes = r.Next(1000,5000);
			DrawLand (cx, cy, numTimes, green);
		}
		
		public void DrawLand(int cx, int cy, int numTimes, Color land)
		{			
			while (numTimes-- > 0)
			{
				double going = r.NextDouble ();
				if (going > 0.75d && cx > 0 ) //left
					cx--;
				else if (going > 0.5d && cx < w -1 ) //right
					cx++;
				else if (going > 0.25d && cy > 0 )//up
					cy--;
				else if (cy < h -1)
					cy++;
				//Console.WriteLine ("setting pixel at {0},{1}", cx, cy);
				try
				{
					//g.FillRectangle (Brushes.Green, cx, cy, cx+1, cy+1);
					//Console.WriteLine ( b.GetPixel (cx,cy) );
					if ( !colorsToPreserve.Contains(b.GetPixel( cx,cy )) )
						b.SetPixel(cx, cy, land);
				}
				catch (Exception e)
				{
					Console.WriteLine ("ERROR: {0},{1},{2}", cx, cy,going);
					throw e;
				}
			}
		}
		
		public void DrawForest ()
		{
			Console.WriteLine ("Drawing forest");
			//first pass.
			for (int x = 0; x < w; x++)
			{
				for (int y = 0; y < h; y++)
				{
					if (NumPixelsInSquareAreColor(x-3,y-3,6, green) > 20 && r.Next (50) == 0 && ! colorsToPreserve.Contains (b.GetPixel(x,y)) )
						DrawLand(x,y,20,darkGreen);
				}
			}
			//smooth.
			Console.WriteLine ("Smoothing forest");
			for (int x = 0; x < w; x++)
			{
				for (int y = 0; y < h; y++)
				{
					Color c = b.GetPixel (x,y);
					if (c != white && NumPixelsInSquareAreColor(x-3, y-3, 6, darkGreen) > 16 && c != blue && c != river)
						b.SetPixel(x,y,darkGreen);
				}
			}
		}
		
		public void Cleanup ()
		{
			Console.WriteLine ("Smoothing");
			//if every pixel around me is green, i should be green.  if every pixel around me is blue,same.
			//first do green.
			for (int x = 0; x < w; x++)
			{
				for (int y = 0; y < h; y++)
				{
					Color c = b.GetPixel (x,y);
					if (c == white)
						EnsureAllPixelsAroundAre(x,y,green);
					else if (colorsToPreserve.Contains (c))
						continue;
					else if (NumPixelsInSquareAreColor(x-3, y-3, 6, green) > 23)
						b.SetPixel(x,y,green);
					else if(NumPixelsInSquareAreColor(x-3,y-3, 6, blue) > 23)
						b.SetPixel(x,y,blue);
				}
			}
		}
		
		public void EnsureAllPixelsAroundAre(int x, int y, Color c)
		{
			foreach (Point p in directions)
			{
				int x2 = Clamp(0,x+p.X,w-1);
				int y2 = Clamp(0,y+p.Y,h-1);
				Color cb = b.GetPixel(x2,y2);
				if (cb != c && ! colorsToPreserve.Contains (cb) )
					b.SetPixel (x2,y2,c);
			}
		}
		
		public int NumPixelsInSquareAreColor(int x1, int y1, int size, Color ca)
		{
			int count = 0;
			if (x1 < 0) x1 = 0;
			if (y1 < 0) y1 = 0;
			for (int x = x1; x < x1+size && x < w; x++)
			{				
				for (int y = y1; y < y1+size && y < h; y++)
				{
					Color cb = b.GetPixel(x,y);
					if (ca == cb) count++;
				}
			}
			return count;			
		}
	}
}

