namespace Compiler.Expressions
{
    class Variable : Value
    {
        public Name Name { get; }

        public Variable(Name name, DataType type) : base(type, true)
        {
            Name = name;
            DoesUseRegisters = false;
        }

        public override string ToString()
        {
            return Name.ToString();
        }

        public new static Variable TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            Name name = Name.TryRead(stream);
            if (name == null)
            {
                state.Restore("invalid name");
                return null;
            }

            VariableDefinition def;
            if ((def = stream.CurrentDefinitions.FindVariable(name)) == null)
            {
                state.Restore($"variable '{name}' is not defined");
                return null;
            }
            
            stream.SkipWhitespace();
            if (stream.Peek() == '(')
            {
                state.Restore("invalid variable usage");
                return null;
            }

            return new Variable(name, def.Type);
        }

        public override MathCalculation CompileAndGetStorage(CodeWriter writer, Definitions definitions)
        {
            int address;
            definitions.FindVariable(Name, out address);

            string prefix = (Type.GetSize() == 1 ? "byte ptr " : "word ptr ");
            if (address == 0)
                return new MathCalculation(MathCalculation.Type.StackValue, $"{prefix}[bp]");
            else if (address < 0)
                return new MathCalculation(MathCalculation.Type.StackValue, $"{prefix}[bp - {-address}]");
            else
                return new MathCalculation(MathCalculation.Type.StackValue, $"{prefix}[bp + {address}]");
        }
    }
}
