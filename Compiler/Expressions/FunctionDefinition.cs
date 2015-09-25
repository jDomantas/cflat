using System.Collections.Generic;

namespace Compiler.Expressions
{
    class FunctionDefinition
    {
        public DataType ReturnType { get; }
        public Name Name { get; }
        public ParameterList Parameters { get; }
        public Block Body { get; private set; }
        public List<Name> CallList { get; } // list of called functions
        public bool ShouldCompile { get; set; }

        public FunctionDefinition(DataType type, Name name, ParameterList parameters, Block body)
        {
            ReturnType = type;
            Name = name;
            Parameters = parameters;
            Body = body;

            CallList = new List<Name>();
        }

        public override string ToString()
        {
            return $"Function: {ReturnType} {Name}{Parameters} {{ ... }}";
        }
        
        public static FunctionDefinition TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            DataType type = DataType.TryRead(stream);
            if (type == null)
            {
                state.Restore("invalid type");
                return null;
            }

            Name name = Name.TryRead(stream);
            if (name == null || !Name.IsValidName(name) || name.Value.ToLower() == "test")
            {
                state.Restore("invalid name");
                return null;
            }

            stream.CurrentDefinitions.EnterFunction();
            stream.SkipWhitespace();
            ParameterList parameters = ParameterList.TryRead(stream);
            if (parameters == null)
            {
                state.Restore("invalid parameter list");
                return null;
            }

            stream.SkipWhitespace();
            if (stream.Peek() == ';')
            {
                stream.Consume();
                // function declaration, no body
                var definition = new FunctionDefinition(type, name, parameters, null);
                string err = stream.CurrentDefinitions.AddFunctionDefinition(definition);
                if (err != null)
                {
                    state.Restore(err);
                    return null;
                }

                stream.CurrentDefinitions.ExitFunction();
                stream.CurrentDefinitions.CurrentFunction = null;
                return definition;
            }
            else
            {
                if (!parameters.AllNamesDefined)
                {
                    state.Restore("function implementation can't skip parameter names");
                    return null;
                }

                var definition = new FunctionDefinition(type, name, parameters, null);
                string err = stream.CurrentDefinitions.AddFunctionDefinition(definition);
                if (err != null)
                {
                    state.Restore(err);
                    return null;
                }
                stream.CurrentDefinitions.CurrentFunction = definition;

                Block body = Block.TryRead(stream);
                if (body == null)
                {
                    state.Restore();
                    return null;
                }

                stream.CurrentDefinitions.ExitFunction();
                stream.CurrentDefinitions.CurrentFunction = null;
                definition.Body = body;
                
                return definition;
            }
        }

        public void Compile(CodeWriter writer, Definitions definitions)
        {
            foreach (var param in Parameters.Parameters)
                definitions.AddVariableDefinition(new VariableDefinition(param.Item1, param.Item2, null));

            definitions.AddVariableDefinition(new VariableDefinition(DataType.UInt, Name.Internal_ReturnAddress, null));
            definitions.AddVariableDefinition(new VariableDefinition(DataType.UInt, Name.Internal_PreviousBasePtr, null));

            definitions.CurrentFunction = this;

            definitions.EnterFunction();
            writer.WriteLine($"{Name} proc");
            writer.WriteLine("; init stack frame");
            writer.WriteLine("push bp");
            writer.WriteLine("mov bp, sp");

            Body.Compile(writer, definitions);

            writer.WriteLine("pop bp");
            if (Parameters.TotalSize > 0)
                writer.WriteLine($"ret {Parameters.TotalSize}");
            else
                writer.WriteLine("ret");

            definitions.ExitFunction();

            writer.WriteLine($"endp {Name}");
            writer.WriteLine($"; end of function {Name}");
            writer.WriteLine();

            definitions.CurrentFunction = null;
            definitions.DefinedVariables.Clear();
        }
    }
}
