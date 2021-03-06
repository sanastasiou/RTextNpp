﻿using RTextNppPlugin.RText;
using RTextNppPlugin.Scintilla;
using RTextNppPlugin.Scintilla.Annotations;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.WpfControls;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace RTextNppPlugin.Forms
{
    [ExcludeFromCodeCoverage]
    partial class ConsoleOutputForm : Form
    {
        internal ConsoleOutputForm(ConnectorManager cmanager, INpp nppHelper, IStyleConfigurationObserver styleObserver, ISettings settings, ILineVisibilityObserver lineVisibilityObserver, IMouseDwellObserver mouseDwellObserver)
        {
            _consoleOutputHost = new ElementHost<WpfControls.ConsoleOutput, ViewModels.ConsoleViewModel>(new ConsoleOutput(cmanager, nppHelper, styleObserver, settings, lineVisibilityObserver, mouseDwellObserver));
            InitializeComponent();
        }
        private System.Windows.Forms.Integration.ElementHost _consoleOutputHost;
    }
}