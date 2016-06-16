namespace Cinteros.XTB.BulkDataUpdater
{
    /// <summary>
    /// Structure holding constants
    /// </summary>
    internal struct Constants
    {
        #region Internal Fields

        /// <summary>
        /// Encoded large icon for plugins
        /// </summary>
        internal const string B64_IMAGE_LARGE = "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";
        
        /// <summary>
        /// Encoded small icon for plugins
        /// </summary>
        internal const string B64_IMAGE_SMALL = "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";
        
        internal const int U_HEADER_MAINWIDTH = 200;

        /// <summary>
        /// Text for solution that is not available
        /// </summary>
        internal const string U_ITEM_NA = "N/A";

        /// <summary>
        /// Default solution unique name
        /// </summary>
        internal const string U_SOLUTION_DEFAULT = "Default";

        #endregion Internal Fields

        #region Internal Structs

        /// <summary>
        /// Structure holding CRM related constants
        /// </summary>
        internal struct Crm
        {
            #region Internal Structs

            /// <summary>
            /// Structure holding CRM Attributes related constants
            /// </summary>
            internal struct Attributes
            {
                #region Internal Fields

                internal const string CULTURE = "culture";
                internal const string EVENT_HANDLER = "eventhandler";
                internal const string FRIENDLY_NAME = "friendlyname";
                internal const string IS_HIDDEN = "ishidden";
                internal const string IS_MANAGED = "ismanaged";
                internal const string IS_VISIBLE = "isvisible";
                internal const string ISOLATION_MODE = "isolationmode";
                internal const string NAME = "name";
                internal const string PLUGIN_TYPE_ID = "plugintypeid";
                internal const string PUBLIC_KEY_TOKEN = "publickeytoken";
                internal const string SOLUTION_ID = "solutionid";
                internal const string STATE_CODE = "statecode";
                internal const string STATUS_CODE = "statuscode";
                internal const string UNIQUE_NAME = "uniquename";
                internal const string PRIMARY_OBJECT_TYPE_CODE = "primaryobjecttypecode";
                internal const string VERSION = "version";

                #endregion Internal Fields
            }

            /// <summary>
            /// Structure holding CRM Entities related constantns
            /// </summary>
            internal struct Entities
            {
                #region Internal Fields

                /// <summary>
                /// Name of the 'pluginassembly' entity
                /// </summary>
                internal const string PLUGIN_ASSEMBLY = "pluginassembly";

                /// <summary>
                /// Name of the 'plugintype' entity
                /// </summary>
                internal const string PLUGIN_TYPE = "plugintype";

                /// <summary>
                /// Name of the 'sdkmessageprocessingstep' entity
                /// </summary>
                internal const string PROCESSING_STEP = "sdkmessageprocessingstep";

                /// <summary>
                /// Name of the 'sdkmessage' entity
                /// </summary>
                internal const string MESSAGE = "sdkmessage";

                /// <summary>
                /// Name of the 'sdkmessagefilter' entity
                /// </summary>
                internal const string MESSAGE_FILTER = "sdkmessagefilter";

                /// <summary>
                /// Name of the 'solution' entity
                /// </summary>
                internal const string SOLUTION = "solution";

                #endregion Internal Fields
            }

            #endregion Internal Structs
        }

        /// <summary>
        /// Structure holding UI related constants
        /// </summary>
        internal struct UI
        {
            #region Internal Fields

            /// <summary>
            /// Name of the plugin toolstrip
            /// </summary>
            internal const string MENU = "tsMenu";

            #endregion Internal Fields

            #region Internal Structs

            /// <summary>
            /// Structure holding UI Buttons related constants
            /// </summary>
            internal struct Buttons
            {
                #region Internal Fields

                /// <summary>
                /// Name of toolstrip's Back button
                /// </summary>
                internal const string BACK = "tsbBack";

                /// <summary>
                /// Name of toolstrip's Compare button
                /// </summary>
                internal const string COMPARE = "tsbCompare";

                /// <summary>
                /// Name of toolstrip's Open drop down button
                /// </summary>
                internal const string OPEN = "tsddOpen";

                internal const string OPEN_CURRENT_CONNECTION = "tsmiCurrentConnection";

                internal const string OPEN_REFERENCE_FILE = "tsmiReferenceFile";

                /// <summary>
                /// Name of toolstrip's Save button
                /// </summary>
                internal const string SAVE = "tsbSave";

                #endregion Internal Fields
            }

            /// <summary>
            /// Structure holding UI Labels related constants
            /// </summary>
            internal struct Labels
            {
                #region Internal Fields

                /// <summary>
                /// Text for solutions group
                /// </summary>
                internal const string ASSEMBLIES = "Assemblies";

                /// <summary>
                /// Text for solutions group
                /// </summary>
                internal const string SOLUTIONS = "Solutions";

                #endregion Internal Fields
            }

            #endregion Internal Structs
        }

        /// <summary>
        /// Structure holding XML related constants
        /// </summary>
        internal struct Xml
        {
            #region Internal Fields

            internal const string ASSEMBLIES = "assemblies";
            internal const string ASSEMBLY = "assembly";
            internal const string FRIENDLY_NAME = "friendly-name";
            internal const string SOLUTION = "solution";
            internal const string SOLUTIONS = "solutions";
            internal const string STEP = "step";
            internal const string STEPS = "steps";
            internal const string UNIQUE_NAME = "unique-name";
            internal const string VERSION = "version";
            internal const string PLUGIN = "plugin";
            internal const string PLUGINS = "plugins";
            internal const string ID = "id";
            internal const string IMAGE = "image";

            #endregion Internal Fields
        }

        #endregion Internal Structs
    }
}