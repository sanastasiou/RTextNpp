﻿using System;
using RTextNppPlugin.RText.Parsing;
namespace RTextNppPlugin.WpfControls
{
    interface ILinkTargetsWindow
    {
        void IssueReferenceLinkRequestCommand(Tokenizer.TokenTag aTokenUnderCursor);
    }
}
