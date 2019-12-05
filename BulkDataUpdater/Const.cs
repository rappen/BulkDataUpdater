// *********************************************************************
// Created by: Latebound Constant Generator 1.2019.12.2 for XrmToolBox
// Author    : Jonas Rapp http://twitter.com/rappen
// Repo      : https://github.com/rappen/LateboundConstantGenerator
// Source Org: https://jonassandbox.crm4.dynamics.com/
// Filename  : C:\Dev\GitHub\BulkDataUpdater\BulkDataUpdater\Const.cs
// Created   : 2019-12-04 21:12:24
// *********************************************************************

namespace Cinteros.XTB.BulkDataUpdater
{
    /// <summary>OwnershipType: BusinessOwned, IntroducedVersion: 5.0.0.0</summary>
    public static class Team
    {
        public const string EntityName = "team";
        public const string EntityCollectionName = "teams";

        #region Attributes

        /// <summary>Type: Uniqueidentifier, RequiredLevel: SystemRequired</summary>
        public const string PrimaryKey = "teamid";
        /// <summary>Type: String, RequiredLevel: SystemRequired, MaxLength: 160, Format: Text</summary>
        public const string PrimaryName = "name";
        /// <summary>Type: Lookup, RequiredLevel: SystemRequired, Targets: businessunit</summary>
        public const string BusinessUnit = "businessunitid";
        /// <summary>Type: Picklist, RequiredLevel: SystemRequired, DisplayName: Team Type, OptionSetType: Picklist, DefaultFormValue: 0</summary>
        public const string TeamType = "teamtype";

        #endregion Attributes

        #region OptionSets

        public enum TeamType_OptionSet
        {
            Owner = 0,
            Access = 1,
            AADSecurityGroup = 2,
            AADOfficeGroup = 3
        }

        #endregion OptionSets
    }

    /// <summary>OwnershipType: BusinessOwned, IntroducedVersion: 5.0.0.0</summary>
    public static class User
    {
        public const string EntityName = "systemuser";
        public const string EntityCollectionName = "systemusers";

        #region Attributes

        /// <summary>Type: Uniqueidentifier, RequiredLevel: SystemRequired</summary>
        public const string PrimaryKey = "systemuserid";
        /// <summary>Type: String, RequiredLevel: None, MaxLength: 200, Format: Text</summary>
        public const string PrimaryName = "fullname";
        /// <summary>Type: Picklist, RequiredLevel: SystemRequired, DisplayName: Access Mode, OptionSetType: Picklist, DefaultFormValue: 0</summary>
        public const string AccessMode = "accessmode";
        /// <summary>Type: Lookup, RequiredLevel: SystemRequired, Targets: businessunit</summary>
        public const string BusinessUnit = "businessunitid";
        /// <summary>Type: String, RequiredLevel: SystemRequired, MaxLength: 100, Format: Email</summary>
        public const string PrimaryEmail = "internalemailaddress";
        /// <summary>Type: Boolean, RequiredLevel: None, True: 1, False: 0, DefaultValue: False</summary>
        public const string Status = "isdisabled";

        #endregion Attributes

        #region OptionSets

        public enum AccessMode_OptionSet
        {
            Read_Write = 0,
            Administrative = 1,
            Read = 2,
            SupportUser = 3,
            Non_interactive = 4,
            DelegatedAdmin = 5
        }

        #endregion OptionSets
    }
}
