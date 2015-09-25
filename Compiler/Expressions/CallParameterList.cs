using System.Collections.Generic;
using System.Text;

namespace Compiler.Expressions
{
    class CallParameterList
    {
        public MathExpression[] Parameters { get; }

        public CallParameterList(MathExpression[] parameters)
        {
            Parameters = parameters;
        }

        public string CheckTypes(ParameterList parameters)
        {
            if (parameters.Parameters.Length != Parameters.Length)
                return $"mismacthed call parameter count, expected {parameters.Parameters.Length}, got {Parameters.Length}";

            for (int i = 0; i < Parameters.Length; i++)
            {
                if (Parameters[i].Type != parameters.Parameters[i].Item1)
                {
                    if (!Parameters[i].Type.CanImplicitlyCastTo(parameters.Parameters[i].Item1))
                        return $"call parameter {i + 1}, can't cast {Parameters[i].Type} to {parameters.Parameters[i].Item1}";
                    else
                        Parameters[i] = new TypeCast(Parameters[i], parameters.Parameters[i].Item1);
                }
            }

            return null;
        }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            str.Append("(");
            for (int i = 0; i < Parameters.Length - 1; i++)
            {
                str.Append(Parameters[i].ToString());
                str.Append(", ");
            }

            if (Parameters.Length > 0)
                str.Append(Parameters[Parameters.Length - 1].ToString());

            str.Append(")");
            return str.ToString();
        }

        public static CallParameterList TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            if (!stream.ExpectAndConsume('(', state))
                return null;

            List<MathExpression> parameters = new List<MathExpression>();

            while (true)
            {
                stream.SkipWhitespace();
                if (stream.Peek() == ')')
                {
                    stream.Consume();
                    return new CallParameterList(parameters.ToArray());
                }
                else
                {
                    MathExpression param = MathExpression.TryRead(stream);
                    if (param == null)
                    {
                        state.Restore("invalid call parameter");
                        return null;
                    }

                    parameters.Add(param);

                    stream.SkipWhitespace();

                    if (stream.Peek() == ',')
                        stream.Consume();
                    else if (!stream.ExpectAndConsume(')', state))
                        return null;
                    else
                        return new CallParameterList(parameters.ToArray());
                }
            }
        }
    }
}
