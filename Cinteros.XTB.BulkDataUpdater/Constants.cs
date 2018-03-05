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
        internal const string B64_IMAGE_LARGE = "iVBORw0KGgoAAAANSUhEUgAAAFAAAABQCAMAAAC5zwKfAAAAFXRFWHRDcmVhdGlvbiBUaW1lAAfiAwUVOAgxjizpAAAAB3RJTUUH4gMFFgYB3tDSGQAAAAlwSFlzAAAK8AAACvABQqw0mAAAAvRQTFRF8PDw3uPrrsDdjqnUbZHLUHzDPm++NWi7YYjIf57QpbrbzNbmnbPYWILFIFm2AEKtDkyxR3XAhaLSydTl5+ntl6/XAkStI1u2eZrO0trn1dzoao/KBkavLGK7TnzGcZbRiqjZjKrapr3iqsDjkq7cd5rTXojLOGy/D02ytMXfCUmwSHfEkK3byNbt+Pr8/v7+3OXzo7vhYYrMGVS1L2S6scLec5XNBUauA0SuS3rF9fj7xtXsa5LPEk+yRHPAHVe1DEqw4ur1nbbfJV65fJzP7e3vZ43JepzU8vX6Mma7W4XK9+/648ft0qbjxo3bvHrVt2/RsWPOr17MrFnKqVLIqFDIp07Hq1bJsGLNtm3Ru3jUxIna0KLh4MHr9Oj4Ckqwt8fgEU6xssbm8OL10aPitWvQpUrGy5je69jy3+f0Q3TCCEiv6/D48eX2wILXuHLSfJ7U+/z94sTspkzG1qzlvM7pElCzUX3Gqb3cAkOt5s3v2LLmW4TGusnh+fT7rVvLL2W8FVKyzp3g6O736uvuoLngsmXOzJne3r7q6tXx+/j8/Pr9+PH67Nnz4cTs7+D1FlK0wc3i2rbo79/0RHXDvn7WmLLd+vb8HFe2Hli2KWC6QXLBZo3N9Pf7VYHI4MLrcpfRmrTe27jo8PT6LGK52rTnqlXJhKTXu3fUypbekavVZIvJGlW00d3wz9zvtcjniKfZMWa81uDx0aTiPnDB4cPszNnuiqXT9vj8G1a19Or4sGDN58/wucvoTXnCt8ro9u754eXr5Ofs7fL4pbzi9u35BEWumrHYorja2ePywdDq+vv9vcvhlK3W4ej1rMHkdJjSdpfOJl23XobHhqXY/P3+lrHdapDP2N7p8/b7bZLQO27AaZDO5ez2cJPMIlu4THrFVYDEWIPJ2uTzb5TQJ165sMTlcJXRO2y9I1y4SnfBgqLWXIbKIFq32+Dqqb/jn7jg/v3+6dPxPG7A3LjoyJLc1Kjjx4/biajZz9jmoLbZOGq8NQ8TrQAAAAF0Uk5TAEDm2GYAAAbDSURBVHjavZl7XFRFFMcxFRGBgRREBBEBNXB3VlFZRIk1ENHUJCQrE0VbQ+QlPtDMJ2qIDygkBbQMlB6KSoZmZT7KbM000yISLSwtLa2s7PFPZ+bO3d3LvXP3kn46/+xy5zffnTtz5sw5g5PT/2w38n3d/4nIQMR2Vl+76N35DmAF+SO6IplNbX/qxn/Clbh4Io75xPxd0Epa/IkOrHNYn8pzNy3R6ab66CtlV8OL2ePijm1aw3M9K3Trc12nx1IzbJsfRttS445qxXV2pj0u1Ziwog3pXyoskpu2t42LpIO7kov5Vtfbn4i8Ghzz/qKTF5CGHZihmS75CUe8/Kkgiw2xjc4SzOy0ZYUUGR1AkBfjVXn7fUAzM93WK9jeXb77U7JE+gQjee0uKrzBIDBeP2nXKUjqgRXSRbcQL+rAd6Du5HXT8ME5v4+dVGgDzqR2y4MQyxgq84/bt8dswKa98KyaR/SFxnm1+NcJZmLTRaBRHE/tMoRuCV/X51HNb3jITOiUqPzWp6DJw4DxK1RrzlvbEojPgDsL31YJmh2/YD1ZbS+llZkC7jevDsRJgti8Tgb8Gfoeo9+YxHwY41wyxnZy3g2Y3zAdVgXehK5NEuAHZOOQeZT7Yww8DcbqwG2gqZIBcVMEQp5+LXhtQduIHQB/ROgnLAdiC3ivs5TXALFvqV4VeLLuOvTrqQTEOTAcbwlwEfTbhbnA5thU4RQYel4RmLsUIqR9NCuB3w7FfKCRbZT5tVgRiHWgGGwHvAY/MFIFuNe69xqVgbg3uKhtw5SAMgSrAA+YTJfrd+/5weoJMqDJ336IMIOb9GpAZk2x8NbKQDwUgqN4crWJtK6eOhBvRaicAzTALJ4SgyBC/rM0Aa/CbucAcThC3zMgBP1RWBOwEoIsD3gFJljYLg3wLU0bEE67Ch5Q3xeh/RToDVFVrwl4GZbyWx4Qj4IwRoHfiEvHAS4ooxZS00xyEwsX2AuhDBoXIbT3UwNK7GvMBTZBcwnwjsBnPRcYIsFlNG7hA/ElhF4D4CCYJT0XOOvCVtEqa9KqRI0icD5CLkIkLMdcIMcUgTlCVLyIUNTdAa4EtwegO0IXVIBVJmZNekfAuTDJADyEUAIfeNpoW5JhX+WkqwFrQRNPN14QHxgo9Rr/IBWgSdh8DoELUqkdF5Dn7hzowZ5/qWuE9z9ed9eAYJ9Dly9UgQ0UeEYr8Hw5Qpu5QB0AYZU/Q2iAViA+B31m8YC7IYEA4KcIlWoGWgAYzQNCpnJWyOI8NAM/AeBpHhCyvRgAfgyakVqBVSAezgOGC2ndUR8xamoAnmQnuCKwmCU41QjVaAViJERjJeBH0NaJAF9HaF9rgLwRQixeRs+UE7BFqzQC9awQUAJGwdlEgUchcVipEXgMgO8rA6tgs38oHPReUk8UgYUKQAMA38O4SAQelnhh5BEB6AYqg63lbSZ+RwH4LkghMV0sAtfamiDlO8RSkQIoGANtLauYeLYCMAGiGRx8B0VgkbUlGn4q367Ei7VlS9lMvF0OPPAWVL7wuY5Jkm1NPRDqas0P/SLtMmKcxdTL5UBIAum7vMkk460t9UjMbJyYK/pb7wNmMPVGBty5i1itbvcekhkbSYB9g0nGWYH7QGd3U+KXapffbGfqHUVYdqawTcXKQfOrYh/Ia2jWIClExZSuUJzxF+GPl6S4YduIIlNUvCz6INRS1ZICsgtkTMXsCmBNijhDL0AUDtjEbHN5RWWvLZJZNk9jQEiV0RRpKQUZDiplNw3jRP3GNVjJlovtSexBGRKyGomRaj5HaH9O7GBetVCOe976e+bVwhNdGLiMrAYvSETill6cbLYhVz9bmDmajrRo7eR166dn29rMG6g8HeJgRom8YO62DHwiTfpO6pZFxSs8ZJUjs2egPgyjxMVLtPBS6JKYoG6UlHl2lr8AxtifqBbmaQA+TcMPGd8iJ465kWugHLLWs5Md4fLmEN7ceaRG5V8uuZKqOIrEicwsdV72U4TXkyR7LmqXVVPI1VcEnchp05/k0SbOoLh0ch+CfJ1UrdtAIholRIrRD44ZO+6h8UkPJ09ISUlJfiRp4qOPPT7picm0UR8US64lXZ0cWJcRhBibMBKrW/ADRJfYyRGP7ML7KTLUxKfph1NcZFy8Bh4McjC94zRGBQ9RxEUPHUZDT4yW4Qnm5+4jXBGH92txq2voPyBCiGQDB2nGUWR38RbbP6BHYM+gXsEhQaG9+/RlD3287msVjr64t7MPUrauvt1ajaPWxs3doyXM0+veO/l3AEDvaduuvXMHYu1dOrpqX4e7Zf8C8pLUSZdCG9MAAAAASUVORK5CYII=";
        
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