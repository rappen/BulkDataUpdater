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
        internal const string B64_IMAGE_SMALL = "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAMAAABEpIrGAAAAFXRFWHRDcmVhdGlvbiBUaW1lAAfgBhAGDS/KOs/LAAAAB3RJTUUH4QsWDygev2CPIQAAAAlwSFlzAAAK8AAACvABQqw0mAAAAPNQTFRF9/f/1t73pb3nhKXWe5zWzt7v3uf3IVq1AEKtCEKtKWO9OWvGQnPGCEq1GFK1a5TO5+f3a4zOxtbv9///////jKXeUoTOtcbnEEq1nLXelK3eY4zO3sbv1q3n3rXn58bv9+f3c5TWjK3epUrGtWPO1rXnrb3n7973vXvW//f/rcbn1qXnzpTevXPWzpze7/f/rVrOMWu9SnvG587vKVq9IUK1GEK1vc7vWoTOQmvGOUK1Slq9UkK9SlK95+/37+//vb3n9+/3tWvW3r3vEFK1SkK9MUK1KUK1rVLOUnvG7+/3MWO9tc7nxozexoTe79b3ztb3aX3qwgAAAAF0Uk5TAEDm2GYAAAH6SURBVHjabZNrW9pAEIU3ySY7G5LIZTEXQIUFtGBBUimBWqxijai9/P9f00lCSoKcb/uc99mZnT1DyF7TRdBttU6ai3tyTPXWEzjWdmsxsKPFB1vZwiwwRKq1b4H1t+yr3tu7KKhu8WbRp0AH17nGZ38QCXg03ftcvZRF3XSEWHjR//vBF2VAyt9IcC3zDa8rUmDTTrTp/HrBwzipUk+B6JZkAMlbfOhL+YIna5k+EEbiABBfkvuE0MFFoMuwnLWM4+eantlKqD9KOXkN360tAkwTogdvcTzndjapEQwnUl4poKn2PVbQE+AHXjqEIAcGUj4i8Io1VN7IAQVoDmxwFggIppEmtrADknnsgHN8RgKENUJnGfAznsOWHAJRtAeeY88eHQEKJapPswKQ9mDVyJCTHCAzlgIuqNhkX0+abBID6jnw4FRSQAfawWeqoK4Bo2XSBFiuVisGvRRoMPh2d/fds9e+h/GkrCFc83Y+n4f+7iv006+cO+FCVJJIGNw/+lltDB6k/11j0wPg8iY7zcI0D1WnlQHjTqLx9eckUhMhNFvJIuVic4eRu/qE4/DzUAbQuyjZ/YEQQ97dxz6AVruT6+z8QgjchG5xMVyHqY3C4rim55OSqjXOaD1j9KYJp8aH7TQoA25WLNMG50Q/ut9EUTVKNbXk/gMHNVZSvASZqAAAAABJRU5ErkJggg==";
        
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