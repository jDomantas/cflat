namespace Compiler.Expressions
{
    class Name
    {
        public static Name Int { get; } = new Name("int");
        public static Name Byte { get; } = new Name("byte");
        public static Name Void { get; } = new Name("void");

        public static Name Internal_ReturnAddress { get; } = new Name("Return address");
        public static Name Internal_PreviousBasePtr { get; } = new Name("Parent base pointer");

        public string Value { get; }

        public Name(string value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value;
        }

        public override bool Equals(object obj)
        {
            Name name = obj as Name;
            return name != null && name.Value == Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(Name a, Name b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
                return false;
            return a.Value.ToLower() == b.Value.ToLower();
        }

        public static bool operator !=(Name a, Name b)
        {
            return !(a == b);
        }

        public static Name TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();
            
            char next = stream.Peek();
            if (!IsValidFirstChar(next))
            {
                state.Restore($"invalid first name symbol: {next}");
                return null;
            }

            stream.Consume();

            string name = next.ToString();

            while (true)
            {
                next = stream.Peek();
                if (!IsValidChar(next))
                {
                    return new Name(name);
                }
                else
                    name += stream.Consume();
            }
        }

        private static bool IsValidFirstChar(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
        }
        
        private static bool IsValidChar(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
        }

        public static bool IsValidName(Name name)
        {
            return
                name.Value != "int" &&
                name.Value != "void" &&
                name.Value != "byte" &&
                name.Value != "for" &&
                name.Value != "while" &&
                name.Value != "if" &&
                name.Value != "else" &&
                name.Value != "return" &&
                name.Value != "sizeof" &&
                name.Value != "__asm" &&
                name.Value != "break" &&
                name.Value != "continue";
        }

        public static bool CanBeTypeName(Name name)
        {
            return name.Value != "for" &&
                name.Value != "while" &&
                name.Value != "if" &&
                name.Value != "else" &&
                name.Value != "return" &&
                name.Value != "flags" &&
                name.Value != "sizeof" &&
                name.Value != "__asm" &&
                name.Value != "break" &&
                name.Value != "continue";
        }
    }
}
