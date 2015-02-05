using System;
using System.Diagnostics;

namespace Statsify.Core.Expressions
{
    [DebuggerDisplay("{Type} {Lexeme} @{Position}")]
    public class Token : IEquatable<Token>
    {
        public string Lexeme { get; set; }

        public TokenPosition Position { get; set; }

        public TokenType Type { get; set; }

        public Token()
        {
        }

        public Token(string lexeme, TokenType type)
        {
            Lexeme = lexeme;
            Type = type;
        }

        public bool Equals(Token other)
        {
            if(ReferenceEquals(null, other)) return false;
            if(ReferenceEquals(this, other)) return true;
            return string.Equals(Lexeme, other.Lexeme) && Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj)) return false;
            if(ReferenceEquals(this, obj)) return true;
            if(obj.GetType() != this.GetType()) return false;
            return Equals((Token)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Lexeme != null ? Lexeme.GetHashCode() : 0) * 397) ^ (int)Type;
            }
        }

        public static bool operator ==(Token left, Token right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Token left, Token right)
        {
            return !Equals(left, right);
        }
    }
}