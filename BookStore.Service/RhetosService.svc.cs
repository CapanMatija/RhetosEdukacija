﻿/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Rhetos.Logging;
using Rhetos.Processing;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Activation;

namespace Rhetos
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class RhetosService : IServerApplication
    {
        private readonly IProcessingEngine _processingEngine;
        private readonly IEnumerable<ICommandInfo> _commands;
        private IDictionary<string, ICommandInfo> _commandsByName;
        private readonly ILogger _performanceLogger;
        private readonly XmlUtility _xmlUtility;

        public RhetosService(
            IProcessingEngine processingEngine,
            IEnumerable<ICommandInfo> commands,
            ILogProvider logProvider,
            XmlUtility xmlUtility)
        {
            _processingEngine = processingEngine;
            _commands = commands;
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
            _xmlUtility = xmlUtility;
        }

        public ServerProcessingResult Execute(params ServerCommandInfo[] commands)
        {
            var totalTime = Stopwatch.StartNew();

            try
            {
                var stopwatch = Stopwatch.StartNew();

                if (_commandsByName == null)
                    PrepareCommandByName();

                if (commands == null || commands.Length == 0)
                    return new ServerProcessingResult { SystemMessage = "Commands missing", Success = false };

                _performanceLogger.Write(stopwatch, "Execute: Server initialization done.");

                var processingCommandsOrError = Deserialize(commands);
                if (processingCommandsOrError.IsError)
                    return new ServerProcessingResult
                    {
                        Success = false,
                        SystemMessage = processingCommandsOrError.Error
                    };
                var processingCommands = processingCommandsOrError.Value;

                _performanceLogger.Write(stopwatch, "Execute: Commands deserialized.");

                var result = _processingEngine.Execute(processingCommands);

                _performanceLogger.Write(stopwatch, "Execute: Commands executed.");

                var convertedResult = ConvertResult(result);

                _performanceLogger.Write(stopwatch, "Execute: Result converted.");

                return convertedResult;
            }
            finally
            {
                _performanceLogger.Write(totalTime, $"Executed {string.Join(",", commands.Select(c => c.CommandName))}.");
            }
        }

        private void PrepareCommandByName()
        {
            var commandNames = _commands
                .SelectMany(command => new[] { command.GetType().Name, command.GetType().FullName, command.GetType().AssemblyQualifiedName }
                    .Select(name => new { command, name }));

            var invalidGroup = commandNames.GroupBy(cn => cn.name).FirstOrDefault(g => g.Count() > 1);
            if (invalidGroup != null)
                throw new FrameworkException(string.Format(
                    "Two commands {0} and {1} have the same name: \"{2}\".",
                    invalidGroup.ToArray()[0].command.GetType().AssemblyQualifiedName,
                    invalidGroup.ToArray()[1].command.GetType().AssemblyQualifiedName,
                    invalidGroup.Key));

            _commandsByName = commandNames.ToDictionary(cn => cn.name, cn => cn.command);
        }

        private ValueOrError<List<ICommandInfo>> Deserialize(IEnumerable<ServerCommandInfo> commands)
        {
            if (commands.Any(c => c == null))
                return ValueOrError.CreateError("Null command sent.");

            var commandsWithType = commands.Select(c =>
                {
                    Type commandType = null;
                    if (_commandsByName.TryGetValue(c.CommandName, out ICommandInfo command))
                        commandType = command.GetType();

                    return new { Command = c, Type = commandType };
                }).ToArray();

            var unknownCommandNames = commandsWithType.Where(c => c.Type == null).Select(c => c.Command.CommandName).ToArray();
            if (unknownCommandNames.Length > 0)
                return ValueOrError.CreateError($"Unknown command type: {string.Join(", ", unknownCommandNames)}.");

            var dataNotSetCommandNames = commands.Where(c => c.Data == null).Select(c => c.CommandName).ToArray();
            if (dataNotSetCommandNames.Length > 0)
                return ValueOrError.CreateError($"Command data not set: {string.Join(", ", dataNotSetCommandNames)}.");

            var processingCommands = new List<ICommandInfo>();
            foreach (var cmd in commandsWithType)
            {
                try
                {
                    var deserializedData = _xmlUtility.DeserializeFromXml(cmd.Type, cmd.Command.Data);
                    if (deserializedData == null)
                        return ValueOrError.CreateError($"Deserialization of {cmd.Command.CommandName} resulted in null value.");

                    if (deserializedData is ICommandInfo commandInfo)
                        processingCommands.Add(commandInfo);
                    else
                        return ValueOrError.CreateError($"Cannot cast {cmd.Command.CommandName} to {nameof(ICommandInfo)}.");
                }
                catch (Exception ex)
                {
                    return ValueOrError.CreateError($"Exception while deserializing {cmd.Command.CommandName}.{Environment.NewLine}{ex}");
                }
            }

            return processingCommands;
        }

        private ServerProcessingResult ConvertResult(ProcessingResult result)
        {
            return new ServerProcessingResult
            {
                Success = result.Success,
                UserMessage = result.UserMessage,
                SystemMessage = result.SystemMessage,
                ServerCommandResults = result.CommandResults
                    ?.Select(c => new ServerCommandResult
                       {
                           Message = c.Message,
                           Data = c.Data?.Value != null ? _xmlUtility.SerializeToXml(c.Data.Value) : null
                       })
                    ?.ToArray()
            };
        }
    }
}
