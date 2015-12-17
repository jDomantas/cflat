namespace Compiler.Expressions.MathOperations
{
    class CompareOperation : Class1Operation
    {
        public CompareOperation(Op operation, MathExpression lhs, MathExpression rhs) : base(false, operation, DataType.Flags, lhs, rhs)
        {
            if (lhs.IsConstant && rhs.IsConstant)
            {
                IsConstant = true;
                switch (operation)
                {
                    case Op.Equal: ConstantValue = (lhs.ConstantValue == rhs.ConstantValue) ? 1 : 0; break;
                    case Op.NotEqual: ConstantValue = (lhs.ConstantValue != rhs.ConstantValue) ? 1 : 0; break;
                    case Op.Greater: ConstantValue = (lhs.ConstantValue > rhs.ConstantValue) ? 1 : 0; break;
                    case Op.GreaterEqual: ConstantValue = (lhs.ConstantValue >= rhs.ConstantValue) ? 1 : 0; break;
                    case Op.Less: ConstantValue = (lhs.ConstantValue < rhs.ConstantValue) ? 1 : 0; break;
                    case Op.LessEqual: ConstantValue = (lhs.ConstantValue <= rhs.ConstantValue) ? 1 : 0; break;
                    default: throw new CompileException($"invalid operator for comparision: {operation}");
                }
            }
        }

        public static string TryCreate(Op operation, MathExpression lhs, MathExpression rhs, out MathExpression result)
        {
            if (!lhs.Type.HasBuiltinAritmetic() && lhs.Type.CanCastTo(DataType.UInt))
                lhs = new TypeCast(lhs, DataType.UInt);

            if (!rhs.Type.HasBuiltinAritmetic() && rhs.Type.CanCastTo(DataType.UInt))
                rhs = new TypeCast(rhs, DataType.UInt);

            MathExpression l = null, r = null;
            string err = Class1Operation.TryCreate(operation, lhs, rhs, false, out l, out r);
            if (err != null)
            {
                result = null;
                return err;
            }

            result = new CompareOperation(operation, lhs, rhs);
            return null;
        }

        public override MathCalculation CompileAndGetStorage(CodeWriter writer, Definitions definitions)
        {
            Compile(writer, definitions, "cmp");
            return new MathCalculation(MathCalculation.Type.None, "error");
        }
    }
}
