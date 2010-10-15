using System;
using System.Linq;
using System.Drawing;
using OpenTK;
using System.Collections.Generic;
using EmergeTk.Model;
using SuperFunCon;

namespace terrain
{
	public class Genner
	{
		int w, h;
		Bitmap b;
		Graphics g;
		int seed = (int)DateTime.Now.Ticks;
		Random r; 
		Color blue = Color.FromArgb (0, 0, 255);
		Color river = Color.FromArgb (25, 255, 255);
		Color green = Color.FromArgb (0, 200, 0);
		Color darkGreen = Color.FromArgb (0, 100, 0);
		Color white = Color.FromArgb (255, 255, 255);
		Color town = Color.FromArgb (255, 255, 0);
		Color[] colorsToPreserve;
		Vector2 min, max;
		List<Triple<Vector2>> rivers = new List<Triple<Vector2>> ();
		Point[] directions = new Point[] { 
			new Point (-1,-1), new Point (-1,0), new Point (-1,1), 
			new Point (0,-1), /*new Point(0,0),*/ new Point (0,1), 
			new Point (1,-1), new Point (1,0), new Point (1,1) };
		Dictionary<int,List<Point>> riverPaths =new Dictionary<int, List<Point>>();
		int[,] riverMap = null;
		Hex[,] hexMap = null;

		public Bitmap Bitmap {
			get {
				return this.b;
			}
			set {
				b = value;
			}
		}

		public int Seed {
			get {
				return this.seed;
			}
			set {
				seed = value;
			}
		}

		public Genner (int w,int h)
		{
			this.w = w; 
			this.h = h;
			colorsToPreserve = new Color[]{white, river};
			min = new Vector2 (0,0);
			max = new Vector2 (w - 1,h - 1);
			b = new Bitmap (w,h);
			riverMap = (int[,])Array.CreateInstance(typeof(int), w, h);
			hexMap = (Hex[,])Array.CreateInstance(typeof(Hex), w, h);
		}
		
		public void Setup ()
		{
			Console.WriteLine ("Random seed: " + seed.ToString ());
			r = new Random (seed);			
			g = Graphics.FromImage (b);
			for (int x = 0; x < w; x++) {
				for (int y = 0; y < h; y++) {
					b.SetPixel (x, y, blue);
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
			
			Vector2 v = new Vector2 (x2 - x1, y2 - y1);
			Vector2 perpRight = Vector2.Normalize (v.PerpendicularRight);
			Vector2 perpLeft = Vector2.Normalize (v.PerpendicularLeft);
			
			while (x1 != x2 && y1 != y2) {
				//75% chance for each axis moving closer to 2s
				double d = r.NextDouble ();
				x1 += (x2 > x1 ? 1 : -1) * (d > 0.45d ? 1 : -1);
				d = r.NextDouble ();
				y1 += (y2 > y1 ? 1 : -1) * (d > 0.45d ? 1 : -1);
				x1 = Clamp (0, x1, w - 5);
				y1 = Clamp (0, y1, h - 5);
				//Console.WriteLine ("{0},{1} -> {2}.{3}", x1, y1, x2, y2);
				b.SetPixel (x1, y1, white);
				
				d = r.NextDouble ();
				if (d < 0.1d) {
					DrawLand (x1, y1, r.Next (200, 600), green);
				}
				
				//1 / 200 chance of left river
				//1 / 200 chance of right river
				d = r.NextDouble ();
				if (d < 0.04)
				{
					rivers.Add (new Triple<Vector2> () {
						X = new Vector2 (x1, y1),
						Y = perpLeft,
						Z = perpRight
					});
				}
			}
		}	
		
		public void SmoothMountains ()
		{
			for (int x = 0; x < w; x++) {
				for (int y = 0; y < h; y++) {
					Color myColor = b.GetPixel(x,y);
					if (myColor != white && NumPixelsIn4DirectionsAreColor(x,y,white) == 4)
					{
						b.SetPixel(x,y,white);
					}
					else if (r.Next(10) == 0)
						b.SetPixel(x,y,green);
				}
			}
		}

		public int Clamp (int min, int a, int max)
		{
			if (a <= min)
				a = min; else if (a >= max)
				a = max;
			return a;
		}

		public void DrawRivers ()
		{
			Console.WriteLine ("Drawing Rivers");
			foreach (var triple in rivers)
				DrawRiver (triple.X, triple.Y, triple.Z);
		}
		
		int HowFarUntil (Vector2 start, Vector2 direction, Color color)
		{
			for (int i = 0; i < h*2; i++)
			{
				start = Vector2.Add (start, direction);
				if (start.X < 0 || start.X >= w || start.Y < 0 || start.Y >= h)
					return -1;
				if (b.GetPixel ((int)start.X, (int)start.Y) == color)
					return i;
			}
			return -1;
		}
		
		int riverId = 0;
		void DrawRiver (Vector2 p, Vector2 left, Vector2 right)
		{
			int rDist = HowFarUntil (p, right, blue);
			int lDist = HowFarUntil (p, left, blue);
			Vector2 direction = rDist > lDist && r.Next (3) != 0 ? left : right;
			riverId++;
			Vector2 currentDirection = direction;
			List<Point> points = new List<Point> ();
			riverPaths[riverId] = points;
			bool foundSea = false;
			while (!foundSea) {
				float xFactor = ((float)r.NextDouble () - 0.5f);
				float yFactor = ((float)r.NextDouble () - 0.5f);
				
				if (r.Next (8) == 0)
					currentDirection = Approximate (currentDirection, direction, 0.2f); else
				currentDirection = Vector2.Add (currentDirection, new Vector2 (xFactor,yFactor));			
				currentDirection.Normalize ();
				
				for (int i = 0; i < 1; i++) {
					p = Vector2.Clamp (Vector2.Add (p, currentDirection), min, max);
					Point ip = new Point((int)p.X, (int)p.Y);
					Color colorAt = b.GetPixel (ip.X, ip.Y);
					if (points.Contains (ip) || colorAt == white ) {
						break;	//try to prevent loopbacks.
					}
					bool usingFillPoint = false;
					Point fillPoint = new Point(-10,-10);
					if (points.Count > 0)
					{						
						var lastP = points[points.Count-1];
						if (lastP.X != ip.X && lastP.Y != ip.Y)
						{
							fillPoint = r.Next(2) == 0 ? new Point(lastP.X, ip.Y) : new Point(ip.X, lastP.Y);
							Console.WriteLine ("filling ? - last: {0}, me: {1}, fill: {2}",
									lastP, ip, fillPoint);
							if (! points.Contains (fillPoint))
							{
								Console.WriteLine ("filled");
								usingFillPoint = true;
							}
						}
					}
					if (usingFillPoint && riverMap[fillPoint.X,fillPoint.Y] > 0)
					{
						addRiverPoint (fillPoint, points, riverId, "fill");
						addRiverPoint (ip, points, riverId, "ip");
						foundSea = true;
						break;
					}
					else if (colorAt == blue || riverMap[ip.X, ip.Y] > 0) {
						foundSea = true;
						if (usingFillPoint)
							addRiverPoint (fillPoint, points, riverId, "fill");
						addRiverPoint (ip, points, riverId, "ip");
						break;
					} 
					else
					{
						Point sea;
						if (usingFillPoint && FirstPixelInSquareOfColor (fillPoint.X-1, fillPoint.Y-1, 3, blue, out sea))
						{
							addRiverPoint (fillPoint, points, riverId, "fill");
							addRiverPoint (ip, points, riverId, "ip");
							//get the square that is the blue
							if (NeedFillPoint (ip, sea, out fillPoint))
								addRiverPoint (fillPoint, points, riverId, "seaFill");
							addRiverPoint (sea, points, riverId, "sea");
							foundSea = true;
							break;
						}
						if (FirstPixelInSquareOfColor(ip.X-1, ip.Y-1, 3, blue, out sea))
						{
							//found the sea next door.  let's connect.
							if (usingFillPoint)
								addRiverPoint (fillPoint, points, riverId, "fill");
							addRiverPoint (ip, points, riverId, "ip");
							//get the square that is the blue
							if (NeedFillPoint (ip, sea, out fillPoint))
								addRiverPoint (fillPoint, points, riverId, "seaFill");
							addRiverPoint (sea, points, riverId, "sea");
							foundSea = true;
							break;
						}
						if (usingFillPoint)
							addRiverPoint (fillPoint, points, riverId, "fill");
						addRiverPoint (ip, points, riverId, "ip");
					}
				}
			}
		}
		
		bool NeedFillPoint (Point a, Point b, out Point c)
		{
			c = default(Point);
			if (a.X != b.X & a.Y != b.Y)
			{
				c = r.Next(2) == 0 ? new Point(a.X, b.Y) : new Point(a.X, b.Y);
				return true;
			}
			return false;
		}
		
		void addRiverPoint (Point p, List<Point> list, int riverId, string descr)
		{
			Console.WriteLine ("adding point {0}, river: {1} -- {2}", p, riverId, descr);
			if (! list.Contains (p))
			{
				list.Add (p);
				riverMap[p.X,p.Y] = riverId;
			}
			else
				Console.WriteLine ("already added!");
		}
		
		public void DrawTowns ()
		{
			Console.WriteLine ("Drawing towns");
			for (int x = 0; x < w; x++) {
				for (int y = 0; y < h; y++) {
					//if square is green, at least 1 river tile adj. and no towns within townSpacing, and 1/3 rand
					Color c = b.GetPixel (x, y);
					float baseProbability = 0.0f;
					if (c == green)
					{
						if (riverMap[x,y] > 0 )
							continue;
						int numTownsFar = NumPixelsInSquareAreColor (x - 8, y - 8, 17, town);
						int numTownsNear = NumPixelsInSquareAreColor (x - 3, y - 3, 7, town);
						if (numTownsFar > 0) 
							baseProbability -= 0.4f;
						if (numTownsNear > 0) 
							baseProbability -= 1.3f;
						//if we are near a river, that is good
						if (NumRiverWithin (x-1, y-1, 3) > 0)
							baseProbability += 0.3f;
						//next to ocean is nice too
						if (NumPixelsInSquareAreColor (x - 1, y - 1, 3, blue) > 0 )
							baseProbability += 0.3f;
						//nice to have some forest neaby as well.
						if (NumPixelsInSquareAreColor (x - 1, y - 1, 3, darkGreen) > 0)
							baseProbability += 0.1f;
						
						if (r.NextDouble () < baseProbability)
							b.SetPixel (x, y, town);
					}
				}
			}
		}

		public Vector2 Approximate (Vector2 v, Vector2 target, float dist)
		{
			if (v.X < target.X)
				v.X += dist; else
				v.X -= dist;
			
			if (v.Y < target.Y)
				v.Y += dist; else
				v.Y -= dist;
			
			return v;
		}

		public void DrawLand ()
		{
			int cx = r.Next (w);
			int cy = r.Next (h);
			int numTimes = r.Next (1000, 5000);
			DrawLand (cx, cy, numTimes, green);
		}

		public void DrawLand (int cx, int cy, int numTimes, Color land)
		{			
			while (numTimes-- > 0) {
				double going = r.NextDouble ();
				if (going > 0.75d && cx > 0) //left
					cx--; else if (going > 0.5d && cx < w - 1) //right
					cx++; else if (going > 0.25d && cy > 0)//up
					cy--; else if (cy < h - 1)
					cy++;
				//Console.WriteLine ("setting pixel at {0},{1}", cx, cy);
				try {
					//g.FillRectangle (Brushes.Green, cx, cy, cx+1, cy+1);
					//Console.WriteLine ( b.GetPixel (cx,cy) );
					if (!colorsToPreserve.Contains (b.GetPixel (cx, cy)))
						b.SetPixel (cx, cy, land);
				} catch (Exception e) {
					Console.WriteLine ("ERROR: {0},{1},{2}", cx, cy, going);
					throw e;
				}
			}
		}

		public void DrawForest ()
		{
			Console.WriteLine ("Drawing forest");
			//first pass.
			for (int x = 0; x < w; x++) {
				for (int y = 0; y < h; y++) {
					if (NumPixelsInSquareAreColor (x - 3, y - 3, 6, green) > 20 && r.Next (50) == 0 && ! colorsToPreserve.Contains (b.GetPixel (x, y)))
						DrawLand (x, y, 20, darkGreen);
				}
			}
			//smooth.
			Console.WriteLine ("Smoothing forest");
			for (int x = 0; x < w; x++) {
				for (int y = 0; y < h; y++) {
					Color c = b.GetPixel (x, y);
					if (c != white && NumPixelsInSquareAreColor (x - 3, y - 3, 6, darkGreen) > 16 && c != blue)
						b.SetPixel (x, y, darkGreen);
				}
			}
		}

		public void Cleanup ()
		{
			Console.WriteLine ("Smoothing");
			//if every pixel around me is green, i should be green.  if every pixel around me is blue,same.
			//first do green.
			for (int x = 0; x < w; x++) {
				for (int y = 0; y < h; y++) {
					Color c = b.GetPixel (x, y);
					if (c == white)
						EnsureAllPixelsAroundAre (x, y, green); else if (colorsToPreserve.Contains (c))
						continue; else if (NumPixelsInSquareAreColor (x - 3, y - 3, 6, green) > 23)
						b.SetPixel (x, y, green); else if (NumPixelsInSquareAreColor (x - 3, y - 3, 6, blue) > 23)
						b.SetPixel (x, y, blue);
				}
			}
		}

		public void EnsureAllPixelsAroundAre (int x, int y, Color c)
		{
			foreach (Point p in directions) {
				int x2 = Clamp (0, x + p.X, w - 1);
				int y2 = Clamp (0, y + p.Y, h - 1);
				Color cb = b.GetPixel (x2, y2);
				if (cb != c && ! colorsToPreserve.Contains (cb))
					b.SetPixel (x2, y2, c);
			}
		}

		public int NumPixelsInSquareAreColor (int x1, int y1, int size, Color ca)
		{
			int count = 0;
			if (x1 < 0)
				x1 = 0;
			if (y1 < 0)
				y1 = 0;
			for (int x = x1; x < x1+size && x < w; x++) {				
				for (int y = y1; y < y1+size && y < h; y++) {
					Color cb = b.GetPixel (x, y);
					if (ca == cb)
						count++;
				}
			}
			return count;			
		}
		
		public bool FirstPixelInSquareOfColor (int x1, int y1, int size, Color ca, out Point result)
		{
			int count = 0;
			if (x1 < 0)
				x1 = 0;
			if (y1 < 0)
				y1 = 0;
			for (int x = x1; x < x1+size && x < w; x++) {				
				for (int y = y1; y < y1+size && y < h; y++) {
					Color cb = b.GetPixel (x, y);
					if (ca == cb)
					{
						result = new Point (x, y);
						return true;
					}
				}
			}
			result = new Point(-1,-1);
			return false;			
		}
		
		public int NumRiverWithin (int x1, int y1, int size)
		{
			int count = 0;
			if (x1 < 0)
				x1 = 0;
			if (y1 < 0)
				y1 = 0;
			for (int x = x1; x < x1+size && x < w; x++) {				
				for (int y = y1; y < y1+size && y < h; y++) {
					if (riverMap[x,y] > 0)
						count++;
				}
			}
			return count;			
		}
		
		public int NumPixelsIn4DirectionsAreColor(int x, int y, Color c)
		{
			int count = 0;
			if (y > 0 && b.GetPixel(x, y-1) == c) count++;
			if (y < h-1 && b.GetPixel(x, y+1) == c) count++;
			if (x > 0 && b.GetPixel(x-1, y) == c) count++;
			if (x < w-1 && b.GetPixel(x+1, y) == c) count++;
			return count;
		}

		public void SaveToDb ()
		{
			Console.WriteLine ("Saving to db");
			RecordList<Hex> hexes = new RecordList<Hex> ();
			for (int x = 0; x < w; x++) {
				for (int y = 0; y < h; y++) {
					Hex hex = new Hex ();
					hex.EnsureId ();
					hex.X = x;
					hex.Y = y;
					Color c = b.GetPixel (x, y);
					if (c == blue)
						hex.Terrain = TerrainType.Sea; 
					else if (c == white)
						hex.Terrain = TerrainType.Mountains;
					else if (c == darkGreen)
						hex.Terrain = TerrainType.Forest; 
					else if (c == town)
					{
						hex.Terrain = TerrainType.Plains;
						AddTown (hex);
					}
					else
						hex.Terrain = TerrainType.Plains;
					hexes.Add (hex);
					hexMap[x,y] = hex;
				}
			}
			
			//check coastals
			foreach (Hex h in hexes)
				if (h.Terrain == TerrainType.Sea)
					CheckCoastal (h);
			
			cities.Save ();
			hexes.Save ();
			foreach (var kvp in riverPaths)
			{
				River r = new River();
				int z = 0;
				foreach(Point p in kvp.Value)
				{
					r.Path.Add (new Triple<int> () {
						X = p.X,
						Y = p.Y,
						Z = z++
					});
				}
				r.Save ();
				r.Path.Save ();
				r.SaveRelations("Path");
			}
		}
		
		public void DrawFinal (string fileName)
		{
			//draw rivers in.
			foreach (var kvp in riverPaths)
			{
				foreach (var p in kvp.Value)
					b.SetPixel(p.X, p.Y, river);
			}
			
			//save file.
			b.Save (fileName);
		}
		
		IRecordList<City> cities = new RecordList<City>();
		public void AddTown(Hex h)
		{
			int size = (int)Math.Log(r.Next(600));
			string name = string.Format("Town at {0},{1}", h.X, h.Y);
			City c = new City () {
				Size = size,
				Name = name,
				Location = h
			};
			cities.Add (c);
			h.City = c;			
		}
		
		public void CheckCoastal (Hex hex )
		{
			List<string> land = new List<string>();
			foreach(string dir in Hex.Directions)
			{
				Point p = Hex.GetNeighborPoint(dir,hex.X,hex.Y);
				if(p.X >= 0 && p.X < w && p.Y >= 0 && p.Y < h)
				{
					Color c = b.GetPixel(p.X,p.Y);
					if (c != blue)
					{
						land.Add(dir);
					}
				}
			}
			if (land.Count > 0)
				hex.ExtraInfo = Coastal.PickTiles (land);
		}
	}
}

