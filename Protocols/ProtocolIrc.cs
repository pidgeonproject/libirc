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
using System.Threading;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace libirc.Protocols
{
    /// <summary>
    /// Protocol
    /// </summary>
    public partial class ProtocolIrc : Protocol, IDisposable
    {
        public class TrafficLogEventArgs : EventArgs
        {
            public string Message = null;
            public bool Incoming = false;
        }

        public delegate void TrafficLogEventHandler(object sender,TrafficLogEventArgs e);

        public event TrafficLogEventHandler TrafficLogEvent;

        /// <summary>
        /// Thread in which the connection to server is handled
        /// </summary>
        public Thread TMain = null;
        /// <summary>
        /// Thread which is handling the delivery of data
        /// </summary>
        public Thread TDeliveryQueue = null;
        /// <summary>
        /// Thread which is keeping the connection online
        /// </summary>
        public Thread TKeep = null;
        /// <summary>
        /// Time of last ping
        /// </summary>
        public DateTime LastPing = DateTime.Now;
        /// <summary>
        /// Network stream
        /// </summary>
        private NetworkStream networkStream = null;
        /// <summary>
        /// SSL
        /// </summary>
        private SslStream networkSsl = null;
        /// <summary>
        /// Stream reader for server
        /// </summary>
        private System.IO.StreamReader streamReader = null;
        private object streamLock = new object();
        /// <summary>
        /// Network associated with this connection (we have only 1 network in direct connection)
        /// </summary>
        public Network IRCNetwork;
        /// <summary>
        /// Stream writer for server
        /// </summary>
        private System.IO.StreamWriter streamWriter = null;
        /// <summary>
        /// Messages
        /// </summary>
        private MessagesClass Messages = new MessagesClass();
        /// <summary>
        /// Logging in using sasl
        /// </summary>
        public bool UsingSasl = false;
        private bool disposed = false;
        /// <summary>
        /// Character that represent underline tags
        /// </summary>
        public static string UnderlineChar = ((char)001).ToString();
        /// <summary>
        /// Character that makes a text bold
        /// </summary>
        public static string BoldChar = ((char)002).ToString();
        /// <summary>
        /// Character that represent color tags
        /// </summary>
        public static string ColorChar = ((char)003).ToString();

        /// <summary>
        /// Releases all resources used by this class
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by this class
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (streamReader != null)
                    {
                        streamReader.Dispose();
                    }
                    if (networkSsl != null)
                    {
                        networkSsl.Dispose();
                    }
                    if (streamWriter != null)
                    {
                        streamWriter.Dispose();
                    }
                    if (!IsDestroyed)
                    {
                        Exit();
                    }
                }
            }
            disposed = true;
        }

        /// <summary>
        /// Part
        /// </summary>
        /// <param name="name">Channel</param>
        /// <param name="network"></param>
        public override void Part(string name, Network network = null)
        {
            Transfer("PART " + name, Defs.Priority.High, network);
        }

        /// <summary>
        /// Transfer
        /// </summary>
        /// <param name="text"></param>
        /// <param name="priority"></param>
        /// <param name="network"></param>
        public override void Transfer(string text, Defs.Priority priority = Defs.Priority.Normal, Network network = null)
        {
            Messages.DeliverMessage(text, priority);
        }

        public void TrafficLog(string text, bool incoming)
        {
            TrafficLogEventArgs args = new TrafficLogEventArgs();
            args.Message = text;
            args.Incoming = incoming;
            if (this.TrafficLogEvent != null)
            {
                this.TrafficLogEvent(this, args);
            }
        }

        private void _Ping()
        {
            try
            {
                while (!IRCNetwork.IsConnected)
                {
                    Thread.Sleep(1000);
                }
                while (IRCNetwork.IsConnected && IsConnected)
                {
                    Transfer("PING :" + IRCNetwork._Protocol.Server, Defs.Priority.High);
                    Thread.Sleep(24000);
                }
            } catch (ThreadAbortException)
            {
                return;
            } catch (System.Net.Sockets.SocketException)
            {
                return;
            }
        }

        public void Start()
        {
            Messages.protocol = this;
            if (!SSL)
            {
                networkStream = new System.Net.Sockets.TcpClient(Server, Port).GetStream();
                streamWriter = new System.IO.StreamWriter(networkStream);
                streamReader = new System.IO.StreamReader(networkStream, NetworkEncoding);
            }
            if (SSL)
            {
                System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient(Server, Port);
                networkSsl = new System.Net.Security.SslStream(client.GetStream(), true,
                                                                new System.Net.Security.RemoteCertificateValidationCallback(Protocol.ValidateServerCertificate), null);
                networkSsl.AuthenticateAsClient(Server);
                streamWriter = new System.IO.StreamWriter(networkSsl);
                streamReader = new System.IO.StreamReader(networkSsl, NetworkEncoding);
            }

            //Hooks._Protocol.BeforeConnect(this);

            Connected = true;
            if (!string.IsNullOrEmpty(Password))
            {
                Send("PASS " + Password);
            }
            Send("USER " + IRCNetwork.Ident + " 8 * :" + IRCNetwork.UserName);
            Send("NICK " + IRCNetwork.Nickname);
            if (!this.ManualThreads)
            {
                TKeep = new Thread(_Ping);
                TKeep.Name = "libirc:" + this.Server + "/pinger";
                ThreadManager.RegisterThread(TKeep);
                TKeep.Start();
            }

            try
            {
                TDeliveryQueue = new System.Threading.Thread(Messages.Run);
                TDeliveryQueue.Start();

                while (!streamReader.EndOfStream && IsConnected)
                {
                    if (!IRCNetwork.IsConnected)
                    {
                        IRCNetwork.SetConnected();
                    }
                    string text = streamReader.ReadLine();
                    text = this.RawTraffic(text);
                    this.TrafficLog(text, true);
                    ProcessorIRC processor = new ProcessorIRC(IRCNetwork, text, ref LastPing);
                    processor.ProfiledResult();
                    LastPing = processor.pong;
                }
            } catch (ThreadAbortException)
            {
                this.Connected = false;
                return;
            } catch (System.Net.Sockets.SocketException)
            {
                SafeDc();
            } catch (System.IO.IOException)
            {
                SafeDc();
            }
            ThreadManager.KillThread(System.Threading.Thread.CurrentThread);
            return;
        }

        private void SafeDc()
        {
            if (IRCNetwork != null)
            {
                IRCNetwork.SetDisconnected();
            }
            if (SSL)
            {
                if (networkSsl != null)
                {
                    networkSsl.Close();
                }
            } else
            {
                if (networkStream != null)
                {
                    networkStream.Close();
                }
            }
            Connected = false;
        }

        /// <summary>
        /// Command
        /// </summary>
        /// <param name="cm"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        public override bool Command(string cm, Network network = null)
        {
            if (cm.StartsWith(" ", StringComparison.Ordinal) != true && cm.Contains(" "))
            {
                // uppercase
                string first_word = cm.Substring(0, cm.IndexOf(" ", StringComparison.Ordinal)).ToUpper();
                string rest = cm.Substring(first_word.Length);
                Transfer(first_word + rest);
                return true;
            }
            Transfer(cm.ToUpper());
            return false;
        }

        private void Send(string ms)
        {
            if (!IsConnected)
            {
                this.DebugLog("NETWORK: attempt to send a packet to disconnected network");
                return;
            }
            lock(streamLock)
            {
                streamWriter.WriteLine(ms);
                this.TrafficLog(ms, false);
                streamWriter.Flush();
            }
        }

        /// <summary>
        /// Send a message either to channel or user
        /// </summary>
        /// <param name="text"></param>
        /// <param name="to"></param>
        /// <param name="network"></param>
        /// <param name="priority"></param>
        /// <param name="pmsg"></param>
        /// <returns></returns>
        public override int Message(string text, string to, Network network, Defs.Priority priority = Defs.Priority.Normal)
        {
            Transfer("PRIVMSG " + to + " :" + text, priority);
            return 0;
        }

        /// <summary>
        /// /me style
        /// </summary>
        /// <param name="text"></param>
        /// <param name="to"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public override int Act(string text, string to, Defs.Priority priority = Defs.Priority.Normal)
        {
            Transfer("PRIVMSG " + to + " :" + delimiter.ToString() + "ACTION " + text + delimiter.ToString(), priority);
            return 0;
        }

        /// <summary>
        /// Disconnect
        /// </summary>
        /// <returns></returns>
        public override bool Disconnect()
        {
            // we lock the function so that it can't be called in same time in different thread
            lock(this)
            {
                if (!IsConnected || IRCNetwork == null)
                {
                    return false;
                }
                try
                {
                    Send("QUIT :" + IRCNetwork.Quit);
                    IRCNetwork.SetDisconnected();
                    if (SSL)
                    {
                        networkSsl.Close();
                    } else
                    {
                        networkStream.Close();
                    }
                    streamWriter.Close();
                    streamReader.Close();
                    Connected = false;
                } catch (System.IO.IOException er)
                {
                    this.DebugLog(er.Message);
                    Connected = false;
                }
            }
            return true;
        }

        /// <summary>
        /// Join
        /// </summary>
        /// <param name="name">Channel</param>
        /// <param name="network"></param>
        public override void Join(string name, Network network = null)
        {
            Transfer("JOIN " + name);
        }

        /// <summary>
        /// Request nick
        /// </summary>
        /// <param name="_Nick"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        public override int RequestNick(string _Nick, Network network = null)
        {
            Transfer("NICK " + _Nick);
            return 0;
        }

        /// <summary>
        /// Reconnect network
        /// </summary>
        /// <param name="network"></param>
        public override void ReconnectNetwork(Network network)
        {
            if (this.ManualThreads)
            {
                throw new Exception("You can't call this method when ManualThreads are being used");
            }

            if (IsConnected)
            {
                Disconnect();
            }

            if (TMain != null)
            {
                ThreadManager.KillThread(TMain);
                TMain = new System.Threading.Thread(Start);
                this.DebugLog("Reconnecting to server " + Server + " port " + Port.ToString());
                TMain.Start();
                ThreadManager.RegisterThread(TMain);
            }
        }

        /// <summary>
        /// Destroy this instance of protocol and release all objects
        /// </summary>
        public override void Exit()
        {
            if (IsDestroyed)
            {
                this.DebugLog("This object is already destroyed " + Server);
                return;
            }

            if (IsConnected)
            {
                Disconnect();
            }

            if (!this.ManualThreads)
            {
                ThreadManager.KillThread(TMain);
            }
            IRCNetwork.Destroy();
            Connected = false;
            Destroyed = true;
            base.Exit();
        }

        /// <summary>
        /// Connect
        /// </summary>
        /// <returns></returns>
        public override Thread Open()
        {
            if (this.ManualThreads)
            {
                throw new Exception("You can't call this method when Protocol is in ManualThreads mode");
            }
            TMain = new Thread(Start);
            this.DebugLog("Connecting to server " + Server + " port " + Port.ToString());
            TMain.Start();
            ThreadManager.RegisterThread(TMain);
            return TMain;
        }
    }
}
