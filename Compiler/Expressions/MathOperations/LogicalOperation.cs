namespace Compiler.Expressions.MathOperations
{
    class LogicalOperation : Class1Operation
    {
        public LogicalOperation(Op operation, DataType type, MathExpression lhs, MathExpression rhs) : base(true, operation, type, lhs, rhs)
        {
            if (lhs.IsConstant && rhs.IsConstant)
            {
                IsConstant = true;
                if (operation == Op.Or) ConstantValue = (lhs.ConstantValue | rhs.ConstantValue);
                else if (operation == Op.And) ConstantValue = (lhs.ConstantValue & rhs.ConstantValue);
                else if (operation == Op.Xor) ConstantValue = (lhs.ConstantValue ^ rhs.ConstantValue);
            }
        }

        public static string TryCreate(Op operation, MathExpression lhs, MathExpression rhs, out MathExpression result)
        {
            if (lhs.IsConstant && rhs.IsConstant)
            {
                result = new LiteralValue(new LogicalOperation(operation, lhs.Type, lhs, rhs).ConstantValue, lhs.Type);
                return null;
            }
            else
            {
                MathExpression l = null, r = null;
                string err = Class1Operation.TryCreate(operation, lhs, rhs, true, out l, out r);
                if (err != null)
                {
                    result = null;
                    return err;
                }

                result = new LogicalOperation(operation, lhs.Type, l, r);
                return null;
            }
        }

        public override MathCalculation CompileAndGetStorage(CodeWriter writer, Definitions definitions)
        {
            return Compile(writer, definitions, Operation.ToString().ToLower());
        }
    }
}
