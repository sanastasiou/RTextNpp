#include "RTextLexerCliWrapper.h"

namespace RTextNppPlugin
{
    RTextLexerCliWrapper::RTextLexerCliWrapper()
    {
        _lexerFactoryPtr = gcnew GetLexerFactoryDelegate(&RTextLexer::LexerFactory);
        gch = GCHandle::Alloc(_lexerFactoryPtr);
    }


    RTextLexerCliWrapper::~RTextLexerCliWrapper()
    {
        gch.Free();
    }
}
