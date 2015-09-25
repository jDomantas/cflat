using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Compiler.Expressions
{
    class ParameterList
    {
        public Tuple<DataType, Name>[] Parameters { get; }
        public bool AllNamesDefined { get; }
        public int TotalSize { get; }

        public ParameterList(Tuple<DataType, Name>[] parameters)
        {
            Parameters = parameters;
            AllNamesDefined = parameters.All(t => t.Item2 != null);
            TotalSize = Parameters.Select(t => t.Item1.GetSize()).Sum();
        }
        
        public bool DoesMatch(ParameterList other)
        {
            if (Parameters.Length != other.Parameters.Length)
                 return false;

            for (int i = 0; i < Parameters.Length; i++)
                if (Parameters[i].Item1 != other.Parameters[i].Item1)
                    return false;

            return true;
        }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            str.Append("(");
            for (int i = 0; i < Parameters.Length - 1; i++) {
                str.Append(Parameters[i].Item1.ToString());
                if (Parameters[i].Item2 != null)
                {
                    str.Append(" ");
                    str.Append(Parameters[i].Item2.ToString());
                }
                str.Append(", ");
            }

            if (Parameters.Length > 0)
            {
                str.Append(Parameters[Parameters.Length - 1].Item1.ToString());
                if (Parameters[Parameters.Length - 1].Item2 != null)
                {
                    str.Append(" ");
                    str.Append(Parameters[Parameters.Length - 1].Item2.ToString());
                }
            }

            str.Append(")");
            return str.ToString();
        }

        public static ParameterList TryRead(SymbolStream stream)
        {
            var state = stream.SaveState();

            if (!stream.ExpectAndConsume('(', state))
                return null;

            List<Tuple<DataType, Name>> parameters = new List<Tuple<DataType, Name>>();

            while (true)
            {
                stream.SkipWhitespace();
                if (stream.Peek() == ')')
                {
                    stream.Consume();
                    for (int i = 0; i < parameters.Count; i++)
                        stream.CurrentDefinitions.AddVariableDefinition(new VariableDefinition(parameters[i].Item1, parameters[i].Item2, null));
                    return new ParameterList(parameters.ToArray());
                }
                else
                {
                    DataType type = DataType.TryRead(stream);
                    if (type == null)
                    {
                        state.Restore("invalid type");
                        return null;
                    }

                    Name name = null;
                    if (stream.Peek() != ',' && stream.Peek() != ')') // allow skipping name
                    {
                        name = Name.TryRead(stream);
                        if (name == null)
                        {
                            state.Restore("invalid parameter name");
                            return null;
                        }
                    }

                    parameters.Add(Tuple.Create(type, name));

                    stream.SkipWhitespace();

                    if (stream.Peek() == ',')
                        stream.Consume();
                    else if (stream.Peek() != ')')
                    {
                        state.Restore($"expected ',' or '(', got: '{stream.Peek()}");
                        return null;
                    }
                }
            }
        }
    }
}
