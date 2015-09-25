namespace Compiler.Expressions
{
    class NullConstant
    {
        public static LiteralValue TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();
            if (!stream.ExpectAndConsumeString("NULL", state))
                return null;

            return new LiteralValue(0, new DataType(Name.Void, 1));
        }
    }
}
