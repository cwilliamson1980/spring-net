#region License

/*
 * Copyright � 2002-2006 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System;
using System.Collections;
using Common.Logging;
using Spring.Collections;
using Spring.Transaction.Support;
using Spring.Util;
using Apache.NMS;

namespace Spring.Messaging.Nms.Connection
{
    /// <summary> IConnection holder, wrapping a NMS IConnection and a NMS ISession.
    /// NmsTransactionManager binds instances of this class to the thread,
    /// for a given NMS IConnectionFactory.
    ///
    /// <p>Note: This is an SPI class, not intended to be used by applications.</p>
    ///
    /// </summary>
    /// <author>Juergen Hoeller</author>
    /// <author>Mark Pollack (.NET)</author>
    public class NmsResourceHolder : ResourceHolderSupport
    {
        #region Logging

        private static readonly ILog logger = LogManager.GetLogger(typeof(NmsResourceHolder));

        #endregion

        #region Fields

        private bool frozen;

        private IList connections = new LinkedList();

        private IList sessions = new LinkedList();

        private IDictionary sessionsPerIConnection = new Hashtable();
        private IConnectionFactory connectionFactory;

        #endregion


        #region Constructor (s)

        /// <summary> Create a new NmsResourceHolder that is open for resources to be added.</summary>
        public NmsResourceHolder()
        {
            this.frozen = false;
        }

        /// <summary> Create a new NmsResourceHolder for the given NMS resources.</summary>
        /// <param name="connection">the NMS IConnection
        /// </param>
        /// <param name="session">the NMS ISession
        /// </param>
        public NmsResourceHolder(Apache.NMS.IConnection connection, ISession session)
        {
            AddConnection(connection);
            AddSession(session, connection);
            this.frozen = true;
        }

        public NmsResourceHolder(IConnectionFactory connectionFactory, IConnection connection, ISession session)
        {
            this.connectionFactory = connectionFactory;
            AddConnection(connection);
            AddSession(session, connection);
            this.frozen = true;
        }
        #endregion
        
        #region Properties

        virtual public bool Frozen
        {
            get
            {
                return frozen;
            }

        }
        #endregion

        #region Methods
        
        public void AddConnection(Apache.NMS.IConnection connection)
        {
            //TODO - update Assert utility class...
            //Assert.isTrue(!this.frozen, "Cannot add IConnection because NmsResourceHolder is frozen");
            AssertUtils.ArgumentNotNull(connection, "IConnection must not be null");
            if (!connections.Contains(connection))
            {
                connections.Add(connection);
            }
        }

        public void AddSession(ISession session)
        {
            AddSession(session, null);
        }

        public void AddSession(ISession session, Apache.NMS.IConnection connection)
        {
            //TOOD update AssertUtils class
            //Assert.isTrue(!this.frozen, "Cannot add ISession because NmsResourceHolder is frozen");
            AssertUtils.ArgumentNotNull(session, "ISession must not be null");
            if (!sessions.Contains(session))
            {
                sessions.Add(session);
                if (connection != null)
                {
                    IList sessionsList = (IList)sessionsPerIConnection[connection];
                    if (sessionsList == null)
                    {
                        sessionsList = new LinkedList();
                        sessionsPerIConnection[connection] = sessionsList;
                    }
                    sessionsList.Add(session);
                }
            }
        }

        public virtual Apache.NMS.IConnection GetConnection()
        {
            return (!(this.connections.Count == 0) ? (Apache.NMS.IConnection)this.connections[0] : null);
        }

        public virtual Apache.NMS.IConnection GetConnection(System.Type connectionType)
        {
            throw new NotImplementedException();
            //TODO Updae CollectionUtils...
            //return (NMS.IConnection)CollectionUtils.FindValueOfType(this.connections, connectionType);
        }

       public virtual ISession GetSession()
        {
           return (!(this.sessions.Count == 0) ? (ISession)this.sessions[0] : null);
        }

        public virtual ISession GetSession(Type sessionType)
        {
            return GetSession(sessionType, null);
        }

        public virtual ISession GetSession(System.Type sessionType, Apache.NMS.IConnection connection)
        {
            IList sessions = this.sessions;
            if (connection != null)
            {
                sessions = (IList)sessionsPerIConnection[connection];
            }
            throw new NotImplementedException();
            //TODO update collection utils
            //return (ISession)CollectionUtils.FindValueOfType(sessions, sessionType);
        }

        /// <summary>
        /// Commits all sessions.
        /// </summary>
        public virtual void CommitAll()
        {
            foreach (ISession session in sessions)
            {
				session.Commit();
            }
        }

        /// <summary>
        /// Closes all sessions then stops and closes all connections, in that order.
        /// </summary>
        public virtual void CloseAll()
        {
            foreach (ISession session in sessions)
            {
                try
                {
                    session.Close();
                }
                catch (Exception ex)
                {
                    logger.Debug("Could not close NMS ISession after transaction", ex);
                }
            }
            foreach (IConnection connection in connections)
            {
                ConnectionFactoryUtils.ReleaseConnection(connection, connectionFactory, true);
            }
        }

        public bool ContainsSession(ISession session)
        {
            return this.sessions.Contains(session);
        }

        #endregion
    }
}
