namespace Compiler.Expressions
{
    class TypeCast : Value
    {
        public MathExpression Inner { get; }
        public DataType To { get; }

        public TypeCast(MathExpression inner, DataType to) : base(to, false)
        {
            Inner = inner;
            To = to;
            
            if (Inner.IsConstant)
            {
                IsConstant = true;
                if (to.GetSize() == 1)
                    ConstantValue = inner.ConstantValue % 256;
                else
                    ConstantValue = inner.ConstantValue;

                DoesUseRegisters = false;
            }

            if (Inner.GetType() == typeof(Variable))
                DoesUseRegisters = false;
        }
        
        public override string ToString()
        {
            //if (Inner.GetType() == typeof(LiteralValue) && To == DataType.Int)
            //    return Inner.ToString();
            //else
                return $"({To}){Inner}";
        }
        
        public new static TypeCast TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            if (!stream.ExpectAndConsume('(', state))
                return null;

            stream.SkipWhitespace();

            DataType type = DataType.TryRead(stream);
            if (type == null)
            {
                state.Restore($"invalid type");
                return null;
            }

            stream.SkipWhitespace();

            if (!stream.ExpectAndConsume(')', state))
                return null;

            stream.SkipWhitespace();

            if (stream.Peek() == '(')
            {
                stream.Consume();
                stream.SkipWhitespace();
                MathExpression value = MathExpression.TryRead(stream);
                stream.SkipWhitespace();
                if (!stream.ExpectAndConsume(')', state))
                    return null;

                if (!value.Type.CanCastTo(type))
                {
                    state.Restore($"can't cast {value.Type} to {type}");
                    return null;
                }

                return new TypeCast(value, type);
            }
            else
            {
                Value value = Value.TryRead(stream);
                if (value == null)
                {
                    state.Restore("invalid value");
                    return null;
                }

                if (!value.Type.CanCastTo(type))
                {
                    state.Restore($"can't cast {value.Type} to {type}");
                    return null;
                }

                return new TypeCast(value, type);
            }
        }

        public static TypeCast Expand(MathExpression expr)
        {
            if (expr.Type == DataType.UByte) return new TypeCast(expr, DataType.UInt);
            else throw new CompileException($"can't expand {expr.Type}");
        }

        public override MathCalculation CompileAndGetStorage(CodeWriter writer, Definitions definitions)
        {
            // cast at compile time to improve performance and readability
            if (Inner.IsConstant)
                return new MathCalculation(MathCalculation.Type.Imediate, (Inner.ConstantValue % (1 << (To.GetSize() * 8))).ToString());

            MathCalculation inner = Inner.CompileAndGetStorage(writer, definitions);
            if (Inner.Type.GetSize() == To.GetSize())
                return inner;
            else if (Inner.Type.GetSize() > To.GetSize())
            {
                if (inner.StorageType == MathCalculation.Type.DataValue ||
                    inner.StorageType == MathCalculation.Type.StackValue)
                {
                    return new MathCalculation(inner.StorageType, $"byte ptr {inner.Value.Substring(9)}");
                }
                else if (inner.StorageType == MathCalculation.Type.Register)
                {
                    if (inner.Value == "ax") return new MathCalculation(MathCalculation.Type.Register, "al");
                    if (inner.Value == "bx") return new MathCalculation(MathCalculation.Type.Register, "bl");
                    if (inner.Value == "cx") return new MathCalculation(MathCalculation.Type.Register, "cl");
                    if (inner.Value == "dx") return new MathCalculation(MathCalculation.Type.Register, "dl");
                }
            }    
            else
            {
                if (inner.StorageType == MathCalculation.Type.DataValue ||
                    inner.StorageType == MathCalculation.Type.StackValue)
                {
                    writer.WriteLine($"mov al, {inner.Value}");
                    writer.WriteLine("mov ah, 0");
                    return new MathCalculation(MathCalculation.Type.Register, "ax");
                }
                else if (inner.StorageType == MathCalculation.Type.Register)
                {
                    writer.WriteLine($"mov {inner.Value.Substring(0, 1)}h, 0");
                    return new MathCalculation(MathCalculation.Type.Register, $"{inner.Value.Substring(0, 1)}x");
                }
            }

            throw new CompileException("halp");
        }
    }
}
