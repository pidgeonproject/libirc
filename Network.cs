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
using System.Threading;
using System.Text;

namespace libirc
{
    /// <summary>
    /// Instance of irc network, this class is typically handled by protocols
    /// </summary>
    public class Network : IDisposable
    {
		public enum EventType
		{
			Quit,
			Join,
			Part,
			Kick,
			Nick,
			Message,
			Notice,
			Generic
		}

		/// <summary>
		/// Information about the channel for list
		/// 
		/// This is not a class for channels, only the list
		/// </summary>
		[Serializable]
		public class ChannelData
		{
			/// <summary>
			/// Name
			/// </summary>
			public string ChannelName = null;
			/// <summary>
			/// Number of users
			/// </summary>
			public uint UserCount = 0;
			/// <summary>
			/// Topic of a channel
			/// </summary>
			public string ChannelTopic = null;

			/// <summary>
			/// Creates a new instance
			/// </summary>
			/// <param name="Users">Number of users</param>
			/// <param name="Name"></param>
			/// <param name="Topic"></param>
			public ChannelData(uint Users, string Name, string Topic)
			{
				ChannelTopic = Topic;
				UserCount = Users;
				ChannelName = Name;
			}

			/// <summary>
			/// This constructor needs to exist for xml deserialization don't remove it
			/// </summary>
			public ChannelData() {}
		}
		
        public class NetworkGenericEventArgs : Protocol.ProtocolGenericEventArgs
        {
			/// <summary>
			/// user!ident@host
			/// </summary>
            public string Source = null;
			/// <summary>
			/// If user object had to be fetch during the processing (which is CPU expensive) it's provided
			/// here as well so that you don't need to fetch it again in case you need to use it.
			/// 
			/// This is NULL most of time, don't rely on it
			/// </summary>
			public User SourceUser = null;
			private UserInfo sourceInfo = null;
			public UserInfo SourceInfo
			{
				get
				{
					if (this.Source == null) return null;
					if (this.sourceInfo == null) sourceInfo = new UserInfo(this.Source);
					return sourceInfo;
				}
			}
            public string ParameterLine = null;
			public List<string> Parameters = null;
			/// <summary>
			/// Text of this event as sent by server, for example :user!xx@bla NOTICE kokos :hello
			/// </summary>
			public string ServerLine;
			
			public NetworkGenericEventArgs(string line)
			{
				this.ServerLine = line;
			}
        }

		public class NetworkGenericDataEventArgs : NetworkGenericEventArgs
		{
			public string Command = null;
			public string Message = null;
			
			public NetworkGenericDataEventArgs(string line) : base(line) {}
		}

		public class NetworkChannelEventArgs : NetworkGenericEventArgs
		{
			public string ChannelName = null;
			public Channel Channel = null;
			
			public NetworkChannelEventArgs(string line) : base(line) {}
		}

        public class NetworkChannelDataEventArgs : NetworkGenericDataEventArgs
        {
            public string ChannelName = null;
            public Channel Channel = null;
			
			public NetworkChannelDataEventArgs(string line) : base(line) {}
        }
		
		public class NetworkParseUserEventArgs : NetworkChannelEventArgs
		{
			public bool IsAway = true;
			public string Server = null;
			public UserInfo User = null;
			public string RealName;
			
			public NetworkParseUserEventArgs(string line) : base(line) {}
		}
		
		public class NetworkKickEventArgs : NetworkChannelEventArgs
		{
			public string Target;
			public string Message;
			
			public NetworkKickEventArgs(string line) : base(line) {}
		}
		
		public class ChannelUserListEventArgs : NetworkChannelEventArgs
		{
			public List<string> UserNicknames = new List<string>();
			public List<User> Users = new List<User>();
			
			public ChannelUserListEventArgs(string line) : base(line) {}
		}
		
        public class NetworkPRIVMSGEventArgs : NetworkGenericEventArgs
        {
            public string Message = null;
            public bool IsAct = false;
            public Channel Channel = null;
			public string ChannelName = null;
			
			public NetworkPRIVMSGEventArgs(string line) : base(line) {}
        }
		
		public class NetworkMODEEventArgs : NetworkChannelDataEventArgs
		{
			public Formatter FormattedMode = null;
            public string SimpleMode = null;
			
			public NetworkMODEEventArgs(string line) : base(line) {}
		}
		
		public class NetworkNICKEventArgs : NetworkChannelDataEventArgs
		{
			public string NewNick = null;
			public string OldNick = null;
			
			public NetworkNICKEventArgs(string line) : base(line) {}
		}

        public class NetworkCTCPEventArgs : NetworkPRIVMSGEventArgs
        {
            public string CTCP = null;
			
			public NetworkCTCPEventArgs(string line) : base(line) {}
        }

		public class NetworkNOTICEEventArgs : NetworkGenericEventArgs
		{
			public string Message = null;
			
			public NetworkNOTICEEventArgs(string line) : base(line) {}
		}

        public class NetworkTOPICEventArgs : NetworkChannelDataEventArgs
        {
            public string Topic;
            public double TopicDate;

            public NetworkTOPICEventArgs(string line) : base(line) {}
        }

		public class NetworkSelfEventArgs : NetworkGenericEventArgs
		{
			public string Message = null;
			public Channel Channel = null;
			/// <summary>
			/// Name of the channel in case it wasn't recognized or known
			/// </summary>
			public string ChannelName = null;
			/// <summary>
			/// This is a new nick in case the event type was NICK
			/// </summary>
			public string NewNick = null;
			/// <summary>
			/// This is an old nickname in case the event type was NICK
			/// </summary>
			public string OldNick = null;
			public EventType Type = EventType.Generic;
			
			public NetworkSelfEventArgs(string line) : base(line) {}
		}

        public delegate void NetworkTopicDataEventHandler(object sender, NetworkTOPICEventArgs e);
		public delegate void NetworkInfoEventHandler(object sender,NetworkGenericDataEventArgs e);
		public delegate void NetworkSelfEventHandler(object sender, NetworkSelfEventArgs e);
		public delegate void NetworkNOTICEEventHandler(object sender, NetworkNOTICEEventArgs e);
        public delegate void NetworkPRIVMSGEventHandler(object sender, NetworkPRIVMSGEventArgs e);
		public delegate void NetworkJOINEventHandler(object sender, NetworkChannelEventArgs e);
		public delegate void NetworkParseUserEventHandler(object sender, NetworkParseUserEventArgs e);
		public delegate void NetworkPARTEventHandler(object sender, NetworkChannelEventArgs e);
		public delegate void NetworkKICKEventHandler(object sender, NetworkKickEventArgs e);
		public delegate void NetworkNICKEventHandler(object sender, NetworkNICKEventArgs e);
        public delegate void NetworkTopicInfoEventHandler(object sender, NetworkTOPICEventArgs e);
		public delegate void NetworkQUITEventHandler(object sender, NetworkGenericEventArgs e);
        public delegate void NetworkChannelInfoEventHandler(object sender, NetworkChannelDataEventArgs e);
        public delegate void NetworkCTCPEventHandler(object sender, NetworkCTCPEventArgs e);
		public delegate void NetworkChannelUserListHandler(object sender, ChannelUserListEventArgs e);
		public delegate void FinishParseUserEventHandler(object sender, NetworkChannelDataEventArgs e);
		public delegate void NetworkMODEEventHandler(object sender, NetworkMODEEventArgs e);
        public delegate void NetworkTOPICEventHandler(object sender, NetworkTOPICEventArgs e);
		/// <summary>
		/// Occurs when some network action that is related to current user happens (for example
		/// when this user join or change nick)
		/// </summary>
		public event NetworkSelfEventHandler On_Self;
		public event NetworkNOTICEEventHandler On_NOTICE;
		public event NetworkParseUserEventHandler On_ParseUser;
        public event NetworkPRIVMSGEventHandler On_PRIVMSG;
		public event NetworkPARTEventHandler On_PART;
		public event NetworkJOINEventHandler On_JOIN;
		public event NetworkInfoEventHandler On_Info;
		public event NetworkKICKEventHandler On_KICK;
		public event NetworkNICKEventHandler On_NICK;
		public event NetworkQUITEventHandler On_QUIT;
        public event NetworkChannelInfoEventHandler On_ChannelInfo;
        public event NetworkCTCPEventHandler On_CTCP;
		public event NetworkChannelUserListHandler On_ChannelUserList;
		public event FinishParseUserEventHandler On_FinishChannelParseUser;
		public event NetworkMODEEventHandler On_MODE;
        public event NetworkTOPICEventHandler On_TOPIC;
        public event NetworkTopicDataEventHandler On_TopicData;
        public event NetworkTopicInfoEventHandler On_TopicInfo;
        
        public Configuration Config = new Configuration();
        /// <summary>
        /// Message that is shown to users when you are away
        /// </summary>
        public string AwayMessage = null;
        /// <summary>
        /// User modes, these are modes that are applied on network, not channel (invisible, oper)
        /// </summary>
        public List<char> UModes = new List<char> { 'i', 'w', 'o', 'Q', 'r', 'A' };
        /// <summary>
        /// Channel user symbols (oper and such)
        /// </summary>
        public List<char> UChars = new List<char> { '~', '&', '@', '%', '+' };
        /// <summary>
        /// Channel user modes (voiced, op)
        /// </summary>
        public List<char> CUModes = new List<char> { 'q', 'a', 'o', 'h', 'v' };
        /// <summary>
        /// Channel modes (moderated, topic)
        /// </summary>
        public List<char> CModes = new List<char> { 'n', 'r', 't', 'm' };
        /// <summary>
        /// Special channel modes with parameter as a string
        /// </summary>
        public List<char> SModes = new List<char> { 'k', 'L' };
        /// <summary>
        /// Special channel modes with parameter as a number
        /// </summary>
        public List<char> XModes = new List<char> { 'l' };
        /// <summary>
        /// Special channel user modes with parameters as a string
        /// </summary>
        public List<char> PModes = new List<char> { 'b', 'I', 'e' };
        /// <summary>
        /// Descriptions for channel and user modes
        /// </summary>
        public Dictionary<char, string> Descriptions = new Dictionary<char, string>();
        /// <summary>
        /// Check if the info is parsed
        /// </summary>
        public bool ParsedInfo = false;
        /// <summary>
        /// Symbol prefix of channels
        /// </summary>
        public string ChannelPrefix = "#";
        /// <summary>
        /// Host name of server
        /// </summary>
        public string ServerName = null;
        /// <summary>
        /// User mode of current user
        /// </summary>
        public NetworkMode usermode = new NetworkMode();
        /// <summary>
        /// User name (real name)
        /// </summary>
        public string UserName = null;
        /// <summary>
        /// List of all channels on network
        /// </summary>
        public Dictionary<string, Channel> Channels = new Dictionary<string, Channel>();
        /// <summary>
        /// Currently rendered channel on main window
        /// </summary>
        public Channel RenderedChannel = null;
        /// <summary>
        /// Nickname of this user
        /// </summary>
        public string Nickname = null;
        /// <summary>
        /// Identification of user
        /// </summary>
        public string Ident = "pidgeon";
        /// <summary>
        /// Quit message
        /// </summary>
        public string Quit = null;
        /// <summary>
        /// Protocol
        /// </summary>
        public Protocol _Protocol = null;
        /// <summary>
        /// Specifies whether this network is using SSL connection
        /// </summary>
        public bool IsSecure = false;
        /// <summary>
        /// List of channels
        /// </summary>
        public List<ChannelData> ChannelList = new List<ChannelData>();
        /// <summary>
        /// If true, the channel data will be suppressed in system window
        /// </summary>
        public bool SuppressData = false;
        /// <summary>
        /// This is true when network is just parsing the list of all channels
        /// </summary>
        public bool DownloadingList = false;
        /// <summary>
        /// If the system already attempted to change the nick
        /// </summary>
        public bool UsingNick2 = false;
        /// <summary>
        /// Whether user is away
        /// </summary>
        public bool IsAway = false;
        /// <summary>
        /// Whether this network is fully loaded
        /// </summary>
        public bool IsLoaded = false;
        /// <summary>
        /// Version of ircd running on this network
        /// </summary>
        public string IrcdVersion = null;
        private bool connected = false;
        /// <summary>
        /// Specifies if you are connected to network
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return connected;
            }
        }
        private bool isDestroyed = false;
        /// <summary>
        /// This will return true in case object was requested to be disposed
        /// you should never work with objects that return true here
        /// </summary>
        public bool IsDestroyed
        {
            get
            {
                return isDestroyed;
            }
        }

        /// <summary>
        /// Creates a new network, requires name and protocol type
        /// </summary>
        /// <param name="Server">Server name</param>
        /// <param name="protocol">Protocol that own this instance</param>
        public Network(string Server, Protocol protocol)
        {
            lock (Descriptions)
            {
                Descriptions.Clear();
                Descriptions.Add('n', "no /knock is allowed on channel");
                Descriptions.Add('r', "registered channel");
                Descriptions.Add('m', "talking is restricted");
                Descriptions.Add('i', "users need to be invited to join");
                Descriptions.Add('s', "channel is secret (doesn't appear on list)");
                Descriptions.Add('p', "channel is private");
                Descriptions.Add('A', "admins only");
                Descriptions.Add('O', "opers chan");
                Descriptions.Add('t', "topic changes can be done only by operators");
            }
            _Protocol = protocol;
            ServerName = Server;
            Quit = Defs.DefaultQuit;
            Nickname = Defs.DefaultNick;
            UserName = Defs.DefaultNick;
            Ident = "libirc";
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~Network()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by this class
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by this class
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDestroyed)
            {
                Destroy();
            }
        }
        
        /// <summary>
        /// Insert a description to list
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="description"></param>
        public virtual void InsertSafeDescription(char mode, string description)
        {
            lock (Descriptions)
            {
                if (Descriptions.ContainsKey(mode))
                {
                    Descriptions.Remove(mode);
                }

                Descriptions.Add(mode, description);
            }
        }

        /// <summary>
        /// This will toggle the connection flag to true
        /// </summary>
        public virtual void SetConnected()
        {
            connected = true;
        }

        /// <summary>
        /// This will mark the network as disconnected
        /// </summary>
        public virtual void SetDisconnected()
        {
            connected = false;
        }

        /// <summary>
        /// Removes the channel symbols (like @ or ~) from user nick
        /// </summary>
        /// <returns>
        /// The username without char
        /// </returns>
        /// <param name='username'>
        /// Username
        /// </param>
        public virtual string RemoveCharFromUser(string username)
        {
            foreach (char xx in UChars)
            {
                if (username.Contains(xx.ToString()))
                {
                    username = username.Replace(xx.ToString(), "");
                }
            }
            return username;
        }

        /// <summary>
        /// Retrieve information about given channel from cache of channel list
        /// </summary>
        /// <param name="channel">Channel that is about to be resolved</param>
        /// <returns></returns>
        public virtual ChannelData ContainsChannel(string channel)
        {
            lock (ChannelList)
            {
                foreach (ChannelData data in ChannelList)
                {
                    if (channel.ToLower() == data.ChannelName.ToLower())
                    {
                        return data;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Part a given channel
        /// </summary>
        /// <param name="ChannelName">Channel name</param>
        public virtual void Part(string ChannelName)
        {
            _Protocol.Part(ChannelName, this);
        }

        /// <summary>
        /// Part
        /// </summary>
        /// <param name="channel"></param>
        public virtual void Part(Channel channel)
        {
            _Protocol.Part(channel.Name, this);
        }

        /// <summary>
        /// Retrieve channel
        /// </summary>
        /// <param name="name">String</param>
        /// <returns>Channel or null if it doesn't exist</returns>
        public virtual Channel GetChannel(string name)
        {
            lock (this.Channels)
            {
                Channel channel = null;
                if (this.Channels.TryGetValue(name.ToLower(), out channel))
                {
                    return channel;
                }
                return null;
            }
        }

        /// <summary>
        /// Reconnect a disconnected network
        /// </summary>
        public virtual void Reconnect()
        {
            if (IsDestroyed)
            {
                return;
            }

            _Protocol.ReconnectNetwork(this);
        }

        /// <summary>
        /// Join
        /// </summary>
        /// <param name="channel">Channel name which is supposed to be joined</param>
        /// <returns></returns>
        public virtual void Join(string channel)
        {
            Transfer("JOIN " + channel, Defs.Priority.Normal);
        }

        /// <summary>
        /// Create a new instance of channel window
        /// </summary>
        /// <param name="channel">Channel</param>
        /// <param name="nf">Don't focus this new window</param>
        /// <returns>Instance of channel object</returns>
        public virtual Channel Channel(string channel)
        {
            Channel previous = GetChannel(channel);
            if (previous == null)
            {
                Channel _channel = new Channel(this);
                RenderedChannel = _channel;
                _channel.Name = channel;
                _channel.lName = channel.ToLower();
                Channels.Add(_channel.lName, _channel);
                return _channel;
            }
            else
            {
                return previous;
            }
        }

        public virtual void __evt_NOTICE(NetworkNOTICEEventArgs args)
        {
            if (this.On_NOTICE != null)
            {
				this.On_NOTICE(this, args);
            }
        }

		public virtual void __evt_Self(NetworkSelfEventArgs args)
		{
			if (this.On_Self != null)
			{
				this.On_Self(this, args);
			}
		}

		public virtual void __evt_PRIVMSG(NetworkPRIVMSGEventArgs args)
		{
			if (this.On_PRIVMSG != null)
			{
				this.On_PRIVMSG(this, args);
			}
		}

		public virtual void __evt_INFO(NetworkGenericDataEventArgs args)
		{
			if (this.On_Info != null)
			{
				this.On_Info(this, args);
			}
		}
		
		public virtual void __evt_ParseUser(NetworkParseUserEventArgs args)
		{
			if (this.On_ParseUser != null)
			{
				this.On_ParseUser(this, args);
			}
		}

		public virtual void __evt_JOIN(NetworkChannelEventArgs args)
		{
			if (this.On_JOIN != null)
			{
				this.On_JOIN(this, args);
			}
		}

		public virtual void __evt_PART(NetworkChannelEventArgs args)
		{
			if (this.On_PART != null)
			{
				this.On_PART(this, args);
			}
		}

		public virtual void __evt_KICK(NetworkKickEventArgs args)
		{
			if (this.On_KICK != null)
			{
				this.On_KICK(this, args);
			}
		}

        public virtual void __evt_CTCP(NetworkCTCPEventArgs args)
        {
            if (this.On_CTCP != null)
            {
                this.On_CTCP(this, args);
            }
        }

        public virtual void __evt_QUIT(NetworkGenericDataEventArgs args)
        {
            if (this.On_QUIT != null)
            {
                this.On_QUIT(this, args);
            }
        }

        public virtual void __evt_ChannelInfo(NetworkChannelDataEventArgs args)
        {
            if (this.On_ChannelInfo != null)
            {
                this.On_ChannelInfo(this, args);
            }
        }

        /// <summary>
        /// Command 333 is handled by this
        /// </summary>
        /// <param name="args"></param>
        public virtual void __evt_TopicInfo(NetworkTOPICEventArgs args)
        {
            if (On_TopicInfo != null)
            {
                this.On_TopicInfo(this, args);
            }
        }

        /// <summary>
        /// Called when server send us 332 command with some topic information
        /// </summary>
        /// <param name="args"></param>
        public virtual void __evt_TopicData(NetworkTOPICEventArgs args)
        {
            if (On_TopicData != null)
            {
                this.On_TopicData(this, args);
            }
        }

        public virtual void __evt_TOPIC(NetworkTOPICEventArgs args)
        {
            if (this.On_TOPIC != null)
            {
                this.On_TOPIC(this, args);
            }
        }

		public virtual void __evt_ChannelUserList(ChannelUserListEventArgs args)
		{
			if (this.On_ChannelUserList != null)
			{
				this.On_ChannelUserList(this, args);
			}
		}
		
		public virtual void __evt_FinishChannelParseUser(NetworkChannelDataEventArgs args)
		{
			if (this.On_FinishChannelParseUser != null)
			{
				this.On_FinishChannelParseUser(this, args);
			}
		}
		
		public virtual void __evt_NICK(NetworkNICKEventArgs args)
		{
			if (this.On_NICK != null)
			{
				this.On_NICK(this, args);
			}
		}
		
		public virtual void __evt_MODE(NetworkMODEEventArgs args)
		{
			if (this.On_MODE != null)
			{
				this.On_MODE(this, args);
			}
		}

        /// <summary>
        /// Send a message to network
        /// </summary>
        /// <param name="text">Text of message</param>
        /// <param name="to">Sending to</param>
        /// <param name="_priority">Priority</param>
        /// <param name="pmsg">If this is private message (so it needs to be handled in a different way)</param>
        public virtual void Message(string text, string to, Defs.Priority _priority = Defs.Priority.Normal)
        {
            _Protocol.Message(text, to, this, _priority);
        }

        /// <summary>
        /// Unregister info for user and channel modes
        /// </summary>
        /// <param name="key">Mode</param>
        /// <returns></returns>
        public virtual bool UnregisterInfo(char key)
        {
            lock (Descriptions)
            {
                if (Descriptions.ContainsKey(key))
                {
                    Descriptions.Remove(key);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Send a message to network
        /// </summary>
        /// <param name="text">Text of message</param>
        /// <param name="to">Sending to</param>
        /// <param name="_priority">Priority</param>
        /// <param name="pmsg">If this is private message (so it needs to be handled in a different way)</param>
        public virtual void Act(string text, string to, Defs.Priority _priority = Defs.Priority.Normal)
        {
            _Protocol.Act(text, to, this, _priority);
        }
        
        /// <summary>
        /// Destroy this class, be careful, it can't be used in any way after you
        /// call this
        /// </summary>
        public virtual void Destroy()
        {
            if (IsDestroyed)
            {
                // avoid calling this function multiple times, otherwise it could crash
                //Core.DebugLog("Destroy() called multiple times on " + ServerName);
                return;
            }

            isDestroyed = true;

            lock (ChannelList)
            {
                ChannelList.Clear();
            }

            lock (Channels)
            {
                foreach (Channel xx in Channels.Values)
                {
                    xx.Destroy();
                }
                Channels.Clear();
            }

            _Protocol = null;

            lock (Descriptions)
            {
                Descriptions.Clear();
            }
        }

        /// <summary>
        /// Register info for channel info
        /// </summary>
        /// <param name="key">Mode</param>
        /// <param name="text">Text</param>
        /// <returns>true on success, false if this info already exist</returns>
        public virtual bool RegisterInfo(char key, string text)
        {
            lock (Descriptions)
            {
                if (Descriptions.ContainsKey(key))
                {
                    return false;
                }
                Descriptions.Add(key, text);
                return true;
            }
        }

        /// <summary>
        /// Transfer data to this network server
        /// </summary>
        /// <param name="data"></param>
        /// <param name="_priority"></param>
        public virtual void Transfer(string data, Defs.Priority _priority = Defs.Priority.Normal)
        {
            if (!string.IsNullOrEmpty(data))
            {
                _Protocol.Transfer(data, _priority, this);
            }
        }

        /// <summary>
        /// Disconnect you from network
        /// </summary>
        public virtual void Disconnect()
        {
            lock (this)
            {
                Transfer("QUIT :" + Quit);
                SetDisconnected();
            }
        }
    }
}
