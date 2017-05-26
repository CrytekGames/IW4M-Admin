﻿using System;
using SharedLibrary;
using SharedLibrary.Extensions;
using System.Threading.Tasks;

namespace Auto_Restart_Plugin
{
    public class Main : IPlugin
    {
        public string Author
        {
            get
            {
                return "RaidMax";
            }
        }

        public float Version
        {
            get
            {
                return 1.0f;
            }
        }

        public string Name
        {
            get
            {
                return "Auto Restart Plugin";
            }
        }

        public async Task OnLoad()
        {
            return;
        }

        public async Task OnUnload()
        {
            return;
        }

        public async Task OnTick(Server S)
        {
            switch (Monitoring.shouldRestart())
            {
                case 300:
                    await S.Broadcast("^1Server will be performing an ^5AUTOMATIC ^1restart in ^55 ^1minutes.");
                    break;
                case 120:
                    await S.Broadcast("^1Server will be performing an ^5AUTOMATIC ^1restart in ^52 ^1minutes.");
                    break;
                case 60:
                    await S.Broadcast("^1Server will be performing an ^5AUTOMATIC ^1restart in ^51 ^1minute.");
                    break;
                case 30:
                    await S.Broadcast("^1Server will be performing an ^5AUTOMATIC ^1restart in ^530 ^1seconds.");
                    break;
                case 0:
                    await S.Broadcast("^1Server now performing an ^5AUTOMATIC ^1restart ^5NOW ^1please reconnect.");
                    Monitoring.Restart(S);
                    break;
            }
        }

        public async Task OnEvent(Event E, Server S)
        {
            return;
        }
    }
}
