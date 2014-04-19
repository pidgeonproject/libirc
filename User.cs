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
    /// <summary>
    /// User, Every user on irc has instance of this class for every channel they are in
    /// </summary>
    [Serializable]
    public class User : IComparable
    {
        /// <summary>
        /// Host name
        /// </summary>
        public string Host = null;
        /// <summary>
        /// Network this user belongs to
        /// </summary>
        [NonSerialized]
        public Network _Network = null;
        /// <summary>
        /// Identifier
        /// </summary>
        public string Ident = null;
        /// <summary>
        /// Channel mode (it could be +o as well as +ov so if you want to retrieve current channel status of user, look for highest level)
        /// </summary>
        public NetworkMode ChannelMode = new NetworkMode();
        /// <summary>
        /// Status
        /// </summary>
        public ChannelStatus Status = ChannelStatus.Regular;
        protected string nick = null;
        protected string lnick = null;
        /// <summary>
        /// Nick
        /// </summary>
        public virtual string Nick
        {
            set
            {
                if (this.Nick != value)
                {
                    if (this.Channel != null)
                    {
                        // rename the key in dictionary
                        this.Channel.RemoveUser(this);
                        // store again this exactly same user
                        this.lnick = value.ToLower();
                        this.nick = value;
                        this.Channel.InsertUser(this);
                        return;
                    }
                    this.lnick = value.ToLower();
                    this.nick = value;
                }
            }
            get
            {
                return nick;
            }
        }
        /// <summary>
        /// Return a lowercase nickname of this user, this function is likely faster than string.ToLower() because it uses caching so if you need to get lowercase
        /// nick very often, you should use this
        /// </summary>
        public virtual string LowNick
        {
            get
            {
                if (this.lnick == null)
                {
                    this.lnick = this.Nick.ToLower();
                }
                return this.lnick;
            }
        }
        /// <summary>
        /// Name
        /// </summary>
        public string RealName = null;
        /// <summary>
        /// Server
        /// </summary>
        public string Server = null;
        /// <summary>
        /// Away message
        /// </summary>
        public string AwayMessage = null;
        /// <summary>
        /// User away
        /// </summary>
        public bool Away = false;
        /// <summary>
        /// Check
        /// </summary>
        public DateTime LastAwayCheck;
        /// <summary>
        /// Primary chan
        /// </summary>
        public Channel Channel = null;
        /// <summary>
        /// Return true if user is owner of a channel
        /// </summary>
        public virtual bool IsOwner
        {
            get
            {
                if (ChannelMode._Mode.Contains("q"))
                {
                    return true;
                }
                return false;
            }
        }
        /// <summary>
        /// Return true if user is admin of a channel
        /// </summary>
        public virtual bool IsAdmin
        {
            get
            {
                if (ChannelMode._Mode.Contains("a"))
                {
                    return true;
                }
                return false;
            }
        }
        /// <summary>
        /// Return true if user is op of a channel
        /// </summary>
        public virtual bool IsOp
        {
            get
            {
                if (ChannelMode._Mode.Contains("o"))
                {
                    return true;
                }
                return false;
            }
        }
        /// <summary>
        /// Return true if user is half_operator of a channel
        /// </summary>
        public virtual bool IsHalfop
        {
            get
            {
                if (ChannelMode._Mode.Contains("h"))
                {
                    return true;
                }
                return false;
            }
        }
        /// <summary>
        /// Return true if user is voiced in this channel
        /// </summary>
        public virtual bool IsVoiced
        {
            get
            {
                if (ChannelMode._Mode.Contains("v"))
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Away time
        /// </summary>
        public DateTime AwayTime;
        private bool destroyed = false;
        /// <summary>
        /// This will return true in case object was requested to be disposed
        /// you should never work with objects that return true here
        /// </summary>
        public virtual bool IsDestroyed
        {
            get
            {
                return destroyed;
            }
        }

        /// <summary>
        /// This return true if we are looking at current user
        /// </summary>
        public virtual bool IsPidgeon
        {
            get
            {
                return (Nick.ToLower() == _Network.Nickname.ToLower());
            }
        }

        /// <summary>
        /// Get a list of all channels this user is in
        /// </summary>
        public virtual List<Channel> ChannelList
        {
            get
            {
                List<Channel> List = new List<Channel>();
                if (_Network == null)
                {
                    return null;
                }
                lock (_Network.Channels)
                {
                    foreach (Channel xx in _Network.Channels.Values)
                    {
                        if (xx.ContainsUser(Nick))
                        {
                            List.Add(xx);
                        }
                    }
                }
                return List;
            }
        }

        protected char ChannelSymbol = '\0';

        /// <summary>
        /// This is a symbol that user has before his name in a channel (for example voiced user would have + on most networks)
        /// </summary>
        public virtual char ChannelPrefix_Char
        {
            get
            {
                if (ChannelSymbol != '\0')
                {
                    return ChannelSymbol;
                }
                if (ChannelMode._Mode.Count > 0)
                {
                    int i = 100;
                    char c = '0';
                    lock (_Network.CUModes)
                    {
                        foreach (char mode in _Network.CUModes)
                        {
                            if (ChannelMode._Mode.Contains(mode.ToString()))
                            {
                                if (_Network.CUModes.IndexOf(mode) < i)
                                {
                                    i = _Network.CUModes.IndexOf(mode);
                                    c = mode;
                                }
                            }
                        }
                    }
                    if (c != '0')
                    {
                        ChannelSymbol = _Network.UChars[i];
                        return ChannelSymbol;
                    }
                }
                return '\0';
            }
            set
            {
                if (_Network.UChars.Contains(value))
                {
                    SymbolMode(value);
                    this.ChannelSymbol = value;
                }
            }
        }

        /// <summary>
        /// This is a symbol that user has before his name in a channel (for example voiced user would have + on most networks)
        /// </summary>
        public virtual string ChannelPrefix
        {
            get
            {
                char symbol = ChannelPrefix_Char;
                if (symbol == '\0')
                {
                    return "";
                }
                return symbol.ToString();
            }
        }

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="user">user!ident@hostname</param>
        /// <param name="network">Network this class belongs to</param>
        public User(string source, Network network)
        {
            UserInfo info = new UserInfo(source);
            MakeUser(info.Nick, info.Host, network, info.Ident);
        }

        public User(UserInfo info, Network network)
        {
            MakeUser(info.Nick, info.Host, network, info.Ident);
        }

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="nick"></param>
        /// <param name="host"></param>
        /// <param name="network"></param>
        /// <param name="ident"></param>
        public User(string nick, string host, string ident, Network network)
        {
            if (network == null)
            {
                throw new Exception("Network can't be null in here");
            }
            MakeUser(nick, host, network, ident);
        }

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="nick">Nick</param>
        /// <param name="host">Host</param>
        /// <param name="network">Network</param>
        /// <param name="ident">Ident</param>
        /// <param name="server">Server</param>
        /// <param name="channel">Channel</param>
        public User(string nick, string host, string ident, Network network, Channel channel)
        {
            this.Channel = channel;
            MakeUser(nick, host, network, ident);
        }

        public User(string source)
        {
            if (source.Contains("!"))
            {
                this.nick = source.Substring(0, source.IndexOf("!", StringComparison.Ordinal));
                this.Ident = source.Substring(source.IndexOf("!") + 1);
                if (this.Ident.Contains("@"))
                {
                    this.Host = this.Ident.Substring(this.Ident.IndexOf("@") + 1);
                    this.Ident = this.Ident.Substring(this.Ident.IndexOf("@"));
                }
            }
            else
            {
                this.nick = source;
            }
        }

        /// <summary>
        /// Reset the user mode back to none
        /// </summary>
        public virtual void ResetMode()
        {
            ChannelSymbol = '\0';
        }

        /// <summary>
        /// Change a user level according to symbol
        /// </summary>
        /// <param name="symbol"></param>
        public virtual void SymbolMode(char symbol)
        {
            if (_Network == null)
            {
                return;
            }

            if (symbol == '\0')
            {
                return;
            }

            if (_Network.UChars.Contains(symbol))
            {
                char mode = _Network.CUModes[_Network.UChars.IndexOf(symbol)];
                ChannelMode.ChangeMode("+" + mode.ToString());
                ResetMode();
            }
        }

        protected virtual void MakeUser(string nick, string host, Network network, string ident)
        {
            _Network = network;
            if (!string.IsNullOrEmpty(nick))
            {
                char prefix = nick[0];
                if (network.UChars.Contains(prefix))
                {
                    SymbolMode(prefix);
                    nick = nick.Substring(1);
                }
            }
            this.nick = nick;
            this.Ident = ident;
            this.Host = host;
            this.Server = network.ServerName;
        }

        /// <summary>
        /// Destroy
        /// </summary>
        public virtual void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }
            Channel = null;
            _Network = null;
            destroyed = true;
        }

        /// <summary>
        /// Converts a user object to string
        /// </summary>
        /// <returns>[nick!ident@host]</returns>
        public override string ToString()
        {
            return Nick + "!" + Ident + "@" + Host;
        }

        /// <summary>
        /// Generate full string
        /// </summary>
        /// <returns></returns>
        public virtual string ConvertToInfoString()
        {
            if (!string.IsNullOrEmpty(RealName))
            {
                return RealName + "\n" + ToString();
            }
            return ToString();
        }

		/// <summary>
		/// Returns a new instance of UserInfo that is filled up
		/// </summary>
		/// <returns>The user info.</returns>
		public virtual UserInfo ToUserInfo()
		{
			return new UserInfo(this.Nick, this.Ident, this.Host);
		}

        /// <summary>
        /// Internal function
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual int CompareTo(object obj)
        {
            if (obj is User)
            {
                return this.Nick.CompareTo((obj as User).Nick);
            }
            return 0;
        }

        /// <summary>
        /// Channel status
        /// </summary>
        public enum ChannelStatus
        {
            /// <summary>
            /// Owner
            /// </summary>
            Owner,
            /// <summary>
            /// Admin
            /// </summary>
            Admin,
            /// <summary>
            /// Operator
            /// </summary>
            Op,
            /// <summary>
            /// Halfop
            /// </summary>
            Halfop,
            /// <summary>
            /// Voice
            /// </summary>
            Voice,
            /// <summary>
            /// Normal user
            /// </summary>
            Regular,
        }
    }
}
