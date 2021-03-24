<Query Kind="Program">
  <Reference Relative="..\bin\Rhetos.AspNetFormsAuth.dll">..\bin\Rhetos.AspNetFormsAuth.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Dom.DefaultConcepts.Interfaces.dll">..\bin\Rhetos.Dom.DefaultConcepts.Interfaces.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Interfaces.dll">..\bin\Rhetos.Interfaces.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Processing.DefaultCommands.Interfaces.dll">..\bin\Rhetos.Processing.DefaultCommands.Interfaces.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Processing.Interfaces.dll">..\bin\Rhetos.Processing.Interfaces.dll</Reference>
  <Reference Relative="..\bin\Rhetos.Security.Interfaces.dll">..\bin\Rhetos.Security.Interfaces.dll</Reference>
  <Reference Relative="..\bin\BookStore.Service.dll">..\bin\BookStore.Service.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.AccountManagement.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Runtime.Serialization.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.ServiceModel.dll</Reference>
  <Namespace>Rhetos</Namespace>
  <Namespace>Rhetos.Processing</Namespace>
  <Namespace>Rhetos.Processing.DefaultCommands</Namespace>
  <Namespace>System</Namespace>
  <Namespace>System.Collections.Generic</Namespace>
  <Namespace>System.DirectoryServices</Namespace>
  <Namespace>System.DirectoryServices.AccountManagement</Namespace>
  <Namespace>System.IO</Namespace>
  <Namespace>System.Linq</Namespace>
  <Namespace>System.Runtime.Serialization</Namespace>
  <Namespace>System.Runtime.Serialization.Json</Namespace>
  <Namespace>System.ServiceModel</Namespace>
  <Namespace>System.ServiceModel.Channels</Namespace>
  <Namespace>System.Text</Namespace>
  <Namespace>System.Xml</Namespace>
  <Namespace>System.Xml.Serialization</Namespace>
  <Namespace>Rhetos.Dom.DefaultConcepts</Namespace>
</Query>

void Main()
{
	const string rhetosServerAddress = @"http://ENTER-APPLICATION-URL-HERE/RhetosService.svc";
	var server = ServerProxy.Create(rhetosServerAddress);
    ServerProcessingResult serverResponse;
    
	// READ FIRST 3 RECORDS FROM Common.Claim:
	
	Console.WriteLine("READ FIRST 3 RECORDS FROM Common.Claim:");
	serverResponse = server.Execute(new ReadCommandInfo
	{
		DataSource = "Common.Claim",
		Top = 3,
        OrderByProperties = new[] { new OrderByProperty { Property = "ClaimResource" } },
        ReadRecords = true
	});
	Short(serverResponse).Dump();
	ParseResponse<ReadCommandResult>(serverResponse).Dump();
    
    // CREATE A PRINCIPAL:
    
	Console.WriteLine("CREATE A Common.Principal:");
    var newPrincipal = new Common.Principal { ID = Guid.NewGuid(), Name = "TempLinqPadTest" };
	serverResponse = server.Execute(new SaveEntityCommandInfo()
	{
		Entity = "Common.Principal",
        DataToInsert = new[] { newPrincipal }
	});
	Short(serverResponse).Dump();

	// READ THE CREATED (OR EXISTING) PRINCIPAL:

	Console.WriteLine("READ Common.Principal:");
	serverResponse = server.Execute(new ReadCommandInfo
	{
		DataSource = "Common.Principal",
		Filters = new [] { new FilterCriteria("Name", "equals", "TempLinqPadTest") },
		ReadRecords = true
	});
	Short(serverResponse).Dump();
	newPrincipal = (Common.Principal)ParseResponse<ReadCommandResult>(serverResponse).Records.FirstOrDefault();

	// DELETE A PRINCIPAL:
    
	Console.WriteLine("DELETE A Common.Principal:");
	serverResponse = server.Execute(new SaveEntityCommandInfo()
	{
		Entity = "Common.Principal",
        DataToDelete = new[] { newPrincipal }
	});
	Short(serverResponse).Dump();
}


//=================================================
// HELPER FUNCTIONS AND CLASSES:

        private static string Short(ServerProcessingResult response)
        {
            var sb = new StringBuilder();
            sb.Append(response.Success ? "Success" : "Failed");
            if (response.UserMessage != null)
                sb.Append("\r\nUserMessage: " + response.UserMessage);
            if (response.SystemMessage != null)
                sb.Append("\r\nSystemMessage: " + response.SystemMessage);
            return sb.ToString();
        }

        private static T ParseResponse<T>(ServerProcessingResult response)
        {
            return (T)DeserializeFromXml(typeof(T), response.ServerCommandResults[0].Data);
        }

        public class ServerProxy : ClientBase<IServerApplication>
        {
            public static ServerProxy Create(string serverAddress)
            {
                var binding = new BasicHttpBinding();
                binding.SendTimeout = new TimeSpan(0, 0, 0, 20);
                binding.OpenTimeout = new TimeSpan(0, 0, 0, 20);
                binding.MaxReceivedMessageSize = 104857600;
                binding.ReaderQuotas.MaxStringContentLength = 104857600;
                binding.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
                binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Windows;
                binding.Security.Transport.ProxyCredentialType = HttpProxyCredentialType.None;

                return new ServerProxy(binding, new EndpointAddress(serverAddress));
            }

            public ServerProxy(Binding binding, EndpointAddress endpointAddress)
                : base(binding, endpointAddress)
            { 
            }

            public ServerProcessingResult Execute(params ICommandInfo[] commandInfos)
            {
                var serverCommands = commandInfos.Select(commandInfo => new ServerCommandInfo
                {
                    CommandName = commandInfo.GetType().Name,
                    Data = SerializeToXml(commandInfo, commandInfo.GetType())
                }).ToArray();

                try
                {
                    return Channel.Execute(serverCommands);
                }
                catch (EndpointNotFoundException ex)
				{
					throw new ApplicationException($"Please review if 'rhetosServerAddress' is correct at the beginning of LINQPad script '{Path.GetFileName(Util.CurrentQueryPath) }', and check if the server is running.", ex);
				}
                catch (Exception ex)
                {
                    if (ex.Message.StartsWith("The HTTP request is unauthorized with client authentication scheme 'Negotiate'."))
                    {
                        // TODO: Implement forms authentication.
                        throw new NotImplementedException("This script currently supports only Windows Authentication web security.");
                    }
                        
                    throw;
                }
            }
        }

        public static DataContractResolver DataContractResolver = new GenericDataContractResolver();

        public static string SerializeToXml(object obj, Type type)
        {
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                CheckCharacters = false,
                NewLineHandling = NewLineHandling.Entitize
            };
            using (var xmlWriter = XmlWriter.Create(sb, settings))
            using (var xmlDict = XmlDictionaryWriter.CreateDictionaryWriter(xmlWriter))
            {
                var serializer = new DataContractSerializer(type);
                serializer.WriteObject(xmlDict, obj, DataContractResolver);
                xmlWriter.Flush();
                return sb.ToString();
            }
        }

        public static object DeserializeFromXml(Type type, string xml)
        {
            using (var sr = new StringReader(xml))
            using (var xmlReader = XmlReader.Create(sr))
            using (var xmlDict = XmlDictionaryReader.CreateDictionaryReader(xmlReader))
            {
                var serializer = new DataContractSerializer(type);
                return serializer.ReadObject(xmlDict, false, DataContractResolver);
            }
        }

        public class GenericDataContractResolver : DataContractResolver
        {
            public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
            {
                XmlDictionary dictionary = new XmlDictionary();
                typeName = dictionary.Add(Encode(type.FullName));
                typeNamespace = dictionary.Add(type.Namespace);
                return true;
            }

            public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
            {
                var decodedTypeName = Decode(typeName);
                Type type = Type.GetType(decodedTypeName + ", " + typeNamespace);
                if (type != null)
                    return type;
					
				type = Type.GetType(decodedTypeName + ", BookStore.Service");
                if (type != null)
                    return type;

                type = Type.GetType(decodedTypeName);
                return type;
            }

            private static string Encode(string value)
            {
                StringBuilder sb = new StringBuilder();
                foreach (char c in value)
                    if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '.')
                        sb.Append(c);
                    else
                        sb.AppendFormat("_{0:x4}", (int)c);
                return sb.ToString();
            }

            private static string Decode(string value)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < value.Length; i++)
                    if (value[i] == '_')
                    {
                        int code = int.Parse(value.Substring(i + 1, 4), System.Globalization.NumberStyles.HexNumber);
                        sb.Append((char)code);
                        i += 4;
                    }
                    else
                        sb.Append(value[i]);

                return sb.ToString();
            }
        }