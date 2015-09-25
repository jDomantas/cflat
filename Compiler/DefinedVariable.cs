using Compiler.Expressions;

namespace Compiler
{
    class DefinedVariable
    {
        public DataType Type { get; }
        public Name Name { get; }

        public bool IsGlobal { get; }

        public DefinedVariable(DataType type, Name name, bool global)
        {
            Type = type;
            Name = name;
            IsGlobal = global;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            DefinedVariable other = obj as DefinedVariable;
            return other != null && other.Name == Name;
        }

        public override string ToString()
        {
            return $"{Type} {Name}";
        }

        public static bool operator ==(DefinedVariable a, DefinedVariable b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
                return false;
            return a.Name == b.Name;
        }
        
        public static bool operator !=(DefinedVariable a, DefinedVariable b)
        {
            return !(a == b);
        }
    }
}
