﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Data.Models.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Dtos;
using SharedLibraryCore.Interfaces;
using WebfrontCore.Permissions;
using WebfrontCore.ViewModels;

namespace WebfrontCore.Controllers
{
    public class ActionController : BaseController
    {
        private readonly ApplicationConfiguration _appConfig;
        private readonly string _banCommandName;
        private readonly string _tempbanCommandName;
        private readonly string _unbanCommandName;
        private readonly string _sayCommandName;
        private readonly string _kickCommandName;
        private readonly string _flagCommandName;
        private readonly string _unflagCommandName;
        private readonly string _setLevelCommandName;

        public ActionController(IManager manager, IEnumerable<IManagerCommand> registeredCommands,
            ApplicationConfiguration appConfig) : base(manager)
        {
            _appConfig = appConfig;

            foreach (var cmd in registeredCommands)
            {
                var type = cmd.GetType().Name;

                switch (type)
                {
                    case nameof(BanCommand):
                        _banCommandName = cmd.Name;
                        break;
                    case nameof(TempBanCommand):
                        _tempbanCommandName = cmd.Name;
                        break;
                    case nameof(UnbanCommand):
                        _unbanCommandName = cmd.Name;
                        break;
                    // todo: this should be flag driven
                    case "SayCommand":
                        _sayCommandName = cmd.Name;
                        break;
                    case nameof(KickCommand):
                        _kickCommandName = cmd.Name;
                        break;
                    case nameof(FlagClientCommand):
                        _flagCommandName = cmd.Name;
                        break;
                    case nameof(UnflagClientCommand):
                        _unflagCommandName = cmd.Name;
                        break;
                    case nameof(SetLevelCommand):
                        _setLevelCommandName = cmd.Name;
                        break;
                }
            }
        }

        public IActionResult BanForm()
        {
            var info = new ActionInfo
            {
                ActionButtonLabel = Localization["WEBFRONT_ACTION_BAN_NAME"],
                Name = "Ban",
                Inputs = new List<InputInfo>
                {
                    new()
                    {
                        Name = "Reason",
                        Label = Localization["WEBFRONT_ACTION_LABEL_REASON"],
                    },
                    new()
                    {
                        Name = "PresetReason",
                        Type = "select",
                        Label = Localization["WEBFRONT_ACTION_LABEL_PRESET_REASON"],
                        Values = GetPresetPenaltyReasons()
                    },
                    new()
                    {
                        Name = "Duration",
                        Label = Localization["WEBFRONT_ACTION_LABEL_DURATION"],
                        Type = "select",
                        Values = _appConfig.BanDurations
                            .Select((item, index) => new
                                {
                                    Id = (index + 1).ToString(),
                                    Value = item.HumanizeForCurrentCulture()
                                }
                            )
                            .Append(new
                            {
                                Id = (_appConfig.BanDurations.Length + 1).ToString(),
                                Value = Localization["WEBFRONT_ACTION_SELECTION_PERMANENT"]
                            }).ToDictionary(duration => duration.Id, duration => duration.Value),
                    }
                },
                Action = "BanAsync",
                ShouldRefresh = true
            };

            return View("_ActionForm", info);
        }

        public async Task<IActionResult> BanAsync(int targetId, string reason, int duration, string presetReason = null)
        {
            var fallthroughReason = presetReason ?? reason;
            string command;
            // permanent
            if (duration > _appConfig.BanDurations.Length)
            {
                command = $"{_appConfig.CommandPrefix}{_banCommandName} @{targetId} {fallthroughReason}";
            }
            // temporary ban
            else
            {
                var durationSpan = _appConfig.BanDurations[duration - 1];
                var durationValue = durationSpan.TotalHours.ToString(CultureInfo.InvariantCulture) +
                                    Localization["GLOBAL_TIME_HOURS"][0];
                command =
                    $"{_appConfig.CommandPrefix}{_tempbanCommandName} @{targetId} {durationValue} {fallthroughReason}";
            }

            var server = Manager.GetServers().First();

            return await Task.FromResult(RedirectToAction("Execute", "Console", new
            {
                serverId = server.EndPoint,
                command
            }));
        }

        public IActionResult UnbanForm()
        {
            var info = new ActionInfo
            {
                ActionButtonLabel = Localization["WEBFRONT_ACTION_UNBAN_NAME"],
                Name = "Unban",
                Inputs = new List<InputInfo>
                {
                    new()
                    {
                        Name = "Reason",
                        Label = Localization["WEBFRONT_ACTION_LABEL_REASON"],
                    }
                },
                Action = "UnbanAsync",
                ShouldRefresh = true
            };

            return View("_ActionForm", info);
        }

        public async Task<IActionResult> UnbanAsync(int targetId, string reason)
        {
            var server = Manager.GetServers().First();

            return await Task.FromResult(RedirectToAction("Execute", "Console", new
            {
                serverId = server.EndPoint,
                command = $"{_appConfig.CommandPrefix}{_unbanCommandName} @{targetId} {reason}"
            }));
        }

        public IActionResult LoginForm()
        {
            var login = new ActionInfo
            {
                ActionButtonLabel = Localization["WEBFRONT_ACTION_LOGIN_NAME"],
                Name = "Login",
                Inputs = new List<InputInfo>
                {
                    new()
                    {
                        Name = "clientId",
                        Label = Localization["WEBFRONT_ACTION_LABEL_ID"],
                        Required = true
                    },
                    new()
                    {
                        Name = "Password",
                        Label = Localization["WEBFRONT_ACTION_LABEL_PASSWORD"],
                        Type = "password",
                        Required = true
                    }
                },
                Action = "Login"
            };

            return View("_ActionForm", login);
        }

        public async Task<IActionResult> Login(int clientId, string password)
        {
            return await Task.FromResult(RedirectToAction("Login", "Account", new {clientId, password}));
        }

        public IActionResult EditForm()
        {
            var info = new ActionInfo
            {
                ActionButtonLabel = Localization["WEBFRONT_ACTION_LABEL_EDIT"],
                Name = "Edit",
                Inputs = new List<InputInfo>
                {
                    new()
                    {
                        Name = "level",
                        Label = Localization["WEBFRONT_PROFILE_LEVEL"],
                        Type = "select",
                        Values = Enum.GetValues(typeof(EFClient.Permission)).OfType<EFClient.Permission>()
                            .Where(p => p <= Client.Level)
                            .Where(p => p != EFClient.Permission.Banned)
                            .Where(p => p != EFClient.Permission.Flagged)
                            .ToDictionary(p => p.ToString(), p => p.ToLocalizedLevelName())
                    },
                },
                Action = "EditAsync",
                ShouldRefresh = true
            };

            return View("_ActionForm", info);
        }

        public async Task<IActionResult> EditAsync(int targetId, string level)
        {
            var server = Manager.GetServers().First();

            return await Task.FromResult(RedirectToAction("Execute", "Console", new
            {
                serverId = server.EndPoint,
                command = $"{_appConfig.CommandPrefix}{_setLevelCommandName} @{targetId} {level}"
            }));
        }

        public IActionResult GenerateLoginTokenForm()
        {
            var info = new ActionInfo
            {
                ActionButtonLabel = Localization["WEBFRONT_ACTION_LABEL_GENERATE_TOKEN"],
                Name = "GenerateLoginToken",
                Action = "GenerateLoginTokenAsync",
                Inputs = new List<InputInfo>()
            };

            return View("_ActionForm", info);
        }

        [Authorize]
        public string GenerateLoginTokenAsync()
        {
            var state = Manager.TokenAuthenticator.GenerateNextToken(Client.NetworkId);
            return string.Format(Utilities.CurrentLocalization.LocalizationIndex["COMMANDS_GENERATETOKEN_SUCCESS"],
                state.Token,
                $"{state.RemainingTime} {Utilities.CurrentLocalization.LocalizationIndex["GLOBAL_MINUTES"]}",
                Client.ClientId);
        }

        public IActionResult ChatForm(long id)
        {
            var info = new ActionInfo
            {
                ActionButtonLabel = Localization["WEBFRONT_ACTION_LABEL_SUBMIT_MESSAGE"],
                Name = "Chat",
                Inputs = new List<InputInfo>
                {
                    new()
                    {
                        Name = "message",
                        Type = "text",
                        Label = Localization["WEBFRONT_ACTION_LABEL_MESSAGE"]
                    },
                    new()
                    {
                        Name = "id",
                        Value = id.ToString(),
                        Type = "hidden"
                    }
                },
                Action = "ChatAsync"
            };

            return View("_ActionForm", info);
        }

        public async Task<IActionResult> ChatAsync(long id, string message)
        {
            var server = Manager.GetServers().First(server => server.EndPoint == id);

            server.ChatHistory.Add(new SharedLibraryCore.Dtos.ChatInfo()
            {
                ClientId = Client.ClientId,
                Message = message,
                Name = Client.Name,
                ServerGame = server.GameName,
                Time = DateTime.Now
            });

            return await Task.FromResult(RedirectToAction("Execute", "Console", new
            {
                serverId = server.EndPoint,
                command = $"{_appConfig.CommandPrefix}{_sayCommandName} {message}"
            }));
        }

        public async Task<IActionResult> RecentClientsForm(PaginationRequest request)
        {
            ViewBag.First = request.Offset == 0;
            
            if (request.Count > 20)
            {
                request.Count = 20;
            }
            
            var clients = await Manager.GetClientService().GetRecentClients(request);
            return View("~/Views/Shared/Components/Client/_RecentClients.cshtml", clients);
        }
        
        public IActionResult RecentReportsForm()
        {
            var serverInfo = Manager.GetServers().Select(server =>
                new ServerInfo
                {
                    Name = server.Hostname,
                    Reports = server.Reports.Where(report => (DateTime.UtcNow - report.ReportedOn).TotalHours <= 24).ToList()
                });

            return View("Partials/_Reports", serverInfo);
        }

        public IActionResult FlagForm()
        {
            var info = new ActionInfo
            {
                ActionButtonLabel = Localization["WEBFRONT_ACTION_FLAG_NAME"],
                Name = "Flag",
                Inputs = new List<InputInfo>
                {
                    new()
                    {
                        Name = "reason",
                        Label = Localization["WEBFRONT_ACTION_LABEL_REASON"],
                    },
                    new()
                    {
                        Name = "PresetReason",
                        Type = "select",
                        Label = Localization["WEBFRONT_ACTION_LABEL_PRESET_REASON"],
                        Values = GetPresetPenaltyReasons()
                    },
                },
                Action = "FlagAsync",
                ShouldRefresh = true
            };

            return View("_ActionForm", info);
        }

        public async Task<IActionResult> FlagAsync(int targetId, string reason, string presetReason = null)
        {
            var server = Manager.GetServers().First();

            return await Task.FromResult(RedirectToAction("Execute", "Console", new
            {
                serverId = server.EndPoint,
                command = $"{_appConfig.CommandPrefix}{_flagCommandName} @{targetId} {presetReason ?? reason}"
            }));
        }

        public IActionResult UnflagForm()
        {
            var info = new ActionInfo
            {
                ActionButtonLabel = Localization["WEBFRONT_ACTION_UNFLAG_NAME"],
                Name = "Unflag",
                Inputs = new List<InputInfo>
                {
                    new()
                    {
                        Name = "reason",
                        Label = Localization["WEBFRONT_ACTION_LABEL_REASON"],
                    }
                },
                Action = "UnflagAsync",
                ShouldRefresh = true
            };

            return View("_ActionForm", info);
        }

        public async Task<IActionResult> UnflagAsync(int targetId, string reason)
        {
            var server = Manager.GetServers().First();

            return await Task.FromResult(RedirectToAction("Execute", "Console", new
            {
                serverId = server.EndPoint,
                command = $"{_appConfig.CommandPrefix}{_unflagCommandName} @{targetId} {reason}"
            }));
        }

        public IActionResult KickForm(int id)
        {
            var info = new ActionInfo
            {
                ActionButtonLabel = Localization["WEBFRONT_ACTION_KICK_NAME"],
                Name = "Kick",
                Inputs = new List<InputInfo>
                {
                    new()
                    {
                        Name = "reason",
                        Label = Localization["WEBFRONT_ACTION_LABEL_REASON"],
                    },
                    new()
                    {
                        Name = "PresetReason",
                        Type = "select",
                        Label = Localization["WEBFRONT_ACTION_LABEL_PRESET_REASON"],
                        Values = GetPresetPenaltyReasons()
                    },
                    new()
                    {
                        Name = "targetId",
                        Type = "hidden",
                        Value = id.ToString()
                    }
                },
                Action = "KickAsync",
                ShouldRefresh = true
            };

            return View("_ActionForm", info);
        }

        public async Task<IActionResult> KickAsync(int targetId, string reason, string presetReason = null)
        {
            var client = Manager.GetActiveClients().FirstOrDefault(client => client.ClientId == targetId);

            if (client == null)
            {
                return BadRequest(Localization["WEBFRONT_ACTION_KICK_DISCONNECT"]);
            }

            return await Task.FromResult(RedirectToAction("Execute", "Console", new
            {
                serverId = client.CurrentServer.EndPoint,
                command = $"{_appConfig.CommandPrefix}{_kickCommandName} {client.ClientNumber} {presetReason ?? reason}"
            }));
        }

        private Dictionary<string, string> GetPresetPenaltyReasons() => _appConfig.PresetPenaltyReasons.Values
            .Concat(_appConfig.GlobalRules)
            .Concat(_appConfig.Servers.SelectMany(server => server.Rules ?? Array.Empty<string>()))
            .Distinct()
            .Select((value, _) => new
            {
                Value = value
            })
            // this is used for the default empty optional value
            .Prepend(new
            {
                Value = ""
            })
            .ToDictionary(item => item.Value, item => item.Value);
    }
}
