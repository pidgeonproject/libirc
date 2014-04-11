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

namespace libirc
{
    public partial class ProcessorIRC
    {
        private bool CreationDatetime(List<string> parameters)
        {
            if (parameters.Count > 2)
            {
                Channel channel = _Network.GetChannel(parameters[1]);
                if (channel != null)
                {
                    double time;
                    if (double.TryParse(parameters[2], out time))
                    {
                        channel.CreationTime = Defs.ConvertFromUNIX(time);
                        return true;
                    }
                }
            }
            return false;
        }

        private bool Website(List<string> parameters, string message)
        {
            if (parameters.Count > 1)
            {
                Channel channel = _Network.GetChannel(parameters[1]);
                if (channel != null)
                {
                    channel.Website = message;
                    return true;
                }
            }
            return false;
        }

        private bool ChannelInfo(List<string> parameters, string command, string source, string _value)
        {
            if (parameters.Count > 2)
            {
                Channel channel = _Network.GetChannel(parameters[1]);
                Network.NetworkChannelDataEventArgs args = new Network.NetworkChannelDataEventArgs(this.ServerLineRawText, this.Date);
                args.Command = command;
                args.Message = _value;
                args.Parameters = parameters;
                args.Channel = channel;
                args.ChannelName = parameters[1];
                if (channel != null)
                {
                    channel.ChannelMode.ChangeMode(parameters[2]);
                    _Network.__evt_ChannelInfo(args);
                    return true;
                }
                _Network.__evt_ChannelInfo(args);
            }
            return IsBacklog;
        }

        private bool ParseUser(List<string> parameters, string realname)
        {
            // :hub.tm-irc.org 352 petr #support pidgeon D3EE8257.8361F8AE.37E3A027.IP hub.tm-irc.org petr H :0 My name is hidden, dude
            if (parameters.Count <= 6)
            {
                return false;
            }
            Network.NetworkParseUserEventArgs ev = new Network.NetworkParseUserEventArgs(ServerLineRawText, this.Date);
            ev.Parameters = parameters;
            ev.ChannelName = parameters[1];
            ev.Channel = _Network.GetChannel(parameters[1]);
            string server = parameters[4];
            ev.User = new UserInfo();
            ev.User.Ident = parameters[2];
            ev.User.Host = parameters[3];
            ev.User.Nick = parameters[5];
            if (realname != null & realname.Length > 2)
            {
                realname = realname.Substring(2);
            }
            else if (realname == "0 ")
            {
                realname = "";
            }
            char mode = '\0';
            ev.IsAway = false;
            if (parameters[6].Length > 0)
            {
                // if user is away we flag him
                if (parameters[6].StartsWith("G", StringComparison.Ordinal))
                {
                    ev.IsAway = true;
                }
                mode = parameters[6][parameters[6].Length - 1];
                if (!_Network.UChars.Contains(mode))
                {
                    mode = '\0';
                }
                ev.StringMode = mode.ToString();
            }
            ev.RealName = realname;
            if (ev.Channel != null)
            {
                if (!IsBacklog)
                {
                    if (!ev.Channel.ContainsUser(ev.User.Nick))
                    {
                        User _user = null;
                        if (mode != '\0')
                        {
                            _user = new User(mode.ToString() + ev.User.Nick, ev.User.Host, ev.User.Ident, _Network);
                        }
                        else
                        {
                            _user = new User(ev.User, _Network);
                        }
                        _user.LastAwayCheck = DateTime.Now;
                        _user.RealName = realname;
                        if (ev.IsAway)
                        {
                            _user.AwayTime = DateTime.Now;
                        }
                        _user.Away = ev.IsAway;
                        ev.Channel.InsertUser(_user);
                        _Network.__evt_ParseUser(ev);
                        return true;
                    }
                    User user = ev.Channel.UserFromName(ev.User.Nick);
                    if (user != null)
                    {
                        user.Ident = ev.User.Ident;
                        user.Host = ev.User.Host;
                        user.Server = server;
                        user.RealName = realname;
                        user.LastAwayCheck = DateTime.Now;
                        if (!user.Away && ev.IsAway)
                        {
                            user.AwayTime = DateTime.Now;
                        }
                        user.Away = ev.IsAway;
                    }
                }
                _Network.__evt_ParseUser(ev);
                if (ev.Channel.IsParsingWhoData)
                {
                    return true;
                }
                return IsBacklog;
            }
            _Network.__evt_ParseUser(ev);
            return IsBacklog;
        }

        private bool ParseInfo(List<string> parameters, string value)
        {
            if (parameters.Count > 2)
            {
                // :irc-2t.tm-irc.org 353 petr = #support :petr user1227554 &OperBot Revi 
                if (IsBacklog)
                {
                    return true;
                }
                Network.ChannelUserListEventArgs ev = new Network.ChannelUserListEventArgs(ServerLineRawText, this.Date);
                ev.UserNicknames.AddRange(value.Split(' '));
                ev.ChannelName = parameters[2];
                Channel channel = _Network.GetChannel(parameters[2]);
                ev.Parameters = parameters;
                if (channel != null)
                {
                    ev.Channel = channel;
                    foreach (string nick in ev.UserNicknames)
                    {
                        if (String.IsNullOrEmpty(nick))
                        {
                            continue;
                        }
                        User user = channel.UserFromName(nick);
                        if (user == null)
                        {
                            user = new User(nick, _Network);
                            channel.InsertUser(user);
                        }
                        else
                        {
                            char UserMode_ = '\0';
                            if (nick.Length > 0)
                            {
                                foreach (char mode in _Network.UChars)
                                {
                                    if (nick[0] == mode)
                                    {
                                        UserMode_ = nick[0];
                                        // there is no need to check for other modes
                                        break;
                                    }
                                }
                                user.SymbolMode(UserMode_);
                            }
                        }
                        ev.Users.Add(user);
                    }
                    _Network.__evt_ChannelUserList(ev);
                    return true;
                }
                _Network.__evt_ChannelUserList(ev);
            }
            return IsBacklog;
        }

        private bool Topic(string source, string parameters, string value)
        {
            string chan = parameters;
            chan = chan.Replace(" ", "");
            Channel channel = _Network.GetChannel(chan);
            Network.NetworkTOPICEventArgs ev = new Network.NetworkTOPICEventArgs(this.ServerLineRawText, this.Date);
            ev.Source = source;
            ev.ChannelName = chan;
            ev.Topic = value;
            if (channel != null)
            {
                channel.Topic = value;
                if (!IsBacklog)
                {
                    double time = Defs.ConvertDateToUnix(DateTime.Now);
                    channel.TopicDate = (int)time;
                    ev.TopicDate = time;
                    channel.TopicUser = source;
                }
                _Network.__evt_TOPIC(ev);
                return true;
            }
            _Network.__evt_TOPIC(ev);
            return IsBacklog;
        }

        private bool TopicInfo(List<string> parameters)
        {
            if (parameters.Count > 3)
            {
                Network.NetworkTOPICEventArgs ev = new Network.NetworkTOPICEventArgs(this.ServerLineRawText, this.Date);
                ev.Parameters = parameters;
                ev.ChannelName = parameters[1];
                string user = parameters[2];
                string time = parameters[3];
                double dt;
                if (!double.TryParse(time, out dt))
                {
                    dt = 0;
                }
                Channel channel = _Network.GetChannel(parameters[1]);
                ev.TopicDate = dt;
                ev.Source = user;
                if (channel != null)
                {
                    channel.TopicDate = (int)dt;
                    channel.TopicUser = user;
                }
                _Network.__evt_TopicInfo(ev);
                return true;
            }
            return IsBacklog;
        }

        private bool ChannelTopic(List<string> parameters, string command, string source, string message)
        {
            if (parameters.Count > 1)
            {
                Network.NetworkTOPICEventArgs ev = new Network.NetworkTOPICEventArgs(this.ServerLineRawText, this.Date);
                ev.Parameters = parameters;
                ev.Topic = message;
                ev.ChannelName = parameters[1];
                string topic = message;
                Channel channel = _Network.GetChannel(parameters[1]);
                ev.Channel = channel;
                _Network.__evt_TopicData(ev);
                if (channel != null)
                {
                    channel.Topic = topic;
                    return true;
                }
            }
            return IsBacklog;
        }

        private bool FinishChan(List<string> code)
        {
            if (code.Count > 0)
            {
                Network.NetworkChannelDataEventArgs ev = new Network.NetworkChannelDataEventArgs(this.ServerLineRawText, this.Date);
                ev.ChannelName = code[1];
                ev.Parameters = code;

                Channel channel = _Network.GetChannel(code[1]);
                if (channel != null)
                {
                    ev.Channel = channel;
                    channel.IsParsingWhoData = false;
                }
                _Network.__evt_FinishChannelParseUser(ev);
                return true;
            }
            return IsBacklog;
        }

        private bool Kick(string source, List<string> parameters, string parameter_line, string value)
        {
            // petan!pidgeon@petan.staff.tm-irc.org KICK #support HelpBot :Removed from the channel
            string chan = parameters[0];
            Network.NetworkKickEventArgs ev = new Network.NetworkKickEventArgs(this.ServerLineRawText, this.Date);
            ev.Source = source;
            ev.Parameters = parameters;
            ev.ChannelName = chan;
            ev.Message = value;
            ev.Target = parameters[1];
            ev.ParameterLine = parameter_line;
            Channel channel = _Network.GetChannel(chan);
            if (channel != null)
            {
                ev.Channel = channel;
                if (!IsBacklog)
                {
                    User user = channel.UserFromName(parameters[1]);
                    if (user != null)
                    {
                        channel.RemoveUser(user);
                        if (user.IsPidgeon)
                        {
                            channel.ChannelWork = false;
                        }
                    }
                }
                _Network.__evt_KICK(ev);
                return true;
            }
            _Network.__evt_KICK(ev);
            return IsBacklog;
        }

        private bool Join(string source, string parameters, string value)
        {
            string channel_name = parameters.Trim();
            if (string.IsNullOrEmpty(channel_name))
            {
                channel_name = value;
            }
            Channel channel = _Network.GetChannel(channel_name);
            Network.NetworkChannelEventArgs ed = new Network.NetworkChannelEventArgs(ServerLineRawText, this.Date);
            ed.ChannelName = channel_name;
            ed.Source = source;
            ed.Channel = channel;
            ed.ParameterLine = parameters;
            if (channel != null)
            {
                if (!IsBacklog)
                {
                    channel.InsertUser(new User(ed.SourceInfo, _Network));
                }
                _Network.__evt_JOIN(ed);
                return true;
            }
            _Network.__evt_JOIN(ed);
            return IsBacklog;
        }

        private bool ChannelBans2(List<string> parameters)
        {
            if (parameters.Count > 1)
            {
                Network.NetworkChannelEventArgs ev = new Network.NetworkChannelEventArgs(this.ServerLineRawText, this.Date);
                ev.ChannelName = parameters[1];
                ev.Parameters = parameters;
                ev.Channel = _Network.GetChannel(parameters[1]);
                if (ev.Channel != null)
                {
                    if (ev.Channel.IsParsingBanData)
                    {
                        ev.Channel.IsParsingBanData = false;
                        _Network.__evt_ChannelFinishBan(ev);
                        return true;
                    }
                }
                _Network.__evt_ChannelFinishBan(ev);
                return IsBacklog;
            }
            return false;
        }

        private bool ChannelBans(List<string> parameters)
        {
            if (parameters.Count > 4)
            {
                Channel channel = _Network.GetChannel(parameters[1]);
                if (channel != null)
                {
                    if (!channel.ContainsBan(parameters[2]))
                    {
                        channel.InsertBan(parameters[2], parameters[3], parameters[4]);
                    }
                    if (channel.IsParsingBanData)
                    {
                        return true;
                    }
                }
                return IsBacklog;
            }
            return false;
        }

        private bool Part(string source, string parameters, string value)
        {
            string chan = parameters.Trim();
            Channel channel = _Network.GetChannel(chan);
            Network.NetworkChannelDataEventArgs ev = new Network.NetworkChannelDataEventArgs(this.ServerLineRawText, this.Date);
            ev.ChannelName = chan;
            ev.Source = source;
            ev.Message = value;
            ev.ParameterLine = parameters;
            if (channel != null)
            {
                if (!IsBacklog)
                {
                    channel.RemoveUser(ev.SourceInfo.Nick);
                }
                _Network.__evt_PART(ev);
                return true;
            }
            _Network.__evt_PART(ev);
            return IsBacklog;
        }

        private bool ProcessNick(string source, string parameters, string value)
        {
            string _new = value;
            Network.NetworkNICKEventArgs ev = new Network.NetworkNICKEventArgs(this.ServerLineRawText, this.Date);
            ev.Source = source;
            if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(parameters))
            {
                // server is fucked
                _new = parameters;
                // server is totally borked
                if (_new.Contains(" "))
                {
                    _new = _new.Substring(0, _new.IndexOf(" ", StringComparison.Ordinal));
                }
            }
            ev.NewNick = _new;
            lock (_Network.Channels.Values)
            {
                foreach (Channel channel in _Network.Channels.Values)
                {
                    if (channel.ChannelWork)
                    {
                        User user = channel.UserFromName(ev.SourceInfo.Nick);
                        if (user != null)
                        {
                            if (!IsBacklog)
                            {
                                user.Nick = _new;
                            }
                        }
                    }
                }
            }
            ev.OldNick = ev.SourceInfo.Nick;
            ev.Source = source;
            _Network.__evt_NICK(ev);
            return true;
        }
    }
}
