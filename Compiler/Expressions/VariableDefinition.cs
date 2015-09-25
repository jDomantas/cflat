namespace Compiler.Expressions
{
    class VariableDefinition : Sentence
    {
        public DataType Type { get; }
        public Name Name { get; }
        public MathExpression InitialValue { get; private set; }

        public VariableDefinition(DataType type, Name name, MathExpression value)
        {
            Type = type;
            Name = name;
            InitialValue = value;
        }
        
        public override string ToString()
        {
            if (InitialValue == null)
                return $"Variable: {Type} {Name};";
            else
                return $"Variable: {Type} {Name} = {InitialValue};";
        }

        public static VariableDefinition TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            DataType type = DataType.TryRead(stream);
            if (type == null)
            {
                state.Restore("invalid variable type");
                return null;
            }

            if (type == DataType.Void)
            {
                state.Restore("can't define variables of void type");
                return null;
            }

            Name name = Name.TryRead(stream);
            if (name == null)
            {
                state.Restore("invalid variable name");
                return null;
            }

            if (stream.CurrentDefinitions.FindVariable(name) != null)
            {
                state.Restore($"variable is already defined: {name}");
                return null;
            }

            if (stream.CurrentDefinitions.FindFunction(name) != null)
            {
                state.Restore($"function with this name is already defined: {name}");
                return null;
            }

            stream.SkipWhitespace();
            if (stream.TestNext('='))
            {
                stream.SkipWhitespace();
                MathExpression value = MathExpression.TryRead(stream);
                if (value == null)
                {
                    state.Restore("invalid initial value");
                    return null;
                }

                if (value.Type != type && !value.Type.CanImplicitlyCastTo(type))
                {
                    state.Restore($"can't implicitly cast {value.Type} to {type}");
                    return null;
                }

                if (value.Type != type)
                    value = new TypeCast(value, type);
                                
                stream.SkipWhitespace();
                if (!stream.ExpectAndConsume(';', state))
                    return null;
                else
                {
                    var def = new VariableDefinition(type, name, value);
                    stream.CurrentDefinitions.AddVariableDefinition(def);
                    return def;
                }
            }

            if (stream.ExpectAndConsume(';', state))
            {
                var def = new VariableDefinition(type, name, null);
                stream.CurrentDefinitions.AddVariableDefinition(def);
                return def;
            }
            else
                return null;
        }

        public override void Compile(CodeWriter writer, Definitions definitions)
        {
            writer.WriteLine($"; {ToString()}");
            if (InitialValue == null)
                writer.WriteLine($"sub sp, {Type.GetSize()}");
            else
                InitialValue.CompileAndPlaceOnStack(writer, definitions);

            definitions.AddVariableDefinition(new VariableDefinition(Type, Name, null));
        }
    }
}
