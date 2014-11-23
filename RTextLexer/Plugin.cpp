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

#include "StdAfx.h"
#include "..\headers\PluginInterface.h"
#include "Version.h"

namespace RText
{
    const int WM_QUERY_RAINMETER = WM_APP + 1000;
    const int RAINMETER_QUERY_ID_SKINS_PATH = 4101;

    HWND g_RainmeterWindow = nullptr;
    HWND g_NppWindow = nullptr;
    WCHAR g_SkinsPath[MAX_PATH] = { 0 };

    LRESULT CALLBACK WndProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
    {
        if (uMsg == WM_COPYDATA)
        {
            COPYDATASTRUCT* cds = (COPYDATASTRUCT*)lParam;
            if (cds->dwData == RAINMETER_QUERY_ID_SKINS_PATH)
            {
                wcsncpy(g_SkinsPath, (const WCHAR*)cds->lpData, _countof(g_SkinsPath));
                g_SkinsPath[_countof(g_SkinsPath) - 1] = L'\0';
            }

            return TRUE;
        }

        return DefWindowProc(hWnd, uMsg, wParam, lParam);
    }

    bool GetRainmeter()
    {
        if (!g_RainmeterWindow || !IsWindow(g_RainmeterWindow))
        {
            HWND trayWindow = FindWindow(L"RainmeterTrayClass", nullptr);
            HWND meterWindow = FindWindow(L"RainmeterMeterWindow", nullptr);
            if (trayWindow && meterWindow)
            {
                // Create window to recieve WM_COPYDATA from Rainmeter
                HWND wnd = CreateWindow(
                    L"STATIC",
                    L"",
                    WS_DISABLED,
                    CW_USEDEFAULT, CW_USEDEFAULT,
                    CW_USEDEFAULT, CW_USEDEFAULT,
                    nullptr,
                    nullptr,
                    nullptr,
                    nullptr);

                if (wnd)
                {
                    SetWindowLongPtr(wnd, GWLP_WNDPROC, (LONG_PTR)WndProc);

                    SendMessage(trayWindow, WM_QUERY_RAINMETER, RAINMETER_QUERY_ID_SKINS_PATH, (LPARAM)wnd);
                    DestroyWindow(wnd);

                    if (*g_SkinsPath)
                    {
                        g_RainmeterWindow = meterWindow;
                        return true;
                    }
                }
            }
        }
        else
        {
            return true;
        }

        return false;
    }

    void RefreshSkin()
    {
        if (!GetRainmeter()) return;

        WCHAR currentPath[MAX_PATH];
        BOOL ret = SendMessage(g_NppWindow, NPPM_GETFULLCURRENTPATH, MAX_PATH, (LPARAM)&currentPath);

        if (ret)
        {
            const size_t skinsPathLen = wcslen(g_SkinsPath);
            const size_t currentPathLen = wcslen(currentPath);

            // Make sure the file is in the skins folder and that extension is .ini
            if (wcsncmp(g_SkinsPath, currentPath, skinsPathLen) == 0 &&
                _wcsicmp(&currentPath[currentPathLen - 4], L".atm") == 0)
            {
                WCHAR* relativePath = &currentPath[skinsPathLen];
                WCHAR* pos = wcsrchr(relativePath, L'\\');
                if (pos)
                {
                    relativePath[pos - relativePath] = L'\0';
                    WCHAR buffer[512];
                    const int len = _snwprintf(
                        buffer, _countof(buffer), L"!Refresh \"%s\"", relativePath);
                    buffer[_countof(buffer) - 1] = L'\0';

                    COPYDATASTRUCT cds;
                    cds.dwData = 1;
                    cds.cbData = (DWORD)(len + 1) * sizeof(WCHAR);
                    cds.lpData = (void*)buffer;
                    SendMessage(g_RainmeterWindow, WM_COPYDATA, 0, (LPARAM)&cds);
                }
            }
        }
    }

    void RefreshAll()
    {
        if (!GetRainmeter()) return;

        COPYDATASTRUCT cds;
        cds.dwData = 1;
        cds.cbData = sizeof(L"!Refresh *");
        cds.lpData = L"!Refresh *";
        SendMessage(g_RainmeterWindow, WM_COPYDATA, 0, (LPARAM)&cds);
    }

    void About()
    {
        MessageBox(
            g_NppWindow,
            L"By S. Anastasiou.\n"
            L"www.github.com/RText",
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
        return NULL;//L"&RText++";
    }

    FuncItem* getFuncsArray(int* count)
    {
        static FuncItem funcItems[] =
        {
            { L"Refresh skin", RefreshSkin, 0, false, nullptr },
            { L"Refresh all", RefreshAll, 0, false, nullptr },
            { L"&About...", About, 0, false, nullptr }
        };

        *count = _countof(funcItems);

        return funcItems;
        //return static_cast<FuncItem*>(g_NppRouter.nppGetFuncsArray(count));
    }

    void setInfo(NppData data)
    {
        g_NppWindow = data._nppHandle;
        //forward this to main plugin
    }

    void beNotified(SCNotification* scn)
    {
    }

    LRESULT messageProc(UINT uMsg, WPARAM wParam, LPARAM lParam)
    {
        return TRUE;
    }

}
