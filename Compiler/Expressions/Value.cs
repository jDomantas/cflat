namespace Compiler.Expressions
{
    abstract class Value : MathExpression
    {
        public Value(DataType type, bool writable) : base(type, writable)
        {
        }

        public new static Value TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            LiteralValue nullConstant = NullConstant.TryRead(stream);
            if (nullConstant != null)
                return nullConstant;

            Increment increment = Increment.TryRead(stream);
            if (increment != null)
                return increment;

            TypeCast cast = TypeCast.TryRead(stream);
            if (cast != null)
                return cast;

            LiteralValue sizeOf = SizeOfExpression.TryRead(stream);
            if (sizeOf != null)
                return sizeOf;

            FunctionCall functionCall = FunctionCall.TryRead(stream);
            if (functionCall != null)
                return functionCall;

            AddressDereference array = ArrayAccess.TryRead(stream);
            if (array != null)
                return array;

            Variable variable = Variable.TryRead(stream);
            if (variable != null)
                return variable;

            LiteralValue literal = LiteralValue.TryRead(stream);
            if (literal != null)
                return literal;

            AddressDereference dereference = AddressDereference.TryRead(stream);
            if (dereference != null)
                return dereference;

            state.Restore("invalid math expression");
            return null;
        }
    }
}
