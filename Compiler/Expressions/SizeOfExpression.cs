namespace Compiler.Expressions
{
    class SizeOfExpression
    {
        public static LiteralValue TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            if (!stream.ExpectAndConsumeString("sizeof", state))
            {
                state.Restore("invalid sizeof keyword");
                return null;
            }

            stream.SkipWhitespace();
            if (!stream.ExpectAndConsume('(', state))
                return null;

            DataType type = DataType.TryRead(stream);
            if (type == null)
            {
                Name name = Name.TryRead(stream);
                if (name == null)
                {
                    state.Restore("invalid type or type");
                    return null;
                }

                VariableDefinition variable = stream.CurrentDefinitions.FindVariable(name);
                if (variable == null)
                {
                    state.Restore("invalid variable or type");
                    return null;
                }

                if (!stream.ExpectAndConsume(')', state))
                    return null;

                return new LiteralValue(variable.ActualSize);
            }
            else
            {
                if (!stream.ExpectAndConsume(')', state))
                    return null;

                return new LiteralValue(type.GetSize());
            }
        }
    }
}
