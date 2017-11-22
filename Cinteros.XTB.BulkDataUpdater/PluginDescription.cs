namespace Cinteros.XTB.BulkDataUpdater
{
    using System.ComponentModel.Composition;
    using XrmToolBox.Extensibility;
    using XrmToolBox.Extensibility.Interfaces;

    [Export(typeof(IXrmToolBoxPlugin)),
    ExportMetadata("Name", "Bulk Data Updater"),
    ExportMetadata("Description", "Update or touch single attributes on a set of records"),
    ExportMetadata("SmallImageBase64", Constants.B64_IMAGE_SMALL), // null for "no logo" image or base64 image content
    ExportMetadata("BigImageBase64", Constants.B64_IMAGE_LARGE), // null for "no logo" image or base64 image content
    ExportMetadata("BackgroundColor", "#f8f8ff"),
    ExportMetadata("PrimaryFontColor", "#00319C"),
    ExportMetadata("SecondaryFontColor", "#9439B5")]
    public class BulkDataUpdaterTool : PluginBase
    {
        #region Public Methods

        public override IXrmToolBoxPluginControl GetControl()
        {
            return new BulkDataUpdater();
        }

        #endregion Public Methods
    }
}