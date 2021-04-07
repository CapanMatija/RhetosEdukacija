using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Web;
using System.ComponentModel.Composition;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;

namespace BookStore.Service.src.Bookstore.Concepts
{
    [Export(typeof(IConceptInfo))] //definition of concept's DSL syntax
    [ConceptKeyword("PhoneNumber")] //DSL keyword for concept
    public class PhoneNumberInfo : ShortStringPropertyInfo
    {
    }

    [Export(typeof (IConceptMacro))]
    public class PhoneNumberMacro : IConceptMacro<PhoneNumberInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(PhoneNumberInfo conceptInfo, IDslModel existingConcepts)
        {
            return new IConceptInfo[]
            {
                new RegExMatchInfo
                {
                    Property = conceptInfo,
                    RegularExpression = "[+]*[(]{0,1}[0-9]{1,4}[)]{0,1}[-\\s\\./0-9]*",
                    ErrorMessage = "Invalid phone number format."
                }
            };
        }
    }
}