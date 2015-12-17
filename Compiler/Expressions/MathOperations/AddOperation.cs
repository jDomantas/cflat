namespace Compiler.Expressions.MathOperations
{
    class AddOperation : Class1Operation
    {
        public AddOperation(DataType type, MathExpression lhs, MathExpression rhs) : base(true, Op.Add, type, lhs, rhs)
        {
            if (lhs.IsConstant && rhs.IsConstant)
            {
                IsConstant = true;
                ConstantValue = (lhs.ConstantValue + rhs.ConstantValue) % (1 << (type.GetSize() * 8));
            }
        }

        public static string TryCreate(MathExpression lhs, MathExpression rhs, out MathExpression result)
        {
            if (lhs.Type.PointerDepth > 0 || rhs.Type.PointerDepth > 0)
                return PointerAddOperation.TryCreate(lhs, rhs, out result);

            if (lhs.IsConstant && rhs.IsConstant)
            { 
                result = new LiteralValue((lhs.ConstantValue + rhs.ConstantValue) % (1 << (lhs.Type.GetSize() * 8)), lhs.Type);
                return null;
            }
            else
            {
                MathExpression l = null, r = null;
                string err = Class1Operation.TryCreate(Op.Add, lhs, rhs, true, out l, out r);
                if (err != null)
                {
                    result = null;
                    return err;
                }

                result = new AddOperation(lhs.Type, l, r);
                return null;
            }
        }

        public override MathCalculation CompileAndGetStorage(CodeWriter writer, Definitions definitions)
        {
            return Compile(writer, definitions, "add");
        }
    }
}
