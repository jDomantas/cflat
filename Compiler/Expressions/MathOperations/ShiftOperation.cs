namespace Compiler.Expressions.MathOperations
{
    class ShiftOperation : MathOperation
    {
        public ShiftOperation(DataType type, MathExpression lhs, MathExpression rhs, Op operation) : base(operation, type, lhs, rhs)
        {
            if (lhs.IsConstant && rhs.IsConstant)
            {
                IsConstant = true;
                if (operation == Op.ShiftRight)
                    ConstantValue = (lhs.ConstantValue / (1 << rhs.ConstantValue));
                else
                    ConstantValue = (lhs.ConstantValue * (1 << rhs.ConstantValue));
            }
        }

        public static string TryCreate(Op direction, MathExpression lhs, MathExpression rhs, out MathOperation result)
        {
            if (!rhs.IsConstant)
            {
                result = null;
                return $"second operand of shift operation must be constant";
            }

            if (!lhs.Type.HasBuiltinAritmetic() || !rhs.Type.HasBuiltinAritmetic())
            {
                result = null;
                return $"can't shift types {lhs.Type} and {rhs.Type}";
            }

            if (lhs.Type == DataType.UInt || rhs.Type == DataType.UInt)
            {
                if (lhs.Type != DataType.UInt)
                    lhs = TypeCast.Expand(lhs);
                if (rhs.Type != DataType.UInt)
                    rhs = TypeCast.Expand(rhs);

                result = new ShiftOperation(DataType.UInt, lhs, rhs, direction);
                return null;
            }
            else // both are bytes
            {
                result = new ShiftOperation(DataType.UByte, lhs, rhs, direction);
                return null;
            }
        }

        public override MathCalculation CompileAndGetStorage(CodeWriter writer, Definitions definitions)
        {
            if (LHS.Type.GetSize() == 1) return CompileByteByte(writer, definitions);
            else return CompileIntInt(writer, definitions);
        }

        private MathCalculation CompileIntInt(CodeWriter writer, Definitions definitions)
        {
            if (IsConstant) return new MathCalculation(MathCalculation.Type.Imediate, ConstantValue.ToString());

            MathCalculation left = LHS.CompileAndGetStorage(writer, definitions);
            bool leftPushed = false;
            
            MathCalculation right = RHS.CompileAndGetStorage(writer, definitions);

            if (right.StorageType == MathCalculation.Type.Imediate)
            {
                if (leftPushed)
                    writer.WriteLine("pop ax");
                else if (left.Value != "ax")
                    writer.WriteLine($"mov ax, {left.Value}");

                if (Operation == Op.ShiftLeft)
                    writer.WriteLine($"shl ax, {right.Value}");
                else
                    writer.WriteLine($"shr ax, {right.Value}");
            }
            else
            {
                throw new CompileException("shit went wrong, got constant rhs, but not immediate (shift operation)");
            }

            return new MathCalculation(MathCalculation.Type.Register, "ax");
        }

        private MathCalculation CompileByteByte(CodeWriter writer, Definitions definitions)
        {
            if (IsConstant) return new MathCalculation(MathCalculation.Type.Imediate, ConstantValue.ToString());

            MathCalculation left = LHS.CompileAndGetStorage(writer, definitions);
            bool leftPushed = false;

            MathCalculation right = RHS.CompileAndGetStorage(writer, definitions);

            if (right.StorageType == MathCalculation.Type.Imediate)
            {
                if (leftPushed)
                    writer.WriteLine("pop al");
                else if (left.Value != "al")
                    writer.WriteLine($"mov al, {left.Value}");

                if (Operation == Op.ShiftLeft)
                    writer.WriteLine($"shl al, {right.Value}");
                else
                    writer.WriteLine($"shr al, {right.Value}");
            }
            else
            {
                throw new CompileException("shit went wrong, got constant rhs, but not immediate (shift operation)");
            }

            return new MathCalculation(MathCalculation.Type.Register, "al");
        }
    }
}
