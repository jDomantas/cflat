using Compiler.Expressions.MathOperations;

namespace Compiler.Expressions
{
    class ArrayAccess
    {
        public static AddressDereference TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            Variable v = Variable.TryRead(stream);
            if (v == null)
            {
                state.Restore("invalid variable");
                return null;
            }

            if (v.Type.PointerDepth == 0)
            {
                state.Restore("non-pointer used as array");
                return null;
            }

            stream.SkipWhitespace();

            if (!stream.ExpectAndConsume('[', state))
                return null;

            MathExpression index = MathExpression.TryRead(stream);
            if (index == null)
            {
                state.Restore("invalid index");
                return null;
            }

            stream.SkipWhitespace();

            if (!stream.ExpectAndConsume(']', state))
                return null;

            if (!index.Type.HasBuiltinAritmetic())
            {
                state.Restore($"invalid index type, got: {index.Type}, expected integer");
                return null;
            }

            if (index.Type != DataType.UInt)
                index = new TypeCast(index, DataType.UInt);

            if (v.Type.Dereferenced().GetSize() > 1)
                index = new MultiplyOperation(DataType.UInt, index, new LiteralValue(v.Type.Dereferenced().GetSize(), DataType.UInt));

            return new AddressDereference(new AddOperation(v.Type, v, index));
        }
    }
}
