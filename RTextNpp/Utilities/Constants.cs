/**
 * \file    Constants.cs
 *
 * \brief   Implements the constants class.
 */
using System;


namespace RTextNppPlugin
{
    /**
     * \class   Constants
     *
     * \brief   Constants used throughout RText.NET.
     *
     * \author  Stefanos Anastasiou
     * \date    25.05.2013
     */
    internal static class Constants
    {
        public const string EX_LEXER_CONFIG_FILENAME = "RTextLexer.xml";        //!< External RText lexer configuration file.
        public const double FORM_INTERVAL_REFRESH = 1000.0;                     //!< Defines an interval in ms, after which a form should be redrawn.
        public const string CONSOLE_OUTPUT_SETTING_KEY = "IsConsoleWindowOpen"; //!< Settings key for the visibility of the console output.
        public const string DEFAULT_EXTENSION = ".atm";                         //!< Default automate extension.
        public static int SYNCHRONOUS_COMMANDS_TIMEOUT = 20000;                 //!< 20 seconds timeout for all synchronous commands to backend
        public static int ASYNC_COMMANDS_TIMEOUT = 80000;                       //!< Asynchronous commands timeout e.g. for load_model command
        public const string WORKSPACE_TYPE = ".rtext";                          //!< Automate workspace settings file.
        public const string VERSION = "1.3.0.1";                                //!< Actual version of plugin
        public const int SEND_TIMEOUT = 2000;                                   //!< The send timeout
        public const int CONNECT_TIMEOUT = 10000;                               //!< indicates timeout for connecting a socket
        public const int BUFFER_SIZE = 1048576;                                 //!< The size of the buffer.
        public const string AUTO_COMPLETION_SET_NAME = "RTextAutoCompletion";   //!< The auto completion set name that this completion source provides
        public const string AUTO_COMPLETION_ERROR = "RTextBackendError";        //!< Set name when the bakcend gives a null response.
        public const string LEFT_COMMAND_BRACKET = "{";                         //!< Opening bracket for RText.Command
        public const string RIGHT_COMMAND_BRACKET = "}";                        //!< Closing bracket for RText.Command
        public const int MAX_CONSUME_PROBLEMS = 100;                            //!< Max number of continuous error tokens than can occur in a single file.
        public const int INITIAL_RESPONSE_TIMEOUT = 20000;                      //!< Compensate for when a pc is under heavy load - the backend process may take a while to start
        public const int OUTPUT_POLL_PERIOD = 10;                               //!< Polling period for output stream threads

        #region NppMenuCommand
        public enum NppMenuCommands : int
        {
            ConsoleWindow = 0,
            Options = 1,
            Outline = 2,
            About = 3
        }
        #endregion

        #region CommandTypes
        public class Commands
        {
            public const string LOAD_MODEL = "load_model";                      //!< Command which loads current model.
            public const string FIND_ELEMENTS = "find_elements";                //!< Command which finds RText elements.
            public const string CONTENT_COMPLETION = "content_complete";        //!< Command to fetch auto complete options.
            public const string LINK_TARGETS = "link_targets";                  //!< Command to fetch references.
            public const string CONTEXT_INFO = "context_info";                  //!< Command to fetch context information.
            public const string PROGRESS = "progress";                          //!< Command which displays current loading progress.
            public const string ERROR = "unknown_command_error";                //!< Erroneous command.
            public const string REQUEST = "request";                            //!< Request command.
        }
        #endregion

        #region Classification Names
        public class Classifications
        {
            public const string RTEXT_COMMENT = "RText.Comment";
            public const string RTEXT_NOTATION = "RText.Notation";
            public const string RTEXT_REFERENCE = "RText.Reference";
            public const string RTEXT_FLOAT = "RText.Float";
            public const string RTEXT_INTEGER = "RText.Integer";
            public const string RTEXT_QUOTED_STRING = "RText.QuotedString";
            public const string RTEXT_BOOLEAN = "RText.Boolean";
            public const string RTEXT_LABEL = "RText.Label";
            public const string RTEXT_COMMAND = "RText.Command";
            public const string RTEXT_IDENTIFIER = "RText.RTextName";
            public const string RTEXT_TEMPLATE = "RText.Template";
            public const string RTEXT_ERROR = "RText.Error";
            public const string RTEXT_OTHER = "RText.Other";
            public const string RTEXT_SPACE = "Rtext.Space";
        }
        #endregion
    };
}