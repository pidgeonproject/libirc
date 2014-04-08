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
        private bool Info(string command, List<string> parameters_, string parameters_line, string value)
        {
            Network.NetworkGenericDataEventArgs args004 = new Network.NetworkGenericDataEventArgs(this.ServerLineRawText, this.Date);
            args004.Parameters = parameters_;
            args004.Command = command;
            args004.ParameterLine = parameters_line;
            args004.Message = value;
            _Network.__evt_INFO(args004);
            if (parameters_line.Contains("PREFIX=("))
            {
                string cmodes = parameters_line.Substring(parameters_line.IndexOf("PREFIX=(", StringComparison.Ordinal) + 8);
                cmodes = cmodes.Substring(0, cmodes.IndexOf(")", StringComparison.Ordinal));
                lock (_Network.CUModes)
                {
                    _Network.CUModes.Clear();
                    _Network.CUModes.AddRange(cmodes.ToArray<char>());
                }
                cmodes = parameters_line.Substring(parameters_line.IndexOf("PREFIX=(", StringComparison.Ordinal) + 8);
                cmodes = cmodes.Substring(cmodes.IndexOf(")", StringComparison.Ordinal) + 1, _Network.CUModes.Count);

                _Network.UChars.Clear();
                _Network.UChars.AddRange(cmodes.ToArray<char>());
            }
            if (parameters_line.Contains("CHANMODES="))
            {
                string xmodes = parameters_line.Substring(parameters_line.IndexOf("CHANMODES=", StringComparison.Ordinal) + 11);
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
            string channel_name = parameters.Substring(parameters.IndexOf(" ", StringComparison.Ordinal) + 1);
            uint user_count = 0;
            if (channel_name.Contains(" "))
            {
                if (!uint.TryParse(channel_name.Substring(channel_name.IndexOf(" ", StringComparison.Ordinal) + 1), out user_count))
                {
                    user_count = 0;
                }

                channel_name = channel_name.Substring(0, channel_name.IndexOf(" ", StringComparison.Ordinal));
            }

            _Network.DownloadingList = true;

            lock (_Network.ChannelList)
            {
                Network.ChannelData channel = _Network.ContainsChannel(channel_name);
                if (channel == null)
                {
                    channel = new Network.ChannelData(user_count, channel_name, value);
                    _Network.ChannelList.Add(channel);
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
            }
            return false;
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
            string chan = null;
            chan = parameters.Replace(" ", "");
            Network.NetworkPRIVMSGEventArgs ev = new Network.NetworkPRIVMSGEventArgs(ServerLineRawText, this.Date);
            ev.Source = source;
            ev.Message = value;
            if (!chan.Contains(_Network.ChannelPrefix))
            {
                string uc;
                if (ev.Message.StartsWith(_Protocol.Separator.ToString(), StringComparison.Ordinal))
                {
                    string trimmed = ev.Message;
                    if (trimmed.StartsWith(_Protocol.Separator.ToString(), StringComparison.Ordinal))
                    {
                        trimmed = trimmed.Substring(1);
                    }
                    if (trimmed.Length > 1 && trimmed[trimmed.Length - 1] == _Protocol.Separator)
                    {
                        trimmed = trimmed.Substring(0, trimmed.Length - 1);
                    }
                    if (ev.Message.StartsWith(_Protocol.Separator.ToString() + "ACTION", StringComparison.Ordinal))
                    {
                        ev.Message = ev.Message.Substring("xACTION".Length);
                        if (ev.Message.Length > 1 && ev.Message.EndsWith(_Protocol.Separator.ToString(), StringComparison.Ordinal))
                        {
                            ev.Message = ev.Message.Substring(0, ev.Message.Length - 1);
                        }
                        ev.IsAct = true;
                        _Network.__evt_PRIVMSG(ev);
                        return true;
                    }

                    uc = ev.Message.Substring(1);
                    if (uc.Contains(_Protocol.Separator.ToString()))
                    {
                        uc = uc.Substring(0, uc.IndexOf(_Protocol.Separator.ToString(), StringComparison.Ordinal));
                    }
                    if (uc.Contains(" "))
                    {
                        uc = uc.Substring(0, uc.IndexOf(" ", StringComparison.Ordinal));
                    }
                    uc = uc.ToUpper();
                    Network.NetworkCTCPEventArgs ctcp = new Network.NetworkCTCPEventArgs(ServerLineRawText, this.Date);
                    ctcp.CTCP = uc;
                    ctcp.Message = ev.Message;
                    _Network.__evt_CTCP(ctcp);
                    return true;
                }
                _Network.__evt_PRIVMSG(ev);
                return true;
            }
            else
            {
                Channel channel = null;
                channel = _Network.GetChannel(chan);
                ev.Channel = channel;
                ev.ChannelName = chan;
                if (channel != null)
                {
                    if (ev.Message.StartsWith(_Protocol.Separator.ToString() + "ACTION", StringComparison.Ordinal))
                    {
			ev.IsAct = true;
                        ev.Message = ev.Message.Substring("xACTION".Length);
                        if (ev.Message.Length > 1 && ev.Message.EndsWith(_Protocol.Separator.ToString(), StringComparison.Ordinal))
                        {
                            ev.Message = ev.Message.Substring(0, ev.Message.Length - 1);
                        }
		    }
		}
                _Network.__evt_PRIVMSG(ev);
                return true;
            }
        }

        private bool Idle2(string source, string parameters, string value)
        {
            Network.NetworkWHOISEventArgs ev = new Network.NetworkWHOISEventArgs(this.ServerLineRawText, this.Date);
            ev.ParameterLine = parameters;
            ev.WhoisType = Network.NetworkWHOISEventArgs.Mode.Uptime;
            _Network.__evt_WHOIS(ev);
            return true;
        }

        private bool WhoisText(string source, string parameters, string value)
        {
            if (parameters.Contains(" "))
            {
                Network.NetworkWHOISEventArgs ev = new Network.NetworkWHOISEventArgs(this.ServerLineRawText, this.Date);
                ev.WhoisType = Network.NetworkWHOISEventArgs.Mode.Info;
                ev.Message = value;
                ev.ParameterLine = parameters;
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
        private bool WhoisLoad(string source, string parameters, string value)
        {
            if (!parameters.Contains(" "))
            {
                return false;
            }
            Network.NetworkWHOISEventArgs ev = new Network.NetworkWHOISEventArgs(this.ServerLineRawText, this.Date);
            ev.WhoisType = Network.NetworkWHOISEventArgs.Mode.Header;
            ev.ParameterLine = parameters;
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
        private bool WhoisFn(string source, string parameters, string value)
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
        private bool WhoisSv(string source, string parameters, string value)
        {
            if (parameters.Contains(" "))
            {
                Network.NetworkWHOISEventArgs ev = new Network.NetworkWHOISEventArgs(this.ServerLineRawText, this.Date);
                ev.ParameterLine = parameters;
                ev.WhoisType = Network.NetworkWHOISEventArgs.Mode.Server;
                string name = parameters.Substring(parameters.IndexOf(" ", StringComparison.Ordinal) + 1);
                if (!name.Contains(" "))
                {
                    _Protocol.DebugLog("Invalid whois record " + parameters);
                    return false;
                }
                ev.WhoisLine = name.Substring(name.IndexOf(" ", StringComparison.Ordinal) + 1);
                ev.Source = name.Substring(0, name.IndexOf(" ", StringComparison.Ordinal));
                _Network.__evt_WHOIS(ev);
                return true;
            }
            return false;
        }

        private bool Invite(string source, string parameters, string value)
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
            UserInfo user = new UserInfo(source);
            Network.NetworkGenericDataEventArgs ev = new Network.NetworkGenericDataEventArgs(this.ServerLineRawText, this.Date);
            ev.Message = value;
            ev.ParameterLine = parameters;
            ev.Source = source;
            _Network.__evt_QUIT(ev);
            foreach (Channel item in _Network.Channels.Values)
            {
                if (item.ChannelWork)
                {
                    if (!IsBacklog)
                    {
                        item.RemoveUser(user.Nick);
                    }
                }
            }
            return true;
        }
    }
}
