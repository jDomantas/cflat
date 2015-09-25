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

        public static string TryCreate(MathExpression lhs, MathExpression rhs, out MathOperation result)
        {
            if (lhs.Type.PointerDepth > 0 || rhs.Type.PointerDepth > 0)
                return PointerAddOperation.TryCreate(lhs, rhs, out result);

            if (!lhs.Type.HasBuiltinAritmetic() || !rhs.Type.HasBuiltinAritmetic())
            {
                result = null;
                return $"can't add types {lhs.Type} and {rhs.Type}";
            }

            if (lhs.Type == DataType.UInt || rhs.Type == DataType.UInt)
            {
                if (lhs.Type != DataType.UInt)
                    lhs = TypeCast.Expand(lhs);
                if (rhs.Type != DataType.UInt)
                    rhs = TypeCast.Expand(rhs);

                result = new AddOperation(DataType.UInt, lhs, rhs);
                return null;
            }
            else // both are bytes
            {
                result = new AddOperation(DataType.UByte, lhs, rhs);
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

            MathCalculation right = RHS.CompileAndGetStorage(writer, definitions);

            if (right.StorageType == MathCalculation.Type.Imediate || 
                right.StorageType == MathCalculation.Type.StackValue || 
                right.StorageType == MathCalculation.Type.DataValue)
            {
                if (leftPushed)
                    writer.WriteLine("pop ax");
                else if (left.Value != "ax")
                    writer.WriteLine($"mov ax, {left.Value}");

                writer.WriteLine($"add ax, {right.Value}");
            }
            else if (right.StorageType == MathCalculation.Type.Register)
            {
                if (!leftPushed)
                {
                    writer.WriteLine($"add {right.Value}, {left.Value}");
                    return new MathCalculation(MathCalculation.Type.Register, right.Value);
                }
                else if (right.Value == "ax")
                {
                    writer.WriteLine("pop bx");
                    writer.WriteLine("add ax, bx");
                }
                else
                {
                    writer.WriteLine($"pop ax");
                    writer.WriteLine($"add ax, {right.Value}");
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

            MathCalculation right = RHS.CompileAndGetStorage(writer, definitions);

            if (right.StorageType == MathCalculation.Type.Imediate ||
                right.StorageType == MathCalculation.Type.StackValue ||
                right.StorageType == MathCalculation.Type.DataValue)
            {
                if (leftPushed)
                    writer.WriteLine("pop_byte al");
                else if (left.Value != "al")
                    writer.WriteLine($"mov al, {left.Value}");

                writer.WriteLine($"add al, {right.Value}");
            }
            else if (right.StorageType == MathCalculation.Type.Register)
            {
                if (!leftPushed)
                {
                    writer.WriteLine($"add {right.Value}, {left.Value}");
                    return new MathCalculation(MathCalculation.Type.Register, right.Value);
                }
                else if (right.Value == "al")
                {
                    writer.WriteLine("pop_byte bl");
                    writer.WriteLine("add al, bl");
                }
                else
                {
                    writer.WriteLine($"pop_byte al");
                    writer.WriteLine($"add al, {right.Value}");
                }
            }

            return new MathCalculation(MathCalculation.Type.Register, "al");
        }
    }
}
