namespace Compiler.Expressions
{
    abstract class Sentence
    {
        public Sentence()
        {

        }
        
        public static Sentence TryRead(SymbolStream stream, bool requireSemicolon = true)
        {
            var state = stream.SaveState();

            BreakStatement breakLoop = BreakStatement.TryRead(stream);
            if (breakLoop != null)
                return breakLoop;

            ContinueStatement continueLoop = ContinueStatement.TryRead(stream);
            if (continueLoop != null)
                return continueLoop;
            
            InlineAssembly asm = InlineAssembly.TryRead(stream);
            if (asm != null)
                return asm;

            IfClause ifClause = IfClause.TryRead(stream);
            if (ifClause != null)
                return ifClause;

            ForLoop forLoop = ForLoop.TryRead(stream);
            if (forLoop != null)
                return forLoop;

            WhileLoop whileLoop = WhileLoop.TryRead(stream);
            if (whileLoop != null)
                return whileLoop;

            ReturnStatement returnStatement = ReturnStatement.TryRead(stream);
            if (returnStatement != null)
                return returnStatement;

            VariableDefinition variable = VariableDefinition.TryRead(stream);
            if (variable != null)
                return variable;

            MathExpression expr = MathExpression.TryRead(stream);
            if (expr != null)
            {
                if (requireSemicolon)
                {
                    stream.SkipWhitespace();
                    if (!stream.ExpectAndConsume(';', state))
                        return null;
                }

                return expr;
            }

            state.Restore();
            return null;
        }

        public abstract void Compile(CodeWriter writer, Definitions definitions);
    }
}
