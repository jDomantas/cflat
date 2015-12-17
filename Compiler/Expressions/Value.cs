namespace Compiler.Expressions
{
    abstract class Value : MathExpression
    {
        public Value(DataType type, bool writable) : base(type, writable)
        {
        }

        public new static MathExpression TryRead(SymbolStream stream)
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

            MathExpression array = ArrayAccess.TryRead(stream);
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

            ReferenceOperator reference = ReferenceOperator.TryRead(stream);
            if (reference != null)
                return reference;

            state.Restore("invalid math expression");
            return null;
        }
    }
}
