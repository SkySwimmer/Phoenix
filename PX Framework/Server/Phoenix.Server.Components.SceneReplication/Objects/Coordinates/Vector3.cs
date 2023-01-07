using Phoenix.Server.SceneReplication.Data;

namespace Phoenix.Server.SceneReplication.Coordinates
{
    /// <summary>
    /// Vector3D - XYZ Coordinates
    /// </summary>
    public class Vector3 : SerializingObject
    {
        public event Action? ChangedEvent;

        private float x;
        private float y;
        private float z;
        private bool readOnly;

        public Vector3() 
        {
        }

        public Vector3(float x, float y, float z, bool readOnly = false)
        {
            this.readOnly = readOnly;
            this.x = x;
            this.y = y;
            this.z = z;
        }

        /// <summary>
        /// X-axis
        /// </summary>
        public float X
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
        public float Y
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
        public float Z
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

        public override string ToString()
        {
            return X + ":" + Y + ":" + Z;
        }

        public static Vector3 operator +(Vector3 first, Vector3 other)
        {
            return new Vector3(first.X + other.X, first.Y + other.Y, first.Z + other.Z);
        }

        public static Vector3 operator -(Vector3 first, Vector3 other)
        {
            return new Vector3(first.X - other.X, first.Y - other.Y, first.Z - other.Z);
        }

        public static Vector3 operator /(Vector3 first, Vector3 other)
        {
            return new Vector3(first.X / other.X, first.Y / other.Y, first.Z / other.Z);
        }

        public static Vector3 operator *(Vector3 first, Vector3 other)
        {
            return new Vector3(first.X * other.X, first.Y * other.Y, first.Z * other.Z);
        }

        public static bool operator >(Vector3 first, Vector3 other)
        {
            return first.X > other.X || first.Y > other.Y || first.Z > other.Z;
        }

        public static bool operator <(Vector3 first, Vector3 other)
        {
            return first.X < other.X || first.Y < other.Y || first.Z < other.Z;
        }

        public static bool operator >=(Vector3 first, Vector3 other)
        {
            return first.X >= other.X || first.Y >= other.Y || first.Z >= other.Z;
        }

        public static bool operator <=(Vector3 first, Vector3 other)
        {
            return first.X <= other.X || first.Y <= other.Y || first.Z <= other.Z;
        }

        public static bool operator ==(Vector3 first, Vector3 other)
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

            return first.X == other.X && first.Y == other.Y && first.Z == other.Z;
        }

        public static bool operator !=(Vector3 first, Vector3 other)
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

            return first.X != other.X || first.Y != other.Y || first.Z != other.Z;
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

            if (obj is Vector3)
            {
                return (Vector3)obj == this;
            }
            return false;
        }

        public void Deserialize(Dictionary<string, object> data)
        {
            x = (float)data["X"];
            y = (float)data["Y"];
            z = (float)data["Z"];
        }

        public void Serialize(Dictionary<string, object> data)
        {
            data["X"] = x;
            data["Y"] = y;
            data["Z"] = z;
        }
    }
}
