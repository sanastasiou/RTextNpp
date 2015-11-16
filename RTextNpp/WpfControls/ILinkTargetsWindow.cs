using System;
using RTextNppPlugin.RText.Parsing;
using System.Collections.Generic;
using RTextNppPlugin.ViewModels;
namespace RTextNppPlugin.WpfControls
{
    interface ILinkTargetsWindow
    {
        void IssueReferenceLinkRequestCommand(Tokenizer.TokenTag aTokenUnderCursor);
        bool IsMouseInsidedWindow();
        void Hide();
        void Show();
        bool IsVisible { get; }
        IList<LinkTargetModel> Targets { get; }
    }
}