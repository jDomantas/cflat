using Compiler.Expressions.MathOperations;
using System.Collections.Generic;

namespace Compiler.Expressions
{
    abstract class MathOperation : MathExpression
    {
        public enum Op
        {
            Unknown = 0,
            Assign = 10,

            Less = 20,
            Greater = 21,
            GreaterEqual = 22,
            LessEqual = 23,
            NotEqual = 24,
            Equal = 25,

            ShiftLeft = 40,
            ShiftRight = 41,
            Or = 42,
            And = 43,
            Xor = 44,

            Add = 60,
            Subtract = 61,
            Multiply = 80,
            Divide = 81,
            Modulo = 82,
        }

        public Op Operation { get; private set; }
        public MathExpression LHS { get; protected set; }
        public MathExpression RHS { get; protected set; }

        public MathOperation(Op operation, DataType type, MathExpression lhs, MathExpression rhs) : base(type, false)
        {
            Operation = operation;
            LHS = lhs;
            RHS = rhs;
        }
        
        public override string ToString()
        {
            switch (Operation)
            {
                case Op.Add:
                    return $"({LHS} + {RHS})";
                case Op.Subtract:
                    return $"({LHS} - {RHS})";
                case Op.Multiply:
                    return $"({LHS} * {RHS})";
                case Op.Divide:
                    return $"({LHS} / {RHS})";
                case Op.Modulo:
                    return $"({LHS} % {RHS})";
                case Op.Equal:
                    return $"({LHS} == {RHS})";
                case Op.Greater:
                    return $"({LHS} > {RHS})";
                case Op.GreaterEqual:
                    return $"({LHS} >= {RHS})";
                case Op.Less:
                    return $"({LHS} < {RHS})";
                case Op.LessEqual:
                    return $"({LHS} <= {RHS})";
                case Op.NotEqual:
                    return $"({LHS} != {RHS})";
                case Op.Assign:
                    return $"({LHS} = {RHS})";
                case Op.ShiftRight:
                    return $"({LHS} >> {RHS})";
                case Op.ShiftLeft:
                    return $"({LHS} << {RHS})";
                case Op.And:
                    return $"({LHS} & {RHS})";
                case Op.Or:
                    return $"({LHS} | {RHS})";
                case Op.Xor:
                    return $"({LHS} ^ {RHS})";
                default:
                    return $"Unknown_op";
            }
        }

        public new static MathExpression TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();
            
            Stack<MathExpression> values = new Stack<MathExpression>();
            Stack<Op> operators = new Stack<Op>();

            MathExpression value = TryReadOperand(stream);
            if (value == null)
            {
                state.Restore("invalid operation");
                return null;
            }

            values.Push(value);

            int valuesDone = 1;

            while (true)
            {
                stream.SkipWhitespace();
                Op op = TryReadOperator(stream);

                while (operators.Count > 0 && IsHigherPrecedence(operators.Peek(), op))
                {
                    var rhs = values.Pop();
                    var lhs = values.Pop();
                    var operation = operators.Pop();
                    MathExpression res;
                    string err = TryCreateOperation(lhs, rhs, operation, out res);
                    if (err != null)
                    {
                        state.Restore(err);
                        return null;
                    }
                    values.Push(res);
                }

                if (op == Op.Unknown)
                    break;

                operators.Push(op);

                stream.SkipWhitespace();

                value = TryReadOperand(stream);
                if (value == null)
                {
                    state.Restore("invalid operand");
                    return null;
                }

                values.Push(value);
                valuesDone++;
            }
            
            return values.Peek();
        }

        private static MathExpression TryReadOperand(SymbolStream stream)
        {
            var state = stream.SaveState();

            TypeCast cast = TypeCast.TryRead(stream);
            if (cast != null)
                return cast;

            if (stream.TestNext('('))
            {
                stream.SkipWhitespace();
                MathExpression value = MathExpression.TryRead(stream);
                stream.SkipWhitespace();
                if (!stream.ExpectAndConsume(')', state))
                    return null;

                return value;
            }
            else
                return Value.TryRead(stream);
        }

        private static bool IsHigherPrecedence(Op left, Op right)
        {
            if (left == Op.Assign && right == Op.Assign)
                return false;

            int valLeft = (int)left / 10;
            int valRight = (int)right / 10;
            return valLeft >= valRight;
        }

        private static Op TryReadOperator(SymbolStream stream)
        {
            var state = stream.SaveState();

            if (stream.TestNext('+'))
                return Op.Add;

            if (stream.TestNext('-'))
                return Op.Subtract;

            if (stream.TestNext('*'))
                return Op.Multiply;

            if (stream.TestNext('/'))
                return Op.Divide;

            if (stream.TestNext('%'))
                return Op.Modulo;
            
            if (stream.TestNext('>'))
            {
                if (stream.TestNext('='))
                    return Op.GreaterEqual;
                else if (stream.TestNext('>'))
                    return Op.ShiftRight;
                else
                    return Op.Greater;
            }

            if (stream.TestNext('<'))
            {
                if (stream.TestNext('='))
                    return Op.LessEqual;
                else if (stream.TestNext('<'))
                    return Op.ShiftRight;
                else
                    return Op.Less;
            }

            if (stream.TestNext('='))
            {
                if (stream.TestNext('='))
                    return Op.Equal;
                else
                    return Op.Assign;
            }

            if (stream.TestNext('!'))
            {
                if (stream.TestNext('='))
                    return Op.NotEqual;

                state.Restore("invalid operator");
                return Op.Unknown;
            }

            if (stream.TestNext('|')) return Op.Or;
            if (stream.TestNext('^')) return Op.Xor;
            if (stream.TestNext('&')) return Op.And;

            state.Restore("invalid operator");
            return Op.Unknown;
        }

        private static string TryCreateOperation(MathExpression lhs, MathExpression rhs, Op op, out MathExpression operation)
        {
            switch (op)
            {
                case Op.Add: return AddOperation.TryCreate(lhs, rhs, out operation);
                case Op.Subtract: return SubtractOperation.TryCreate(lhs, rhs, out operation);
                case Op.Divide: return DivideOperation.TryCreate(lhs, rhs, out operation);
                case Op.Modulo: return ModuloOperation.TryCreate(lhs, rhs, out operation);
                case Op.Multiply: return MultiplyOperation.TryCreate(lhs, rhs, out operation);

                case Op.Greater: 
                case Op.GreaterEqual: 
                case Op.Less: 
                case Op.LessEqual:
                case Op.Equal:
                case Op.NotEqual: return CompareOperation.TryCreate(op, lhs, rhs, out operation);
                    
                case Op.Assign: return AssignOperation.TryCreate(lhs, rhs, out operation);

                case Op.Xor:
                case Op.And:
                case Op.Or: return LogicalOperation.TryCreate(op, lhs, rhs, out operation);

                case Op.ShiftLeft:
                case Op.ShiftRight: return ShiftOperation.TryCreate(op, lhs, rhs, out operation);

                default:
                    operation = null;
                    return $"unknown operation: {op}";
            }
        }
    }
}
