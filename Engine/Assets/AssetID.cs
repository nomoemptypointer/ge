namespace Engine.Assets
{
    public struct AssetID : IEquatable<AssetID>
    {
        private string _value;
        public string Value { get { return _value; } set { _value = value; } }

        public bool IsEmpty => string.IsNullOrEmpty(_value);

        public AssetID(string value) => _value = value.Replace("\\", "/");

        public bool Equals(AssetID other) => other._value.Equals(_value);

        public override bool Equals(object obj) => obj is AssetID && ((AssetID)(obj)).Equals(this);

        public override int GetHashCode() => _value != null ? _value.GetHashCode() : 0;

        public override string ToString() => $"ID:{_value}";

        public static implicit operator AssetID(string s) => new AssetID(s);
        public static implicit operator string(AssetID id) => id._value;

        public static bool operator ==(AssetID left, AssetID right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AssetID left, AssetID right)
        {
            return !(left == right);
        }
    }
}
