namespace Compiler.Expressions.MathOperations
{
    class PointerSubtractOperation
    {
        public static string TryCreate(MathExpression lhs, MathExpression rhs, out MathOperation result)
        {
            if ((lhs.Type.PointerDepth > 0 && rhs.Type.PointerDepth > 0) ||
                (lhs.Type.PointerDepth > 0 && !rhs.Type.HasBuiltinAritmetic()) ||
                (!lhs.Type.HasBuiltinAritmetic() && rhs.Type.PointerDepth > 0))
            {
                result = null;
                return $"can't subtract types {lhs.Type} and {rhs.Type}";
            }

            if (lhs.Type.PointerDepth > 0)
            {
                if (rhs.Type != DataType.UInt)
                    rhs = TypeCast.Expand(rhs);

                int size = lhs.Type.Dereferenced().GetSize();
                if (size > 1)
                    rhs = new MultiplyOperation(DataType.UInt, rhs, new LiteralValue(size, DataType.UInt));

                result = new SubtractOperation(lhs.Type, lhs, rhs);
                return null;
            }
            else
            {
                if (lhs.Type != DataType.UInt)
                    lhs = TypeCast.Expand(lhs);

                int size = rhs.Type.Dereferenced().GetSize();
                if (size > 1)
                    rhs = new MultiplyOperation(DataType.UInt, lhs, new LiteralValue(size, DataType.UInt));

                result = new SubtractOperation(rhs.Type, lhs, rhs);
                return null;
            }
        }
    }
}
