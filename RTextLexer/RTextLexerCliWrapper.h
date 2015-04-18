#pragma once

#include "Lexer.h"

namespace RTextNppPlugin
{
    using namespace RText;
    using namespace System;
    using namespace System::Runtime::InteropServices;

    public ref class RTextLexerCliWrapper
    {
    public:

        delegate ILexer * GetLexerFactoryDelegate();

        IntPtr GetLexerFactory()
        {
            return System::Runtime::InteropServices::Marshal::GetFunctionPointerForDelegate(_lexerFactoryPtr);
        }

        RTextLexerCliWrapper();

    private:
        GetLexerFactoryDelegate ^ _lexerFactoryPtr;
        GCHandle gch;

        ~RTextLexerCliWrapper();
    };

}

