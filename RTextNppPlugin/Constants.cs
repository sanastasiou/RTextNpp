/**
 * \file    Constants.cs
 *
 * \brief   Implements the constants class.
 */
using System;


namespace ESRLabs.RTextEditor
{
    /**
     * \class   Constants
     *
     * \brief   Constants used throughout RText.NET.
     *
     * \author  Stefanos Anastasiou
     * \date    25.05.2013
     */
    public static class Constants
    {
        public const string guidRTextEditorPluginPkgString             = "116d475d-0f62-4fcf-b7e8-6ca0484922fd";                                               //!< Package UUID
        public const string guidRTextEditorPluginCmdSetString          = "0b78d5d5-f9e4-4428-8bfd-f5d425d8cc52";                                               //!< CmdSet UUID
        public const string guidToolWindowPersistanceString            = "90551dd2-615f-498d-a4dd-1c989724df94";                                               //!< Persistence string UUID
        public const string guidRTextEditorPluginEditorFactoryString   = "b03403ad-57be-4665-ae32-8c1f2c623547";                                               //!< Editor Factory UUID
        public const string guidRTextEditorOptions                     = "8ACA7448-B10D-4534-B6B6-234331DE58A1";                                               //!< Options UUID
        public const string guidNoSolutionExists                       = "ADFC4E64-0397-11D1-9F4E-00A0C911004F";                                               //!< The unique identifier no solution exists VsConstants.UICONTEXT_NoSolution
        public const string guidUIContextSolutionExists                = "f1536ef8-92ec-443c-9ed7-fdadf150da82";                                               //!< The unique identifier user interface context solution exists VsConstants.UICONTEXT_SolutionExists
        public const string guidFindRTextElementCommandString          = "8FBF03D3-A748-4E46-9872-2F01AF23A1DF";                                               //!< Find RText Element command string UUID
        public const string guidLanguageInfoString                     = "D35E1846-2219-4547-BDE5-F5A9F70BAC0B";                                               //!< Language info GUID as string
        public static Guid guidRTextEditorPluginCmdSet        = new Guid(guidRTextEditorPluginCmdSetString);                                                   //!< CmdSet UUID
        public static Guid guidLanguageInfo                   = new Guid(guidLanguageInfoString);                                                              //!< Language info GUID
        public static readonly Guid guidRTextEditorPluginEditorFactory = new Guid(guidRTextEditorPluginEditorFactoryString);                                   //!< Editor Factory UUID
        public static readonly Guid guidFindRTextElementCommand        = new Guid(guidFindRTextElementCommandString);                                          //!< Find RText Element command string UUID
        public const string RTextEditorFactoryGuid                     = "1B37B740-497B-476F-97F4-EF79E9AC244B";
        public const string RTextEditorFactoryPromptForEncodingGuid    = "14CBF72D-927A-4901-BD34-118B3731B981";
        public const string RTextEditorExtension                       = ".atm";
        public static int SYNCHRONOUS_COMMANDS_TIMEOUT                 = 20000;                                                                                //!< 20 seconds timeout for all synchronous commands to backend
        public static int ASYNC_COMMANDS_TIMEOUT                       = 80000;                                                                                //!< Asynchronous commands timeout e.g. for load_model command
        public const string RTextRootKey                               = "RText.NET";                                                                          //!< Registry Root key for options
        public const string CONTENT_TYPE                               = "RText.NET";
        public const string WORKSPACE_TYPE                             = ".rtext";
        public const string VERSION                                    = "1.3.0.1";                                                                            //!< Actual version of plugin
        public const int SEND_TIMEOUT                                  = 2000;                                                                                 //!< The send timeout
        public const int CONNECT_TIMEOUT                               = 10000;                                                                                 //!< indicates timeout for connecting a socket
        public const int BUFFER_SIZE                                   = 1048576;                                                                              //!< The size of the buffer.
        public const string AUTO_COMPLETION_SET_NAME                   = "RTextAutoCompletion";                                                                //!< The auto completion set name that this completion source provides
        public const string AUTO_COMPLETION_ERROR                      = "RTextBackendError";                                                                  //!< Set name when the bakcend gives a null response.
        public const string LEFT_COMMAND_BRACKET                       = "{";                                                                                  //!< Opening bracket for RText.Command
        public const string RIGHT_COMMAND_BRACKET                      = "}";                                                                                  //!< Closing bracket for RText.Command
        public const string ELLIPSIS_TEXT                              = "Click on + to expand collapsed region";                                              //!<the characters that are displayed when the region is collapsed.
        public const int MAX_DISPLAYBLE_TEXT_LENGTH                    = 1000;                                                                                 //!< Max number of characters to be displayed for a collapsed region.
        public const int MAX_CONSUME_PROBLEMS                          = 100;                                                                                  //!< Max number of continuous error tokens than can occur in a single file.
        public const int INITIAL_RESPONSE_TIMEOUT                      = 20000;                                                                                //!< Compensate for when a pc is under heavy load - the backend process may take a while to start
        public const int OUTPUT_POLL_PERIOD                            = 10;                                                                                   //!< Polling period for output stream threads

        #region CommandTypes
        public class Commands
        {
            public const string LOAD_MODEL         = "load_model";                                                                                              //!< Command which loads current model.
            public const string FIND_ELEMENTS      = "find_elements";                                                                                           //!< Command which finds RText elements.
            public const string CONTENT_COMPLETION = "content_complete";
            public const string LINK_TARGETS       = "link_targets";
            public const string CONTEXT_INFO       = "context_info";
            public const string PROGRESS           = "progress";
            public const string ERROR              = "unknown_command_error";
            public const string REQUEST            = "request";
        }
        #endregion

        #region Classification Names
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
        #endregion
    };
}