using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Threading;
using System.Collections;

namespace ToolBelt
{
    public sealed class RemoteOutputter : MarshalByRefObject, IOutputter, IDisposable
    {
        #region Private Fields
        private IOutputter outputter;
        private IChannel remotingChannel;
        private string remotingUrl;
        private EventWaitHandle blockingEvent = new EventWaitHandle(
            false, EventResetMode.ManualReset, typeof(RemoteOutputter).ToString() + ".WaitingEvent");

        #endregion

        #region Constructors
        /// <summary>
        /// Construct the server side of the remote build engine
        /// </summary>
        /// <param name="outputter"></param>
        public RemoteOutputter(IOutputter outputter)
        {
            this.outputter = outputter;

            // Set up a remoting channel
            BinaryServerFormatterSinkProvider sinkProvider = new BinaryServerFormatterSinkProvider();

            sinkProvider.TypeFilterLevel = TypeFilterLevel.Full;

            IDictionary properties = new Hashtable();

            properties["name"] = Guid.NewGuid().ToString();
            properties["portName"] = this.GetType().ToString();
            properties["exclusiveAddressUse"] = true;

            IpcServerChannel channel = new IpcServerChannel(properties, sinkProvider);
            ChannelServices.RegisterChannel(channel, true);

            this.remotingChannel = channel;

            // Set up the remoting server for the build engine
            RemotingServices.Marshal(this, Guid.NewGuid().ToString(), typeof(IOutputter));

            // Create the URL
            string[] urls = channel.GetUrlsForUri(RemotingServices.GetObjectUri(this));

            Debug.Assert(urls.Length == 1);

            remotingUrl = urls[0];
        }

        /// <summary>
        /// Construct the client side of the remote build engine
        /// </summary>
        /// <param name="remotingUrl"></param>
        public RemoteOutputter(string remotingUrl)
        {
            this.remotingUrl = remotingUrl;

            BinaryClientFormatterSinkProvider sinkProvider = new BinaryClientFormatterSinkProvider();
            IpcClientChannel channel = new IpcClientChannel(Guid.NewGuid().ToString(), sinkProvider);
            ChannelServices.RegisterChannel(channel, true);
            
            this.remotingChannel = channel;

            try
            {
                outputter = (IOutputter)Activator.GetObject(typeof(IOutputter), remotingUrl);
            }
            catch (RemotingException)
            {
                // Can't access remote build engine; do not log anything
            }
        }

        #endregion

        #region Public Properties
        public string RemotingUrl
        {
            get { return this.remotingUrl; }
        }

        public bool IsServer
        {
            get
            {
                return remotingChannel is IpcServerChannel;
            }
        }

        public WaitHandle BlockingEvent
        {
            get { return blockingEvent; }
        }

        #endregion

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            if (this.remotingChannel != null)
            {
                ChannelServices.UnregisterChannel(this.remotingChannel);
                this.remotingChannel = null;
                this.remotingUrl = null;
                this.outputter = null;
            }
        }
        #endregion

        #region Private Methods
        delegate T Action<T>(IOutputter outputter);

        private T TryRemoteAction<T>(Action<T> action)
        {
            try
            {
                if (this.outputter != null)
                    return action(this.outputter);
            }
            catch (RemotingException)
            {
                this.outputter = null;
            }
            return default(T);

        }

        #endregion

        #region IOutputter Members

        void IOutputter.OutputCustomEvent(OutputCustomEventArgs e)
        {
            TryRemoteAction(delegate(IOutputter outputter)
            {
                // Check property to indicate if we are the server.  If so and the string is 
                // "blocked" then the child process is waiting for something big and we can 
                // set an event to unblock ourselves.
                if (IsServer)
                {
                    RemoteOutputEventArgs args = e as RemoteOutputEventArgs;

                    if (args != null)
                    {
                        if (args.IsBlocking)
                            blockingEvent.Set();
                        else
                            blockingEvent.Reset();
                    }
                }
                else
                    outputter.OutputCustomEvent(e);

                return 0;
            });
        }

        void IOutputter.OutputErrorEvent(OutputErrorEventArgs e)
        {
            TryRemoteAction(delegate(IOutputter outputter)
            {
                outputter.OutputErrorEvent(e);
                return 0;
            });
        }

        void IOutputter.OutputMessageEvent(OutputMessageEventArgs e)
        {
            TryRemoteAction(delegate(IOutputter outputter)
            {
                outputter.OutputMessageEvent(e);
                return 0;
            });
        }

        void IOutputter.OutputWarningEvent(OutputWarningEventArgs e)
        {
            TryRemoteAction(delegate(IOutputter outputter)
            {
                outputter.OutputWarningEvent(e);
                return 0;
            });
        }

        #endregion
    }

    [Serializable]
    public class RemoteOutputEventArgs : OutputCustomEventArgs
    {
        public bool IsBlocking { get; internal set; }

        public RemoteOutputEventArgs(bool isBlocking)
        {
            IsBlocking = isBlocking;
        }
    }
}
