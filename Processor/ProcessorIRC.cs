//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published
//  by the Free Software Foundation; either version 2 of the License, or
//  (at your option) version 3.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.

//  You should have received a copy of the GNU Lesser General Public License
//  along with this program; if not, write to the
//  Free Software Foundation, Inc.,
//  51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

using System;
using System.Collections.Generic;
using System.Text;

namespace libirc
{
    public partial class ProcessorIRC
    {
        /// <summary>
        /// Network
        /// </summary>
        private Network _Network = null;
        /// <summary>
        /// Protocol of this network
        /// </summary>
        private Protocol _Protocol = null;
        /// <summary>
        /// This is a text we received from server
        /// </summary>
        private string ServerLineRawText;
        /// <summary>
        /// If true the information is considered to be a backlog from irc bouncer and will not be processed
        /// in some special parts of irc protocol like part or join
        /// </summary>
        public bool IsBacklog = false;
        /// <summary>
        /// Time
        /// </summary>
        public DateTime pong;
        private long Date;

        private bool Pong(string source, string parameters, string _value)
        {
            _Network.Transfer("PONG :" + _value);
            return true;
        }

        /// <summary>
        /// Process the line
        /// </summary>
        /// <returns></returns>
        public bool ProfiledResult()
        {
            if (Defs.UsingProfiler)
            {
                Profiler profiler = new Profiler("IRC.ProfiledResult()");
                bool result = Result();
                profiler.Done();
                return result;
            }
            return Result();
        }

        private bool Result()
        {
            bool OK = false;
            if (String.IsNullOrEmpty(ServerLineRawText))
            {
                // there is nothing to process
                return false;
            }
            // every IRC command that is sent from server should being with colon according to RFC
            if (ServerLineRawText[0] == ':')
            {
                Network.IncomingDataEventArgs info = new Network.IncomingDataEventArgs();
                info.Date = this.Date;
                info.ServerLine = ServerLineRawText;
                info.Source = ServerLineRawText.Substring(1);
                // some functions prefer parameters not to be converted to list so this is them
                if (info.Source.Contains(" "))
                {
                    // we store the value of indexof so that we don't need to call this CPU expensive
                    // method too often
                    int index = info.Source.IndexOf(" ", StringComparison.Ordinal);
                    info.Command = info.Source.Substring(index + 1);
                    info.Source = info.Source.Substring(0, index);
                    if (info.Command.Contains(" :"))
                    {
                        index = info.Command.IndexOf(" :", StringComparison.Ordinal);
                        if (index < 0)
                        {
                            _Protocol.DebugLog("Malformed text, probably hacker: " + ServerLineRawText);
                            return false;
                        }
                        info.Message = info.Command.Substring(index + 2);
                        info.Command = info.Command.Substring(0, index);
                    }
                    // check if there aren't some extra parameters for this command
                    if (info.Command.Contains(" "))
                    {
                        // we remove the command name and split all the parameters by space
                        index = info.Command.IndexOf(" ", StringComparison.Ordinal);
                        info.ParameterLine = info.Command.Substring(index + 1);
                        info.Parameters.AddRange(info.ParameterLine.Split(' '));
                        info.Command = info.Command.Substring(0, index);
                    }
                    // commands are meant to be uppercase but for compatibility reasons we ensure it is
                    info.Command = info.Command.ToUpper();
                }
                if (_Network.__evt__IncomingData(info))
                {
                    return true;
                }
                if (ProcessSelf(info.Source, info.Command, info.Parameters, info.Message))
                {
                    // purposefuly change OK to true only, we don't want to change it to false in case it was already true, so don't touch this
                    OK = true;
                }
                switch (info.Command)
                {
                    case "001":
                    case "002":
                    case "003":
                    case "004":
                        Network.NetworkGenericDataEventArgs args004 = new Network.NetworkGenericDataEventArgs(this.ServerLineRawText, this.Date);
                        args004.Command = info.Command;
                        args004.ParameterLine = info.ParameterLine;
                        args004.Parameters = info.Parameters;
                        args004.Message = info.Message;
                        _Network.__evt_INFO(args004);
                        break;
                    case "005":
                        Info(info);
                        // this is usually a last datagram we get during load and that implies we are done logging on to IRC
                        this._Network.IsLoaded = true;
                        this._Network.JoinChannelsInQueue();
                        break;
                    case "301":
                        if (Idle2(info))
                            return true;
                        break;
                    case "305":
                        if (!IsBacklog)
                            _Network.IsAway = false;
                        break;
                    case "306":
                        if (!IsBacklog)
                            _Network.IsAway = true;
                        break;
                    case "311":
                        if (WhoisLoad(info))
                            return true;
                        break;
                    case "312":
                        if (WhoisSv(info))
                            return true;
                        break;
                    case "315":
                        if (FinishChan(info))
                            return true;
                        break;
                    case "317":
                        if (IdleTime(info))
                            return true;
                        break;
                    case "318":
                        if (WhoisFn(info.Command, info.ParameterLine))
                            return true;
                        break;
                    case "319":
                        if (WhoisCh(info.Command, info.ParameterLine, info.Message))
                            return true;
                        break;
                    case "321":
                        if (_Network.SuppressData || IsBacklog)
                            return true;
                        break;
                    case "322":
                        if (ChannelData(info.Command, info.ParameterLine, info.Message))
                            return true;
                        break;
                    case "323":
                        if (IsBacklog)
                            return true;
                        if (_Network.SuppressData)
                        {
                            _Network.SuppressData = false;
                            return true;
                        }
                        _Network.DownloadingList = false;
                        break;
                    case "324":
                        if (ChannelInfo(info))
                            return true;
                        break;
                    case "328":
                        if (Website(info.Parameters, info.Message))
                            return true;
                        break;
                    case "329":
                        if (CreationDatetime(info.Parameters))
                            return true;
                        break;
                    case "332":
                        if (ChannelTopic(info))
                            return true;
                        break;
                    case "333":
                        if (TopicInfo(info))
                            return true;
                        break;
                    case "352":
                        if (ParseUser(info))
                            return true;
                        break;
                    case "353":
                        if (ParseInfo(info))
                            return true;
                        break;
                    case "366":
                        return true;
                    case "367":
                        if (ChannelBans(info) && !this._Network.Config.ForwardModes)
                            return true;
                        break;
                    case "368":
                        if (ChannelBans2(info) && !this._Network.Config.ForwardModes)
                            return true;
                        break;
                    case "372":
                        Network.NetworkGenericDataEventArgs ev372 = new Network.NetworkGenericDataEventArgs(info);
                        _Network.__evt_OnMOTD(ev372);
                        return true;
                    case "375":
                        Network.NetworkGenericDataEventArgs ev375 = new Network.NetworkGenericDataEventArgs(info);
                        _Network.__evt_StartMOTD(ev375);
                        return true;
                    case "376":
                        Network.NetworkGenericDataEventArgs ev376 = new Network.NetworkGenericDataEventArgs(info);
                        _Network.__evt_CloseMOTD(ev376);
                        return true;
                    case "433":
                        if (!IsBacklog && !_Network.UsingNick2)
                        {
                            string nick = _Network.Config.GetNick2();
                            _Network.UsingNick2 = true;
                            _Network.Transfer("NICK " + nick, Defs.Priority.High);
                            _Network.Nickname = nick;
                        }
                        break;
                    case "473":
                        // invite needed
                        Network.NetworkJoinErrorEventArgs ev473 = new Network.NetworkJoinErrorEventArgs(info, 473);
                        _Network.__evt_JOINERROR(ev473);
                        break;
                    case "474":
                        // banned from channel
                        Network.NetworkJoinErrorEventArgs ev474 = new Network.NetworkJoinErrorEventArgs(info, 474);
                        _Network.__evt_JOINERROR(ev474);
                        break;
                    case "307":
                    case "310":
                    case "313":
                    case "378":
                    case "671":
                        if (WhoisText(info))
                            return true;
                        break;
                    case "PING":
                        if (Pong(info.Command, info.ParameterLine, info.Message))
                            return true;
                        break;
                    case "PONG":
                        pong = DateTime.Now;
                        return true;
                    //case "INFO":
                    //_Network.SystemWindow.scrollback.InsertText(text.Substring(text.IndexOf("INFO", StringComparison.Ordinal) + 5), Pidgeon.ContentLine.MessageStyle.User,                                                                     true, date, !updated_text);
                    //    return true;
                    case "NOTICE":
                        Network.NetworkNOTICEEventArgs notice = new Network.NetworkNOTICEEventArgs(ServerLineRawText, this.Date);
                        notice.Source = info.Source;
                        notice.Message = info.Message;
                        notice.ParameterLine = info.ParameterLine;
                        _Network.__evt_NOTICE(notice);
                        return true;
                    case "NICK":
                        if (ProcessNick(info))
                            return true;
                        break;
                    case "INVITE":
                        if (Invite(info.Source, info.ParameterLine))
                            return true;
                        break;
                    case "PRIVMSG":
                        if (ProcessPM(info.Source, info.ParameterLine, info.Message))
                            return true;
                        break;
                    case "TOPIC":
                        if (Topic(info))
                            return true;
                        break;
                    case "MODE":
                        if (Mode(info.Source, info.ParameterLine) && !this._Network.Config.ForwardModes)
                            return true;
                        break;
                    case "PART":
                        if (Part(info))
                            return true;
                        break;
                    case "QUIT":
                        if (Quit(info.Source, info.ParameterLine, info.Message))
                            return true;
                        break;
                    case "JOIN":
                        if (Join(info))
                            return true;
                        break;
                    case "KICK":
                        if (Kick(info))
                            return true;
                        break;
                }
            }
            else
            {
                // malformed requests this needs to exist so that it works with some broken ircd
                string command = ServerLineRawText;
                string value = "";
                if (command.Contains(" :"))
                {
                    value = command.Substring(command.IndexOf(" :", StringComparison.Ordinal) + 2);
                    command = command.Substring(0, command.IndexOf(" :", StringComparison.Ordinal));
                }
                // for extra borked ircd
                if (command.Contains(" "))
                {
                    command = command.Substring(0, command.IndexOf(" ", StringComparison.Ordinal));
                }

                switch (command)
                {
                    case "PING":
                        Pong(command, null, value);
                        OK = true;
                        break;
                }
            }
            if (!OK)
            {
                // we have no idea what we just were to parse so flag it as unknown data and let client parse that
                Network.UnknownDataEventArgs ev = new Network.UnknownDataEventArgs(this.ServerLineRawText);
                ev.Date = this.Date;
                _Network.HandleUnknownData(ev);
            }
            return true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="libirc.ProcessorIRC"/> class.
        /// </summary>
        /// <param name="_network">Network we parse this raw IRC data on</param>
        /// <param name="_text">Raw text data as received from ircd</param>
        /// <param name="_pong">_pong.</param>
        /// <param name="_date">Date of this message, if you specify 0 the current time will be used</param>
        /// <param name="updated">If true this text will be considered as newly obtained information</param>
        public ProcessorIRC(Network _network, string _text, ref DateTime _pong, long _date = 0, bool isBacklog = false)
        {
            _Network = _network;
            _Protocol = _network._Protocol;
            ServerLineRawText = _text;
            Date = _date;
            pong = _pong;
            IsBacklog = isBacklog;
        }
    }
}
