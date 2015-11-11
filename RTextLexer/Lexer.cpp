#include "Lexer.h"
#include <string>

namespace RText
{
    //static initializations
    const std::string RTextLexer::BOOLEAN_TRUE  = "true";
    const std::string RTextLexer::BOOLEAN_FALSE = "false";
    ILexer* RTextLexer::LexerFactory()
    {
        return new RTextLexer();
    }
    RTextLexer::RTextLexer() : _firstTokenInLine(true)
    {
    }

    RTextLexer:: ~RTextLexer()
    {
    }

    void SCI_METHOD RTextLexer::Release()
    {
        delete this;
    }
    int SCI_METHOD RTextLexer::Version() const
    {
        return lvOriginal;
    }
    int SCI_METHOD RTextLexer::WordListSet(int n, const char *wl)
    {
        return -1;
    }
    unsigned int RTextLexer::SkipDigitsUntil(Accessor & accessor, char const delimiter, unsigned int & currentPos)const
    {
        unsigned int length = 0;
        while (::iswdigit(accessor[currentPos]))
        {
            ++currentPos;
            ++length;
        }
        if (accessor[currentPos] == delimiter)
        {
            ++currentPos;
            return ++length;
        }
        else
        {
            return 0;
        }
    }
    bool RTextLexer::IdentifyFloat(Accessor & accessor, StyleContext const & context, unsigned int & length)const
    {
        length = 0;
        unsigned int currentPos = context.currentPos;
        char a = accessor[currentPos];
        //regex cannot be used -- thanks Scintilla..
        if (accessor[currentPos] == '+' || accessor[currentPos] == '-' || ::iswdigit(accessor[currentPos]))
        {
            bool const isSignFound = (accessor[currentPos] == '+' || accessor[currentPos] == '-');
            length = 1;
            ++currentPos;
            length += SkipDigitsUntil(accessor, '.', currentPos);
            if ((length == 1) || (isSignFound && (length == 2)) || !(::iswdigit(accessor[currentPos])))
            {
                return false;
            }
            else
            {
                //eat all digits
                while (::iswdigit(accessor[currentPos]))
                {
                    ++currentPos;
                    ++length;
                }
                return true;
            }
        }
        return false;
    }
    bool RTextLexer::IdentifyInt(Accessor & accessor, StyleContext const & context, unsigned int & length)const
    {
        bool aRet = false;
        unsigned int aCurrentPos = context.currentPos;
        length = 0;
        if (context.Match('0', 'x'))
        {
            length = 2;
            aCurrentPos += 2;
            while (::iswdigit(accessor[aCurrentPos]) || IsHex(accessor[aCurrentPos]))
            {
                ++length;
                ++aCurrentPos;
            }
            if (length > 2)
            {
                aRet = true;
            }
        }
        else if (::iswdigit(accessor[aCurrentPos]))
        {
            while (::iswdigit(accessor[aCurrentPos++]))
            {
                ++length;
            }
            aRet = true;
        }
        return aRet;
    }
    bool RTextLexer::IdentifyQuotedString(Accessor & accessor, StyleContext const & context, unsigned int & length)const
    {
        bool aRet                = false;
        unsigned int aCurrentPos = context.currentPos;
        length                   = 0;
        char delimiter = '"';
        if (context.Match('\'') || context.Match('\"'))
        {
            delimiter    = accessor[aCurrentPos];
            length      += 1;
            bool consume = true;
            do
            {
                ++aCurrentPos;
                //if string doesn't end till EOF this is an error
                if (((accessor[aCurrentPos] != delimiter) || (accessor[aCurrentPos - 1] == '\\')) && (accessor[aCurrentPos] != '\n'))
                {
                    ++length;
                }
                else
                {
                    ++length;
                    consume = false;
                }
            } while (consume);
            //only return true if delimiter is found at end of quoted string, emptry string is also OK
            return (accessor[aCurrentPos] == delimiter) && (length >= 2);
        }
        return false;
    }
    bool RTextLexer::IdentifyLabel(Accessor & accessor, StyleContext const & context, unsigned int & length)const
    {
        unsigned int aCurrentPos = context.currentPos;
        length                   = 0;
        while (::iswalnum(accessor[aCurrentPos]) || (accessor[aCurrentPos] == '_'))
        {
            ++length;
            ++aCurrentPos;
        }
        //skip whitespace
        while (::iswspace(accessor[aCurrentPos]))
        {
            ++aCurrentPos;
        }
        if (accessor[aCurrentPos] == ':' && (length > 0))
        {
            ++length;
            return true;
        }
        return false;
    }
    bool RTextLexer::IdentifyCharSequence(Accessor & accessor, unsigned int & currentPos, std::string match)const
    {
        for (auto c : match)
        {
            if (accessor.SafeGetCharAt(currentPos) != c) return false;
            ++currentPos;
        }
        return true;
    }
    bool RTextLexer::IdentifyBoolean(Accessor & accessor, StyleContext const & context, unsigned int & length)const
    {
        unsigned int aCurrentPos = context.currentPos;
        length                   = 0;
        if (accessor[aCurrentPos] == 't')
        {
            //check for rue + non word character [^\w]
            if (IdentifyCharSequence(accessor, aCurrentPos, BOOLEAN_TRUE))
            {
                length = 4;
            }
        }
        else if (accessor[aCurrentPos] == 'f')
        {
            //check for alse + [^\w]
            if (IdentifyCharSequence(accessor, aCurrentPos, BOOLEAN_FALSE))
            {
                length = 5;
            }
        }
        return (length > 0);
    }
    bool RTextLexer::IdentifyName(Accessor & accessor, StyleContext const & context, unsigned int & length)const
    {
        unsigned int aCurrentPos = context.currentPos - 1;
        length                   = 0;
        if (::iswalpha(context.ch) || (context.ch == '_'))
        {
            while (::iswalnum(accessor[aCurrentPos + 1]) || (accessor[aCurrentPos + 1] == '_'))
            {
                ++length;
                ++aCurrentPos;
            }
        }
        return (length > 0);
    }
    bool RTextLexer::IsLineExtended(int startPos, char const * const buffer)const
    {
        //no reason to check previous characters
        if (startPos == 0)
        {
            return false;
        }
        //go back till we find \,[
        while (startPos-- >= 0)
        {
            if (::iswspace(buffer[startPos]))
            {
                continue;
            }
            else
            {
        //IgnoreWhitespace(startPos, buffer);
        //not space
        if (IsLineBreakChar(buffer[startPos]))
        {
            //we have a line break, but we need to take care the fact that we may be inside a labeled child list - in that case this is no line break...
            if (buffer[startPos] == '[')
            {
                //go back ignoring whitespaces and check for : , if found this is a label
                while (startPos-- >= 0)
                {
                    if (::iswspace(buffer[startPos]) || buffer[startPos] == '\\')
                    {
                        continue;
                    }
                    else
                    {
                        //end of label detected - this is not a line break - go till start of line - label must be the only element there
                        if (buffer[startPos] != ':')
                        {
                            return true;
                        }
                        else
                        {
                            //go back till next token - first consume label
                            while (startPos-- >= 0)
                            {
                                if (::iswalpha(buffer[startPos]))
                                {
                                    continue;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            while (::iswspace(buffer[startPos]) || buffer[startPos] == '\\')
                            {
                                --startPos;
                                continue;
                            }
                            //label after a comma, so this is an extended line - label is not the first element
                            return (buffer[startPos] == ',');
                        }
                    }
                }
            }
            return true;
        }
        else
        {
            //some other char -> no line break!
            return false;
        }
            }
        }
        return false;
    }
    void SCI_METHOD RTextLexer::Lex(unsigned int startPos, int length, int initStyle, IDocument* pAccess)
    {
        Accessor styler(pAccess, nullptr);
        StyleContext context(startPos, length, initStyle, styler);
        unsigned int aTokenLength = 0;
        _firstTokenInLine = true;
        while(context.More())
        {
            switch (context.state)
            {
            case TokenType_Default:
                aTokenLength = 0;
                //ignore spaces
                while (IsWhitespace(context))
                {
                    context.SetState(TokenType_Space);
                    context.Forward();
                    context.SetState(TokenType_Default);
                }
                //handle new line
                if (context.Match('\n') || context.Match('\r', '\n'))
                {
                    _firstTokenInLine = true;
                    if (context.Match('\r', '\n'))
                    {
                        context.Forward();
                    }
                    context.Forward();
                    continue;
                }
                if (context.Match('#'))
                {
                    context.SetState(TokenType_Comment);
                }
                else if (context.Match('@'))
                {
                    context.SetState(TokenType_Notation);
                }
                else if (context.Match('/'))
                {
                    context.SetState(TokenType_Reference);
                }
                else if (IdentifyFloat(styler, context, aTokenLength))
                {
                    context.SetState(TokenType_Float);
                }
                else if (IdentifyInt(styler, context, aTokenLength))
                {
                    context.SetState(TokenType_Integer);
                }
                else if (IdentifyQuotedString(styler, context, aTokenLength))
                {
                    context.SetState(TokenType_Quoted_string);
                }
                else if (IdentifyLabel(styler, context, aTokenLength))
                {
                    context.SetState(TokenType_Label);
                    _firstTokenInLine = false;
                }
                else if (IdentifyBoolean(styler, context, aTokenLength))
                {
                    context.SetState(TokenType_Boolean);
                }
                else if (IdentifyName(styler, context, aTokenLength))
                {
                    bool const isExtended = IsLineExtended(context.currentPos, pAccess->BufferPointer());
                    if (_firstTokenInLine && !isExtended)
                    {
                        context.SetState(TokenType_Command);
                        _firstTokenInLine = false;
                    }
                    else
                    {
                        context.SetState(TokenType_Identifier);
                    }
                }
                else if (context.Match(',') || context.Match('{') || context.Match('}') || context.Match('[') || context.Match(']') || context.Match('\\'))
                {
                    context.SetState(TokenType_Other);
                    context.Forward();
                    context.SetState(TokenType_Default);
                }
                else
                {
                    context.SetState(TokenType_Error);
                    //don't care about this char/token
                    context.Forward();
                    context.SetState(TokenType_Default);
                }
                break;
            case TokenType_Command:
            case TokenType_Identifier:
            case TokenType_Boolean:
            case TokenType_Label:
            case TokenType_Quoted_string:
            case TokenType_Integer:
            case TokenType_Float:
                while (aTokenLength > 0)
                {
                    --aTokenLength;
                    context.Forward();
                }
                context.SetState(TokenType_Default);
                break;
            case TokenType_Reference:
                while (::iswalnum(context.ch) || context.Match('/') || context.Match('_'))
                {
                    context.Forward();
                }
                context.SetState(TokenType_Default);
                break;
            case TokenType_Notation:
            case TokenType_Comment:
                if (IsEndOfLineReached(context))
                {
                    context.SetState(TokenType_Default);
                }
                else
                {
                    context.Forward();
                }
                break;
            }
        }
        context.Complete();
    }
    void SCI_METHOD RTextLexer::Fold(unsigned int startPos, int length, int initStyle, IDocument* pAccess)
    {
        LexAccessor styler(pAccess);
        unsigned int endPos = startPos + length;
        char chNext = styler[startPos];
        int lineCurrent = styler.GetLine(startPos);
        int levelPrev = styler.LevelAt(lineCurrent) & SC_FOLDLEVELNUMBERMASK;
        int levelCurrent = levelPrev;
        char ch;
        bool atEOL;
        for (unsigned int i = startPos; i < endPos; i++)
        {
            ch = chNext;
            chNext = styler.SafeGetCharAt(i + 1);
            atEOL = ((ch == '\r' && chNext != '\n') || (ch == '\n'));
            if (ch == '{')
            {
                levelCurrent++;
            }
            if (ch == '}')
            {
                levelCurrent--;
            }
            if (atEOL || (i == (endPos - 1)))
            {
                int lev = levelPrev;
                if (levelCurrent > levelPrev)
                {
                    lev |= SC_FOLDLEVELHEADERFLAG;
                }
                if (lev != styler.LevelAt(lineCurrent))
                {
                    styler.SetLevel(lineCurrent, lev);
                }
                lineCurrent++;
                levelPrev = levelCurrent;
            }
        }
    }
    bool RTextLexer::IsWhitespace(StyleContext const & context)const
    {
        return (!context.atLineEnd && context.Match(' ') || context.Match('\t'));
    }
    bool RTextLexer::IsEndOfLineReached(StyleContext const & context)const
    {
        return (context.atLineEnd);
    }
    void* SCI_METHOD RTextLexer::PrivateCall(int operation, void* pointer)
    {
        return nullptr;
    }
}    // namespace RText
