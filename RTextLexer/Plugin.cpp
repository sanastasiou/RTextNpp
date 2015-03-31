#include "StdAfx.h"
#include "PluginInterface.h"
#include "Version.h"

namespace RText
{
    HWND g_NppWindow = nullptr;

    LRESULT CALLBACK WndProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
    {
        return DefWindowProc(hWnd, uMsg, wParam, lParam);
    }
   
    void About()
    {
        MessageBox(
            g_NppWindow,
            L"External Lexer which provides syntax highlighting for RText based languages.\n\nBy S. Anastasiou.\n\n"
            L"https://github.com/sanastasiou/RTextNpp",
            RTEXTLEXER_TITLE,
            MB_OK);
    }

    //
    // Notepad++ exports
    //
    BOOL isUnicode()
    {
        return TRUE;
    }

    /**
     * Gets the name of plugin which shall appear under Menu/Plugins.
     *
     * \return  The name of the plguin.
     */
    const WCHAR* getName()
    {
        return L"&RTextLexer";
    }

    FuncItem* getFuncsArray(int* count)
    {
        static FuncItem funcItems[] =
        {
            { L"&About...", About, 0, false, nullptr }
        };

        *count = _countof(funcItems);

        return funcItems;
    }

    void setInfo(NppData data)
    {
        g_NppWindow = data._nppHandle;
    }

    void beNotified(SCNotification* scn)
    {
    }

    LRESULT messageProc(UINT uMsg, WPARAM wParam, LPARAM lParam)
    {
        return TRUE;
    }

}
