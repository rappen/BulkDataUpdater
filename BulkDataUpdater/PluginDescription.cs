﻿namespace Cinteros.XTB.BulkDataUpdater
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using XrmToolBox.Constants;
    using XrmToolBox.Extensibility;
    using XrmToolBox.Extensibility.Interfaces;

    [Export(typeof(IXrmToolBoxPlugin)),
    ExportMetadata("Name", "Bulk Data Updater"),
    ExportMetadata("Description", "BDU can update one or multiple columns, for one or a gazillion records in Microsoft Dataverse! Empower yourself to achieve more."),
    ExportMetadata("SmallImageBase64", "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAMAAABEpIrGAAAAFXRFWHRDcmVhdGlvbiBUaW1lAAfiBgwUIyQeSiFdAAAAB3RJTUUH4wMKDgw3ao+eMQAAAAlwSFlzAAAK8AAACvABQqw0mAAAAPZQTFRF//+93ue9rca9jKW1e5y1hKW1pb291t69IVqtAEKtAEq9c5S17/e9a5S1AEK1AFLWAFrvAGP/WoS1vc69GFKtAFLeEGvvIXPeGHPnCGv3AFrnEEqtlK21AErOAGP3Qoy9rc5S7/cQ//8A5+8Ya6WUe62Ec5y11ucpnMZjjLVzhLV7zt4xKXvWnLW9OYTGc62M9/cICEqtWpyl3u8hEGvnxt45lL1rSnu15++9rc5Ke7V7pcZaxta9MYTOKWO1Y6Wc9/+9vdZCjK21AErGtdZK1uchSpStIXvWc62Ext4xUoS1KXvOY4y19/8IQnO1UpStSpS1Y5yc45ezngAAAAF0Uk5TAEDm2GYAAAIjSURBVHjabZNtd6IwEIXRxZdQBUcL1pcixKKoLIJWtq51q1Lb3ZbW3f//Z3aSSMXTzgdPknlO5t54kaRTbe67gyS56DY20ldVSjpEVeI4VklYv//Uzr0QRb/1PO/ZAFOPyXvpvF/sKG2AEWW18ABqSrjL9gtEA6wx/ddcO/QPW+sk2WT6OjsDh/r4u6d80yZJ2s9j3z8cfJjSv6ZxCLawnh0OSByn3HXw/hWlQ7jhGgIPXErnOCUUShMVL4wEED3cRG8LBuzxMH7nBgnqZwYmOIJpWNFXl+HohTQQGKjCAAJC5JZ6EduxK14QKGvCALob05UzXmIPgS13ElZwQo0tf7OjUWDbdrBtMuCRnZo4I0+46ykCpo/1bApFFj9Wd9KOS2AOH6+4SzsymKIVP46rkqzw1QMCBt3766clzkLAFUA9A1gmXQC3OUPJUQocRzAAhPQmtU5ANRXJgYADYA8/ABR5J2xmADOYOEcNJsFo/dJOI7iGHo1SQO9gJgrq0abV4oAxpBgaYVOps3871AWwNPANRm6fOvBTAG3Cw1tV8fFmmIDb/nze7y9mAJYQqfzgeahcXgP4+IZTOFYv4BstzIlINTCyLRuJoTsaj0fuhL24hwO+p6HsYihF3D7Kwf7gFPsuuTZ7lp12+1YPNCJnP4zGpYpejCarqxbqVzrF80+rUiWqVhMaa5pC6rnPX6dcJkTBIqR8UZK+rFz+mywXimfd/6N4X19fDix1AAAAAElFTkSuQmCC"), // null for "no logo" image or base64 image content
    ExportMetadata("BigImageBase64", "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"), // null for "no logo" image or base64 image content
        ExportMetadata("BackgroundColor", "#FFFFC0"),
        ExportMetadata("PrimaryFontColor", "#0000C0"),
        ExportMetadata("SecondaryFontColor", "#0000FF")]
    public class BulkDataUpdaterTool : PluginBase, IPayPalPlugin
    {
        public string DonationDescription => "Bulk Data Updater Fan Club";

        public string EmailAccount => "jonas@rappen.net";

        public override IXrmToolBoxPluginControl GetControl() => new BulkDataUpdater();

        public BulkDataUpdaterTool()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolveEventHandler);
        }

        /// <summary>
        /// Event fired by CLR when an assembly reference fails to load
        /// Assumes that related assemblies will be loaded from a subfolder named the same as the Plugin
        /// For example, a folder named Sample.XrmToolBox.MyPlugin
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private Assembly AssemblyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            Assembly loadAssembly = null;
            Assembly currAssembly = Assembly.GetExecutingAssembly();

            // base name of the assembly that failed to resolve
            var argName = args.Name.Substring(0, args.Name.IndexOf(","));

            // check to see if the failing assembly is one that we reference.
            List<AssemblyName> refAssemblies = currAssembly.GetReferencedAssemblies().ToList();
            var refAssembly = refAssemblies.Where(a => a.Name == argName).FirstOrDefault();

            // if the current unresolved assembly is referenced by our plugin, attempt to load
            if (refAssembly != null)
            {
                // load from the path to this plugin assembly, not host executable
                string dir = Path.GetDirectoryName(currAssembly.Location).ToLower();
                string folder = Path.GetFileNameWithoutExtension(currAssembly.Location);
                dir = Path.Combine(dir, folder);

                var assmbPath = Path.Combine(dir, $"{argName}.dll");

                if (File.Exists(assmbPath))
                {
                    loadAssembly = Assembly.LoadFrom(assmbPath);
                }
                else
                {
                    throw new FileNotFoundException($"Unable to locate dependency: {assmbPath}");
                }
            }

            return loadAssembly;
        }

        public override Guid GetId() => XrmToolBoxToolIds.BulkDataUpdater;
    }
}