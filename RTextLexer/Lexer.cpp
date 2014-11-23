#include "StdAfx.h"
#include "Lexer.h"
#include <string>
#include <locale>

namespace RText
{ 
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

    void SCI_METHOD RTextLexer::Lex(unsigned int startPos, int length, int initStyle, IDocument* pAccess)
    {
        Accessor styler(pAccess, nullptr);        
        StyleContext context(startPos, length, initStyle, styler);

        unsigned int aTokenLength = 0;
        for (; context.More(); context.Forward())
        {            
            switch (context.state)
            {
            case TokenType_Default:
                //ignore spaces
                if (isWhitespace(context)) continue;
                if (context.Match('#'))
                {
                    context.SetState(TokenType_Comment);
                    context.Forward();
                }
                else if (context.Match('@'))
                {
                    context.SetState(TokenType_Notation);
                    context.Forward();
                }
                else if (context.Match('/'))
                {
                    context.SetState(TokenType_Reference);
                    context.Forward();
                }
                else if (identifyFloat(styler, context, aTokenLength))
                {
                    context.SetState(TokenType_Float);
                    --aTokenLength;
                }
                else if (identifyInt(styler, context, aTokenLength))
                {
                    context.SetState(TokenType_Integer);
                    --aTokenLength;
                }
                else if (identifyQuotedString(styler, context, aTokenLength))
                {
                    context.SetState(TokenType_Quoted_string);
                    --aTokenLength;
                }
                else if (identifyLabel(styler, context, aTokenLength))
                {
                    context.SetState(TokenType_Label);
                    --aTokenLength;
                }
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
                    context.SetState(TokenType_Default);
                }
                break;
            case TokenType_Comment:
                if (isEndOfLineReached(context))
                {
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
        /*Accessor styler(pAccess, nullptr);

        length += startPos;
        int line = styler.GetLine(startPos);

        for (unsigned int i = startPos, isize = (unsigned int)length; i < isize; ++i)
        {
        if ((styler[i] == '\n') || (i == length - 1))
        {
        int level = (styler.StyleAt(i - 2) == TS_SECTION)
        ? SC_FOLDLEVELBASE | SC_FOLDLEVELHEADERFLAG
        : SC_FOLDLEVELBASE + 1;

        if (level != styler.LevelAt(line))
        {
        styler.SetLevel(line, level);
        }

        ++line;
        }
        }

        styler.Flush();*/
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
        return (context.Match(' ') || context.Match('\r') || context.Match('\r', '\n') || context.Match('\n'));
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