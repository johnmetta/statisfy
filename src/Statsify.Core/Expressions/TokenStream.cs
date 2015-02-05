using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Statsify.Core.Expressions
{
    public class TokenStream
    {
        private readonly IEnumerator<Token> tokens;
        private Token lookahead;

        public Token Lookahead
        {
            get
            {
                if(lookahead == null)
                {
                    if(!tokens.MoveNext())
                        return null;

                    lookahead = tokens.Current;
                } // if

                return lookahead;
            }
        }

        public bool Any
        {
            get { return Lookahead != null; }
        }

        [DebuggerStepThrough]
        public TokenStream(IEnumerable<Token> tokens)
        {
            this.tokens = tokens.GetEnumerator();
        }

        public Token Read(params TokenType[] types)
        {
            Token token;

            if(lookahead != null)
            {
                token = lookahead;
                lookahead = null;
            } // if
            else
            {
                if(!tokens.MoveNext())
                    return null;
                
                token = tokens.Current;
            } // else

            if(types != null && types.Length > 0 && !types.Contains(token.Type))
                throw new Exception(string.Format("Unexpected '{0}' at {1}", token.Type, token.Position));

            return token;
        }

        public Token Read(string lexeme)
        {
            var token = Read();
            if(!token.Lexeme.Equals(lexeme, StringComparison.InvariantCulture))
                throw new Exception(string.Format("Unexpected '{0}' at {1}; expected '{2}'", token.Lexeme, token.Position, lexeme));

            return token;
        }
    }
}