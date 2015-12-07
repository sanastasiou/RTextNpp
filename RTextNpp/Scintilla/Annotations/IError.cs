﻿using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTextNppPlugin.Scintilla.Annotations
{
    internal interface IError
    {
        void OnSettingChanged(object source, Settings.SettingChangedEventArgs e);

        void AddErrors(ErrorListViewModel model, IntPtr scintilla);

        void OnBufferActivated(string file);
    }
}
