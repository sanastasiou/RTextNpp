#include "StdAfx.h"
#include "Lexer.h"
#include <string>
#include <locale>

namespace RText
{ 
    //static initializations
    const std::string RTextLexer::BOOLEAN_TRUE = "true";
    const std::string RTextLexer::BOOLEAN_FALSE = "false";

    ILexer* RTextLexer::LexerFactory()
    {
        return new RTextLexer();
    }

    //
    // ILexer
    //

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
        //if (n < _countof(m_WordLists)) {
        //    WordList wlNew;
        //    wlNew.Set(wl);
        //    if (m_WordLists[n] != wlNew) {
        //        m_WordLists[n].Set(wl);
        //        return 0;
        //    }
        //}
        return -1;
    }

    unsigned int RTextLexer::skipDigitsUntil(Accessor & accessor, char const delimiter, unsigned int & currentPos)const
    {
        unsigned int length = 0;
        while (::isdigit(accessor[currentPos]))
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

    bool RTextLexer::identifyFloat(Accessor & accessor, StyleContext const & context, unsigned int & length)const
    {
        length = 0;
        unsigned int currentPos = context.currentPos;
        //regex cannot be used -- thanks Scintilla..
        if (accessor[currentPos] == '+' || accessor[currentPos] == '-' || ::isdigit(accessor[currentPos]))
        {
            bool const isSignFound = (accessor[currentPos] == '+' || accessor[currentPos] == '-');
            length = 1;
            ++currentPos;
            length += skipDigitsUntil(accessor, '.', currentPos);
            if ((length == 1) || (isSignFound && (length == 2)) || !(::isdigit(accessor[currentPos])))
            {
                return false;
            }
            else
            {
                //eat all digits
                while (::isdigit(accessor[currentPos]))
                {
                    ++currentPos;
                    ++length;
                }
                return true;
            }
        }
        return false;
    }

    bool RTextLexer::identifyInt(Accessor & accessor, StyleContext const & context, unsigned int & length)const
    {
        bool aRet = false;
        unsigned int aCurrentPos = context.currentPos;
        length = 0;
        if (context.Match('0', 'x'))
        {
            length = 2;
            aCurrentPos += 2;
            while (::isdigit(accessor[aCurrentPos]) || isHex(accessor[aCurrentPos]))
            {
                ++length;
                ++aCurrentPos;
            }
            if (length > 2)
            {
                aRet = true;
            }
        }
        else if (::isdigit(accessor[aCurrentPos]))
        {
            while (::isdigit(accessor[aCurrentPos++]))
            {
                ++length;                
            }
            aRet = true;
        }
        return aRet;
    }

    bool RTextLexer::identifyQuotedString(Accessor & accessor, StyleContext const & context, unsigned int & length)const
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

    bool RTextLexer::identifyLabel(Accessor & accessor, StyleContext const & context, unsigned int & length)const
    {
        unsigned int aCurrentPos = context.currentPos;
        length                   = 0;
        while (::isalnum(accessor[aCurrentPos]) || (accessor[aCurrentPos] == '_'))
        {
            ++length;
            ++aCurrentPos;
        }
        std::locale loc;
        //skip whitespace
        while (std::isspace(accessor[aCurrentPos], loc))
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

    bool RTextLexer::identifyCharSequence(Accessor & accessor, unsigned int & currentPos, std::string match)const
    {
        for (auto c : match)
        {
            if (accessor.SafeGetCharAt(currentPos) != c) return false;
            ++currentPos;
        }
        return true;
    }

    bool RTextLexer::identifyBoolean(Accessor & accessor, StyleContext const & context, unsigned int & length)const
    {
        unsigned int aCurrentPos = context.currentPos;
        length                   = 0;
        if (accessor[aCurrentPos] == 't')
        {
            //check for rue + non word character [^\w]            
            if (identifyCharSequence(accessor, aCurrentPos, BOOLEAN_TRUE))
            {
                length = 4;                
            }
        }
        else if (accessor[aCurrentPos] == 'f')
        {
            //check for alse + [^\w]
            if (identifyCharSequence(accessor, aCurrentPos, BOOLEAN_FALSE))
            {
                length = 5;
            }
        }
        return (length > 0);
    }

    bool RTextLexer::identifyLineBreak(StyleContext const & context)const
    {
        return (context.Match('\\'));
    }

    bool RTextLexer::identifyName(Accessor & accessor, StyleContext const & context, unsigned int & length)const
    {
        unsigned int aCurrentPos = context.currentPos;
        length                   = 0;     
        if (::isalpha(context.ch) || (context.ch == '_'))
        {
            ++length;
            ++aCurrentPos;
            while (::isalnum(accessor[aCurrentPos]) || (accessor[aCurrentPos] == '_'))
            {
                ++length;
                ++aCurrentPos;
            }
        }       
        return (length > 0);
    }

    void SCI_METHOD RTextLexer::Lex(unsigned int startPos, int length, int initStyle, IDocument* pAccess)
    {
        Accessor styler(pAccess, nullptr);        
        StyleContext context(startPos, length, initStyle, styler);        

        unsigned int aTokenLength = 0;
        bool linebreak            = false;
        bool firstTokenInLine     = true;        

        for (; context.More(); context.Forward())
        {            
            switch (context.state)
            {
            case TokenType_Default:
                //ignore spaces
                if (isWhitespace(context)) continue;
                if (identifyLineBreak(context))
                {
                    linebreak = true;
                }
                //handle new line
                if (context.Match('\n') || context.Match('\r', '\n'))
                {
                    if (linebreak)
                    {
                        linebreak = false;
                    }
                    else
                    {
                        firstTokenInLine = true;
                    }
                    if (context.Match('\r', '\n'))
                    {
                        context.Forward();
                    }
                    context.Forward();
                }                
                if (context.Match('#'))
                {
                    context.SetState(TokenType_Comment);
                    context.Forward();
                    firstTokenInLine = false;
                }
                else if (context.Match('@'))
                {
                    context.SetState(TokenType_Notation);
                    context.Forward();
                    firstTokenInLine = false;
                }
                else if (context.Match('/'))
                {
                    context.SetState(TokenType_Reference);
                    context.Forward();
                    firstTokenInLine = false;
                }
                else if (identifyFloat(styler, context, aTokenLength))
                {
                    context.SetState(TokenType_Float);
                    --aTokenLength;
                    firstTokenInLine = false;
                }
                else if (identifyInt(styler, context, aTokenLength))
                {
                    context.SetState(TokenType_Integer);
                    --aTokenLength;
                    firstTokenInLine = false;
                }
                else if (identifyQuotedString(styler, context, aTokenLength))
                {
                    context.SetState(TokenType_Quoted_string);
                    --aTokenLength;
                    firstTokenInLine = false;
                }
                else if (identifyLabel(styler, context, aTokenLength))
                {
                    context.SetState(TokenType_Label);
                    --aTokenLength;
                    firstTokenInLine = false;
                }
                else if (identifyBoolean(styler, context, aTokenLength))
                {
                    context.SetState(TokenType_Boolean);
                    --aTokenLength;
                    firstTokenInLine = false;
                }
                else if (identifyName(styler, context, aTokenLength))
                {
                    if (firstTokenInLine)
                    {
                        context.SetState(TokenType_Command);
                    }
                    else
                    {
                        context.SetState(TokenType_Identifier);
                    }
                    --aTokenLength;
                    firstTokenInLine = false;
                }
                break;
            case TokenType_Command:
                while (aTokenLength-- > 0)
                {
                    context.SetState(TokenType_Command);
                    context.Forward();
                }
                context.SetState(TokenType_Default);
                break;
            case TokenType_Identifier:
                while (aTokenLength-- > 0)
                {
                    context.SetState(TokenType_Identifier);
                    context.Forward();
                }
                context.SetState(TokenType_Default);
                break;
            case TokenType_Boolean:
                while (aTokenLength-- > 0)
                {
                    context.SetState(TokenType_Boolean);
                    context.Forward();
                }
                context.SetState(TokenType_Default);
                break;
            case TokenType_Label:
                while (aTokenLength-- > 0)
                {
                    context.SetState(TokenType_Label);
                    context.Forward();
                }
                context.SetState(TokenType_Default);
                break;
            case TokenType_Quoted_string:
                while (aTokenLength-- > 0)
                {
                    context.SetState(TokenType_Quoted_string);
                    context.Forward();
                }
                context.SetState(TokenType_Default);
                break;
            case TokenType_Integer:
                while (aTokenLength-- > 0)
                {
                    context.SetState(TokenType_Integer);
                    context.Forward();
                }
                context.SetState(TokenType_Default);
                break;
            case TokenType_Float:
                while (aTokenLength-- > 0)
                {
                    context.SetState(TokenType_Float);
                    context.Forward();
                }
                context.SetState(TokenType_Default);
                break;
            case TokenType_Reference:
                while (::isalnum(styler[context.currentPos]) || context.Match('/') || context.Match('_'))
                {
                    context.SetState(TokenType_Reference);
                    context.Forward();
                }
                context.SetState(TokenType_Default);
                break;
            case TokenType_Notation:
                if (isEndOfLineReached(context))
                {
                    firstTokenInLine = true;
                    context.SetState(TokenType_Default);
                }
                break;
            case TokenType_Comment:
                if (isEndOfLineReached(context))
                {
                    firstTokenInLine = true;
                    context.SetState(TokenType_Default);
                }
                break;
            default:
                context.SetState(TokenType_Default);
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

    void* SCI_METHOD RTextLexer::PrivateCall(int operation, void* pointer)
    {
        return nullptr;
    }

    //
    // Scintilla exports
    //

    int SCI_METHOD GetLexerCount()
    {
        return 1;
    }

    void SCI_METHOD GetLexerName(unsigned int index, char* name, int buflength)
    {
        strncpy(name, "RTextLexer", buflength);
        name[buflength - 1] = '\0';
    }

    void SCI_METHOD GetLexerStatusText(unsigned int index, WCHAR* desc, int buflength)
    {
        wcsncpy(desc, L"RTextLexer skin file", buflength);
        desc[buflength - 1] = L'\0';
    }

    bool RTextLexer::isWhitespace(StyleContext const & context)const
    {
        return (context.Match(' ') || context.Match('\t'));
    }

    bool RTextLexer::isEndOfLineReached(StyleContext const & context)const
    {
        return (context.Match('\r', '\n') || context.Match('\n'));
    }

    LexerFactoryFunction SCI_METHOD GetLexerFactory(unsigned int index)
    {
        return (index == 0) ? RTextLexer::LexerFactory : nullptr;
    }

}	// namespace RText