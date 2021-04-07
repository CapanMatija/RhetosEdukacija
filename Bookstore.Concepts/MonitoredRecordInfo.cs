using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;

namespace Bookstore.Concepts
{
    /// <summary>
    /// When user tries to deletes the records, it will automatically modify the operation deactivate those records instead
    /// (set the Active property value to false).
    /// Automatic deactivation is only applied for user requests.
    /// It is not applied if the Delete method is explicitly called from code, as a part of some other operation.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("MonitoredRecord")]
    public class MonitoredRecordInfo : IConceptInfo
    {
        [ConceptKey]
        public EntityInfo Entity { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    public class MonitoredRecordMacro : IConceptMacro<MonitoredRecordInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(MonitoredRecordInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            newConcepts.Add(new EntityLoggingInfo { Entity = conceptInfo.Entity });

            var createdAt = new DateTimePropertyInfo { DataStructure = conceptInfo.Entity, Name = "CreatedAt" };
            newConcepts.Add(createdAt);
            newConcepts.Add(new CreationTimeInfo { Property = createdAt });


            return newConcepts;
        }
    }
}
