using Phoenix.Common.SceneReplication.Data;

namespace Phoenix.Server.SceneReplication.Coordinates
{
    /// <summary>
    /// Vector4D - XYZW Coordinates
    /// </summary>
    public class Quaternion : SerializingObject
    {
        public event Action? ChangedEvent;
        private bool readOnly;

        private double x;
        private double y;
        private double z;
        private double w;

        public Quaternion()
        {
        }

        public Quaternion(double x, double y, double z, double w, bool readOnly = false)
        {
            this.readOnly = readOnly;
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        /// <summary>
        /// X-axis
        /// </summary>
        public double X
        {
            get
            {
                return x;
            }

            set
            {
                if (readOnly)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                x = value;
                ChangedEvent?.Invoke();
            }
        }

        /// <summary>
        /// Y-axis
        /// </summary>
        public double Y
        {
            get
            {
                return y;
            }

            set
            {
                if (readOnly)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                y = value;
                ChangedEvent?.Invoke();
            }
        }

        /// <summary>
        /// Z-axis
        /// </summary>
        public double Z
        {
            get
            {
                return z;
            }

            set
            {
                if (readOnly)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                z = value;
                ChangedEvent?.Invoke();
            }
        }

        /// <summary>
        /// W-axis
        /// </summary>
        public double W
        {
            get
            {
                return w;
            }

            set
            {
                if (readOnly)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                w = value;
                ChangedEvent?.Invoke();
            }
        }

        public override string ToString()
        {
            return X + ":" + Y + ":" + Z + ":" + W;
        }

        public static Quaternion operator +(Quaternion first, Quaternion other)
        {
            return new Quaternion(first.X + other.X, first.Y + other.Y, first.Z + other.Z, first.W + other.W);
        }

        public static Quaternion operator -(Quaternion first, Quaternion other)
        {
            return new Quaternion(first.X - other.X, first.Y - other.Y, first.Z - other.Z, first.W - other.W);
        }

        public static Quaternion operator /(Quaternion first, Quaternion other)
        {
            return new Quaternion(first.X / other.X, first.Y / other.Y, first.Z / other.Z, first.W / other.W);
        }

        public static Quaternion operator *(Quaternion first, Quaternion other)
        {
            return new Quaternion(first.X * other.X, first.Y * other.Y, first.Z * other.Z, first.W * other.W);
        }

        public static bool operator >(Quaternion first, Quaternion other)
        {
            return first.X > other.X || first.Y > other.Y || first.Z > other.Z || first.W > other.W;
        }

        public static bool operator <(Quaternion first, Quaternion other)
        {
            return first.X < other.X || first.Y < other.Y || first.Z < other.Z || first.W < other.W;
        }

        public static bool operator >=(Quaternion first, Quaternion other)
        {
            return first.X >= other.X || first.Y >= other.Y || first.Z >= other.Z || first.W >= other.W;
        }

        public static bool operator <=(Quaternion first, Quaternion other)
        {
            return first.X <= other.X || first.Y <= other.Y || first.Z <= other.Z || first.W <= other.W;
        }

        public static bool operator ==(Quaternion first, Quaternion other)
        {
            if (ReferenceEquals(first, other))
            {
                return true;
            }
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            if (ReferenceEquals(first, null))
            {
                return false;
            }

            return first.X == other.X && first.Y == other.Y && first.Z == other.Z && first.W == other.W;
        }

        public static bool operator !=(Quaternion first, Quaternion other)
        {
            if (ReferenceEquals(first, other))
            {
                return false;
            }
            if (ReferenceEquals(other, null))
            {
                return true;
            }
            if (ReferenceEquals(first, null))
            {
                return true;
            }

            return first.X != other.X || first.Y != other.Y || first.Z != other.Z || first.W != other.W;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            if (obj is Quaternion)
            {
                return (Quaternion)obj == this;
            }
            return false;
        }

        public void Deserialize(Dictionary<string, object> data)
        {
            x = (float)data["X"];
            y = (float)data["Y"];
            z = (float)data["Z"];
            w = (float)data["W"];
        }

        public void Serialize(Dictionary<string, object> data)
        {
            data["X"] = x;
            data["Y"] = y;
            data["Z"] = z;
            data["W"] = w;
        }
    }
}
