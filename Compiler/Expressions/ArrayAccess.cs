using Compiler.Expressions.MathOperations;

namespace Compiler.Expressions
{
    class ArrayAccess : Value
    {
        public Variable Variable { get; }
        public int ConstantAdress { get; }

        public ArrayAccess(Variable variable, int address) : base(variable.Type.Dereferenced(), true)
        {
            Variable = variable;
            ConstantAdress = address;
        }

        public override string ToString()
        {
            return $"{Variable}[{ConstantAdress}]";
        }
        
        public override MathCalculation CompileAndGetStorage(CodeWriter writer, Definitions definitions)
        {
            MathCalculation variable = Variable.CompileAndGetStorage(writer, definitions);
            writer.WriteLine($"mov bx, {variable.Value}");

            string offset = "";
            if (ConstantAdress > 0) offset = " + " + ConstantAdress;
            else if (ConstantAdress < 0) offset = " - " + -ConstantAdress;

            if (Variable.Type.Dereferenced().GetSize() == 2)
                return new MathCalculation(MathCalculation.Type.DataValue, $"word ptr [bx{offset}]");
            else
                return new MathCalculation(MathCalculation.Type.DataValue, $"byte ptr [bx{offset}]");
        }
        
        public new static MathExpression TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            Variable v = Variable.TryRead(stream);
            if (v == null)
            {
                state.Restore("invalid variable");
                return null;
            }

            if (v.Type.PointerDepth == 0)
            {
                state.Restore("non-pointer used as array");
                return null;
            }

            stream.SkipWhitespace();

            if (!stream.ExpectAndConsume('[', state))
                return null;

            MathExpression index = MathExpression.TryRead(stream);
            if (index == null)
            {
                state.Restore("invalid index");
                return null;
            }

            stream.SkipWhitespace();

            if (!stream.ExpectAndConsume(']', state))
                return null;

            if (!index.Type.HasBuiltinAritmetic())
            {
                state.Restore($"invalid index type, got: {index.Type}, expected integer");
                return null;
            }

            if (index.Type != DataType.UInt)
                index = new TypeCast(index, DataType.UInt);

            if (v.Type.Dereferenced().GetSize() > 1)
                index = new MultiplyOperation(DataType.UInt, index, new LiteralValue(v.Type.Dereferenced().GetSize(), DataType.UInt));

            if (index.IsConstant)
                return new ArrayAccess(v, index.ConstantValue);
            else
                return new AddressDereference(new AddOperation(v.Type, v, index));
        }
    }
}
