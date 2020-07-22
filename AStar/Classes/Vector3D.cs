using System;

namespace AStar
{
	public class Vector3D
	{
		public Vector3D(double[] Coordinates)
		{
			if (Coordinates == null)
			{
				throw new ArgumentNullException();
			}
			if (Coordinates.Length != 3)
			{
				throw new ArgumentException("The Coordinates' array must contain exactly 3 elements.");
			}
			this.DX = Coordinates[0];
			this.DY = Coordinates[1];
			this.DZ = Coordinates[2];
		}

		public Vector3D(double DeltaX, double DeltaY, double DeltaZ)
		{
			this.DX = DeltaX;
			this.DY = DeltaY;
			this.DZ = DeltaZ;
		}

		public Vector3D(Point3D P1, Point3D P2)
		{
			this.DX = P2.X - P1.X;
			this.DY = P2.Y - P1.Y;
			this.DZ = P2.Z - P1.Z;
		}

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

		public double DX
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

		public double DY
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

		public double DZ
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

		public static Vector3D operator *(Vector3D V, double Factor)
		{
			double[] array = new double[3];
			for (int i = 0; i < 3; i++)
			{
				array[i] = V[i] * Factor;
			}
			return new Vector3D(array);
		}

		public static Vector3D operator /(Vector3D V, double Divider)
		{
			if (Divider == 0.0)
			{
				throw new ArgumentException("Divider cannot be 0 !\n");
			}
			double[] array = new double[3];
			for (int i = 0; i < 3; i++)
			{
				array[i] = V[i] / Divider;
			}
			return new Vector3D(array);
		}

		public double SquareNorm
		{
			get
			{
				double num = 0.0;
				for (int i = 0; i < 3; i++)
				{
					num += this._Coordinates[i] * this._Coordinates[i];
				}
				return num;
			}
		}

		public double Norm
		{
			get
			{
				return Math.Sqrt(this.SquareNorm);
			}
			set
			{
				double norm = this.Norm;
				if (norm == 0.0)
				{
					throw new InvalidOperationException("Cannot set norm for a nul vector !");
				}
				if (norm != value)
				{
					double num = value / norm;
					for (int i = 0; i < 3; i++)
					{
						int coordinateIndex;
						this[coordinateIndex = i] = this[coordinateIndex] * num;
					}
				}
			}
		}

		public static double operator |(Vector3D V1, Vector3D V2)
		{
			double num = 0.0;
			for (int i = 0; i < 3; i++)
			{
				num += V1[i] * V2[i];
			}
			return num;
		}

		public static Point3D operator +(Point3D P, Vector3D V)
		{
			double[] array = new double[3];
			for (int i = 0; i < 3; i++)
			{
				array[i] = P[i] + V[i];
			}
			return new Point3D(array);
		}

		private double[] _Coordinates = new double[3];
	}
}
