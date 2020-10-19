using MyNW.Classes;
using System;
using System.Text;

namespace AStar
{
	[Serializable]
	public class Point3D : IEquatable<Point3D>
    {
        public Point3D(double[] Coordinates)
		{
			if (Coordinates is null)
				throw new ArgumentNullException();

            if (Coordinates.Length != 3)
				throw new ArgumentException("The Coordinates' array must contain exactly 3 elements.");

            _Coordinates[0] = Coordinates[0];
            _Coordinates[1] = Coordinates[1];
            _Coordinates[2] = Coordinates[2];
#if _isValid
            _isValid = !(_Coordinates[0] == 0 && _Coordinates[1] == 0 && _Coordinates[2] == 0); 
#endif
        }

        public Point3D(double CoordinateX, double CoordinateY, double CoordinateZ)
		{
            _Coordinates[0] = CoordinateX;
			_Coordinates[1] = CoordinateY;
			_Coordinates[2] = CoordinateZ;
#if _isValid
            _isValid = !(_Coordinates[0] == 0 && _Coordinates[1] == 0 && _Coordinates[2] == 0); 
#endif
        }

        public double this[int CoordinateIndex]
        {
            get => _Coordinates[CoordinateIndex];
            set
            {
                _Coordinates[CoordinateIndex] = value;
#if IsValid
                _isValid = !(_Coordinates[0] == 0 && _Coordinates[1] == 0 && _Coordinates[2] == 0); 
#endif
            }
        }

        public double X
        {
            get => _Coordinates[0];
            set
            {
                _Coordinates[0] = value;
#if IsValid
                _isValid = !(_Coordinates[0] == 0 && _Coordinates[1] == 0 && _Coordinates[2] == 0); 
#endif
            }
        }

        public double Y
        {
            get => _Coordinates[1];
            set
            {
                _Coordinates[1] = value;
#if IsValid
                _isValid = !(_Coordinates[0] == 0 && _Coordinates[1] == 0 && _Coordinates[2] == 0); 
#endif
            }
        }

        public double Z
        {
            get => _Coordinates[2];
            set
            {
                _Coordinates[2] = value;
#if IsValid
                _isValid = !(_Coordinates[0] == 0 && _Coordinates[1] == 0 && _Coordinates[2] == 0); 
#endif
            }
        }

#if IsValid
        public bool IsValid
        {
            get => _isValid;
        }
        bool _isValid = true;
#endif
        /// <summary>
        /// Совпадает с началом координат
        /// </summary>
        public bool IsOrigin
        {
            get => _Coordinates[0] == 0 && _Coordinates[1] == 0 && _Coordinates[2] == 0;
        }

        /// <summary>
        /// Евклидово расстояние между точками
        /// </summary>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <returns></returns>
        public static double DistanceBetween(Point3D P1, Point3D P2)
		{
            double dx = P1._Coordinates[0] - P2._Coordinates[0],
                   dy = P1._Coordinates[1] - P2._Coordinates[1],
                   dz = P1._Coordinates[2] - P2._Coordinates[2];

			return Math.Sqrt(dx * dx + dy * dy + dz * dz);
		}

        public static double DistanceBetween(double x1, double y1, double z1,
                                             double x2, double y2, double z2)
        {
            double dx = x1 - x2,
                   dy = y1 - y2,
                   dz = z1 - z2;

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        public static double SquaredDistanceBetween(Point3D P1, Point3D P2)
        {
            double dx = P1.X - P2.X,
                   dy = P1.Y - P2.Y,
                   dz = P1.Z - P2.Z;

            return dx * dx + dy * dy + dz * dz;
        }
        public static double SquaredDistanceBetween(double x1, double y1, double z1,
                                             double x2, double y2, double z2)
        {
            double dx = x1 - x2,
                   dy = y1 - y2,
                   dz = z1 - z2;

            return dx * dx + dy * dy + dz * dz;
        }

        public static Point3D ProjectOnLine(Point3D Pt, Point3D P1, Point3D P2)
		{
			if (Pt is null || P1 is null || P2 is null)
				throw new ArgumentNullException("None of the arguments can be null.");

            if (P1.Equals(P2))
				throw new ArgumentException("P1 and P2 must be different.");
			
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

		public override bool Equals(object Point)
		{
#if Original_AStar
            Point3D point3D = (Point3D)Point;
            if (Point is null)
                throw new ArgumentException("Object must be of type " + base.GetType());
            bool flag = true;
			for (int i = 0; i < 3; i++)
				flag &= point3D[i].Equals(this[i]);

            return flag;
#else
            if (Point is Point3D point3D)
#if false
                return Math.Abs(point3D._Coordinates[0] - _Coordinates[0]) < 2 * double.Epsilon
                               && Math.Abs(point3D._Coordinates[1] - _Coordinates[1]) < 2 * double.Epsilon
                               && Math.Abs(point3D._Coordinates[2] - _Coordinates[2]) < 2 * double.Epsilon; 
#else
                return point3D._Coordinates[0] == _Coordinates[0]
                       && point3D._Coordinates[1] == _Coordinates[1]
                       && point3D._Coordinates[2] == _Coordinates[2];
#endif
            return false;
#endif
        }

        public bool Equals(Point3D point3D)
        {
            if (point3D != null)
#if false
		        return Math.Abs(point3D._Coordinates[0] - _Coordinates[0]) < 2 * double.Epsilon
                       && Math.Abs(point3D._Coordinates[1] - _Coordinates[1]) < 2 * double.Epsilon
                       && Math.Abs(point3D._Coordinates[2] - _Coordinates[2]) < 2 * double.Epsilon;  
#else
                return point3D._Coordinates[0] == _Coordinates[0]
                       && point3D._Coordinates[1] == _Coordinates[1]
                       && point3D._Coordinates[2] == _Coordinates[2];
#endif
            return false;
        } 

        public override int GetHashCode()
		{
			double num = 0.0;
            num = _Coordinates[0] + _Coordinates[1] + _Coordinates[2];
            return (int)num;
		}

		public override string ToString()
        {
            if (_Coordinates.Length == 3)
                return string.Concat('{', _Coordinates[0].ToString("N2"), "; ", _Coordinates[1].ToString("N2"), "; " ,
                    _Coordinates[2].ToString("N2"), '}');

            StringBuilder sb = new StringBuilder('{');

            for (int i = 0; i < _Coordinates.Length - 1; i++)
            {
                sb.Append(_Coordinates[i].ToString("N2")).Append("; ");
            }
            sb.Append(_Coordinates[_Coordinates.Length - 1].ToString("N2")).Append('}');

            return sb.ToString();
        }

		private double[] _Coordinates = new double[3];

        public static implicit operator Vector3(Point3D point)
        {
            return new Vector3((float)point._Coordinates[0], (float)point._Coordinates[1], (float)point._Coordinates[2]);
        }
    }
}
