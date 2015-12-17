using Compiler.Expressions.MathOperations;

namespace Compiler.Expressions
{
    class WhileLoop : Sentence
    {
        public MathOperation Condition { get; }
        public Block Body { get; }

        public WhileLoop(MathOperation condition, Block body)
        {
            Condition = condition;
            Body = body;
        }
        
        public override string ToString()
        {
            return $"while ({Condition}) {{ ... }}";
        }

        public static WhileLoop TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            if (!stream.ExpectAndConsumeString("while", state))
                return null;

            stream.SkipWhitespace();
            if (!stream.ExpectAndConsume('(', state))
                return null;

            MathExpression condition = MathOperation.TryRead(stream);
            if (condition == null)
            {
                state.Restore("invalid condition");
                return null;
            }

            MathOperation cond = condition as MathOperation;
            if (cond == null)
            {
                state.Restore("invalid condition");
                return null;
            }

            stream.SkipWhitespace();
            if (!stream.ExpectAndConsume(')', state))
                return null;

            if (condition.Type.PointerDepth > 0)
                condition = new CompareOperation(MathOperation.Op.NotEqual, condition, new LiteralValue(0, condition.Type));
            if (condition.Type == DataType.UInt)
                condition = new CompareOperation(MathOperation.Op.NotEqual, condition, new LiteralValue(0, DataType.UInt));
            if (condition.Type == DataType.UByte)
                condition = new CompareOperation(MathOperation.Op.NotEqual, condition, new LiteralValue(0, DataType.UByte));

            if (condition.Type != DataType.Flags)
            {
                state.Restore($"invalid condition type, expected: flags, got: {condition.Type}");
                return null;
            }

            string prevBreak = stream.CurrentDefinitions.CurrentBreakStatement;
            string prevCont = stream.CurrentDefinitions.CurrentContinueStatement;
            stream.CurrentDefinitions.CurrentBreakStatement = "";
            stream.CurrentDefinitions.CurrentContinueStatement = "";
            
            stream.SkipWhitespace();
            Block body = Block.TryRead(stream);

            stream.CurrentDefinitions.CurrentBreakStatement = prevBreak;
            stream.CurrentDefinitions.CurrentContinueStatement = prevCont;

            if (body == null)
            {
                state.Restore("invalid while body");
                return null;
            }

            return new WhileLoop(cond, body);
        }

        public override void Compile(CodeWriter writer, Definitions definitions)
        {
            int label = definitions.GetNextLabel();

            writer.WriteLine($"; {ToString()}");
            writer.WriteLine($"label_{label}_while_condition:");
            Condition.Compile(writer, definitions);

            writer.WriteLine($"{IfClause.SelectInstruction(Condition.Operation)} label_{label}_while_body");
            writer.WriteLine($"jmp label_{label}_while_end");
            writer.WriteLine($"label_{label}_while_body:");

            string prevBreak = definitions.CurrentBreakStatement;
            string prevCont = definitions.CurrentContinueStatement;
            definitions.CurrentBreakStatement = $"label_{label}_while_end";
            definitions.CurrentContinueStatement = $"label_{label}_while_condition";

            Body.Compile(writer, definitions);

            definitions.CurrentBreakStatement = prevBreak;
            definitions.CurrentContinueStatement = prevCont;

            writer.WriteLine($"jmp label_{label}_while_condition");
            writer.WriteLine($"label_{label}_while_end:");
        }
    }
}
