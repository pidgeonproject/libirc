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
        /// Process a data that affect the current user
        /// </summary>
        /// <param name="source"></param>
        /// <param name="command"></param>
        /// <param name="parameters"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool ProcessSelf(string source, string command, List<string> parameters, string message)
        {
            if (source.StartsWith(_Network.Nickname + "!", StringComparison.Ordinal))
            {
                Network.NetworkSelfEventArgs data = null;
                // we are handling a message that is affecting this user session
                if (command == "JOIN")
                {
                    if (IsBacklog)
                    {
                        // there is no need to update anything this is a message from bouncer and that already handled
                        // everything so we just need to emit event and ignore this

                        return true;
                    }
                    // check what channel we joined
                    string channel = null;
                    if (parameters.Count > 0)
                    {
                        channel = parameters[0];
                    }
                    if (String.IsNullOrEmpty(channel) && !String.IsNullOrEmpty(message))
                    {
                        // this is a hack for uber-fucked servers that provide name of channel in message
                        // instead as a parameter
                        channel = message;
                    }
                    data = new Network.NetworkSelfEventArgs(ServerLineRawText, this.Date);
                    data.ChannelName = channel;
                    Channel joined_chan = _Network.GetChannel(channel);
                    data.Channel = joined_chan;
                    data.Source = source;
                    data.Type = Network.EventType.Join;
                    _Network.__evt_Self(data);
                    if (joined_chan == null)
                    {
                        // we aren't in this channel yet, which is expected, let's create a new instance of it
                        joined_chan = _Network.MakeChannel(channel);
                    }
                    else
                    {
                        // this is set for some unknown reasons and needs to be cleaned up
                        joined_chan.ChannelWork = true;
                        joined_chan.PartRequested = false;
                    }

                    // if this channel is not a backlog we need to reflect the change
                    if (!IsBacklog)
                    {
                        if (_Network.Config.AggressiveMode)
                        {
                            _Network.Transfer("MODE " + channel, Defs.Priority.Low);
                        }

                        if (_Network.Config.AggressiveExceptions)
                        {
                            joined_chan.IsParsingExceptionData = true;
                            _Network.Transfer("MODE " + channel + " +e", Defs.Priority.Low);
                        }

                        if (_Network.Config.AggressiveBans)
                        {
                            joined_chan.IsParsingBanData = true;
                            _Network.Transfer("MODE " + channel + " +b", Defs.Priority.Low);
                        }

                        if (_Network.Config.AggressiveInvites)
                        {
                            joined_chan.IsParsingInviteData = true;
                            _Network.Transfer("MODE " + channel + " +I", Defs.Priority.Low);
                        }

                        if (_Network.Config.AggressiveUsers)
                        {
                            joined_chan.IsParsingWhoData = true;
                            _Network.Transfer("WHO " + channel, Defs.Priority.Low);
                        }
                    }
                    return true;
                }

                if (command == "NICK")
                {
                    // it seems we changed the nickname
                    string new_nickname = message;
                    if (string.IsNullOrEmpty(new_nickname) && parameters.Count > 0 && !string.IsNullOrEmpty(parameters[0]))
                    {
                        new_nickname = parameters[0];
                        if (new_nickname.Contains(" "))
                        {
                            // server is totally borked
                            new_nickname = new_nickname.Substring(0, new_nickname.IndexOf(" ", StringComparison.Ordinal));
                        }
                    }
                    data = new Network.NetworkSelfEventArgs(ServerLineRawText, this.Date);
                    data.Source = source;
                    data.OldNick = _Network.Nickname;
                    data.NewNick = new_nickname;
                    data.Type = Network.EventType.Nick;
                    _Network.__evt_Self(data);
                    _Network.Nickname = new_nickname;
                }

                if (command == "PART")
                {
                    // we parted the channel
                    if (parameters.Count == 0)
                    {
                        // there is no information of what channel, so server is fucked up
                        return false;
                    }
                    string channel = parameters[0];
                    data = new Network.NetworkSelfEventArgs(ServerLineRawText, this.Date);
                    data.ChannelName = channel;
                    data.Source = source;
                    data.Message = message;
                    data.Type = Network.EventType.Part;
                    Channel chan = _Network.GetChannel(channel);
                    if (chan != null)
                    {
                        chan.ChannelWork = false;
                        data.Channel = chan;
                        _Network.__evt_Self(data);
                        return true;
                    }
                    _Network.__evt_Self(data);
                }
            }
            return false;
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
            if (ServerLineRawText.StartsWith(":", StringComparison.Ordinal))
            {
                List<string> parameters = new List<string>();
                string message = "";
                string source = ServerLineRawText.Substring(1);
                // some functions prefer parameters not to be converted to list so this is them
                string parameters_line = "";
                string command = "";
                if (source.Contains(" "))
                {
                    // we store the value of indexof so that we don't need to call this CPU expensive
                    // method too often
                    int index = source.IndexOf(" ", StringComparison.Ordinal);
                    command = source.Substring(index + 1);
                    source = source.Substring(0, index);
                    if (command.Contains(" :"))
                    {
                        index = command.IndexOf(" :", StringComparison.Ordinal);
                        if (index < 0)
                        {
                            _Protocol.DebugLog("Malformed text, probably hacker: " + ServerLineRawText);
                            return false;
                        }
                        message = command.Substring(index + 2);
                        command = command.Substring(0, index);
                    }
                    // check if there aren't some extra parameters for this command
                    if (command.Contains(" "))
                    {
                        // we remove the command name and split all the parameters by space
                        index = command.IndexOf(" ", StringComparison.Ordinal);
                        parameters_line = command.Substring(index + 1);
                        parameters.AddRange(parameters_line.Split(' '));
                        command = command.Substring(0, index);
                    }
                    // commands are meant to be uppercase but for compatibility reasons we ensure it is
                    command = command.ToUpper();
                }
                if (_Network.__evt_SubscribedNetworkRawData)
                {
                    Network.IncomingDataEventArgs info = new Network.IncomingDataEventArgs();
                    info.Message = message;
                    info.Date = this.Date;
                    info.ParameterLine = parameters_line;
                    info.Source = source;
                    info.ServerLine = ServerLineRawText;
                    info.Command = command;
                    info.Parameters = parameters;
                    if (_Network.__evt__IncomingData(info))
                    {
                        return true;
                    }
                }
                if (ProcessSelf(source, command, parameters, message))
                {
                    OK = true;
                }
                switch (command)
                {
                    case "001":
                    case "002":
                    case "003":
                    case "004":
                        Network.NetworkGenericDataEventArgs args004 = new Network.NetworkGenericDataEventArgs(this.ServerLineRawText, this.Date);
                        args004.Command = command;
                        args004.ParameterLine = parameters_line;
                        args004.Parameters = parameters;
                        args004.Message = message;
                        _Network.__evt_INFO(args004);
                        break;
                    case "005":
                        Info(command, parameters, parameters_line, message);
                        if (!_Network.IsLoaded)
                        {
                            //Hooks._Network.AfterConnectToNetwork(_Network);
                        }
                        break;
                    case "301":
                        if (Idle2(command, parameters_line, message))
                        {
                            return true;
                        }
                        break;
                    case "305":
                        if (!IsBacklog)
                        {
                            _Network.IsAway = false;
                        }
                        break;
                    case "306":
                        if (!IsBacklog)
                        {
                            _Network.IsAway = true;
                        }
                        break;
                    case "311":
                        if (WhoisLoad(command, parameters_line, message))
                        {
                            return true;
                        }
                        break;
                    case "312":
                        if (WhoisSv(command, parameters_line, message))
                        {
                            return true;
                        }
                        break;
                    case "315":
                        if (FinishChan(parameters))
                        {
                            return true;
                        }
                        break;
                    case "317":
                        if (IdleTime(command, parameters_line))
                        {
                            return true;
                        }
                        break;
                    case "318":
                        if (WhoisFn(command, parameters_line, message))
                        {
                            return true;
                        }
                        break;
                    case "319":
                        if (WhoisCh(command, parameters_line, message))
                        {
                            return true;
                        }
                        break;
                    case "321":
                        if (_Network.SuppressData)
                        {
                            return true;
                        }
                        break;
                    case "322":
                        if (ChannelData(command, parameters_line, message))
                        {
                            return true;
                        }
                        break;
                    case "323":
                        if (IsBacklog)
                        {
                            return true;
                        }
                        if (_Network.SuppressData)
                        {
                            _Network.SuppressData = false;
                            return true;
                        }
                        _Network.DownloadingList = false;
                        break;
                    case "324":
                        if (ChannelInfo(parameters, command, source, message))
                        {
                            return true;
                        }
                        break;
                    case "332":
                        if (ChannelTopic(parameters, command, source, message))
                        {
                            return true;
                        }
                        break;
                    case "333":
                        if (TopicInfo(parameters))
                        {
                            return true;
                        }
                        break;
                    case "352":
                        if (ParseUser(parameters, message))
                        {
                            return true;
                        }
                        break;
                    case "353":
                        if (ParseInfo(parameters, message))
                        {
                            return true;
                        }
                        break;
                    case "366":
                        return true;
                    case "367":
                        if (ChannelBans(parameters))
                        {
                            return true;
                        }
                        break;
                    case "368":
                        if (ChannelBans2(parameters))
                        {
                            return true;
                        }
                        break;
                    case "433":
                        if (!IsBacklog && !_Network.UsingNick2)
                        {
                            string nick = _Network.Config.GetNick2();
                            _Network.UsingNick2 = true;
                            _Network.Transfer("NICK " + nick, Defs.Priority.High);
                            _Network.Nickname = nick;
                        }
                        break;
                    case "307":
                    case "310":
                    case "313":
                    case "378":
                    case "671":
                        if (WhoisText(command, parameters_line, message))
                        {
                            return true;
                        }
                        break;
                    case "PING":
                        if (Pong(command, parameters_line, message))
                        {
                            return true;
                        }
                        break;
                    case "PONG":
                        pong = DateTime.Now;
                        return true;
                    case "INFO":
                        //_Network.SystemWindow.scrollback.InsertText(text.Substring(text.IndexOf("INFO", StringComparison.Ordinal) + 5), Pidgeon.ContentLine.MessageStyle.User,                                                                     true, date, !updated_text);
                        return true;
                    case "NOTICE":
                        Network.NetworkNOTICEEventArgs notice = new Network.NetworkNOTICEEventArgs(ServerLineRawText, this.Date);
                        notice.Source = source;
                        notice.Message = message;
                        notice.ParameterLine = parameters_line;
                        _Network.__evt_NOTICE(notice);
                        return true;
                    case "NICK":
                        if (ProcessNick(source, parameters_line, message))
                        {
                            return true;
                        }
                        break;
                    case "INVITE":
                        if (Invite(source, parameters_line, message))
                        {
                            return true;
                        }
                        break;
                    case "PRIVMSG":
                        if (ProcessPM(source, parameters_line, message))
                        {
                            return true;
                        }
                        break;
                    case "TOPIC":
                        if (Topic(source, parameters_line, message))
                        {
                            return true;
                        }
                        break;
                    case "MODE":
                        if (Mode(source, parameters_line))
                        {
                            return true;
                        }
                        break;
                    case "PART":
                        if (Part(source, parameters_line, message))
                        {
                            return true;
                        }
                        break;
                    case "QUIT":
                        if (Quit(source, parameters_line, message))
                        {
                            return true;
                        }
                        break;
                    case "JOIN":
                        if (Join(source, parameters_line, message))
                        {
                            return true;
                        }
                        break;
                    case "KICK":
                        if (Kick(source, parameters, parameters_line, message))
                        {
                            return true;
                        }
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
                // we have no idea what we just were to parse so flag is as unknown data and let client parse that
                Network.UnknownDataEventArgs ev = new Network.UnknownDataEventArgs(this.ServerLineRawText);
                ev.Date = this.Date;
                _Network.HandleUnknownData(ev);
            }
            return true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="libirc.ProcessorIRC"/> class.
        /// </summary>
        /// <param name="_network">_network.</param>
        /// <param name="_text">_text.</param>
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
