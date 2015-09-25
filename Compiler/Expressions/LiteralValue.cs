namespace Compiler.Expressions
{
    class LiteralValue : Value
    {
        public int Value { get; }
        public bool IsCharacter { get; }

        public LiteralValue(char ch) : this((int)ch, DataType.UByte)
        {
            IsCharacter = true;
        }

        public LiteralValue(int value) : this(value, value < 256 ? DataType.UByte : DataType.UInt)
        {

        }

        public LiteralValue(int value, DataType type) : base(type, false)
        {
            Value = value;
            IsConstant = true;
            ConstantValue = Value;
            DoesUseRegisters = false;
        }
        
        public override string ToString()
        {
            if (IsCharacter)
                return $"'{(char)Value}'";
            else
                return Value.ToString();
        }

        public new static LiteralValue TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            char next = stream.Peek();
            long sign = 1;
            if (next == '-')
            {
                sign = -1;
                stream.Consume();
            }

            if (next == '\'')
            {
                stream.Consume();
                char ch = stream.Consume();
                bool escaped = (ch == '\\');

                if (ch == '\\') // escape character
                {
                    ch = stream.Consume();
                    if (ch == 'n') ch = '\n';
                    else if (ch == 'r') ch = '\r';
                    else if (ch == '\'') ch = '\'';
                    else
                    {
                        state.Restore($"unknown escape sequence: \\{ch}");
                        return null;
                    }
                }

                if (!stream.ExpectAndConsume('\'', state))
                    return null;
                if (!escaped)
                    return new LiteralValue(ch);
                else
                    return new LiteralValue(ch, DataType.UByte);
            }

            long value = 0;
            int digits = 0;
            while (true)
            {
                long digit = stream.Peek() - '0';
                if (digit >= 0 && digit <= 9)
                {
                    stream.Consume();
                    value = 10L * value + digit;
                    digits++;
                    if (digits > 6)
                    {
                        state.Restore($"integer literal is out of bounds: {value}");
                        return null;
                    }
                }
                else if (digits > 0)
                {
                    value = value * sign;
                    if (value > ushort.MaxValue || value < short.MinValue)
                    {
                        state.Restore($"integer literal is out of bounds: {value}");
                        return null;
                    }

                    return new LiteralValue((int)value);
                }
                else
                {
                    state.Restore("invalid integer literal");
                    return null;
                }
            }
        }

        public override MathCalculation CompileAndGetStorage(CodeWriter writer, Definitions definitions)
        {
            if (IsCharacter)
                return new MathCalculation(MathCalculation.Type.Imediate, $"'{(char)Value}'");
            else
                return new MathCalculation(MathCalculation.Type.Imediate, Value.ToString());
        }
    }
}
