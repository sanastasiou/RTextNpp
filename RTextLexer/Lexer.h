/*
  Copyright (C) 2010-2012 Birunthan Mohanathas <http://poiru.net>

  This program is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

#ifndef RTEXTLEXER_LEXER_H_
#define RTEXTLEXER_LEXER_H_

#include "Scintilla.h"
#include "ILexer.h"
#include "WordList.h"
#include "LexAccessor.h"
#include "Accessor.h"
#include "LexerModule.h"
#include "StyleContext.h"
#include "CharacterSet.h"
#include <regex>

namespace RText {

class RTextLexer final : public ILexer
{
public:

    /**
     * \brief   Destructor.
     */
    virtual ~RTextLexer()
    {
    }

    /**
     * \brief   Creates singleton instance of lexer.
     *
     * \return  null if it fails, else an ILexer*.
     */
    static ILexer* LexerFactory();

    /**
     * \brief   Releases singleton instance of lexer.
     *
     */
    virtual void SCI_METHOD Release();

    /**
     * \brief   Gets the version. Must return lvOriginal for lexer deriving from ILexer.
     *
     * \return  A SCI_METHOD.
     */
    virtual int SCI_METHOD Version() const;


    virtual const char* SCI_METHOD PropertyNames()
    {
        return nullptr;
    }


    virtual int SCI_METHOD PropertyType(const char* name)
    {
        return -1;
    }


    virtual const char* SCI_METHOD DescribeProperty(const char* name)
    {
        return nullptr;
    }


    virtual int SCI_METHOD PropertySet(const char* key, const char* val)
    {
        return -1;
    }

    virtual const char* SCI_METHOD DescribeWordListSets()
    {
        return nullptr;
    }

    virtual int SCI_METHOD WordListSet(int n, const char* wl);

    virtual void SCI_METHOD Lex(unsigned int startPos, int length, int initStyle, IDocument* pAccess);

    virtual void SCI_METHOD Fold(unsigned int startPos, int length, int initStyle, IDocument* pAccess);

    virtual void* SCI_METHOD PrivateCall(int operation, void* pointer);

private:  
    enum TokenType
    {
        TokenType_Default,
        TokenType_Comment,
        TokenType_Notation,
        TokenType_Reference,
        TokenType_Float,
        TokenType_Integer,
        TokenType_Quoted_string,
        TokenType_Boolean,
        TokenType_Label,
        TokenType_Command,
        TokenType_Identifier,
        TokenType_Template,
        TokenType_Error,
        TokenType_Other
    };

    /**
     * \brief   Query if end of line is reached.
     *
     * \param   context The context accessor.
     *
     * \return  true if end of line is reached, false if not.
     */
    bool isEndOfLineReached(StyleContext const & context)const;

    /**
     * \brief   Query if next characters in context are whitespace.
     *
     * \param   context The context.
     *
     * \return  true if whitespace is detected, false if not.
     */
    bool isWhitespace(StyleContext const & context)const;

    bool identifyFloat(Accessor & accessor, StyleContext const & context, unsigned int & length)const;

    unsigned int skipDigitsUntil(Accessor & accessor, char const delimiter, unsigned int & currentPos)const;

    bool identifyInt(Accessor & accessor, StyleContext const & context, unsigned int & length)const;

    bool identifyQuotedString(Accessor & accessor, StyleContext const & context, unsigned int & length)const;

    bool identifyLabel(Accessor & accessor, StyleContext const & context, unsigned int & length)const;

    inline bool isHex(char const c)const
    {
        switch (c)
        {
        case 'a':
        case 'b':
        case 'c':
        case 'd':
        case 'e':
        case 'f':
        case 'A':
        case 'B':
        case 'C':
        case 'D':
        case 'E':
        case 'F':
            return true;
        default:
            return false;
            break;
        }
    }
};

}	// namespace RText

extern "C" __declspec(dllexport) ILexer* getLexer()
{
    return RText::RTextLexer::LexerFactory();
}

#endif
