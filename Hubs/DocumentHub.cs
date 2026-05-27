using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace CDE.Hubs
{
    public class DocumentHub : Hub
    {
        private const string ConnectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CDE;Integrated Security=True;";

        private static readonly ConcurrentDictionary<string, ConnectionInfo> Connections =
            new ConcurrentDictionary<string, ConnectionInfo>();

        public async Task JoinDocument(int documentId, string userId, string userName)
        {
            if (!CanAccessDocument(documentId, userId))
            {
                return;
            }

            string groupName = GetGroupName(documentId);
            Connections[Context.ConnectionId] = new ConnectionInfo
            {
                DocumentId = documentId,
                UserId = userId,
                UserName = userName
            };

            await Groups.Add(Context.ConnectionId, groupName);

            string content = GetDocumentContent(documentId);
            Clients.Caller.loadDocument(content);
            Clients.OthersInGroup(groupName).userJoined(userId, userName);
            Clients.OthersInGroup(groupName).requestLiveSync(userId);
        }

        public void BroadcastDocumentState(int documentId, string userId, string content)
        {
            if (!CanAccessDocument(documentId, userId))
            {
                return;
            }

            string groupName = GetGroupName(documentId);
            Clients.OthersInGroup(groupName).receiveDocumentState(userId, content ?? string.Empty);
        }

        public async Task LeaveDocument(int documentId, string userId)
        {
            string groupName = GetGroupName(documentId);
            await Groups.Remove(Context.ConnectionId, groupName);
            ConnectionInfo removed;
            Connections.TryRemove(Context.ConnectionId, out removed);
            Clients.OthersInGroup(groupName).userLeft(userId);
        }

        public void SendCursor(int documentId, string userId, string userName, int caretIndex)
        {
            if (!CanAccessDocument(documentId, userId))
            {
                return;
            }

            string groupName = GetGroupName(documentId);
            Clients.OthersInGroup(groupName).updateCursor(userId, userName, caretIndex);
        }

        public void SendTextChange(int documentId, string userId, string userName, int index, string removed, string inserted, int caretIndex)
        {
            if (!CanAccessDocument(documentId, userId))
            {
                return;
            }

            string groupName = GetGroupName(documentId);
            Clients.OthersInGroup(groupName).applyTextChange(userId, userName, index, removed ?? string.Empty, inserted ?? string.Empty, caretIndex);
        }

        public void PersistContent(int documentId, string userId, string content)
        {
            if (!CanAccessDocument(documentId, userId))
            {
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    con.Open();
                    string query = "UPDATE Documents SET Content = @Content, UpdatedAt = @UpdatedAt WHERE DocumentID = @DocumentID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Content", content ?? string.Empty);
                        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                        cmd.Parameters.AddWithValue("@DocumentID", documentId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // Ignore persistence errors during live editing
            }
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            ConnectionInfo info;
            if (Connections.TryRemove(Context.ConnectionId, out info))
            {
                string groupName = GetGroupName(info.DocumentId);
                Clients.OthersInGroup(groupName).userLeft(info.UserId);
            }

            return base.OnDisconnected(stopCalled);
        }

        private static string GetGroupName(int documentId)
        {
            return "doc-" + documentId;
        }

        private static string GetDocumentContent(int documentId)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                string query = "SELECT Content FROM Documents WHERE DocumentID = @DocumentID";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DocumentID", documentId);
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    return result != null && result != DBNull.Value ? result.ToString() : string.Empty;
                }
            }
        }

        private static bool CanAccessDocument(int documentId, string userId)
        {
            int uid;
            if (documentId <= 0 || !int.TryParse(userId, out uid))
            {
                return false;
            }

            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                string query = "SELECT COUNT(1) FROM Documents d " +
                               "LEFT JOIN Collaborators c ON d.DocumentID = c.DocumentID " +
                               "WHERE d.DocumentID = @DocumentID AND (d.OwnerID = @UserID OR c.UserID = @UserID)";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DocumentID", documentId);
                    cmd.Parameters.AddWithValue("@UserID", uid);
                    con.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        private class ConnectionInfo
        {
            public int DocumentId { get; set; }
            public string UserId { get; set; }
            public string UserName { get; set; }
        }
    }
}
