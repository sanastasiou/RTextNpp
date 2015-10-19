/**
 * \file    Constants.cs
 *
 * \brief   Implements the constants class.
 */


namespace RTextNppPlugin
{
    /**
     * \class   Constants
     *
     * \brief   Constants used throughout RText.NET.
     *
     */
    public static class Constants
    {
        public const string EX_LEXER_CONFIG_FILENAME = "RTextNpp.xml";          //!< External RText lexer configuration file.
        public const double FORM_INTERVAL_REFRESH = 500.0;                      //!< Defines an interval in ms, after which a form should be redrawn.
        public const string DEFAULT_EXTENSION = ".atm";                         //!< Default automate extension.
        public static int SYNCHRONOUS_COMMANDS_TIMEOUT = 20000;                 //!< 20 seconds timeout for all synchronous commands to backend
        public static int ASYNC_COMMANDS_TIMEOUT = 80000;                       //!< Asynchronous commands timeout e.g. for load_model command
        public const string WORKSPACE_TYPE = ".rtext";                          //!< Automate workspace settings file.
        public const string VERSION = "1.3.0.1";                                //!< Actual version of plugin
        public const int SEND_TIMEOUT = 2000;                                   //!< The send timeout
        public const int CONNECT_TIMEOUT = 10000;                               //!< indicates timeout for connecting a socket
        public const int BUFFER_SIZE = 16384;                                   //!< The size of the buffer.
        public const string AUTO_COMPLETION_SET_NAME = "RTextAutoCompletion";   //!< The auto completion set name that this completion source provides
        public const string AUTO_COMPLETION_ERROR = "RTextBackendError";        //!< Set name when the bakcend gives a null response.
        public const string LEFT_COMMAND_BRACKET = "{";                         //!< Opening bracket for RText.Command
        public const string RIGHT_COMMAND_BRACKET = "}";                        //!< Closing bracket for RText.Command
        public const int MAX_CONSUME_PROBLEMS = 100;                            //!< Max number of continuous error tokens than can occur in a single file.
        public const int INITIAL_RESPONSE_TIMEOUT = 20000;                      //!< Compensate for when a pc is under heavy load - the backend process may take a while to start. 20s timeout.
        public const int OUTPUT_POLL_PERIOD = 10;                               //!< Polling period for output stream threads
        public const string GENERAL_CHANNEL = "General";                        //!< General output channel.
        public const string DEBUG_CHANNEL = "Debug";                            //!< Debug channel - disabled on release mode.
        public const string NPP_BACKUP_DIR = @"\Notepad++\backup";              //!< Notepad ++ back up directory.
        public const char BACKSPACE = '\b';                                     //!< Backspace char.
        public const char SPACE = ' ';                                          //!< Space char.
        public const char TAB = '\t';                                           //!< Tab char.
        public const char COMMA = ',';                                          //!< Comma char.
        public const int BACKEND_COLUMN_OFFSET = 1;                             //!< Backend columns start at one, but the tokenizer starts at 0.
        public const string PluginName = "RTextNpp";                            //!< Plugin name.
        public const double MAX_AUXILIARY_WINDOWS_HEIGHT = 400.0;               //!< Max height of auto completion and link reference windows.
        public const double MAX_AUXILIARY_WINDOWS_WIDTH  = 600.0;               //!< Max width of auto completion and link reference windows.
        public const double MIN_AUXILIARY_WINDOWS_WIDTH = 300.0;                //!< Max width of auto completion and link reference windows.
        public const double MAX_AUTO_COMPLETION_TOOLTIP_WIDTH = 300.0;          //!< Max width of auto completion tooltip.        
        public const double ZOOM_FACTOR = 0.08;                                 //!< Relation between actual zoom and scintilla zoom factor for various plugin windows.
        public const double INITIAL_WIDTH_LINK_REFERENCE_LABELS = 70.0;         //!< Initial width of link reference labels in row details template. This is used to align all labels.
        public const double MAX_WIDTH_LINK_REFERENCE_LABELS = 600.0;            //!< Initial width of link reference labels in row details template. This is used to align all labels.

        #region [Error Severity Strings]
        public const string SEVERITY_DEBUG = "debug";
        public const string SEVERITY_INFO = "info";
        public const string SEVERITY_WARNING = "warn";
        public const string SEVERITY_ERROR = "error";
        public const string SEVERITY_FATAL = "fatal";
        #endregion

        #region [NppMenuCommand]
        public enum NppMenuCommands : int
        {
            ConsoleWindow  = 0,
            Options        = 1,
            AutoCompletion = 2,
            Outline        = 3,
            About          = 4
        }
        #endregion

        #region [CommandTypes]
        public class Commands
        {
            public const string LOAD_MODEL         = "load_model";            //!< Command which loads current model.
            public const string FIND_ELEMENTS      = "find_elements";         //!< Command which finds RText elements.
            public const string CONTENT_COMPLETION = "content_complete";      //!< Command to fetch auto complete options.
            public const string LINK_TARGETS       = "link_targets";          //!< Command to fetch references.
            public const string CONTEXT_INFO       = "context_info";          //!< Command to fetch context information.
            public const string PROGRESS           = "progress";              //!< Command which displays current loading progress.
            public const string ERROR              = "unknown_command_error"; //!< Erroneous command.
            public const string REQUEST            = "request";               //!< Request command.
            public const string STOP               = "stop";                  //!< Stops backend.
        }
        #endregion

        #region [Classification Names]
        public class Classifications
        {
            public const string RTEXT_COMMENT       = "RText.Comment";
            public const string RTEXT_NOTATION      = "RText.Notation";
            public const string RTEXT_REFERENCE     = "RText.Reference";
            public const string RTEXT_FLOAT         = "RText.Float";
            public const string RTEXT_INTEGER       = "RText.Integer";
            public const string RTEXT_QUOTED_STRING = "RText.QuotedString";
            public const string RTEXT_BOOLEAN       = "RText.Boolean";
            public const string RTEXT_LABEL         = "RText.Label";
            public const string RTEXT_COMMAND       = "RText.Command";
            public const string RTEXT_IDENTIFIER    = "RText.RTextName";
            public const string RTEXT_TEMPLATE      = "RText.Template";
            public const string RTEXT_ERROR         = "RText.Error";
            public const string RTEXT_OTHER         = "RText.Other";
            public const string RTEXT_SPACE         = "Rtext.Space";
        }

        public class Wordstyles
        {
            public const string WORDSTYLES_ELEMENT_NAME = "WordsStyle";
            public const string DEFAULT                 = "DEFAULT"               ;
            public const string COMMENT                 = "COMMENT"               ;
            public const string NOTATION                = "NOTATION"              ;
            public const string REFERENCE               = "REFERENCE"             ;
            public const string FLOAT                   = "FLOAT"                 ;
            public const string INTEGER                 = "INTEGER"               ;
            public const string QUOTED_STRING           = "QUOTED_STRING"         ;
            public const string BOOLEAN                 = "BOOLEAN"               ;
            public const string LABEL                   = "LABEL"                 ;
            public const string COMMAND                 = "COMMAND"               ;
            public const string IDENTIFIER              = "IDENTIFIER"            ;
            public const string TEMPLATE                = "TEMPLATE"              ;
            public const string SPACE                   = "SPACE"                 ;
            public const string OTHER                   = "OTHER"                 ;
            public const string ERROR                   = "ERROR"                 ;
            public const string REFERENCE_LINK          = "REFERENCE_LINK"        ;
            public const string ANNOTATION_DEBUG        = "ANNOTATION_DEBUG"      ;
            public const string ANNOTATION_INFO         = "ANNOTATION_INFO"       ;
            public const string ANNOTATION_WARNING      = "ANNOTATION_WARNING"    ;
            public const string ANNOTATION_ERROR        = "ANNOTATION_ERROR"      ;
            public const string ANNOTATION_FATAL_ERROR  = "ANNOTATION_FATAL_ERROR";
            public const string ERROR_OVERVIEW          = "ERROR_OVERVIEW"        ;

            public const string STYLE_ATTRIBUTE_NAME      = "name";
            public const string STYLE_ATTRIBUTE_BGCOLOR   = "bgColor";
            public const string STYLE_ATTRIBUTE_FGCOLOR   = "fgColor";
            public const string STYLE_ATTRIBUTE_STYLEID   = "styleID";
            public const string STYLE_ATTRIBUTE_FONTNAME  = "fontName";
            public const string STYLE_ATTRIBUTE_FONTSTYLE = "fontStyle";
            public const string STYLE_ATTRIBUTE_FONTSIZE  = "fontSize";
        }
        #endregion
    };
}