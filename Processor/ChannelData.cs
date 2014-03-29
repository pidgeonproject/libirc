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
        private bool ChannelInfo(List<string> parameters, string command, string source, string _value)
        {
			if (parameters.Count > 2)
            {
				Channel channel = _Network.GetChannel(parameters[1]);
                Network.NetworkChannelDataEventArgs args = new Network.NetworkChannelDataEventArgs();
                args.Command = command;
                args.Message = _value;
                args.Parameters = parameters;
                args.Channel = channel;
                args.ChannelName = parameters[1];
                _Network.__evt_ChannelInfo(args);
                if (channel != null)
                {
                    channel.ChannelMode.ChangeMode(parameters[2]);
                    return true;
                }
            }
            return false;
        }

        private bool ParseUser(List<string> parameters, string realname)
        {
			// :hub.tm-irc.org 352 petr #support pidgeon D3EE8257.8361F8AE.37E3A027.IP hub.tm-irc.org petr H :0 My name is hidden, dude
            if (parameters.Count > 6)
            {
                Channel channel = _Network.GetChannel(parameters[1]);
                string server = parameters[4];
				UserInfo ui = new UserInfo();
				ui.Ident = parameters[2];
				ui.Host = parameters[3];
				ui.Nick = parameters[5];
                if (realname != null & realname.Length > 2)
                {
                    realname = realname.Substring(2);
                }
                else if (realname == "0 ")
                {
                    realname = "";
                }
				Network.NetworkParseUserEventArgs ev = new Network.NetworkParseUserEventArgs(ServerLineRawText);
				ev.Channel = channel;
				ev.ChannelName = parameters[1];
				ev.User = ui;
                char mode = '\0';
                bool IsAway = false;
                if (parameters[6].Length > 0)
                {
                    // if user is away we flag him
                    if (parameters[6].StartsWith("G", StringComparison.Ordinal))
                    {
                        IsAway = true;
                    }
                    mode = parameters[6][parameters[6].Length - 1];
                    if (!_Network.UChars.Contains(mode))
                    {
                        mode = '\0';
                    }
                }
				ev.IsAway = IsAway;
                if (channel != null)
                {
                    if (!IsBacklog)
                    {
                        if (!channel.ContainsUser(ui.Nick))
                        {
                            User _user = null;
                            if (mode != '\0')
                            {
                                _user = new User(mode.ToString() + ui.Nick, ui.Host, ui.Ident, _Network);
                            }
                            else
                            {
                                _user = new User(ui, _Network);
                            }
                            _user.LastAwayCheck = DateTime.Now;
                            _user.RealName = realname;
                            if (IsAway)
                            {
                                _user.AwayTime = DateTime.Now;
                            }
                            _user.Away = IsAway;
                            channel.InsertUser(_user);
                            return true;
                        }
                        User user = channel.UserFromName(ui.Nick);
                        if (user != null)
                        {
                            user.Ident = ui.Ident;
                            user.Host = ui.Host;
                            user.Server = server;
                            user.RealName = realname;
                            user.LastAwayCheck = DateTime.Now;
                            if (!user.Away && IsAway)
                            {
                                user.AwayTime = DateTime.Now;
                            }
                            user.Away = IsAway;
                        }
                    }
					_Network.__evt_ParseUser(ev);
					if (channel.IsParsingWhoData)
					{
                    	return true;
					}
                }
				_Network.__evt_ParseUser(ev);
            }
            return false;
        }

        private bool ParseInfo(List<string> parameters, string value)
        {
			if (parameters.Count > 0)
            {
                if (IsBacklog)
                {
                    return true;
                }
				Network.ChannelUserListEventArgs ev = new Network.ChannelUserListEventArgs(ServerLineRawText);
				ev.UserNicknames.AddRange(value.Split(' '));
				ev.ChannelName = parameters[0];
                Channel channel = _Network.GetChannel(parameters[0]);
                if (channel != null)
                {
					ev.Channel = channel;
                    foreach (var nick in ev.UserNicknames)
                    {
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
            return false;
        }

        private bool ChannelTopic(List<string> parameters, string command, string source, string message)
        {
			if (parameters.Count > 0)
            {
                string channelName_ = parameters[0];
                string topic = message;
                Channel channel = _Network.GetChannel(channelName_);
                if (channel != null)
                {
                    channel.Topic = topic;
                    return true;
                }
            }
            return false;
        }

        private bool FinishChan(List<string> code)
        {
            if (code.Count > 0)
            {
                Channel channel = _Network.GetChannel(code[1]);
                if (channel != null)
                {

                    channel.IsParsingWhoData = false;
                }
            }
            return false;
        }

        private bool TopicInfo(List<string> parameters)
        {
			if (parameters.Count > 2)
            {
				string name = parameters[0];
				string user = parameters[1];
				string time = parameters[2];
                Channel channel = _Network.GetChannel(name);
                if (channel != null)
                {
                    channel.TopicDate = int.Parse(time);
                    channel.TopicUser = user;
                }
            }
            return false;
        }

        private bool Kick(string source, string parameters, string value)
        {
            string user = parameters.Substring(parameters.IndexOf(" ", StringComparison.Ordinal) + 1);
            // petan!pidgeon@petan.staff.tm-irc.org KICK #support HelpBot :Removed from the channel
            string chan = parameters.Substring(0, parameters.IndexOf(" ", StringComparison.Ordinal));
            Channel channel = _Network.GetChannel(chan);
            if (channel != null)
            {
                if (!IsBacklog)
                {
                    User delete = channel.UserFromName(user);
                    if (delete != null && delete.IsPidgeon)
                    {
	                    channel.ChannelWork = false;
                        channel.RemoveUser(user);
                    }
				}
                return true;
            }
            return false;
        }

        private bool Join(string source, string parameters, string value)
        {
            string chan = parameters;
            chan = chan.Replace(" ", "");
            if (string.IsNullOrEmpty(chan))
            {
                chan = value;
            }
            UserInfo user = new UserInfo(source);
            Channel channel = _Network.GetChannel(chan);
			Network.NetworkChannelEventArgs ed = new Network.NetworkChannelEventArgs(ServerLineRawText);
			ed.ChannelName = chan;
			ed.Source = source;
			ed.Parameters = parameters;
            if (channel != null)
            {
				ed.Channel = channel;
                if (!IsBacklog)
                {
                    channel.InsertUser(new User(user, _Network));
                }
				_Network.__evt_JOIN(ed);
                return true;
            }
			_Network.__evt_JOIN(ed);
            return false;
        }

        private bool ChannelBans2(List<string> parameters)
        {
            if (parameters.Count > 0)
            {
                Channel channel = _Network.GetChannel(parameters[0]);
                if (channel != null)
                {
                    if (channel.IsParsingBanData)
                    {
                        channel.IsParsingBanData = false;
                        return true;
                    }
                }
            }
            return false;
        }

        private bool ChannelBans(List<string> parameters)
        {
			if (parameters.Count > 3)
            {
                Channel channel = _Network.GetChannel(parameters[0]);
                if (channel != null)
                {
                    if (channel.Bans == null)
                    {
                        channel.Bans = new List<SimpleBan>();
                    }
                    if (!channel.ContainsBan(parameters[1]))
                    {
                        channel.Bans.Add(new SimpleBan(parameters[2], parameters[1], parameters[3]));
                    }
                }
            }
            return false;
        }

        private bool Part(string source, string parameters, string value)
        {
            string chan = parameters;
            chan = chan.Replace(" ", "");
            UserInfo ui = new UserInfo(source);
            Channel channel = _Network.GetChannel(chan);
            if (channel != null)
            {
                if (!IsBacklog)
                {
                    channel.RemoveUser(ui.Nick);
				}
                return true;
            }
            return false;
        }

        private bool Topic(string source, string parameters, string value)
        {
            string chan = parameters;
            chan = chan.Replace(" ", "");
            Channel channel = _Network.GetChannel(chan);
            if (channel != null)
            {
                channel.Topic = value;
                if (!IsBacklog)
                {
                    channel.TopicDate = (int)Defs.ConvertDateToUnix(DateTime.Now);
                    channel.TopicUser = source;
                }
                return true;
            }
            return false;
        }

        private bool ProcessNick(string source, string parameters, string value)
        {
            string nick = source.Substring(0, source.IndexOf("!", StringComparison.Ordinal));
            string _new = value;
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
            lock (_Network.Channels.Values)
            {
                foreach (Channel channel in _Network.Channels.Values)
                {
                    if (channel.ChannelWork)
                    {
                        User user = channel.UserFromName (nick);
                        if (user != null)
                        {
                            if (!IsBacklog)
                            {
                                channel.RemoveUser(user);
                                user.SetNick(_new);
                                channel.InsertUser(user);
                            }
                        }
                    }
                }
            }
            return true;
        }

        private bool Mode(string source, string parameters, string value)
        {
            if (parameters.Contains(" "))
            {
                string chan = parameters.Substring(0, parameters.IndexOf(" ", StringComparison.Ordinal));
                chan = chan.Replace(" ", "");
                string user = source;
                if (chan.StartsWith(_Network.ChannelPrefix, StringComparison.Ordinal))
                {
                    Channel channel = _Network.GetChannel(chan);
                    if (channel != null)
                    {
                        string change = parameters.Substring(parameters.IndexOf(" ", StringComparison.Ordinal));
                        if (IsBacklog)
                        {
                            return true;
                        }
                        while (change.StartsWith(" ", StringComparison.Ordinal))
                        {
                            change = change.Substring(1);
                        }

                        Formatter formatter = new Formatter();

                        while (change.EndsWith(" ", StringComparison.Ordinal) && change.Length > 1)
                        {
                            change = change.Substring(0, change.Length - 1);
                        }

                        // we get all the mode changes for this channel
                        formatter.RewriteBuffer(change, _Network);

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
                                        if (channel.Bans == null)
                                        {
                                            channel.Bans = new List<SimpleBan>();
                                        }
                                        lock (channel.Bans)
                                        {
                                            channel.Bans.Add(new SimpleBan(user, m.Parameter, ""));
                                        }
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
                                        if (channel.Bans == null)
                                        {
                                            channel.Bans = new List<SimpleBan>();
                                        }
                                        channel.RemoveBan(m.Parameter);
                                        break;
                                }
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
