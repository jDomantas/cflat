using System.Text;

namespace Compiler.Expressions
{
    class DataType
    {
        public static DataType UInt { get; } = new DataType(Name.Int, 0);
        public static DataType UByte { get; } = new DataType(Name.Byte, 0);
        public static DataType Void { get; } = new DataType(Name.Void, 0);
        public static DataType Flags { get; } = new DataType(new Name("flags"), 0);

        public Name Name { get; }
        public int PointerDepth { get; }

        public DataType(Name name, int pointerDepth)
        {
            Name = name;
            PointerDepth = pointerDepth;
        }

        public int GetSize()
        {
            if (PointerDepth > 0)
                return 2;

            if (this == UByte) return 1;
            if (this == UInt) return 2;

            return 1;
        }

        public int GetPaddedSize()
        {
            return GetSize() + GetSize() % 2;
        }

        public DataType Dereferenced()
        {
            if (PointerDepth == 0)
                throw new CompileException("can't dereference non-pointer");
            return new DataType(Name, PointerDepth - 1);
        }
        
        public bool CanCastTo(DataType other)
        {
            // allow casting to itself
            if (other == this)
                return true;

            // allow casting pointers
            if (PointerDepth > 0 && other.PointerDepth > 0)
                return true;

            // allow casting 2 byte sized stuff to pointers
            if ((GetSize() == 2 && other.PointerDepth > 0) || (other.GetSize() == 2 && PointerDepth > 0))
                return true;

            // allow casting arithmetic types
            return HasBuiltinAritmetic() && other.HasBuiltinAritmetic();
        }
        
        public bool CanImplicitlyCastTo(DataType other)
        {
            // if can't cast, then don't bother to do that implicitly
            if (!CanCastTo(other))
                return false;

            // allow auto cast to itself
            if (other == this)
                return true;

            // allow auto casting from and to void pointers
            if (PointerDepth > 0 && other.PointerDepth > 0 && (Name == Name.Void || other.Name == Name.Void))
                return true;

            // and from byte to int
            if (other == UInt && this == UByte)
                return true;

            return false;
        }

        public bool HasBuiltinAritmetic()
        {
            return this == UInt || this == UByte;
        }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder(Name.ToString());
            for (int i = 0; i < PointerDepth; i++)
                str.Append("*");
            return str.ToString();
        }

        public override bool Equals(object obj)
        {
            DataType type = obj as DataType;
            return type != null && type.PointerDepth == PointerDepth && type.Name == Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ (1 << PointerDepth);
        }

        public static bool operator ==(DataType a, DataType b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
                return false;
            return a.Name == b.Name && a.PointerDepth == b.PointerDepth;
        }

        public static bool operator !=(DataType a, DataType b)
        {
            return !(a == b);
        }

        public static DataType TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            Name name = Name.TryRead(stream);
            if (name == null)
            {
                state.Restore("invalid name");
                return null;
            }

            if (!stream.CurrentDefinitions.IsTypeDefined(new DataType(name, 0)))
            {
                state.Restore($"undefined type: {name}");
                return null;
            }

            stream.SkipWhitespace();
            int pointers = 0;
            while (stream.Peek() == '*')
            {
                pointers++;
                stream.Consume();
                stream.SkipWhitespace();
            }

            return new DataType(name, pointers);
        }
    }
}
