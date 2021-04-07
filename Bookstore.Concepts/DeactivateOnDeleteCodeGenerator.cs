using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Rhetos.Compiler;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Extensibility;

namespace Bookstore.Concepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(DeactivateOnDeleteInfo))]
    public class DeactivateOnDeleteCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DeactivateOnDeleteInfo)conceptInfo;

            string moduleName = info.Deactivatable.Entity.Module.Name;
            string entityName = info.Deactivatable.Entity.Name;

            var code = $@"if (checkUserPermissions) // DeactivateOnDelete is applied only on direct calls from web API.
            {{
                foreach (var item in deleted)
                    item.Active = false;
                updatedNew = updatedNew.Concat(deleted).ToArray();
                updated = updated.Concat(deleted).ToArray();
                deletedIds = Array.Empty<{moduleName}.{entityName}>();
                deleted = Array.Empty<Common.Queryable.{moduleName}_{entityName}>();
            }}
            ";

            codeBuilder.InsertCode(code, WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.Deactivatable.Entity);
        }
    }
}
