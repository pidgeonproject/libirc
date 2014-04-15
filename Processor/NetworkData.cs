//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or   
//  (at your option) version 3.                                         

//  This program is distributed in the hope that it will be useful,     
//  but WITHOUT ANY WARRANTY; without even the implied warranty of      
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the       
//  GNU General Public License for more details.                        

//  You should have received a copy of the GNU General Public License   
//  along with this program; if not, write to the                       
//  Free Software Foundation, Inc.,                                     
//  51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace libirc
{
    /// <summary>
    /// IRC protocol handler
    /// </summary>
    public partial class ProcessorIRC
    {
        /// <summary>
        /// Retrieve information about the server
        /// </summary>
        /// <param name="command">Command</param>
        /// <param name="parameters">Parameters</param>
        /// <param name="value">Text</param>
        /// <returns></returns>
        private bool Info(Network.IncomingDataEventArgs info)
        {
            Network.NetworkGenericDataEventArgs args004 = new Network.NetworkGenericDataEventArgs(info);
            _Network.__evt_INFO(args004);
            if (info.ParameterLine.Contains("PREFIX=("))
            {
                string cmodes = info.ParameterLine.Substring(info.ParameterLine.IndexOf("PREFIX=(", StringComparison.Ordinal) + 8);
                cmodes = cmodes.Substring(0, cmodes.IndexOf(")", StringComparison.Ordinal));
                lock (_Network.CUModes)
                {
                    _Network.CUModes.Clear();
                    _Network.CUModes.AddRange(cmodes.ToArray<char>());
                }
                cmodes = info.ParameterLine.Substring(info.ParameterLine.IndexOf("PREFIX=(", StringComparison.Ordinal) + 8);
                cmodes = cmodes.Substring(cmodes.IndexOf(")", StringComparison.Ordinal) + 1, _Network.CUModes.Count);
                _Network.UChars.Clear();
                _Network.UChars.AddRange(cmodes.ToArray<char>());
            }
            if (info.ParameterLine.Contains("CHANMODES="))
            {
                string xmodes = info.ParameterLine.Substring(info.ParameterLine.IndexOf("CHANMODES=", StringComparison.Ordinal) + 11);
                xmodes = xmodes.Substring(0, xmodes.IndexOf(" ", StringComparison.Ordinal));
                string[] _mode = xmodes.Split(',');
                _Network.ParsedInfo = true;
                if (_mode.Length == 4)
                {
                    _Network.PModes.Clear();
                    _Network.CModes.Clear();
                    _Network.XModes.Clear();
                    _Network.SModes.Clear();
                    _Network.PModes.AddRange(_mode[0].ToArray<char>());
                    _Network.XModes.AddRange(_mode[1].ToArray<char>());
                    _Network.SModes.AddRange(_mode[2].ToArray<char>());
                    _Network.CModes.AddRange(_mode[3].ToArray<char>());
                }
            }
            return true;
        }

        private bool ChannelData(string command, string parameters, string value)
        {
            if (!parameters.Contains(" "))
            {
                return false;
            }
            string channel_name = parameters.Substring(parameters.IndexOf(" ", StringComparison.Ordinal) + 1);
            uint user_count = 0;
            if (channel_name.Contains(" "))
            {
                int index = channel_name.IndexOf(" ", StringComparison.Ordinal);
                if (!uint.TryParse(channel_name.Substring(index + 1), out user_count))
                {
                    user_count = 0;
                }
                channel_name = channel_name.Substring(0, index);
            }
            _Network.DownloadingList = true;
            Network.ChannelData channel = _Network.ContainsChannel(channel_name);
            if (channel == null)
            {
                channel = new Network.ChannelData(user_count, channel_name, value);
                lock (_Network.ChannelList)
                {
                    _Network.ChannelList.Add(channel);
                }
            }
            else
            {
                channel.UserCount = user_count;
                channel.ChannelTopic = value;
            }
            if (_Network.SuppressData)
            {
                return true;
            }
            return IsBacklog;
        }

        /// <summary>
        /// This is called for PRIVMSG from server
        /// </summary>
        /// <param name="source"></param>
        /// <param name="parameters"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool ProcessPM(string source, string parameters, string value)
        {
            string message_target = null;
            message_target = parameters.Trim();
            Network.NetworkPRIVMSGEventArgs ev = new Network.NetworkPRIVMSGEventArgs(ServerLineRawText, this.Date);
            ev.Source = source;
            ev.Message = value;
            if (!message_target.StartsWith(_Network.ChannelPrefix))
            {
                // target is not a channel
                if (ev.Message.StartsWith(_Protocol.Separator.ToString(), StringComparison.Ordinal))
                {
                    // this seems to be a CTCP message
                    string trimmed = ev.Message.Trim(_Protocol.Separator);
                    if (ev.Message.StartsWith(_Protocol.Separator.ToString() + "ACTION", StringComparison.Ordinal))
                    {
                        // it's an ACT type
                        ev.Message = ev.Message.Substring(7).TrimEnd(_Protocol.Separator);
                        ev.IsAct = true;
                        _Network.__evt_PRIVMSG(ev);
                        return true;
                    }
                    string message_ctcp = trimmed;
                    string text = "";
                    if (message_ctcp.Contains(" "))
                    {
                        // remove all CTCP parameters so that we have only the CTCP command as it is
                        // like PING etc
                        int index = message_ctcp.IndexOf(" ", StringComparison.Ordinal);
                        text = message_ctcp.Substring(index + 1);
                        message_ctcp = message_ctcp.Substring(0, index);
                    }
                    message_ctcp = message_ctcp.ToUpper();
                    Network.NetworkCTCPEventArgs ctcp = new Network.NetworkCTCPEventArgs(ServerLineRawText, this.Date);
                    ctcp.CTCP = message_ctcp;
                    ctcp.Args = text;
                    ctcp.Message = ev.Message;
                    _Network.__evt_CTCP(ctcp);
                    return true;
                }
                // it's a private message
                _Network.__evt_PRIVMSG(ev);
                return true;
            }
            else
            {
                Channel channel = null;
                channel = _Network.GetChannel(message_target);
                ev.Channel = channel;
                ev.ChannelName = message_target;
                if (channel != null)
                {
                    if (ev.Message.StartsWith(_Protocol.Separator.ToString() + "ACTION", StringComparison.Ordinal))
                    {
                        ev.IsAct = true;
                        ev.Message = ev.Message.Substring(7).Trim(_Protocol.Separator);
                    }
                }
                _Network.__evt_PRIVMSG(ev);
                return true;
            }
        }

        private bool Mode(string source, string parameters)
        {
            if (!parameters.Contains(" "))
            {
                // this is some borked server text
                return false;
            }
            int index = parameters.IndexOf(" ", StringComparison.Ordinal);
            string channel_name = parameters.Substring(0, index).Trim();
            if (channel_name.StartsWith(_Network.ChannelPrefix, StringComparison.Ordinal))
            {
                Channel channel = _Network.GetChannel(channel_name);
                Network.NetworkMODEEventArgs ev = new Network.NetworkMODEEventArgs(this.ServerLineRawText, this.Date);
                ev.ChannelName = channel_name;
                ev.Channel = channel;
                ev.Source = source;
                ev.ParameterLine = parameters;
                if (channel != null)
                {
                    if (IsBacklog)
                    {
                        // we don't want to apply this mode here
                        return true;
                    }
                    string change = parameters.Substring(index).Trim();
                    Formatter formatter = new Formatter();
                    ev.SimpleMode = change;
                    // we get all the mode changes for this channel
                    formatter.RewriteBuffer(change, _Network);
                    ev.FormattedMode = formatter;
                    channel.ChannelMode.ChangeMode("+" + formatter.channelModes);
                    foreach (SimpleMode m in formatter.getMode)
                    {
                        if (_Network.CUModes.Contains(m.Mode) && m.ContainsParameter)
                        {
                            User flagged_user = channel.UserFromName(m.Parameter);
                            if (flagged_user != null)
                            {
                                flagged_user.ChannelMode.ChangeMode("+" + m.Mode);
                                flagged_user.ResetMode();
                            }
                        }
                        if (m.ContainsParameter)
                        {
                            switch (m.Mode.ToString())
                            {
                                case "b":
                                    channel.InsertBan(m.Parameter, source);
                                    break;
                            }
                        }
                    }
                    foreach (SimpleMode m in formatter.getRemovingMode)
                    {
                        if (_Network.CUModes.Contains(m.Mode) && m.ContainsParameter)
                        {
                            User flagged_user = channel.UserFromName(m.Parameter);
                            if (flagged_user != null)
                            {
                                flagged_user.ChannelMode.ChangeMode("-" + m.Mode);
                                flagged_user.ResetMode();
                            }
                        }
                        if (m.ContainsParameter)
                        {
                            switch (m.Mode.ToString())
                            {
                                case "b":
                                    channel.RemoveBan(m.Parameter);
                                    break;
                            }
                        }
                    }
                    // we know the channel and we changed its mode, no need to diplay this line
                    _Network.__evt_MODE(ev);
                    return true;
                }
            }
            else
            {
                // this is a change of user mode, let's display it in system window
                return false;
            }
            return IsBacklog;
        }

        private bool Idle2(string source, string parameters)
        {
            Network.NetworkWHOISEventArgs ev = new Network.NetworkWHOISEventArgs(this.ServerLineRawText, this.Date);
            ev.ParameterLine = parameters;
            ev.WhoisType = Network.NetworkWHOISEventArgs.Mode.Uptime;
            _Network.__evt_WHOIS(ev);
            return true;
        }

        private bool WhoisText(string source, string parameter_line, List<string> parameters, string value)
        {
            if (parameter_line.Contains(" "))
            {
                Network.NetworkWHOISEventArgs ev = new Network.NetworkWHOISEventArgs(this.ServerLineRawText, this.Date);
                ev.WhoisType = Network.NetworkWHOISEventArgs.Mode.Info;
                ev.Parameters = parameters;
                ev.Message = value;
                ev.ParameterLine = parameter_line;
                _Network.__evt_WHOIS(ev);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Parsing the first line of whois
        /// </summary>
        /// <param name="source"></param>
        /// <param name="parameters"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool WhoisLoad(string source, string parameterl, List<String> parameters, string message)
        {
            if (!parameterl.Contains(" "))
            {
                return false;
            }
            Network.NetworkWHOISEventArgs ev = new Network.NetworkWHOISEventArgs(this.ServerLineRawText, this.Date);
            ev.WhoisType = Network.NetworkWHOISEventArgs.Mode.Header;
            ev.ParameterLine = parameterl;
            ev.Parameters = parameters;
            ev.Message = message;
            _Network.__evt_WHOIS(ev);
            return true;
        }

        /// <summary>
        /// Parsing last line of whois
        /// </summary>
        /// <param name="source"></param>
        /// <param name="parameters"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool WhoisFn(string source, string parameters)
        {
            Network.NetworkWHOISEventArgs ev = new Network.NetworkWHOISEventArgs(this.ServerLineRawText, this.Date);
            ev.WhoisType = Network.NetworkWHOISEventArgs.Mode.Footer;
            ev.ParameterLine = parameters;
            ev.Source = source;
            _Network.__evt_WHOIS(ev);
            return true;
        }

        /// <summary>
        /// Parsing the channels of whois
        /// </summary>
        /// <param name="source"></param>
        /// <param name="parameters"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool WhoisCh(string source, string parameters, string value)
        {
            if (parameters.Contains(" "))
            {
                Network.NetworkWHOISEventArgs ev = new Network.NetworkWHOISEventArgs(this.ServerLineRawText, this.Date);
                ev.WhoisType = Network.NetworkWHOISEventArgs.Mode.Channels;
                ev.Source = source;
                ev.Message = value;
                ev.ParameterLine = parameters;
                _Network.__evt_WHOIS(ev);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Parsing the line of whois text
        /// </summary>
        /// <param name="source"></param>
        /// <param name="parameters"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool WhoisSv(string source, string parameters)
        {
            if (parameters.Contains(" "))
            {
                string name = parameters.Substring(parameters.IndexOf(" ", StringComparison.Ordinal) + 1);
                if (!name.Contains(" "))
                {
                    _Protocol.DebugLog("Invalid whois record " + parameters);
                    return false;
                }
                Network.NetworkWHOISEventArgs ev = new Network.NetworkWHOISEventArgs(this.ServerLineRawText, this.Date);
                ev.ParameterLine = parameters;
                ev.WhoisType = Network.NetworkWHOISEventArgs.Mode.Server;
                ev.WhoisLine = name.Substring(name.IndexOf(" ", StringComparison.Ordinal) + 1);
                ev.Source = name.Substring(0, name.IndexOf(" ", StringComparison.Ordinal));
                _Network.__evt_WHOIS(ev);
                return true;
            }
            return false;
        }

        private bool Invite(string source, string parameters)
        {
            Network.NetworkChannelDataEventArgs ev = new Network.NetworkChannelDataEventArgs(this.ServerLineRawText, this.Date);
            ev.Source = source;
            ev.ChannelName = parameters;
            _Network.__evt_INVITE(ev);
            return true;
        }

        private bool IdleTime(string source, string parameters)
        {
            if (parameters.Contains(" "))
            {
                Network.NetworkWHOISEventArgs ev = new Network.NetworkWHOISEventArgs(this.ServerLineRawText, this.Date);
                ev.Source = source;
                ev.ParameterLine = parameters;
                ev.WhoisType = Network.NetworkWHOISEventArgs.Mode.Uptime;
                _Network.__evt_WHOIS(ev);
                return true;
            }
            return false;
        }

        private bool Quit(string source, string parameters, string value)
        {
            Network.NetworkGenericDataEventArgs ev = new Network.NetworkGenericDataEventArgs(this.ServerLineRawText, this.Date);
            ev.Message = value;
            ev.ParameterLine = parameters;
            ev.Source = source;
            _Network.__evt_QUIT(ev);
            foreach (Channel item in _Network.Channels.Values)
            {
                if (item.ChannelWork && !IsBacklog)
                {
                    item.RemoveUser(ev.SourceInfo.Nick);
                }
            }
            return true;
        }
    }
}
