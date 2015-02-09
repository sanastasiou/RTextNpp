using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using RTextNppPlugin.WpfControls;
using RTextNppPlugin.Parsing;
using System.Collections.ObjectModel;
using System.Reflection;

namespace RTextNppPlugin.ViewModels
{
    internal class AutoCompletionViewModel : BindableObject, IDisposable
    {
        internal class Completion
        {
            #region [Interface]
            public Completion(string displayText, string insertionText, string description, Image glyph)
            {
                _displayText   = displayText;
                _insertionText = insertionText;
                _description   = description;
                _glyph         = glyph;
            }

            public Image Glyph { get { return _glyph; } }

            public string DisplayText { get { return _displayText; } }

            public string Description { get { return _description; } }

            public string InsertionText { get { return _insertionText; } }

            public string ImageSource { get { return Assembly.GetExecutingAssembly().FullName + @";RTextNpp/Resources/" + Properties.Resources.CONSTRUCTOR_IMG; } }

            #endregion

            #region [Helpers]
            
            #endregion

            #region [Data Members]

            private readonly string _displayText;
            private readonly string _insertionText;
            private readonly string _description;
            private readonly Image  _glyph;

            #endregion
        }

        #region [Interface]
        public AutoCompletionViewModel()
        {
            _completionList.Add(new Completion("test", "test", "test", Properties.Resources._event));
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public void Filter(string filter)
        {

        }

        public ObservableCollection<Completion> CompletionList
        {
            get
            {
                return _completionList;
            }
        }
        #endregion

        #region [Helpers]

        Image GetImageForToken(RTextTokenTypes token)
        {
            switch (token)
            {
                case RTextTokenTypes.Reference:
                case RTextTokenTypes.Float:
                case RTextTokenTypes.Integer:
                case RTextTokenTypes.QuotedString:
                case RTextTokenTypes.Boolean:
                    return Properties.Resources.field;
                case RTextTokenTypes.Label:
                    return Properties.Resources.label;
                case RTextTokenTypes.Command:
                case RTextTokenTypes.RTextName:
                case RTextTokenTypes.Template:
                    return Properties.Resources.field;
                default:
                    return Properties.Resources.field;
            }
        }

        #endregion

        #region [Data Members]
        private ObservableCollection<Completion> _completionList = new ObservableCollection<Completion>();
        #endregion
    }
}
