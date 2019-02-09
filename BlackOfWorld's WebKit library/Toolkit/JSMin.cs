using System;
using System.Collections.Generic;
using System.Text;

namespace BlackOfWorld.Webkit.Toolkit
{
    class JSMin
    {
        // JSMin converted from C to C#
        const int EOF = -1;
        static string JSCode;
        static int nowAt = 0;
        static string final;
        static int theA;
        static int theB;
        static int theLookahead = EOF;
        static int theX = EOF;
        static int theY = EOF;
        public static string MinifyJSCode(string code)
        {
            JSCode = code;
            jsmin();
            return final;
        }
        static bool isAlphanum(int c)
        {
            return ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ||
                    (c >= 'A' && c <= 'Z') || c == '_' || c == '$' || c == '\\' ||
                    c > 126);
        }
        static int get()
        {
            int c = theLookahead;
            theLookahead = EOF;
            if (c == EOF && nowAt != JSCode.Length)
            {
                c = JSCode[nowAt];
                nowAt = nowAt + 1;
            }
            if (c >= ' ' || c == '\n' || c == EOF)
            {
                return c;
            }
            if (c == '\r')
            {
                return '\n';
            }
            return ' ';
        }
        static int peek()
        {
            theLookahead = get();
            return theLookahead;
        }
        static int next()
        {
            int c = get();
            if (c == '/')
            {
                switch (peek())
                {
                    case '/':
                        {
                            for (; ; )
                            {
                                c = get();
                                if (c <= '\n')
                                {
                                    break;
                                }
                            }
                            break;
                        }
                    case '*':
                        {
                            get();
                            while (c != ' ')
                            {
                                switch (get())
                                {
                                    case '*':
                                        {
                                            if (peek() == '/')
                                            {
                                                get();
                                                c = ' ';
                                            }
                                            break;
                                        }
                                    case EOF:
                                        {
                                            throw new Exception("Error: JSMIN Unterminated comment.\n");
                                        }
                                }
                            }
                            break;
                        }

                }
            }
            theY = theX;
            theX = c;
            return c;
        }
        static void action(int d)
        {
            if (d <= 1)
            {
                final += (char)theA;
                if (
                    (theY == '\n' || theY == ' ') &&
                    (theA == '+' || theA == '-' || theA == '*' || theA == '/') &&
                    (theB == '+' || theB == '-' || theB == '*' || theB == '/')
                )
                {
                    final += (char)theY;
                }
            }

            if (d <= 2)
            {
                theA = theB;
                if (theA == '\'' || theA == '"' || theA == '`')
                {
                    for (; ; )
                    {
                        final += (char)theA;
                        theA = get();
                        if (theA == theB)
                        {
                            break;
                        }

                        if (theA == '\\')
                        {
                            final += (char)theA;
                            theA = get();
                        }
                        if (theA == EOF)
                        {
                            throw new FormatException("Unterminated Regular Expression literal.");
                        }
                    }
                }
            }
            if (d <= 3)
            {
                theB = next();
                if (theB == '/' && (
                    theA == '(' || theA == ',' || theA == '=' || theA == ':' ||
                    theA == '[' || theA == '!' || theA == '&' || theA == '|' ||
                    theA == '?' || theA == '+' || theA == '-' || theA == '~' ||
                    theA == '*' || theA == '/' || theA == '{' || theA == '\n'
                ))
                {
                    final += (char)theA;
                    if (theA == '/' || theA == '*')
                    {
                        final += ' ';
                    }
                    final += (char)theB;

                    for (; ; )
                    {
                        theA = get();
                        if (theA == '[')
                        {
                            for (; ; )
                            {
                                final += (char)theA;
                                theA = get();
                                if (theA == ']')
                                {
                                    break;
                                }
                                if (theA == '\\')
                                {
                                    final += (char)theA;
                                    theA = get();
                                }
                                if (theA == EOF)
                                {
                                    throw new Exception("Unterminated set in Regular Expression literal.");
                                }
                            }
                        }
                        else if (theA == '/')
                        {
                            switch (peek())
                            {
                                case '/':
                                case '*':
                                    throw new Exception("Unterminated Regular Expression literal.");
                            }
                            break;
                        }
                        else if (theA == '\\')
                        {
                            final += (char)theA;
                            theA = get();
                        }
                        if (theA == EOF)
                        {
                            throw new Exception("Unterminated Regular Expression literal.");
                        }
                        final += (char)theA;
                    }
                    theB = next();
                }
            }
        }

        static void jsmin()
        {
            theA = '\n';
            action(3);
            while (theA != EOF)
            {
                switch (theA)
                {
                    case ' ':
                        {
                            if (isAlphanum(theB))
                            {
                                action(1);
                            }
                            else
                            {
                                action(2);
                            }
                            break;
                        }
                    case '\n':
                        {
                            switch (theB)
                            {
                                case '{':
                                case '[':
                                case '(':
                                case '+':
                                case '-':
                                    {
                                        action(1);
                                        break;
                                    }
                                case ' ':
                                    {
                                        action(3);
                                        break;
                                    }
                                default:
                                    {
                                        if (isAlphanum(theB))
                                        {
                                            action(1);
                                        }
                                        else
                                        {
                                            action(2);
                                        }
                                        break;
                                    }
                            }
                            break;
                        }
                    default:
                        {
                            switch (theB)
                            {
                                case ' ':
                                    {
                                        if (isAlphanum(theA))
                                        {
                                            action(1);
                                            break;
                                        }
                                        action(3);
                                        break;
                                    }
                                case '\n':
                                    {
                                        switch (theA)
                                        {
                                            case '}':
                                            case ']':
                                            case ')':
                                            case '+':
                                            case '-':
                                            case '"':
                                            case '\'':
                                                {
                                                    action(1);
                                                    break;
                                                }
                                            default:
                                                {
                                                    if (isAlphanum(theA))
                                                    {
                                                        action(1);
                                                    }
                                                    else
                                                    {
                                                        action(3);
                                                    }
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        action(1);
                                        break;
                                    }
                            }
                            break;
                        }
                }
            }
        }
    }
}