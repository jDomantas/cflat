namespace Compiler.Expressions.MathOperations
{
    class CompareOperation : MathOperation
    {
        public CompareOperation(Op operation, MathExpression lhs, MathExpression rhs) : base(operation, DataType.Flags, lhs, rhs)
        {

        }

        public static string TryCreate(Op operation, MathExpression lhs, MathExpression rhs, out MathOperation result)
        {
            if ((!lhs.Type.HasBuiltinAritmetic() && lhs.Type.PointerDepth == 0) ||
                (!rhs.Type.HasBuiltinAritmetic() && rhs.Type.PointerDepth == 0))
            {
                result = null;
                return $"can't compare {rhs.Type} and {lhs.Type}";
            }

            if (lhs.Type.PointerDepth > 0 && (rhs.Type == DataType.UInt || rhs.Type.CanImplicitlyCastTo(DataType.UInt)))
            {
                if (rhs.Type != DataType.UInt)
                    rhs = new TypeCast(rhs, DataType.UInt);
            }
            else if (rhs.Type.PointerDepth > 0 && (lhs.Type == DataType.UInt || lhs.Type.CanImplicitlyCastTo(DataType.UInt)))
            {
                if (lhs.Type != DataType.UInt)
                    lhs = new TypeCast(lhs, DataType.UInt);
            }
            else if (lhs.Type != rhs.Type && lhs.Type.CanImplicitlyCastTo(rhs.Type))
                lhs = new TypeCast(lhs, rhs.Type);
            else if (lhs.Type != rhs.Type && rhs.Type.CanImplicitlyCastTo(lhs.Type))
                rhs = new TypeCast(rhs, lhs.Type);
            else if(lhs.Type != rhs.Type)
            {
                result = null;
                return $"can't compare {rhs.Type} and {lhs.Type}";
            }
            
            result = new CompareOperation(operation, lhs, rhs);
            return null;
        }

        public override MathCalculation CompileAndGetStorage(CodeWriter writer, Definitions definitions)
        {
            if (LHS.Type.GetSize() == 1) return CompileByteByte(writer, definitions);
            else return CompileIntInt(writer, definitions);
        }

        private MathCalculation CompileIntInt(CodeWriter writer, Definitions definitions)
        {
            MathCalculation left = LHS.CompileAndGetStorage(writer, definitions);
            bool leftPushed = false;

            if ((left.StorageType == MathCalculation.Type.Register || left.StorageType == MathCalculation.Type.DataValue) && RHS.DoesUseRegisters)
            {
                writer.WriteLine($"push {left.Value}");
                leftPushed = true;
            }

            MathCalculation right = RHS.CompileAndGetStorage(writer, definitions);

            if (right.StorageType == MathCalculation.Type.Imediate)
            {
                if (leftPushed)
                {
                    writer.WriteLine("pop ax");
                    writer.WriteLine($"cmp ax, {right.Value}");
                }
                else
                {
                    writer.WriteLine($"cmp {left.Value}, {right.Value}");
                }
            }
            else if (right.StorageType == MathCalculation.Type.StackValue ||
                     right.StorageType == MathCalculation.Type.DataValue)
            {
                if (leftPushed)
                    writer.WriteLine("pop ax");
                else if (left.Value != "ax")
                    writer.WriteLine($"mov ax, {left.Value}");

                writer.WriteLine($"cmp ax, {right.Value}");
            }
            else if (right.StorageType == MathCalculation.Type.Register)
            {
                if (!leftPushed)
                {
                    writer.WriteLine($"cmp {right.Value}, {left.Value}");
                }
                else if (right.Value == "ax")
                {
                    writer.WriteLine("pop bx");
                    writer.WriteLine("cmp ax, bx");
                }
                else
                {
                    writer.WriteLine($"pop ax");
                    writer.WriteLine($"cmp ax, {right.Value}");
                }
            }

            return new MathCalculation();
        }

        private MathCalculation CompileByteByte(CodeWriter writer, Definitions definitions)
        {
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

            if (right.StorageType == MathCalculation.Type.Imediate)
            {
                if (leftPushed)
                {
                    writer.WriteLine("pop al");
                    writer.WriteLine($"cmp al, {right.Value}");
                }
                else
                {
                    writer.WriteLine($"cmp {left.Value}, {right.Value}");
                }
            }
            else if (right.StorageType == MathCalculation.Type.StackValue ||
                     right.StorageType == MathCalculation.Type.DataValue)
            {
                if (leftPushed)
                    writer.WriteLine("pop_byte al");
                else if (left.Value != "al")
                    writer.WriteLine($"mov al, {left.Value}");

                writer.WriteLine($"cmp al, {right.Value}");
            }
            else if (right.StorageType == MathCalculation.Type.Register)
            {
                if (!leftPushed)
                {
                    writer.WriteLine($"cmp {right.Value}, {left.Value}");
                }
                else if (right.Value == "al")
                {
                    writer.WriteLine("pop_byte bl");
                    writer.WriteLine("cmp al, bl");
                }
                else
                {
                    writer.WriteLine($"pop_byte al");
                    writer.WriteLine($"cmp al, {right.Value}");
                }
            }

            return new MathCalculation();
        }
    }
}
