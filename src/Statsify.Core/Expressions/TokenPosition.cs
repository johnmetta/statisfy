using System.Diagnostics;

namespace Statsify.Core.Expressions
{
    [DebuggerDisplay("({Line}, {Column})")]
    public struct TokenPosition
    {
        public int Line { get; private set; }

        public int Column { get; private set; }

        public TokenPosition(int line, int column) : this()
        {
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", Line, Column);
        }
    }
}