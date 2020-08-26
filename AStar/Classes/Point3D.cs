using System;

namespace AStar
{
	[Serializable]
	public class Point3D
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
            _isValid = !(_Coordinates[0] == 0 && _Coordinates[1] == 0 && _Coordinates[2] == 0);
        }

        public Point3D(double CoordinateX, double CoordinateY, double CoordinateZ)
		{
            _Coordinates[0] = CoordinateX;
			_Coordinates[1] = CoordinateY;
			_Coordinates[2] = CoordinateZ;
            _isValid = !(_Coordinates[0] == 0 && _Coordinates[1] == 0 && _Coordinates[2] == 0);
        }

        public double this[int CoordinateIndex]
        {
            get => _Coordinates[CoordinateIndex];
            set
            {
                _Coordinates[CoordinateIndex] = value;
                _isValid = !(_Coordinates[0] == 0 && _Coordinates[1] == 0 && _Coordinates[2] == 0);
            }
        }

        public double X
        {
            get => _Coordinates[0];
            set
            {
                _Coordinates[0] = value;
                _isValid = !(_Coordinates[0] == 0 && _Coordinates[1] == 0 && _Coordinates[2] == 0);
            }
        }

        public double Y
        {
            get => _Coordinates[1];
            set
            {
                _Coordinates[1] = value;
                _isValid = !(_Coordinates[0] == 0 && _Coordinates[1] == 0 && _Coordinates[2] == 0);
            }
        }

        public double Z
        {
            get => _Coordinates[2];
            set
            {
                _Coordinates[2] = value;
                _isValid = !(_Coordinates[0] == 0 && _Coordinates[1] == 0 && _Coordinates[2] == 0);
            }
        }

        public bool IsValid
        {
            get => _isValid;
        }
        bool _isValid = false;

		public static double DistanceBetween(Point3D P1, Point3D P2)
		{
			return Math.Sqrt((P1._Coordinates[0] - P2._Coordinates[0]) * (P1._Coordinates[0] - P2._Coordinates[0]) 
                + (P1._Coordinates[1] - P2._Coordinates[1]) * (P1._Coordinates[1] - P2._Coordinates[1]));
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
#if false
            Point3D point3D = (Point3D)Point;
            if (Point is null)
                throw new ArgumentException("Object must be of type " + base.GetType());
            bool flag = true;
			for (int i = 0; i < 3; i++)
				flag &= point3D[i].Equals(this[i]);

            return flag;
#else
            if (_isValid 
                && Point is Point3D point3D 
                && point3D._isValid)
                return Math.Abs(point3D._Coordinates[0] - _Coordinates[0]) < 2 * double.Epsilon
                       && Math.Abs(point3D._Coordinates[1] - _Coordinates[1]) < 2 * double.Epsilon
                       && Math.Abs(point3D._Coordinates[2] - _Coordinates[2]) < 2 * double.Epsilon;
            return false;
#endif
        }
        public bool Equals(Point3D point3D)
        {
            if(_isValid 
                && point3D != null 
                && point3D._isValid)
                return Math.Abs(point3D._Coordinates[0] - _Coordinates[0]) < 2 * double.Epsilon
                       && Math.Abs(point3D._Coordinates[1] - _Coordinates[1]) < 2 * double.Epsilon
                       && Math.Abs(point3D._Coordinates[2] - _Coordinates[2]) < 2 * double.Epsilon;
            return false;
        }

        public override int GetHashCode()
		{
			double num = 0.0;
#if false
            for (int i = 0; i < 3; i++)
                num += this[i]; 
#else
            num = _Coordinates[0] + _Coordinates[1] + _Coordinates[2];
#endif

            return (int)num;
		}

		public override string ToString()
        {

            if (_Coordinates.Length == 3)
                //return $"{{{_Coordinates[0]:N2}; {_Coordinates[1]:N2}; {_Coordinates[2]:N2}}}";
                return string.Concat('{', _Coordinates[0].ToString("N2"), "; ", _Coordinates[1].ToString("N2"), "; " ,
                    _Coordinates[2].ToString("N2"), '}');

			char text2 = ';';
			char text3 = '}';
			string result = "{";
			int num = 3;
			for (int i = 0; i < num; i++)
				result = result + _Coordinates[i].ToString("N2") + ((i != num - 1) ? text2 : text3);

            return result;
        }

		private double[] _Coordinates = new double[3];
	}
}
