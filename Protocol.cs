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

// Documentation
///////////////////////
// This file contains a default class for protocols which all the other classes are inherited from
// some functions are returning integer, which should be 0 on success and 2 by default
// which means that the function was never overriden, so that a function working with that can catch it

using System;
using System.Collections.Generic;
using System.Threading;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace libirc
{
    /// <summary>
    /// This is lowest level of protocol interface
    /// 
    /// Every protocol is inherited from this class. Protocols are handling connections to various servers,
    /// these are the lowest level object you will handle in this library.
    /// </summary>
    [Serializable]
    public class IProtocol
    {
        public class DebugLogEventArgs : EventArgs
        {
            public string Message = null;
            public int Verbosity = 0;
        }
		
		public class ServerDcEventArgs : EventArgs
		{
			public Exception Exception = null;
			public string Reason = null;
		}
		
		public class TrafficLogEventArgs : EventArgs
        {
            public string Message = null;
            public bool Incoming = false;
        }
		
        public class RawTrafficEventArgs : EventArgs
        {
            public string Data = null;
        }
		
		public delegate void ServerDisconnectEventHandler(object sender, ServerDcEventArgs e);
        public delegate void RawTrafficEventHandler(object sender, RawTrafficEventArgs e);
        public delegate void DebugLogEventHandler(object sender, DebugLogEventArgs e);
		public delegate void TrafficLogEventHandler(object sender,TrafficLogEventArgs e);
        public event TrafficLogEventHandler TrafficLogEvent;
		public event ServerDisconnectEventHandler DisconnectEvent;
        public event DebugLogEventHandler DebugLogEvent;
        /// <summary>
        /// Occurs when raw traffic is incoming to protocol, you can alter this raw traffic as well by changing the Data
        /// property in RawTrafficEventArgs
        /// </summary>
        public event RawTrafficEventHandler RawTrafficEvent;
        /// <summary>
        /// Whether this server is connected or not
        /// </summary>
        protected bool Connected = false;
        /// <summary>
        /// Whether is destroyed
        /// </summary>
        protected bool Destroyed = false;
        /// <summary>
        /// This is a time when this connection was open
        /// </summary>
        protected DateTime _time;
        /// <summary>
        /// Password for server
        /// </summary>
        public string Password = null;
        /// <summary>
        /// Change this to true if you want to create all threads for all subsystems
        /// of this protocols yourself.
        /// 
        /// This makes it harder for you to implement the irc in your application but it
        /// gives you bigger control over the parts of this library. If you leave this
        /// on false the library will manage all threads itself as well as all exceptions.
        /// </summary>
        public bool ManualThreads = false;
        /// <summary>
        /// Server
        /// </summary>
        public string Server = null;
        /// <summary>
        /// Port
        /// </summary>
        public int Port = 6667;
        /// <summary>
        /// Whether the connection is being encrypted or not
        /// </summary>
        public bool SSL = false;
        /// <summary>
        /// Encoding
        /// </summary>
        public Encoding NetworkEncoding = System.Text.Encoding.UTF8;
        /// <summary>
        /// Time since you connected to this protocol
        /// </summary>
        public TimeSpan ConnectionTime
        {
            get
            {
                return DateTime.Now - _time;
            }
        }

        /// <summary>
        /// Whether it is working
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return Connected;
            }
        }

        /// <summary>
        /// This will return true in case object was requested to be disposed
        /// you should never work with objects that return true here
        /// </summary>
        public bool IsDestroyed
        {
            get
            {
                return Destroyed;
            }
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public IProtocol()
        {
            _time = DateTime.Now;
        }

        protected virtual string RawTraffic(string traffic)
        {
            if (RawTrafficEvent != null)
            {
                RawTrafficEventArgs args = new RawTrafficEventArgs();
                args.Data = traffic;
                RawTrafficEvent(this, args);
                return args.Data;
            }
            return traffic;
        }
		
		protected virtual void DisconnectExec(string reason, Exception ex = null)
		{
			if (DisconnectEvent != null)
			{
				ServerDcEventArgs e = new ServerDcEventArgs();
				e.Exception = ex;
				e.Reason = reason;
				DisconnectEvent(this, e);
			}
		}
		
        /// <summary>
        /// This function get an input from user, if it return false, it is handled by core
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public virtual bool ParseInput(string input)
        {
            return false;
        }

        /// <summary>
        /// Release all memory associated with this object and destroy it
        /// </summary>
        public virtual void Exit()
        {
            // we removed lot of memory now, let's clean it
            System.GC.Collect();
        }

        /// <summary>
        /// This will connect this protocol
        /// </summary>
        /// <returns></returns>
        public virtual Thread Open()
        {
            this.DebugLog("Open() is not implemented");
            return null;
        }

        /// <summary>
        /// Deliver raw data to server
        /// </summary>
        /// <param name="data"></param>
        /// <param name="priority"></param>
        /// <param name="network"></param>
        public virtual void Transfer(string data, Defs.Priority priority = Defs.Priority.Normal, Network network = null)
        {
            this.DebugLog("Transfer(string data, Configuration.Priority _priority = Configuration.Priority.Normal, Network network = null) is not implemented");
        }

        /// <summary>
        /// This will disconnect the protocol but leave it in memory
        /// </summary>
        /// <returns></returns>
        public virtual bool Disconnect()
        {
            this.DebugLog("Disconnect() is not implemented");
            return false;
        }
		
		public virtual void TrafficLog(string text, bool incoming)
        {
            TrafficLogEventArgs args = new TrafficLogEventArgs();
            args.Message = text;
            args.Incoming = incoming;
            if (this.TrafficLogEvent != null)
            {
                this.TrafficLogEvent(this, args);
            }
        }
		
        /// <summary>
        /// Reconnect
        /// </summary>
        /// <returns></returns>
        public virtual bool Reconnect()
        {
            this.DebugLog("Reconnect() is not implemented");
            return false;
        }

        /// <summary>
        /// Parse a command
        /// </summary>
        /// <param name="cm"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        public virtual bool Command(string cm, Network network = null)
        {
            this.DebugLog("Command(string cm, Network network = null) is not implemented");
            return false;
        }

        public virtual void DebugLog(string Text, int Verbosity = 1)
        {
            if (this.DebugLogEvent != null)
            {
                DebugLogEventArgs args = new DebugLogEventArgs();
                args.Verbosity = Verbosity;
                args.Message = Text;
                this.DebugLogEvent(this, args);
            }
        }
    }

    /// <summary>
    /// Connection
    /// </summary>
    [Serializable]
    public class Protocol : IProtocol
    {
        /// <summary>
        /// Character which is separating the special commands (such as CTCP part)
        /// </summary>
        public char delimiter = (char)001;
        /// <summary>
        /// If changes to windows should be suppressed (no color changes on new messages)
        /// </summary>
        public bool SuppressChanges = false;

        /// <summary>
        /// Reconnect
        /// </summary>
        /// <param name="network">Network</param>
        public virtual void ReconnectNetwork(Network network)
        {
            this.DebugLog("ReconnectNetwork(Network network) is not implemented");
        }
        
        /// <summary>
        /// This will ignore all certificate issues
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        /// <summary>
        /// /me
        /// </summary>
        /// <param name="text"></param>
        /// <param name="to"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public virtual int Act(string text, string to, Defs.Priority priority = Defs.Priority.Normal)
        {
            this.DebugLog("Message2(string text, string to, Configuration.Priority priority = Configuration.Priority.Normal) is not implemented");
            return 2;
        }

        /// <summary>
        /// Send a message to server
        /// </summary>
        /// <param name="text">Message</param>
        /// <param name="to">User or a channel (needs to be prefixed with #)</param>
        /// <param name="network"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public virtual int Message(string text, string to, Network network, Defs.Priority priority = Defs.Priority.Normal)
        {
             this.DebugLog("Message(string text, string to, Network network, Configuration.Priority priority = "
                           +"Configuration.Priority.Normal, bool pmsg = false) is not implemented");
            return 2;
        }

        /// <summary>
        /// Change nick
        /// </summary>
        /// <param name="_Nick"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        public virtual int RequestNick(string _Nick, Network network = null)
        {
            this.DebugLog("requestNick(string _Nick, Network network = null) is not implemented");
            return 2;
        }

        /// <summary>
        /// Write a mode
        /// </summary>
        /// <param name="_x">Mode</param>
        /// <param name="target">Channel or user</param>
        /// <param name="network">Network</param>
        public virtual void WriteMode(NetworkMode _x, string target, Network network = null)
        {
            this.DebugLog("WriteMode(NetworkMode _x, string target, Network network = null) is not implemented");
            return;
        }

        /// <summary>
        /// /join
        /// </summary>
        /// <param name="name">Channel</param>
        /// <param name="network">Network</param>
        public virtual void Join(string name, Network network = null)
        {
            this.DebugLog("Join() is not implemented");
            return;
        }

        /// <summary>
        /// /connect
        /// </summary>
        /// <param name="server">Server</param>
        /// <param name="port">Port</param>
        /// <returns></returns>
        public virtual bool ConnectTo(string server, int port)
        {
            this.DebugLog("Disconnect() is not implemented");
            return false;
        }

        /// <summary>
        /// /part
        /// </summary>
        /// <param name="name">Channel</param>
        /// <param name="network">Network</param>
        public virtual void Part(string name, Network network = null)
        {
            this.DebugLog("Part(string name, Network network = null) is not implemented");
            return;
        }
    }
}
