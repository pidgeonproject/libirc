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

        private bool ChannelInfo(Network.IncomingDataEventArgs info)
        {
            if (info.Parameters.Count > 2)
            {
                Channel channel = _Network.GetChannel(info.Parameters[1]);
                Network.NetworkChannelDataEventArgs args = new Network.NetworkChannelDataEventArgs(this.ServerLineRawText, this.Date);
                args.Command = info.Command;
                args.Message = info.Message;
                args.Parameters = info.Parameters;
                args.Channel = channel;
                args.ChannelName = info.Parameters[1];
                if (channel != null)
                {
                    channel.ChannelMode.ChangeMode(info.Parameters[2]);
                    _Network.__evt_ChannelInfo(args);
                    return true;
                }
                _Network.__evt_ChannelInfo(args);
            }
            return IsBacklog;
        }

        private bool ParseUser(Network.IncomingDataEventArgs info)
        {
            // :hub.tm-irc.org 352 petr #support pidgeon D3EE8257.8361F8AE.37E3A027.IP hub.tm-irc.org petr H :0 My name is hidden, dude
            if (info.Parameters.Count <= 6)
            {
                return false;
            }
            Network.NetworkParseUserEventArgs ev = new Network.NetworkParseUserEventArgs(ServerLineRawText, this.Date);
            ev.Parameters = info.Parameters;
            ev.ChannelName = info.Parameters[1];
            ev.Channel = _Network.GetChannel(info.Parameters[1]);
            string server = info.Parameters[4];
            ev.User = new UserInfo();
            ev.User.Ident = info.Parameters[2];
            ev.User.Host = info.Parameters[3];
            ev.User.Nick = info.Parameters[5];
            string realname = info.Message;
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
            if (info.Parameters[6].Length > 0)
            {
                // if user is away we flag him
                if (info.Parameters[6].StartsWith("G", StringComparison.Ordinal))
                {
                    ev.IsAway = true;
                }
                mode = info.Parameters[6][info.Parameters[6].Length - 1];
                if (!_Network.UChars.Contains(mode))
                {
                    mode = '\0';
                }
                ev.StringMode = mode.ToString();
            }
            ev.RealName = realname;
            _Network.__evt_ParseUser(ev);
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
                        return ev.Channel.IsParsingWhoData;
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
                if (ev.Channel.IsParsingWhoData)
                {
                    return true;
                }
            }
            return IsBacklog;
        }

        private bool ParseInfo(Network.IncomingDataEventArgs info)
        {
            if (info.Parameters.Count < 3)
            {
                return false;
            }
            // :irc-2t.tm-irc.org 353 petr = #support :petr user1227554 &OperBot Revi 
            if (IsBacklog)
            {
                return true;
            }
            Network.ChannelUserListEventArgs ev = new Network.ChannelUserListEventArgs(ServerLineRawText, this.Date);
            ev.Parameters = info.Parameters;
            ev.ParameterLine = info.ParameterLine;
            ev.UserNicknames.AddRange(info.Message.Split(' '));
            ev.ChannelName = info.Parameters[2];
            Channel channel = _Network.GetChannel(info.Parameters[2]);
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
            return IsBacklog;
        }

        private bool Topic(Network.IncomingDataEventArgs info)
        {
            Network.NetworkTOPICEventArgs ev = new Network.NetworkTOPICEventArgs(this.ServerLineRawText, this.Date);
            ev.Source = info.Source;
            ev.ParameterLine = info.ParameterLine;
            ev.Parameters = info.Parameters;
            ev.ChannelName = info.ParameterLine.Trim();
            ev.Channel = _Network.GetChannel(ev.ChannelName);
            ev.Topic = info.Message;
            double time = Defs.ConvertDateToUnix(DateTime.Now);
            ev.TopicDate = time;
            _Network.__evt_TOPIC(ev);
            if (ev.Channel != null)
            {
                ev.Channel.Topic = info.Message;
                if (!IsBacklog)
                {
                    ev.Channel.TopicDate = (int)time;
                    ev.Channel.TopicUser = info.Source;
                }
                return true;
            }
            return IsBacklog;
        }

        private bool TopicInfo(Network.IncomingDataEventArgs info)
        {
            if (info.Parameters.Count < 4)
                return false;
            Network.NetworkTOPICEventArgs ev = new Network.NetworkTOPICEventArgs(this.ServerLineRawText, this.Date);
            ev.Source = info.Source;
            ev.Parameters = info.Parameters;
            ev.ParameterLine = info.ParameterLine;
            ev.ChannelName = info.Parameters[1];
            string user = info.Parameters[2];
            string time = info.Parameters[3];
            double dt;
            if (!double.TryParse(time, out dt))
                dt = 0;
            ev.TopicDate = dt;
            ev.Source = user;
            ev.Channel = _Network.GetChannel(info.Parameters[1]);
            _Network.__evt_TopicInfo(ev);
            if (ev.Channel != null)
            {
                ev.Channel.TopicDate = (int)dt;
                ev.Channel.TopicUser = user;
            }
            return true;
        }

        private bool ChannelTopic(Network.IncomingDataEventArgs info)
        {
            if (info.Parameters.Count < 2)
            {
                return false;
            }
            Network.NetworkTOPICEventArgs ev = new Network.NetworkTOPICEventArgs(this.ServerLineRawText, this.Date);
            ev.Parameters = info.Parameters;
            ev.Topic = info.Message;
            ev.ChannelName = info.Parameters[1];
            string topic = info.Message;
            Channel channel = _Network.GetChannel(info.Parameters[1]);
            ev.Channel = channel;
            _Network.__evt_TopicData(ev);
            if (channel != null)
                channel.Topic = topic;
            return true;
        }

        private bool FinishChan(Network.IncomingDataEventArgs info)
        {
            if (info.Parameters.Count == 0)
                return false;
            Network.NetworkChannelDataEventArgs ev = new Network.NetworkChannelDataEventArgs(this.ServerLineRawText, this.Date);
            ev.ChannelName = info.Parameters[1];
            ev.ParameterLine = info.ParameterLine;
            ev.Parameters = info.Parameters;
            ev.Channel = _Network.GetChannel(info.Parameters[1]);
            if (ev.Channel != null)
                ev.Channel.IsParsingWhoData = false;

            _Network.__evt_FinishChannelParseUser(ev);
            return true;
        }

        private bool Kick(Network.IncomingDataEventArgs info)
        {
            // petan!pidgeon@petan.staff.tm-irc.org KICK #support HelpBot :Removed from the channel
            Network.NetworkKickEventArgs ev = new Network.NetworkKickEventArgs(this.ServerLineRawText, this.Date);
            ev.Source = info.Source;
            ev.Parameters = info.Parameters;
            ev.ChannelName = info.Parameters[0];
            ev.Message = info.Message;
            ev.Target = info.Parameters[1];
            ev.ParameterLine = info.ParameterLine;
            ev.Channel = _Network.GetChannel(info.Parameters[0]);
            if (ev.Channel != null)
            {
                if (!IsBacklog)
                {
                    User user = ev.Channel.UserFromName(info.Parameters[1]);
                    if (user != null)
                    {
                        ev.Channel.RemoveUser(user);
                        if (user.IsPidgeon)
                            ev.Channel.ChannelWork = false;
 
                    }
                }
                _Network.__evt_KICK(ev);
                return true;
            }
            _Network.__evt_KICK(ev);
            return IsBacklog;
        }

        private bool Join(Network.IncomingDataEventArgs info)
        {
            string channel_name = info.ParameterLine.Trim();
            if (string.IsNullOrEmpty(channel_name))
            {
                channel_name = info.Message;
            }
            Channel channel = _Network.GetChannel(channel_name);
            Network.NetworkChannelEventArgs ed = new Network.NetworkChannelEventArgs(ServerLineRawText, this.Date);
            ed.ChannelName = channel_name;
            ed.Source = info.Source;
            ed.Channel = channel;
            ed.ParameterLine = info.ParameterLine;
            if (channel != null)
            {
                if (!IsBacklog)
                    channel.InsertUser(new User(ed.SourceInfo, _Network));

                _Network.__evt_JOIN(ed);
                return true;
            }
            _Network.__evt_JOIN(ed);
            return IsBacklog;
        }

        private bool ChannelBans2(Network.IncomingDataEventArgs info)
        {
            if (info.Parameters.Count == 0)
                return false;
            Network.NetworkChannelEventArgs ev = new Network.NetworkChannelEventArgs(this.ServerLineRawText, this.Date);
            ev.ChannelName = info.Parameters[1];
            ev.ParameterLine = info.ParameterLine;
            ev.Parameters = info.Parameters;
            ev.Channel = _Network.GetChannel(ev.Parameters[1]);
            if (ev.Channel.Bans == null)
                ev.Channel.Bans = new List<ChannelBan>();
            _Network.__evt_ChannelFinishBan(ev);
            if (ev.Channel != null)
            {
                if (ev.Channel.IsParsingBanData)
                {
                    ev.Channel.IsParsingBanData = false;
                    return true;
                }
            }
            return IsBacklog;
        }

        private bool ChannelBans(Network.IncomingDataEventArgs info)
        {
            if (info.Parameters.Count > 4)
            {
                Channel channel = _Network.GetChannel(info.Parameters[1]);
                if (channel != null)
                {
                    if (!channel.ContainsBan(info.Parameters[2]))
                        channel.InsertBan(info.Parameters[2], info.Parameters[3], info.Parameters[4]);
                    if (channel.IsParsingBanData)
                        return true;
                }
                return IsBacklog;
            }
            return false;
        }

        private bool Part(Network.IncomingDataEventArgs info)
        {
            string chan = info.ParameterLine.Trim();
            Channel channel = _Network.GetChannel(chan);
            Network.NetworkChannelDataEventArgs ev = new Network.NetworkChannelDataEventArgs(this.ServerLineRawText, this.Date);
            ev.ChannelName = chan;
            ev.Channel = channel;
            ev.Source = info.Source;
            ev.Message = info.Message;
            ev.ParameterLine = info.ParameterLine;
            _Network.__evt_PART(ev);
            if (channel != null)
            {
                if (!IsBacklog)
                    channel.RemoveUser(ev.SourceInfo.Nick);
                return true;
            }
            return IsBacklog;
        }

        private bool ProcessNick(Network.IncomingDataEventArgs info)
        {
            string _new = info.Message;
            Network.NetworkNICKEventArgs ev = new Network.NetworkNICKEventArgs(this.ServerLineRawText, this.Date);
            ev.Source = info.Source;
            ev.OldNick = ev.SourceInfo.Nick;
            if (string.IsNullOrEmpty(info.Message) && !string.IsNullOrEmpty(info.ParameterLine))
            {
                // server is fucked
                _new = info.ParameterLine;
                // server is totally borked
                if (_new.Contains(" "))
                {
                    _new = _new.Substring(0, _new.IndexOf(" ", StringComparison.Ordinal));
                }
            }
            ev.NewNick = _new;
            _Network.__evt_NICK(ev);
            lock (_Network.Channels)
            {
                foreach (Channel channel in _Network.Channels.Values)
                {
                    if (channel.ChannelWork)
                    {
                        User user = channel.UserFromName(ev.SourceInfo.Nick);
                        if (user != null && !IsBacklog)
                            user.Nick = _new;
                    }
                }
            }
            return true;
        }
    }
}
