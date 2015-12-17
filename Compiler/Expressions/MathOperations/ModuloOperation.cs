namespace Compiler.Expressions.MathOperations
{
    class ModuloOperation : MathOperation
    {
        public ModuloOperation(DataType type, MathExpression lhs, MathExpression rhs) : base(Op.Modulo, type, lhs, rhs)
        {
            if (lhs.IsConstant && rhs.IsConstant)
            {
                IsConstant = true;
                ConstantValue = (lhs.ConstantValue % rhs.ConstantValue) % (1 << (type.GetSize() * 8));
            }
        }

        public static string TryCreate(MathExpression lhs, MathExpression rhs, out MathExpression result)
        {
            if (!lhs.Type.HasBuiltinAritmetic() || !rhs.Type.HasBuiltinAritmetic())
            {
                result = null;
                return $"can't divide types {lhs.Type} and {rhs.Type}";
            }

            if (lhs.Type == DataType.UInt || rhs.Type == DataType.UInt)
            {
                if (lhs.Type != DataType.UInt)
                    lhs = TypeCast.Expand(lhs);
                if (rhs.Type != DataType.UInt)
                    rhs = TypeCast.Expand(rhs);

                result = new ModuloOperation(DataType.UInt, lhs, rhs);
                return null;
            }
            else // both are bytes
            {
                result = new ModuloOperation(DataType.UByte, lhs, rhs);
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

                if (right.StorageType == MathCalculation.Type.Imediate)
                {
                    writer.WriteLine($"mov bx, {right.Value}");
                    writer.WriteLine("mov dx, 0");
                    writer.WriteLine("div bx");
                }
                else
                {
                    writer.WriteLine("mov dx, 0");
                    writer.WriteLine($"div {right.Value}");
                }
            }
            else if (right.StorageType == MathCalculation.Type.Register)
            {
                if (!leftPushed)
                {
                    if (left.Value == "ax")
                    {
                        if (right.Value == "dx")
                        {
                            writer.WriteLine("mov cx, dx");
                            writer.WriteLine("mov dx, 0");
                            writer.WriteLine($"div cx");
                        }
                        else
                        {
                            writer.WriteLine("mov dx, 0");
                            writer.WriteLine($"div {right.Value}");
                        }
                    }
                    else if (right.Value == "ax")
                    {
                        writer.WriteLine($"push ax");
                        writer.WriteLine($"mov ax, {left.Value}");
                        writer.WriteLine("pop bx");
                        writer.WriteLine("mov dx, 0");
                        writer.WriteLine("div bx");
                    }
                    else
                    {
                        writer.WriteLine($"mov ax, {left.Value}");
                        if (right.Value == "dx")
                        {
                            writer.WriteLine("mov cx, dx");
                            writer.WriteLine("mov dx, 0");
                            writer.WriteLine($"div cx");
                        }
                        else
                        {
                            writer.WriteLine("mov dx, 0");
                            writer.WriteLine($"div {right.Value}");
                        }
                    }
                }
                else if (right.Value == "ax")
                {
                    writer.WriteLine("pop bx");
                    writer.WriteLine("mov dx, 0");
                    writer.WriteLine("div bx");
                }
                else
                {
                    writer.WriteLine($"pop ax");
                    if (right.Value == "dx")
                    {
                        writer.WriteLine("mov cx, dx");
                        writer.WriteLine("mov dx, 0");
                        writer.WriteLine($"div cx");
                    }
                    else
                    {
                        writer.WriteLine("mov dx, 0");
                        writer.WriteLine($"div {right.Value}");
                    }
                }
            }

            return new MathCalculation(MathCalculation.Type.Register, "dx");
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

                if (right.StorageType == MathCalculation.Type.Imediate)
                {
                    writer.WriteLine($"mov bl, {right.Value}");
                    writer.WriteLine("mov ah, 0");
                    writer.WriteLine("div bl");
                }
                else
                {
                    writer.WriteLine("mov ah, 0");
                    writer.WriteLine($"div {right.Value}");
                }
            }
            else if (right.StorageType == MathCalculation.Type.Register)
            {
                if (!leftPushed)
                {
                    if (left.Value == "al")
                    {
                        writer.WriteLine("mov ah, 0");
                        writer.WriteLine($"div {right.Value}");
                    }
                    else if (right.Value == "al")
                    {
                        writer.WriteLine($"push_byte al");
                        writer.WriteLine($"mov al, {left.Value}");
                        writer.WriteLine("pop_byte bl");
                        writer.WriteLine("mov ah, 0");
                        writer.WriteLine("div bl");
                    }
                    else
                    {
                        writer.WriteLine($"mov al, {left.Value}");
                        writer.WriteLine("mov ah, 0");
                        writer.WriteLine($"div {right.Value}");
                    }
                }
                else if (right.Value == "al")
                {
                    writer.WriteLine("pop_byte bl");
                    writer.WriteLine("mov ah, 0");
                    writer.WriteLine("div bl");
                }
                else
                {
                    writer.WriteLine($"pop_byte al");
                    writer.WriteLine("mov ah, 0");
                    writer.WriteLine($"div {right.Value}");
                }
            }

            return new MathCalculation(MathCalculation.Type.Register, "ah");
        }
    }
}
