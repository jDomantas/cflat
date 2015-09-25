namespace Compiler.Expressions
{
    class FunctionCall : Value
    {
        public Name Name { get; }
        public CallParameterList Parameters { get; }

        public FunctionCall(Name name, DataType type, CallParameterList parameters) : base(type, false)
        {
            Name = name;
            Parameters = parameters;
        }
        
        public override string ToString()
        {
            return $"{Name}{Parameters}";
        }

        public new static FunctionCall TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            Name name = Name.TryRead(stream);
            if (name == null)
            {
                state.Restore("invalid function name");
                return null;
            }

            CallParameterList parameters = CallParameterList.TryRead(stream);
            if (parameters == null)
            {
                state.Restore("invalid call parameters");
                return null;
            }

            FunctionDefinition def = stream.CurrentDefinitions.FindFunction(name);
            if (def == null)
            {
                state.Restore($"function is not defined: {name}");
                return null;
            }

            string error;
            if ((error = parameters.CheckTypes(def.Parameters)) != null)
            {
                state.Restore(error);
                return null;
            }

            stream.CurrentDefinitions.CurrentFunction.CallList.Add(name);
            return new FunctionCall(name, def.ReturnType, parameters);
        }
        
        public override MathCalculation CompileAndGetStorage(CodeWriter writer, Definitions definitions)
        {
            for (int i = 0; i < Parameters.Parameters.Length; i++)
                Parameters.Parameters[i].CompileAndPlaceOnStack(writer, definitions);

            writer.WriteLine($"call {Name}");

            if (Type.GetSize() == 1)
                return new MathCalculation(MathCalculation.Type.Register, "al");
            else
                return new MathCalculation(MathCalculation.Type.Register, "ax");
        }
    }
}
