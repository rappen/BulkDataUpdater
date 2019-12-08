// *********************************************************************
// Created by: Latebound Constant Generator 1.2019.12.2 for XrmToolBox
// Author    : Jonas Rapp http://twitter.com/rappen
// Repo      : https://github.com/rappen/LateboundConstantGenerator
// Source Org: https://jonassandbox.crm4.dynamics.com/
// Filename  : C:\Dev\GitHub\BulkDataUpdater\BulkDataUpdater\Const.cs
// Created   : 2019-12-08 15:18:00
// *********************************************************************

namespace Cinteros.XTB.BulkDataUpdater
{
    public static class _common_
    {
        #region Attributes

        /// <summary>Type: Lookup, RequiredLevel: SystemRequired, Targets: businessunit
        /// Used by entities: team, systemuser</summary>
        public const string BusinessUnit = "businessunitid";
        /// <summary>Type: State, RequiredLevel: SystemRequired, DisplayName: Status, OptionSetType: State
        /// Used by entities: lead, opportunity, opportunityclose</summary>
        public const string Status = "statecode";
        /// <summary>Type: Status, RequiredLevel: None, DisplayName: Status Reason, OptionSetType: Status
        /// Used by entities: lead, opportunity, opportunityclose</summary>
        public const string StatusReason = "statuscode";

        #endregion Attributes
    }

    /// <summary>OwnershipType: UserOwned, IntroducedVersion: 5.0.0.0</summary>
    public static class Lead
    {
        public const string EntityName = "lead";
        public const string EntityCollectionName = "leads";

        #region OptionSets

        public enum Status_OptionSet
        {
            Open = 0,
            Qualified = 1,
            Disqualified = 2
        }
        public enum StatusReason_OptionSet
        {
            New = 1,
            ContactEd = 2,
            Qualified = 3,
            Lost = 4,
            CannotContact = 5,
            NoLongerInterested = 6,
            Canceled = 7
        }

        #endregion OptionSets
    }

    /// <summary>OwnershipType: UserOwned, IntroducedVersion: 5.0.0.0</summary>
    public static class Opportunity
    {
        public const string EntityName = "opportunity";
        public const string EntityCollectionName = "opportunities";

        #region OptionSets

        public enum Status_OptionSet
        {
            Open = 0,
            Won = 1,
            Lost = 2
        }
        public enum StatusReason_OptionSet
        {
            InProgress = 1,
            OnHold = 2,
            Won = 3,
            Canceled = 4,
            OutSold = 5
        }

        #endregion OptionSets
    }

    /// <summary>DisplayName: Opportunity Close, OwnershipType: UserOwned, IntroducedVersion: 5.0.0.0</summary>
    public static class OpportunityClose
    {
        public const string EntityName = "opportunityclose";
        public const string EntityCollectionName = "opportunitycloses";

        #region Attributes

        /// <summary>Type: Lookup, RequiredLevel: ApplicationRequired, Targets: opportunity</summary>
        public const string Opportunity = "opportunityid";
        /// <summary>Type: Picklist, RequiredLevel: None, DisplayName: Status Reason, OptionSetType: Picklist, DefaultFormValue: -1</summary>
        public const string StatusReason = "opportunitystatuscode";

        #endregion Attributes

        #region OptionSets

        public enum Status_OptionSet
        {
            Open = 0,
            Completed = 1,
            Canceled = 2
        }
        public enum OpportunityStatusReason_OptionSet
        {
            InProgress = 1,
            OnHold = 2,
            Won = 3,
            Canceled = 4,
            OutSold = 5
        }
        public enum StatusReason_OptionSet
        {
            Open = 1,
            Completed = 2,
            Canceled = 3
        }

        #endregion OptionSets
    }

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
        /// <summary>Type: String, RequiredLevel: SystemRequired, MaxLength: 100, Format: Email</summary>
        public const string PrimaryEmail = "internalemailaddress";
        /// <summary>Type: Boolean, RequiredLevel: None, True: 1, False: 0, DefaultValue: False</summary>
        public const string Status = "isdisabled";

        #endregion Attributes

        #region OptionSets

        public enum AccessMode_OptionSet
        {
            ReadWrite = 0,
            Administrative = 1,
            Read = 2,
            SupportUser = 3,
            NonInteractive = 4,
            DelegatedAdmin = 5
        }

        #endregion OptionSets
    }
}
