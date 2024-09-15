using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cinteros.XTB.BulkDataUpdater.AppCode
{
    public class BDUEntity : Entity
    {
        public string Name { get; }
        public AttributeCollection OldAttribues = new AttributeCollection();
        public FormattedValueCollection OldFormatted = new FormattedValueCollection();
        public Dictionary<string, string> Action = new Dictionary<string, string>();

        public BDUEntity(string entityName, Guid id, string name) : base(entityName, id)
        {
            Name = name;
        }

        public BDUEntity CloneEmpty() => new BDUEntity(LogicalName, Id, Name);
    }

    public class BDUEntityCollection : List<BDUEntity>
    {
        public EntityCollection ToEntityCollection() => new EntityCollection(this.Select(e => e as Entity).ToArray()) { EntityName = this.FirstOrDefault()?.LogicalName };
    }
}