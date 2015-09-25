namespace Compiler.Expressions
{
    class ContinueStatement : Sentence
    {
        public ContinueStatement()
        {

        }

        public static ContinueStatement TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();
            if (!stream.ExpectAndConsumeString("continue", state))
                return null;
            stream.SkipWhitespace();
            if (!stream.ExpectAndConsume(';', state))
                return null;

            if (stream.CurrentDefinitions.CurrentBreakStatement == null)
            {
                state.Restore("can't continue while not in loop");
                return null;
            }

            return new ContinueStatement();
        }

        public override void Compile(CodeWriter writer, Definitions definitions)
        {
            writer.WriteLine("; continue");
            writer.WriteLine($"jmp {definitions.CurrentContinueStatement}");
        }
    }
}
