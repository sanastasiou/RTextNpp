#ifndef RTEXTLEXER_LEXER_H__
#define RTEXTLEXER_LEXER_H__

#include "Scintilla.h"
#include "ILexer.h"
#include "WordList.h"
#include "LexAccessor.h"
#include "Accessor.h"
#include "LexerModule.h"
#include "StyleContext.h"
#include "CharacterSet.h"
#include <string>

namespace RText
{
    class RTextLexer final : public ILexer
    {
    public:
        virtual ~RTextLexer();
        
        /**
         * \brief   Creates singleton instance of lexer.
         *
         * \return  null if it fails, else an ILexer*.
         */
        static ILexer* LexerFactory();
        
        virtual void SCI_METHOD Release();
        
        /**
         * \brief   Gets the version. Must return lvOriginal for lexer deriving from ILexer.
         *
         */
        virtual int SCI_METHOD Version() const;
        
        virtual const char* SCI_METHOD PropertyNames();
        
        virtual int SCI_METHOD PropertyType(const char*);
        
        virtual const char* SCI_METHOD DescribeProperty(const char*);
        
        virtual int SCI_METHOD PropertySet(const char*, const char*);
        
        virtual const char* SCI_METHOD DescribeWordListSets();
        
        virtual int SCI_METHOD WordListSet(int, const char*);
        
        virtual void SCI_METHOD Lex(unsigned int startPos, int length, int initStyle, IDocument* pAccess);
        
        virtual void SCI_METHOD Fold(unsigned int startPos, int length, int initStyle, IDocument* pAccess);
       
        virtual void* SCI_METHOD PrivateCall(int operation, void* pointer);
    private:
        static const std::string BOOLEAN_TRUE;        
        static const std::string BOOLEAN_FALSE;
        
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
            TokenType_Space,
            TokenType_Other,
            TokenType_Error
        };

        bool _firstTokenInLine;
        
        /**
         * \brief   Query if end of line is reached.
         *
         * \param   context The context accessor.
         *
         * \return  true if end of line is reached, false if not.
         */
        bool IsEndOfLineReached(StyleContext const & context)const;
        
        /**
         * \brief   Query if next characters in context are whitespace.
         *
         * \param   context The context.
         *
         * \return  true if whitespace is detected, false if not.
         */
        bool IsWhitespace(StyleContext const & context)const;
        
        /**
         * Identify float.
         *
         * \param [in,out]  accessor    The accessor.
         * \param   context             The context.
         * \param [in,out]  length      The length of the identified token.
         *
         * \return  true if a floating point literal is identified, false if it fails.
         */
        bool IdentifyFloat(Accessor & accessor, StyleContext const & context, unsigned int & length)const;
        
        /**
        * Skips digits from currentPos until delimiter char.
        *
        * \param [in,out]  accessor    The accessor.
        * \param delimiter             The delimiter. When this is encountered skipping is stopped.
        * \param [in,out]  currentPos  Current position of the accessor.
        *
        * \return  Length of digits skipped, including the delimiter. 0 if delimiter is not found.
        */
        unsigned int SkipDigitsUntil(Accessor & accessor, char const delimiter, unsigned int & currentPos)const;
        
        /**
        * Identify integer.
        *
        * \param [in,out]  accessor    The accessor.
        * \param   context             The context.
        * \param [in,out]  length      The length of the identified token.
        *
        * \return  true if an integer literal is identified, false if it fails.
        */
        bool IdentifyInt(Accessor & accessor, StyleContext const & context, unsigned int & length)const;
        
        /**
        * Identify quoted string.
        *
        * \param [in,out]  accessor    The accessor.
        * \param   context             The context.
        * \param [in,out]  length      The length of the identified token.
        *
        * \return  true if a quoted string is identified, false if it fails.
        */
        bool IdentifyQuotedString(Accessor & accessor, StyleContext const & context, unsigned int & length)const;
        
        /**
        * Identify label.
        *
        * \param [in,out]  accessor    The accessor.
        * \param   context             The context.
        * \param [in,out]  length      The length of the identified token.
        *
        * \return  true if a label is identified, false if it fails.
        */
        bool IdentifyLabel(Accessor & accessor, StyleContext const & context, unsigned int & length)const;
        
        bool IdentifyName(Accessor & accessor, StyleContext const & context, unsigned int & length)const;
        
        bool IdentifyBoolean(Accessor & accessor, StyleContext const & context, unsigned int & length)const;
        
        bool IsHex(int c)const;
        
        bool IdentifyCharSequence(Accessor & accessor, unsigned int & currentPos, std::string match)const;
        
        RTextLexer();
        
        bool IsLineExtended(int startPos, char const * const buffer, LexAccessor & styler)const;
        
        bool IsLineBreakChar(char const c)const;
    
        void IgnoreWhitespace(int startPos, char const * const buffer)const;
    
        int MaskActive(int const style)const;
    };

    inline bool RTextLexer::IsHex(int c)const
    {
        c = ::towlower(c);
        switch (c)
        {
        case 'a':
        case 'b':
        case 'c':
        case 'd':
        case 'e':
        case 'f':
            return true;
        default:
            return false;
        }
    }

    inline bool RTextLexer::IsLineBreakChar(char const c)const
    {
        return (c == '\\' || c == ',' || c == '[');
    }

    inline void RTextLexer::IgnoreWhitespace(int startPos, char const * const buffer)const
    {
        if (startPos != 0)
        {
            if (::iswspace(buffer[startPos]))
            {
                while (startPos-- >= 0)
                {
                    if (::iswspace(buffer[startPos]))
                    {
                        continue;
                    }
                }
            }
        }
    }

    inline int RTextLexer::MaskActive(int const style)const
    {
        return style & ~0x40;
    }

    inline RTextLexer::~RTextLexer()
    {
    }

    inline RTextLexer::RTextLexer() : _firstTokenInLine(true)
    {
    }

    inline ILexer* RTextLexer::LexerFactory()
    {
        return new RTextLexer();
    }

    inline void* SCI_METHOD RTextLexer::PrivateCall(int, void*)
    {
        return nullptr;
    }

    inline bool RTextLexer::IsWhitespace(StyleContext const & context)const
    {
        return (!context.atLineEnd && context.Match(' ') || context.Match('\t'));
    }
    
    inline bool RTextLexer::IsEndOfLineReached(StyleContext const & context)const
    {
        return (context.atLineEnd);
    }

    inline void SCI_METHOD RTextLexer::Release()
    {
        ::delete this;
    }

    inline int SCI_METHOD RTextLexer::Version() const
    {
        return lvOriginal;
    }

    inline const char* SCI_METHOD RTextLexer::PropertyNames()
    {
        return nullptr;
    }

    inline int SCI_METHOD RTextLexer::PropertyType(const char*)
    {
        return -1;
    }

    inline const char* SCI_METHOD RTextLexer::DescribeProperty(const char*)
    {
        return nullptr;
    }

    inline int SCI_METHOD RTextLexer::PropertySet(const char*, const char*)
    {
        return -1;
    }

    inline const char* SCI_METHOD RTextLexer::DescribeWordListSets()
    {
        return nullptr;
    }

    inline int SCI_METHOD RTextLexer::WordListSet(int, const char*)
    {
        return -1;
    }
} // namespace RText
#endif // ifndef RTEXTLEXER_LEXER_H__
