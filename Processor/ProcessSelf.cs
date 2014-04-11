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
    }
}
