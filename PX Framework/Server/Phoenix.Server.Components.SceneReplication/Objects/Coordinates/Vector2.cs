using Phoenix.Common.SceneReplication.Data;

namespace Phoenix.Server.SceneReplication.Coordinates
{
    /// <summary>
    /// Vector2D - XY Coordinates
    /// </summary>
    public class Vector2 : SerializingObject
    {
        public event Action? ChangedEvent;
        private bool readOnly;

        private float x;
        private float y;

        public Vector2()
        {
        }

        public Vector2(float x, float y, bool readOnly = false)
        {
            this.readOnly = readOnly;
            this.x = x;
            this.y = y;
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

        public override string ToString()
        {
            return X + ":" + Y;
        }

        public static Vector2 operator +(Vector2 first, Vector2 other)
        {
            return new Vector2(first.X + other.X, first.Y + other.Y);
        }

        public static Vector2 operator -(Vector2 first, Vector2 other)
        {
            return new Vector2(first.X - other.X, first.Y - other.Y);
        }

        public static Vector2 operator /(Vector2 first, Vector2 other)
        {
            return new Vector2(first.X / other.X, first.Y / other.Y);
        }

        public static Vector2 operator *(Vector2 first, Vector2 other)
        {
            return new Vector2(first.X * other.X, first.Y * other.Y);
        }

        public static bool operator >(Vector2 first, Vector2 other)
        {
            return first.X > other.X || first.Y > other.Y;
        }

        public static bool operator <(Vector2 first, Vector2 other)
        {
            return first.X < other.X || first.Y < other.Y;
        }

        public static bool operator >=(Vector2 first, Vector2 other)
        {
            return first.X >= other.X || first.Y >= other.Y;
        }

        public static bool operator <=(Vector2 first, Vector2 other)
        {
            return first.X <= other.X || first.Y <= other.Y;
        }

        public static bool operator ==(Vector2 first, Vector2 other)
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

            return first.X == other.X && first.Y == other.Y;
        }

        public static bool operator !=(Vector2 first, Vector2 other)
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

            return first.X != other.X || first.Y != other.Y;
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

            if (obj is Vector2)
            {
                return (Vector2)obj == this;
            }
            return false;
        }

        public void Deserialize(Dictionary<string, object> data)
        {
            x = (float)data["X"];
            y = (float)data["Y"];
        }

        public void Serialize(Dictionary<string, object> data)
        {
            data["X"] = x;
            data["Y"] = y;
        }
    }
}
