namespace Compiler.Expressions.MathOperations
{
    class LogicalOperation : MathOperation
    {
        public LogicalOperation(DataType type, MathExpression lhs, MathExpression rhs, Op operation) : base(operation, type, lhs, rhs)
        {
            if (lhs.IsConstant && rhs.IsConstant)
            {
                IsConstant = true;
                if (operation == Op.Or)
                    ConstantValue = (lhs.ConstantValue | rhs.ConstantValue);
                else if (operation == Op.And)
                    ConstantValue = (lhs.ConstantValue & rhs.ConstantValue);
                else if (operation == Op.Xor)
                    ConstantValue = (lhs.ConstantValue ^ rhs.ConstantValue);
            }
        }

        public static string TryCreate(Op operation, MathExpression lhs, MathExpression rhs, out MathOperation result)
        {
            if (!lhs.Type.HasBuiltinAritmetic() || !rhs.Type.HasBuiltinAritmetic())
            {
                result = null;
                return $"can't {operation.ToString().ToLower()} types {lhs.Type} and {rhs.Type}";
            }

            if (lhs.Type == DataType.UInt || rhs.Type == DataType.UInt)
            {
                if (lhs.Type != DataType.UInt)
                    lhs = TypeCast.Expand(lhs);
                if (rhs.Type != DataType.UInt)
                    rhs = TypeCast.Expand(rhs);

                result = new LogicalOperation(DataType.UInt, lhs, rhs, operation);
                return null;
            }
            else // both are bytes
            {
                result = new LogicalOperation(DataType.UByte, lhs, rhs, operation);
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

            if ((left.StorageType == MathCalculation.Type.Register || left.StorageType == MathCalculation.Type.DataValue) && RHS.DoesUseRegisters)
            {
                writer.WriteLine($"push {left.Value}");
                leftPushed = true;
            }

            string op = Operation.ToString().ToLower();

            MathCalculation right = RHS.CompileAndGetStorage(writer, definitions);

            if (right.StorageType == MathCalculation.Type.Imediate ||
                right.StorageType == MathCalculation.Type.StackValue ||
                right.StorageType == MathCalculation.Type.DataValue)
            {
                if (leftPushed)
                    writer.WriteLine("pop ax");
                else if (left.Value != "ax")
                    writer.WriteLine($"mov ax, {left.Value}");

                writer.WriteLine($"{op} ax, {right.Value}");
            }
            else if (right.StorageType == MathCalculation.Type.Register)
            {
                if (!leftPushed)
                {
                    writer.WriteLine($"{op} {right.Value}, {left.Value}");
                    return new MathCalculation(MathCalculation.Type.Register, right.Value);
                }
                else if (right.Value == "ax")
                {
                    writer.WriteLine("pop bx");
                    writer.WriteLine($"{op} ax, bx");
                }
                else
                {
                    writer.WriteLine($"pop ax");
                    writer.WriteLine($"{op} ax, {right.Value}");
                }
            }

            return new MathCalculation(MathCalculation.Type.Register, "ax");
        }

        private MathCalculation CompileByteByte(CodeWriter writer, Definitions definitions)
        {
            if (IsConstant) return new MathCalculation(MathCalculation.Type.Imediate, ConstantValue.ToString());

            MathCalculation left = LHS.CompileAndGetStorage(writer, definitions);
            bool leftPushed = false;

            if ((left.StorageType == MathCalculation.Type.Register || left.StorageType == MathCalculation.Type.DataValue) && RHS.DoesUseRegisters)
            {
                if (left.StorageType == MathCalculation.Type.DataValue)
                {
                    writer.WriteLine($"mov al, {left.Value}");
                    writer.WriteLine("push_byte al");
                }
                else
                    writer.WriteLine($"push_byte {left.Value}");
                leftPushed = true;
            }

            string op = Operation.ToString().ToLower();

            MathCalculation right = RHS.CompileAndGetStorage(writer, definitions);

            if (right.StorageType == MathCalculation.Type.Imediate ||
                right.StorageType == MathCalculation.Type.StackValue ||
                right.StorageType == MathCalculation.Type.DataValue)
            {
                if (leftPushed)
                    writer.WriteLine("pop_byte al");
                else if (left.Value != "al")
                    writer.WriteLine($"mov al, {left.Value}");

                writer.WriteLine($"{op} al, {right.Value}");
            }
            else if (right.StorageType == MathCalculation.Type.Register)
            {
                if (!leftPushed)
                {
                    writer.WriteLine($"{op} {right.Value}, {left.Value}");
                    return new MathCalculation(MathCalculation.Type.Register, right.Value);
                }
                else if (right.Value == "al")
                {
                    writer.WriteLine("pop_byte bl");
                    writer.WriteLine($"{op} al, bl");
                }
                else
                {
                    writer.WriteLine($"pop_byte al");
                    writer.WriteLine($"{op} al, {right.Value}");
                }
            }

            return new MathCalculation(MathCalculation.Type.Register, "al");
        }
    }
}
