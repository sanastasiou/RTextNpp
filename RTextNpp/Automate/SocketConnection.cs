using System;
using System.Net.Sockets;
using System.Text;

namespace RTextNppPlugin.Automate
{
    public class SocketConnection
    {
        #region [Data Members]
        private byte[] mBuffer                 = new byte[Constants.BUFFER_SIZE];
        private StringBuilder mReceivedMessage = new StringBuilder(Constants.BUFFER_SIZE);
        private Socket mSocket                 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        #endregion

        #region [Interface]
        public StringBuilder ReceivedMessage { get { return mReceivedMessage; } set { mReceivedMessage = value; } }

        public bool LengthMatched { get; set; }
        public int RequiredLength { get; set; }
        public int JSONLength { get; set; }
        public int BytesToRead { get; private set; }

        /**
         * \brief   Sends a request synchronously.
         *
         * \param   request The request.
         *
         * \return  The bytes that were actually send.
         */
        public int SendRequest(byte[] request)
        {
            return mSocket.Send(request);
        }

        public bool Connected
        {
            get
            {
                return mSocket.Connected;
            }
        }

        /**
         * \brief   Cleans up and disposes the socket.
         */
        public void CleanUpSocket()
        {
            if (mSocket != null && mSocket.Connected)
            {
                mSocket.Disconnect(false);
                mSocket.Shutdown(SocketShutdown.Both);
                mSocket.Close();
                mSocket.Dispose();
            }
            mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        /**
         * \brief   Ends asynchronous receiving.
         *
         * \param   asyncResult The asynchronous result.
         *
         * \return  An int.
         */
        public void EndReceive(IAsyncResult asyncResult)
        {
            BytesToRead = mSocket.EndReceive(asyncResult);
        }

        /**
         * \brief   Begins socket connection.
         *
         * \param   host            The host.
         * \param   port            The port.
         * \param   requestCallback The request callback.
         *
         * \return  An IAsyncResult.
         */
        public IAsyncResult BeginConnect(string host, int port, AsyncCallback requestCallback)
        {
            return mSocket.BeginConnect(host, port, requestCallback, this);
        }

        /**
         * \brief   Begins asynchronous receiving.
         *
         * \param   buffer      The buffer.
         * \param   offset      The offset.
         * \param   size        The size.
         * \param   socketFlags The socket flags.
         * \param   callback    The callback.
         *
         * \return  An IAsyncResult.
         */
        public IAsyncResult BeginReceive(AsyncCallback callback)
        {
            return mSocket.BeginReceive(mBuffer, 0, Constants.BUFFER_SIZE, SocketFlags.None, callback, this);
        }

        public void Append()
        {
            mReceivedMessage.Append(Encoding.ASCII.GetString(mBuffer, 0, BytesToRead));
        }
        #endregion

        internal void EndConnect(IAsyncResult ar)
        {
            mSocket.EndConnect(ar);
        }
    }
}
