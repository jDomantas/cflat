namespace Compiler.Expressions
{
    class AddressDereference : Value
    {
        public MathExpression Address { get; }

        public AddressDereference(MathExpression address) : base(address.Type.Dereferenced(), true)
        {
            Address = address;
        }
        
        public override string ToString()
        {
            return $"*{Address}";
        }

        public new static AddressDereference TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            if (!stream.ExpectAndConsume('*', state))
                return null;

            stream.SkipWhitespace();
            if (stream.Peek() == '(')
            {
                stream.Consume();
                stream.SkipWhitespace();
                MathExpression expression = MathExpression.TryRead(stream);
                if (expression == null)
                {
                    state.Restore("invalid mathematical expression");
                    return null;
                }

                stream.SkipWhitespace();
                if (!stream.ExpectAndConsume(')', state))
                    return null;

                if (expression.Type.PointerDepth == 0)
                {
                    state.Restore("trying to dereference a non-pointer");
                    return null;
                }

                return new AddressDereference(expression);
            }
            else
            {
                Variable variable = Variable.TryRead(stream);
                if (variable == null)
                {
                    state.Restore("invalid variable");
                    return null;
                }

                if (variable.Type.PointerDepth == 0)
                {
                    state.Restore("trying to dereference a non-pointer");
                    return null;
                }

                return new AddressDereference(variable);
            }
        }

        public override MathCalculation CompileAndGetStorage(CodeWriter writer, Definitions definitions)
        {
            MathCalculation inner = Address.CompileAndGetStorage(writer, definitions);

            if (inner.StorageType == MathCalculation.Type.DataValue ||
                inner.StorageType == MathCalculation.Type.StackValue)
            {
                writer.WriteLine($"mov bx, {inner.Value}");
            }
            else if (inner.StorageType == MathCalculation.Type.Imediate)
            {
                if (Type.GetSize() == 1)
                    return new MathCalculation(MathCalculation.Type.DataValue, $"byte ptr ds:[{inner.Value}]");
                else
                    return new MathCalculation(MathCalculation.Type.DataValue, $"word ptr ds:[{inner.Value}]");
            }
            else if (inner.StorageType == MathCalculation.Type.Register)
            {
                if (inner.Value != "bx")
                    writer.WriteLine($"mov bx, {inner.Value}");
            }

            if (Type.GetSize() == 1)
                return new MathCalculation(MathCalculation.Type.DataValue, "byte ptr ds:[bx]");
            else
                return new MathCalculation(MathCalculation.Type.DataValue, "word ptr ds:[bx]");
        }
    }
}
