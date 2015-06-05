using System;
using RTextNppPlugin.DllExport;

namespace RTextNppPlugin
{
    partial class Plugin
    {
        #region " Fields "
        internal static NppData nppData;
        internal static FuncItems _funcItems = new FuncItems();
        #endregion

        #region " Helper "
        internal static void SetCommand(int index, string commandName, NppFuncItemDelegate functionPointer, string shortcut)
        {
            SetCommand(index, commandName, functionPointer, new ShortcutKey(shortcut), false);
        }

        internal static void SetCommand(int index, string commandName, NppFuncItemDelegate functionPointer)
        {
            SetCommand(index, commandName, functionPointer, new ShortcutKey(), false);
        }
        internal static void SetCommand(int index, string commandName, NppFuncItemDelegate functionPointer, ShortcutKey shortcut)
        {
            SetCommand(index, commandName, functionPointer, shortcut, false);
        }
        internal static void SetCommand(int index, string commandName, NppFuncItemDelegate functionPointer, bool checkOnInit)
        {
            SetCommand(index, commandName, functionPointer, new ShortcutKey(), checkOnInit);
        }
        internal static void SetCommand(int index, string commandName, NppFuncItemDelegate functionPointer, ShortcutKey shortcut, bool checkOnInit)
        {
            FuncItem funcItem = new FuncItem();
            funcItem._cmdID = index;
            funcItem._itemName = commandName;
            if (functionPointer != null)
                funcItem._pFunc = new NppFuncItemDelegate(functionPointer);
            if (shortcut._key != 0)
                funcItem._pShKey = shortcut;
            funcItem._init2Check = checkOnInit;
            _funcItems.Add(funcItem);
        }

        /**
         * Parse as shortcut key.
         *
         * \param   shortcutSpec    Information describing the shortcut.
         *
         * \return  A ShortcutKey.
         * \todo    Provide a function which check against saved settings when assigning keys to the key interceptor hook.         
         */
        internal static ShortcutKey ParseAsShortcutKey(string shortcutSpec)
        {
            //var parts = shortcutSpec.Split(':');

            //string shortcutName = parts[0];
            //string shortcutData = parts[1];

            //try
            //{
            //    var actualData = Config.Shortcuts.GetValue(shortcutName, shortcutData);
            //    return new ShortcutKey(actualData);
            //}
            //catch
            //{
            //    Config.Shortcuts.SetValue(shortcutName, shortcutData);
            //    return new ShortcutKey(shortcutData);
            //}
            return new ShortcutKey();
        }
        #endregion        
    }
}
