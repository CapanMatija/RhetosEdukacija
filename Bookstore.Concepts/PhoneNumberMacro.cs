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
    [Export(typeof(IConceptMacro))]
    public class PhoneNumberMacro : IConceptMacro<PhoneNumberInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(PhoneNumberInfo conceptInfo, IDslModel existingConcepts)
        {
            return new IConceptInfo[]
            {
                new RegExMatchInfo
                {
                    Property = conceptInfo,
                    RegularExpression = @"^[+]*[(]{0,1}[0-9]{1,4}[)]{0,1}[-\s\./0-9]*$",
                    ErrorMessage = "Invalid phone number format."
                }
            };
        }
    }
}
