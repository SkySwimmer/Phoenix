namespace Phoenix.Server.SceneReplication.Coordinates
{
    public enum ReplicatingTransformProperty
    {
        POSITION,
        ROTATION,
        SCALE
    }

    /// <summary>
    /// Basic Transform Object - Position, Scale and Rotation vectors
    /// </summary>
    public class Transform
    {
        private bool readOnly = false;
        public event Replicate? OnReplicate;
        public delegate void Replicate(ReplicatingTransformProperty property);
        private Vector3 _position = new Vector3();
        private Vector3 _scale = new Vector3();
        private Vector3 _rotation = new Vector3();

        public Transform()
        {
            _position.ChangedEvent += () =>
            {
                OnReplicate?.Invoke(ReplicatingTransformProperty.POSITION);
            };
            _scale.ChangedEvent += () =>
            {
                OnReplicate?.Invoke(ReplicatingTransformProperty.SCALE);
            };
            _rotation.ChangedEvent += () =>
            {
                OnReplicate?.Invoke(ReplicatingTransformProperty.ROTATION);
            };
        }

        public Transform(Vector3 pos, Vector3 scale, Vector3 rotation, bool readOnly = false)
        {
            this.readOnly = readOnly;
            _position = pos;
            _scale = scale;
            _rotation = rotation;
            if (!readOnly)
            {
                _position.ChangedEvent += () =>
                {
                    OnReplicate?.Invoke(ReplicatingTransformProperty.POSITION);
                };
                _scale.ChangedEvent += () =>
                {
                    OnReplicate?.Invoke(ReplicatingTransformProperty.SCALE);
                };
                _rotation.ChangedEvent += () =>
                {
                    OnReplicate?.Invoke(ReplicatingTransformProperty.ROTATION);
                };
            }
            }

        /// <summary>
        /// Position vector
        /// </summary>
        public Vector3 Position
        {
            get
            {
                return _position;
            }
            set
            {
                if (readOnly)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                if (!object.ReferenceEquals(_position, value))
                {
                    _position = value;
                    OnReplicate?.Invoke(ReplicatingTransformProperty.POSITION);
                    _position.ChangedEvent += () =>
                    {
                        OnReplicate?.Invoke(ReplicatingTransformProperty.POSITION);
                    };
                }
            }
        }

        /// <summary>
        /// Scale vector
        /// </summary>
        public Vector3 Scale
        {
            get
            {
                return _scale;
            }
            set
            {
                if (readOnly)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                if (!object.ReferenceEquals(_scale, value))
                {
                    _scale = value;
                    OnReplicate?.Invoke(ReplicatingTransformProperty.SCALE);
                    _scale.ChangedEvent += () =>
                    {
                        OnReplicate?.Invoke(ReplicatingTransformProperty.SCALE);
                    };
                }
            }
        }

        /// <summary>
        /// Rotation vector
        /// </summary>
        public Vector3 Rotation
        {
            get
            {
                return _rotation;
            }
            set
            {
                if (readOnly)
                    throw new ArgumentException("Cannot change properties of a read-only object");
                if (!object.ReferenceEquals(_rotation, value))
                {
                    _rotation = value;
                    OnReplicate?.Invoke(ReplicatingTransformProperty.ROTATION);
                    _rotation.ChangedEvent += () =>
                    {
                        OnReplicate?.Invoke(ReplicatingTransformProperty.ROTATION);
                    };
                }
            }
        }

        /// <summary>
        /// Rotation quaternion (note: returns READ-ONLY quaternion, re-assign to update values)
        /// </summary>
        public Quaternion RotationQuat
        {
            get
            {
                double cr = Math.Cos(Rotation.X * 0.5);
                double sr = Math.Sin(Rotation.X * 0.5);
                double cp = Math.Cos(Rotation.Y * 0.5);
                double sp = Math.Sin(Rotation.Y * 0.5);
                double cy = Math.Cos(Rotation.Z * 0.5);
                double sy = Math.Sin(Rotation.Z * 0.5);
                return new Quaternion(cr * cp * cy + sr * sp * sy, sr * cp * cy - cr * sp * sy, cr * sp * cy + sr * cp * sy, cr * cp * sy - sr * sp * cy, true);
            }
            set
            {
                if (readOnly)
                    throw new ArgumentException("Cannot change properties of a read-only object");

                // roll (x-axis rotation)
                double sinr_cosp = 2 * (value.W * value.X + value.Y * value.Z);
                double cosr_cosp = 1 - 2 * (value.X * value.X + value.Y * value.Y);
                float x = (float)Math.Atan2(sinr_cosp, cosr_cosp);

                // pitch (y-axis rotation)
                double sinp = Math.Sqrt(1 + 2 * (value.W * value.X - value.Y * value.Z));
                double cosp = Math.Sqrt(1 - 2 * (value.W * value.X - value.Y * value.Z));
                float y = 2f * (float)(Math.Atan2(sinp, cosp) - Math.PI / 2);

                // yaw (z-axis rotation)
                double siny_cosp = 2 * (value.W * value.Z + value.X * value.Y);
                double cosy_cosp = 1 - 2 * (value.Y * value.Y + value.Z * value.Z);
                float z = (float)Math.Atan2(siny_cosp, cosy_cosp);
                Rotation = new Vector3(x, y, z);
            }
        }

        public override string ToString()
        {
            return Position + " " + Scale + " " + Rotation;
        }

        public static Transform operator +(Transform first, Transform other)
        {
            return new Transform(first.Position + other.Position, first.Scale + other.Scale, first.Rotation + other.Rotation);
        }

        public static Transform operator -(Transform first, Transform other)
        {
            return new Transform(first.Position - other.Position, first.Scale - other.Scale, first.Rotation - other.Rotation);
        }

        public static Transform operator /(Transform first, Transform other)
        {
            return new Transform(first.Position / other.Position, first.Scale / other.Scale, first.Rotation / other.Rotation);
        }

        public static Transform operator *(Transform first, Transform other)
        {
            return new Transform(first.Position * other.Position, first.Scale * other.Scale, first.Rotation * other.Rotation);
        }

        public static bool operator ==(Transform first, Transform other)
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

            return first.Position == other.Position && first.Scale == other.Scale && first.Rotation == other.Rotation;
        }

        public static bool operator !=(Transform first, Transform other)
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

            return first.Position != other.Position || first.Scale != other.Scale || first.Rotation != other.Rotation;
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

            if (obj is Transform)
            {
                return (Transform)obj == this;
            }
            return false;
        }

        public Common.SceneReplication.Packets.Transform ToPacketTransform()
        {
            return new Common.SceneReplication.Packets.Transform(
                new Common.SceneReplication.Packets.Vector3(Position.X, Position.Y, Position.Z),
                new Common.SceneReplication.Packets.Vector3(Scale.X, Scale.Y, Scale.Z),
                new Common.SceneReplication.Packets.Vector3(Rotation.X, Rotation.Y, Rotation.Z));
        }
    }
}
