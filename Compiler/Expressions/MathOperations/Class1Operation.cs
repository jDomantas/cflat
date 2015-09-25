namespace Compiler.Expressions.MathOperations
{
    abstract class Class1Operation : MathOperation
    {
        private bool AllowSwappingOperands;

        public Class1Operation(bool allowSwapping, Op operation, DataType type, MathExpression lhs, MathExpression rhs) 
            : base(operation, type, lhs, rhs)
        {
            AllowSwappingOperands = allowSwapping;
        }

        public static string TryCreate(Op operation, MathExpression lhs, MathExpression rhs, bool allowSwap, out MathExpression left, out MathExpression right)
        {
            if (!lhs.Type.HasBuiltinAritmetic() || !rhs.Type.HasBuiltinAritmetic())
            {
                left = right = null;
                return $"can't {operation.ToString().ToLower()} types {lhs.Type} and {rhs.Type}";
            }

            if (lhs.Type != rhs.Type && rhs.Type.CanImplicitlyCastTo(lhs.Type))
                rhs = new TypeCast(rhs, lhs.Type);
            else if (lhs.Type != rhs.Type && lhs.Type.CanImplicitlyCastTo(rhs.Type))
                lhs = new TypeCast(lhs, rhs.Type);
            else
            {
                left = right = null;
                return $"can't {operation.ToString().ToLower()} types {lhs.Type} and {rhs.Type}";
            }

            if (allowSwap)
            {
                if (lhs.IsConstant)
                {
                    MathExpression temp = lhs;
                    lhs = rhs;
                    rhs = lhs;
                }
            }

            left = lhs;
            right = rhs;
            return null;
        }

        protected MathCalculation Compile(CodeWriter writer, Definitions definitions, string operation)
        {
            return Compile(writer, definitions, operation, LHS.Type.GetSize() == 1);
        }

        protected MathCalculation Compile(CodeWriter writer, Definitions definitions, string operation, bool small)
        {
            string push = (small ? "push_byte" : "push");
            string pop = (small ? "pop_byte" : "pop");
            string regA = (small ? "al" : "ax");
            string regB = (small ? "bl" : "bx");

            MathCalculation left = LHS.CompileAndGetStorage(writer, definitions);
            bool leftPushed = false;

            if ((left.StorageType == MathCalculation.Type.Register || left.StorageType == MathCalculation.Type.DataValue) 
                && RHS.DoesUseRegisters)
            {
                writer.WriteLine($"push {left.Value}");
                leftPushed = true;
            }

            MathCalculation right = RHS.CompileAndGetStorage(writer, definitions);
            
            if (right.StorageType == MathCalculation.Type.Imediate ||
                right.StorageType == MathCalculation.Type.DataValue ||
                right.StorageType == MathCalculation.Type.StackValue)
            {
                if (leftPushed)
                {
                    writer.WriteLine($"{pop} {regA}");
                    writer.WriteLine($"{operation} {regA} {right.Value}");
                    return new MathCalculation(MathCalculation.Type.Register, regA);
                }
                else if (left.StorageType == MathCalculation.Type.DataValue ||
                         left.StorageType == MathCalculation.Type.StackValue ||
                         left.StorageType == MathCalculation.Type.Imediate)
                {
                    writer.WriteLine($"mov {regA} {left.Value}");
                    writer.WriteLine($"{operation} {regA} {right.Value}");
                    return new MathCalculation(MathCalculation.Type.Register, regA);
                }
                else if (left.StorageType == MathCalculation.Type.Register)
                {
                    writer.WriteLine($"{operation} {left.Value} {right.Value}");
                    return left;
                }
                 
            }
            else if (right.StorageType == MathCalculation.Type.Register)
            {
                if (AllowSwappingOperands)
                {
                    writer.WriteLine($"{operation} {right.Value} {left.Value}");
                    return right;
                }

                if (left.StorageType == MathCalculation.Type.DataValue ||
                    left.StorageType == MathCalculation.Type.StackValue ||
                    left.StorageType == MathCalculation.Type.Imediate)
                {
                    if (right.Value == regA)
                    {
                        writer.WriteLine($"mov {regB} {left.Value}");
                        writer.WriteLine($"{operation} {regB} {regA}");
                        return new MathCalculation(MathCalculation.Type.Register, regB);
                    }
                    else
                    {
                        writer.WriteLine($"mov {regA} {left.Value}");
                        writer.WriteLine($"{operation} {regA} {right.Value}");
                        return new MathCalculation(MathCalculation.Type.Register, regA);
                    }
                }
                else if (left.StorageType == MathCalculation.Type.Register)
                {
                    writer.WriteLine($"{operation} {left.Value} {right.Value}");
                    return left;
                }
            }

            throw new CompileException($"unexpected storage types for operation {operation}: {left.StorageType} and {right.StorageType}");
        }
    }
}
