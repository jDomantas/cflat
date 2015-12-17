namespace Compiler.Expressions.MathOperations
{
    class SubtractOperation : Class1Operation
    {
        public SubtractOperation(DataType type, MathExpression lhs, MathExpression rhs) : base(false, Op.Subtract, type, lhs, rhs)
        {
            if (lhs.IsConstant && rhs.IsConstant)
            {
                IsConstant = true;
                ConstantValue = (lhs.ConstantValue - rhs.ConstantValue + (1 << (type.GetSize() * 8))) % (1 << (type.GetSize() * 8));
            }
        }

        public static string TryCreate(MathExpression lhs, MathExpression rhs, out MathExpression result)
        {
            if (lhs.Type.PointerDepth > 0 || rhs.Type.PointerDepth > 0)
                return PointerSubtractOperation.TryCreate(lhs, rhs, out result);

            if (lhs.IsConstant && rhs.IsConstant)
            {
                int mod = (1 << (lhs.Type.GetSize() * 8));
                result = new LiteralValue((lhs.ConstantValue - rhs.ConstantValue + mod) % mod, lhs.Type);
                return null;
            }
            else
            {
                MathExpression l = null, r = null;
                string err = Class1Operation.TryCreate(Op.Subtract, lhs, rhs, false, out l, out r);
                if (err != null)
                {
                    result = null;
                    return err;
                }

                result = new SubtractOperation(lhs.Type, lhs, rhs);
                return null;
            }
        }

        public override MathCalculation CompileAndGetStorage(CodeWriter writer, Definitions definitions)
        {
            return Compile(writer, definitions, "sub");
        }
    }
}
