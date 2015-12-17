namespace Compiler.Expressions.MathOperations
{
    class AssignOperation : MathOperation
    {
        public AssignOperation(DataType type, MathExpression lhs, MathExpression rhs) : base(Op.Assign, type, lhs, rhs)
        {

        }

        public static string TryCreate(MathExpression lhs, MathExpression rhs, out MathExpression result)
        {
            if (lhs.Type != rhs.Type && !rhs.Type.CanImplicitlyCastTo(lhs.Type))
            {
                result = null;
                return $"can't assign {rhs.Type} and {lhs.Type}";
            }

            if (lhs.Type != rhs.Type)
                rhs = new TypeCast(rhs, lhs.Type);

            result = new AssignOperation(lhs.Type, lhs, rhs);
            return null;
        }

        public override MathCalculation CompileAndGetStorage(CodeWriter writer, Definitions definitions)
        {
            if (LHS.Type.GetSize() == 1) return CompileByteByte(writer, definitions);
            else return CompileIntInt(writer, definitions);
        }

        private MathCalculation CompileIntInt(CodeWriter writer, Definitions definitions)
        {
            MathCalculation right = RHS.CompileAndGetStorage(writer, definitions);
            bool rightPushed = false;

            if ((right.StorageType == MathCalculation.Type.Register || right.StorageType == MathCalculation.Type.DataValue) && LHS.DoesUseRegisters)
            {
                writer.WriteLine($"push {right.Value}");
                rightPushed = true;
            }

            MathCalculation left = LHS.CompileAndGetStorage(writer, definitions);

            if (rightPushed)
                writer.WriteLine($"pop {left.Value}");
            else if ((left.StorageType == MathCalculation.Type.DataValue || left.StorageType == MathCalculation.Type.StackValue) &&
                     (right.StorageType == MathCalculation.Type.DataValue || right.StorageType == MathCalculation.Type.StackValue))
            {
                writer.WriteLine($"mov ax, {right.Value}");
                writer.WriteLine($"mov {left.Value}, ax");
            }
            else
                writer.WriteLine($"mov {left.Value}, {right.Value}");

            return new MathCalculation(left.StorageType, left.Value);
        }

        private MathCalculation CompileByteByte(CodeWriter writer, Definitions definitions)
        {
            MathCalculation right = RHS.CompileAndGetStorage(writer, definitions);
            bool rightPushed = false;

            if ((right.StorageType == MathCalculation.Type.Register || right.StorageType == MathCalculation.Type.DataValue) && LHS.DoesUseRegisters)
            {
                if (right.StorageType == MathCalculation.Type.DataValue)
                {
                    writer.WriteLine($"mov al, {right.Value}");
                    writer.WriteLine("push_byte al");
                }
                else
                    writer.WriteLine($"push_byte {right.Value}");
                rightPushed = true;
            }

            MathCalculation left = LHS.CompileAndGetStorage(writer, definitions);

            if (rightPushed)
            {
                if (left.StorageType == MathCalculation.Type.Register)
                    writer.WriteLine($"pop_byte {left.Value}");
                else
                {
                    writer.WriteLine("pop_byte al");
                    writer.WriteLine($"mov {left.Value} al");
                }
            }
            else if ((left.StorageType == MathCalculation.Type.DataValue || left.StorageType == MathCalculation.Type.StackValue) &&
                     (right.StorageType == MathCalculation.Type.DataValue || right.StorageType == MathCalculation.Type.StackValue))
            {
                writer.WriteLine($"mov al, {right.Value}");
                writer.WriteLine($"mov {left.Value}, al");
            }
            else
                writer.WriteLine($"mov {left.Value}, {right.Value}");

            return new MathCalculation(left.StorageType, left.Value);
        }
    }
}
