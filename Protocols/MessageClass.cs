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
    public partial class ProtocolIrc : Protocol
    {
        public class MessagesClass
        {
            /// <summary>
            /// Message
            /// </summary>
            public struct Message
            {
                /// <summary>
                /// Priority
                /// </summary>
                public Defs.Priority _Priority;
                /// <summary>
                /// Message
                /// </summary>
                public string message;
            }
            /// <summary>
            /// List of all messages that can be processed
            /// </summary>
            public List<Message> messages = new List<Message>();
            /// <summary>
            /// List of all new messages that need to be parsed
            /// </summary>
            public List<Message> newmessages = new List<Message>();
            /// <summary>
            /// Protocol
            /// </summary>
            public ProtocolIrc protocol = null;

            public int Size()
            {
                return newmessages.Count + messages.Count;
            }

            /// <summary>
            /// Send a message to server
            /// </summary>
            /// <param name="Message"></param>
            /// <param name="Pr"></param>
            public void DeliverMessage(string Message, Defs.Priority Pr = Defs.Priority.Normal)
            {
                Message text = new Message();
                text._Priority = Pr;
                text.message = Message;
                lock (messages)
                {
                    messages.Add(text);
                    return;
                }
            }

            /// <summary>
            /// This is main event loop in which the protocol is running, it is meant to run in own thread
            /// since this function 
            /// </summary>
            public void Run()
            {
                try
                {
                    // give the ircd some time to process the authentication before sending messages
                    while (!protocol.IRCNetwork.IsConnected)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                    while (protocol.IsConnected && protocol.IRCNetwork.IsConnected)
                    {
                        try
                        {
                            if (messages.Count > 0)
                            {
                                lock (messages)
                                {
                                    newmessages.AddRange(messages);
                                    messages.Clear();
                                }
                            }
                            if (newmessages.Count > 0)
                            {
                                List<Message> Processed = new List<Message>();
                                Defs.Priority highest = Defs.Priority.Low;
                                while (newmessages.Count > 0)
                                {
                                    // we need to get all messages that have been scheduled to be send
                                    lock (messages)
                                    {
                                        if (messages.Count > 0)
                                        {
                                            newmessages.AddRange(messages);
                                            messages.Clear();
                                        }
                                    }
                                    highest = Defs.Priority.Low;
                                    // we need to check the priority we need to handle first
                                    foreach (Message message in newmessages)
                                    {
                                        if (message._Priority > highest)
                                        {
                                            highest = message._Priority;
                                            if (message._Priority == Defs.Priority.High)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                    // send highest priority first
                                    foreach (Message message in newmessages)
                                    {
                                        if (message._Priority >= highest)
                                        {
                                            Processed.Add(message);
                                            protocol.Send(message.message);
                                            System.Threading.Thread.Sleep(protocol.IRCNetwork.Config.TrafficInterval);
                                            if (highest != Defs.Priority.High)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                    foreach (Message message in Processed)
                                    {
                                        if (newmessages.Contains(message))
                                        {
                                            newmessages.Remove(message);
                                        }
                                    }
                                }
                            }
                            newmessages.Clear();
                            Thread.Sleep(20);
                        }
                        catch (ThreadAbortException)
                        {
                            ThreadManager.RemoveThread(System.Threading.Thread.CurrentThread);
                            return;
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    ThreadManager.RemoveThread(System.Threading.Thread.CurrentThread);
                    return;
                }
                catch (Exception fail)
                {
                    if (protocol != null)
                    {
                        protocol.HandleException(fail);
                    }
                }
            }
        }
    }
}

