// Scintilla source code edit control
/** @file PropSetSimple.h
 ** A basic string to string map.
 **/
// Copyright 1998-2009 by Neil Hodgson <neilh@scintilla.org>
// The License.txt file describes the conditions under which this software may be distributed.
#ifndef PROPSETSIMPLE_H
#define PROPSETSIMPLE_H
#ifdef SCI_NAMESPACE
namespace Scintilla {
#endif
class PropSetSimple {
    void *impl;
    void Set(const char *keyVal);
public:
    PropSetSimple()
    {
    }

    virtual ~PropSetSimple()
    {
    }
    
    void Set(const char *, const char *, int =-1, int =-1) {}
    
    void SetMultiple(const char *) {}
    
    const char *Get(const char *) const { return ""; }
    
    char *Expanded(const char *) const { return nullptr; }
    
    int GetExpanded(const char *, char *) const { return 0; }
    
    int GetInt(const char *, int defaultValue=0) const { return defaultValue; }
};
#ifdef SCI_NAMESPACE
}
#endif
#endif
