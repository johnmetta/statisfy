namespace Statsify.Core.Expressions
{
    public class Argument
    {
        public string Name { get; private set; }

        public Expression Value { get; private set; }

        public Argument(string name, Expression value)
        {
            Name = name;
            Value = value;
        }
    }
}