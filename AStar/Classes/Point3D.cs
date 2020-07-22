using System;

namespace AStar
{
	// Token: 0x0200000B RID: 11
	[Serializable]
	public class Point3D
	{
		// Token: 0x0600009C RID: 156 RVA: 0x000041E8 File Offset: 0x000023E8
		public Point3D(double[] Coordinates)
		{
			if (Coordinates == null)
			{
				throw new ArgumentNullException();
			}
			if (Coordinates.Length != 3)
			{
				throw new ArgumentException("The Coordinates' array must contain exactly 3 elements.");
			}
			this.X = Coordinates[0];
			this.Y = Coordinates[1];
			this.Z = Coordinates[2];
		}

		// Token: 0x0600009D RID: 157 RVA: 0x0000423C File Offset: 0x0000243C
		public Point3D(double CoordinateX, double CoordinateY, double CoordinateZ)
		{
			this.X = CoordinateX;
			this.Y = CoordinateY;
			this.Z = CoordinateZ;
		}

		// Token: 0x17000033 RID: 51
		public double this[int CoordinateIndex]
		{
			get
			{
				return this._Coordinates[CoordinateIndex];
			}
			set
			{
				this._Coordinates[CoordinateIndex] = value;
			}
		}

		// Token: 0x17000034 RID: 52
		// (get) Token: 0x060000A1 RID: 161 RVA: 0x00004285 File Offset: 0x00002485
		// (set) Token: 0x060000A0 RID: 160 RVA: 0x0000427A File Offset: 0x0000247A
		public double X
		{
			get
			{
				return this._Coordinates[0];
			}
			set
			{
				this._Coordinates[0] = value;
			}
		}

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x060000A3 RID: 163 RVA: 0x0000429A File Offset: 0x0000249A
		// (set) Token: 0x060000A2 RID: 162 RVA: 0x0000428F File Offset: 0x0000248F
		public double Y
		{
			get
			{
				return this._Coordinates[1];
			}
			set
			{
				this._Coordinates[1] = value;
			}
		}

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x060000A5 RID: 165 RVA: 0x000042AF File Offset: 0x000024AF
		// (set) Token: 0x060000A4 RID: 164 RVA: 0x000042A4 File Offset: 0x000024A4
		public double Z
		{
			get
			{
				return this._Coordinates[2];
			}
			set
			{
				this._Coordinates[2] = value;
			}
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x000042B9 File Offset: 0x000024B9
		public static double DistanceBetween(Point3D P1, Point3D P2)
		{
			return Math.Sqrt((P1.X - P2.X) * (P1.X - P2.X) + (P1.Y - P2.Y) * (P1.Y - P2.Y));
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x000042F8 File Offset: 0x000024F8
		public static Point3D ProjectOnLine(Point3D Pt, Point3D P1, Point3D P2)
		{
			if (Pt == null || P1 == null || P2 == null)
			{
				throw new ArgumentNullException("None of the arguments can be null.");
			}
			if (P1.Equals(P2))
			{
				throw new ArgumentException("P1 and P2 must be different.");
			}
			Vector3D vector3D = new Vector3D(P1, P2);
			Vector3D v = new Vector3D(P1, Pt);
			Vector3D v2 = vector3D * (vector3D | v) / vector3D.SquareNorm;
			Point3D point3D = P1 + v2;
			Vector3D v3 = new Vector3D(P1, point3D);
			double num = v3 | vector3D;
			if (num < 0.0)
			{
				return P1;
			}
			Vector3D v4 = new Vector3D(P2, point3D);
			double num2 = v4 | vector3D;
			if (num2 > 0.0)
			{
				return P2;
			}
			return point3D;
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x000043A4 File Offset: 0x000025A4
		public override bool Equals(object Point)
		{
			Point3D point3D = (Point3D)Point;
			if (point3D == null)
			{
				throw new ArgumentException("Object must be of type " + base.GetType());
			}
			bool flag = true;
			for (int i = 0; i < 3; i++)
			{
				flag &= point3D[i].Equals(this[i]);
			}
			return flag;
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x000043FC File Offset: 0x000025FC
		public override int GetHashCode()
		{
			double num = 0.0;
			for (int i = 0; i < 3; i++)
			{
				num += this[i];
			}
			return (int)num;
		}

		// Token: 0x060000AA RID: 170 RVA: 0x0000442C File Offset: 0x0000262C
		public override string ToString()
		{
			string text = "{";
			string text2 = ";";
			string text3 = "}";
			string text4 = text;
			int num = 3;
			for (int i = 0; i < num; i++)
			{
				text4 = text4 + this._Coordinates[i].ToString() + ((i != num - 1) ? text2 : text3);
			}
			return text4;
		}

		// Token: 0x0400001B RID: 27
		private double[] _Coordinates = new double[3];
	}
}
