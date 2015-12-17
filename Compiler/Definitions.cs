using Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Compiler
{
    class Definitions
    {
        public Stack<DataType> DefinedTypes;
        public Stack<FunctionDefinition> DefinedFunctions;
        public Stack<VariableDefinition> DefinedVariables;
        private int LabelsUsed;

        private static VariableDefinition FunctionMark { get; } = new VariableDefinition(null, new Name("function mark"), null);
        private static VariableDefinition BlockMark { get; } = new VariableDefinition(null, new Name("block mark"), null);

        public string CurrentBreakStatement;
        public string CurrentContinueStatement;

        public FunctionDefinition CurrentFunction;

        public Definitions()
        {
            DefinedTypes = new Stack<DataType>();
            DefinedTypes.Push(DataType.UInt);
            DefinedTypes.Push(DataType.UByte);
            DefinedTypes.Push(DataType.Void);

            DefinedVariables = new Stack<VariableDefinition>();
            DefinedFunctions = new Stack<FunctionDefinition>();

            LabelsUsed = 0;

            CurrentBreakStatement = null;
            CurrentContinueStatement = null;
        }

        public void EnterFunction()
        {
            DefinedVariables.Push(FunctionMark);
        }

        public int ExitFunction()
        {
            int totalSize = 0;
            while (DefinedVariables.Peek() != FunctionMark)
            {
                if (DefinedVariables.Peek() != BlockMark)
                    totalSize += DefinedVariables.Peek().Type.GetPaddedSize();
                DefinedVariables.Pop();
            }

            DefinedVariables.Pop();

            return totalSize;
        }

        public int CountFunctionExiting()
        {
            int totalSize = 0;
            foreach (var v in DefinedVariables)
            {
                if (v == FunctionMark)
                    return totalSize;
                if (v != BlockMark)
                    totalSize += v.Type.GetPaddedSize();
            }

            return totalSize;
        }

        public void EnterBlock()
        {
            DefinedVariables.Push(BlockMark);
        }

        public int ExitBlock()
        {
            int totalSize = 0;
            while (DefinedVariables.Peek() != BlockMark)
            {
                totalSize += DefinedVariables.Peek().Type.GetPaddedSize();
                DefinedVariables.Pop();
            }

            DefinedVariables.Pop();

            return totalSize;
        }

        public string AddFunctionDefinition(FunctionDefinition function)
        {
            FunctionDefinition declaration = DefinedFunctions.FirstOrDefault(func => func.Name == function.Name);

            if (declaration == null)
            {
                DefinedFunctions.Push(function);
                return null;
            }
            else if (declaration.Body != null) // trying to overwrite implementation, not declaration
            {
                return $"function {function.Name} is already implemented";
            }
            else if (!function.Parameters.DoesMatch(declaration.Parameters))
            {
                return $"function's {function.Name} implementation parameters do not match parameters of its declaration";
            }
            else if (function.ReturnType != declaration.ReturnType)
            {
                return $"function's {function.Name} implementation return type does not match return type of its declaration";
            }
            else
            {
                // replace declaration with given implementation
                DefinedFunctions = new Stack<FunctionDefinition>(DefinedFunctions.Select(func => func == declaration ? function : func));
                return null;
            }
        }
        
        public void AddVariableDefinition(VariableDefinition variable)
        {
            DefinedVariables.Push(variable);
        }

        public bool IsTypeDefined(DataType type)
        {
            return DefinedTypes.Any(t => t.Name == type.Name);
        }

        public VariableDefinition FindVariable(Name name)
        {
            int ignore;
            return FindVariable(name, out ignore);
        }

        public VariableDefinition FindVariable(Name name, out int stackAddress)
        {
            stackAddress = 0;
            int bp = -1;
            VariableDefinition variable = null;
            foreach (var v in DefinedVariables)
            {
                if (v == BlockMark || v == FunctionMark)
                    continue;

                if (v.Name == name)
                    variable = v;

                if (v.Name == Name.Internal_PreviousBasePtr)
                    bp = 0;

                if (variable != null)
                    stackAddress += v.Type.GetPaddedSize();
                if (bp != -1)
                    bp += v.Type.GetPaddedSize();
            }

            stackAddress = bp - stackAddress;
            return variable;
        }

        public FunctionDefinition FindFunction(Name name)
        {
            return DefinedFunctions.FirstOrDefault(f => f.Name == name);
        }

        public int GetNextLabel()
        {
            return ++LabelsUsed;
        }

        public Tuple<int, int, int> GetCurrentDefinitionCount()
        {
            return Tuple.Create(DefinedTypes.Count, DefinedFunctions.Count, DefinedVariables.Count);
        }

        public void RestoreDefinitions(Tuple<int, int, int> count)
        {
            while (DefinedTypes.Count > count.Item1) DefinedTypes.Pop();
            while (DefinedFunctions.Count > count.Item2) DefinedFunctions.Pop();
            while (DefinedVariables.Count > count.Item3) DefinedVariables.Pop();
        }
    }
}
