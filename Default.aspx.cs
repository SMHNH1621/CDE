using System;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Web;
using System.Data;
using System.Drawing;

namespace CDE
{
    public partial class _Default : Page
    {
        // Hidden field is declared in the designer file (hfCurrentDocId).
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
            {
                phAnonymous.Visible = true;
                phLoggedIn.Visible = false;
            }
            else
            {
                phAnonymous.Visible = false;
                phLoggedIn.Visible = true;
                string displayName = Session["DisplayName"] != null ? Session["DisplayName"].ToString() : "User";
                litDisplayName.Text = displayName;
                litAvatarLetter.Text = !string.IsNullOrEmpty(displayName) ? displayName[0].ToString().ToUpper() : "U";
                hfCollabUserId.Value = Session["UserID"].ToString();
                hfCollabUserName.Value = displayName;
            }

            if (!IsPostBack)
            {
                if (Request.QueryString["open"] == "1")
                {
                    if (Session["UserID"] == null)
                    {
                        Response.Redirect("~/Login.aspx");
                        return;
                    }
                    ShowDocumentList();
                }
                else if (Request.QueryString["docId"] != null)
                {
                    int docId;
                    if (int.TryParse(Request.QueryString["docId"], out docId))
                    {
                        LoadDocument(docId);
                    }
                }
            }

            UpdateCollaborationState();
        }

        private void UpdateCollaborationState()
        {
            int documentId = GetCurrentDocumentId();
            bool canCollaborate = Session["UserID"] != null && documentId > 0;
            hfCollabEnabled.Value = canCollaborate ? "true" : "false";
            if (canCollaborate)
            {
                hfCurrentDocId.Value = documentId.ToString();
            }
        }

        private void LoadDocument(int docId)
        {
            if (Session["UserID"] == null)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }
            int userId = Convert.ToInt32(Session["UserID"]);

            string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CDE;Integrated Security=True;";
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // Verify ownership or collaboration access
                    string accessQuery = "SELECT COUNT(1) FROM Documents d " +
                                         "LEFT JOIN Collaborators c ON d.DocumentID = c.DocumentID " +
                                         "WHERE d.DocumentID = @DocumentID AND (d.OwnerID = @UserID OR c.UserID = @UserID)";
                    using (SqlCommand cmdAccess = new SqlCommand(accessQuery, con))
                    {
                        cmdAccess.Parameters.AddWithValue("@DocumentID", docId);
                        cmdAccess.Parameters.AddWithValue("@UserID", userId);
                        int count = Convert.ToInt32(cmdAccess.ExecuteScalar());
                        if (count == 0)
                        {
                            lblStatus.Text = "You do not have permission to view this document.";
                            lblStatus.ForeColor = System.Drawing.Color.Red;
                            return;
                        }
                    }

                    string query = "SELECT Title, Content FROM Documents WHERE DocumentID = @DocumentID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DocumentID", docId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                docTitle.InnerText = reader["Title"].ToString();
                                docEditor.Value = reader["Content"].ToString();
                                // Set hidden field to current document ID for client-side checks
                                hfCurrentDocId.Value = docId.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error loading document: " + ex.Message;
                lblStatus.ForeColor = System.Drawing.Color.Red;
            }

            UpdateCollaborationState();
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            txtSaveTitle.Text = docTitle.InnerText;
            lblSaveModalStatus.Text = "";
            saveFilesModal.Style["display"] = "flex";
        }

        protected void btnConfirmSave_Click(object sender, EventArgs e)
        {
            string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CDE;Integrated Security=True;";
            int currentDocumentId = 0;
            
            if (Request.QueryString["docId"] != null)
            {
                int.TryParse(Request.QueryString["docId"], out currentDocumentId);
            }

            string newTitle = txtSaveTitle.Text.Trim();
            if (string.IsNullOrEmpty(newTitle))
            {
                lblSaveModalStatus.Text = "Document name cannot be empty.";
                saveFilesModal.Style["display"] = "flex";
                return;
            }

            if (Session["UserID"] == null)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }
            int ownerId = Convert.ToInt32(Session["UserID"]);

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    if (currentDocumentId > 0)
                    {
                        // Verify ownership or collaboration access before updating
                        string accessQuery = "SELECT COUNT(1) FROM Documents d " +
                                             "LEFT JOIN Collaborators c ON d.DocumentID = c.DocumentID " +
                                             "WHERE d.DocumentID = @DocumentID AND (d.OwnerID = @UserID OR c.UserID = @UserID)";
                        using (SqlCommand cmdAccess = new SqlCommand(accessQuery, con))
                        {
                            cmdAccess.Parameters.AddWithValue("@DocumentID", currentDocumentId);
                            cmdAccess.Parameters.AddWithValue("@UserID", ownerId);
                            int count = Convert.ToInt32(cmdAccess.ExecuteScalar());
                            if (count == 0)
                            {
                                lblSaveModalStatus.Text = "You do not have permission to modify this document.";
                                saveFilesModal.Style["display"] = "flex";
                                return;
                            }
                        }

                        string updateDocQuery = "UPDATE Documents SET Title = @Title, Content = @Content, UpdatedAt = @UpdatedAt WHERE DocumentID = @DocumentID";
                        using (SqlCommand cmdUpdate = new SqlCommand(updateDocQuery, con))
                        {
                            cmdUpdate.Parameters.AddWithValue("@Title", newTitle);
                            cmdUpdate.Parameters.AddWithValue("@Content", docEditor.Value);
                            cmdUpdate.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                            cmdUpdate.Parameters.AddWithValue("@DocumentID", currentDocumentId);
                            cmdUpdate.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string insertDocQuery = "INSERT INTO Documents (OwnerID, Title, Content, CreatedAt, UpdatedAt) VALUES (@OwnerID, @Title, @Content, @CreatedAt, @UpdatedAt); SELECT SCOPE_IDENTITY();";
                        using (SqlCommand cmdInsert = new SqlCommand(insertDocQuery, con))
                        {
                            cmdInsert.Parameters.AddWithValue("@OwnerID", ownerId);
                            cmdInsert.Parameters.AddWithValue("@Title", newTitle);
                            cmdInsert.Parameters.AddWithValue("@Content", docEditor.Value);
                            cmdInsert.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                            cmdInsert.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                            currentDocumentId = Convert.ToInt32(cmdInsert.ExecuteScalar());
                        }
                    }

                    int nextVersion = 1;
                    string getMaxVersionQuery = "SELECT ISNULL(MAX(VersionNumber), 0) FROM DocumentVersions WHERE DocumentID = @DocumentID";
                    using (SqlCommand cmdGetVersion = new SqlCommand(getMaxVersionQuery, con))
                    {
                        cmdGetVersion.Parameters.AddWithValue("@DocumentID", currentDocumentId);
                        object result = cmdGetVersion.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            nextVersion = Convert.ToInt32(result) + 1;
                        }
                    }

                    string insertVersionQuery = "INSERT INTO DocumentVersions (DocumentID, VersionNumber, Content, Comment, EditorID, CreatedAt) VALUES (@DocumentID, @VersionNumber, @Content, @Comment, @EditorID, @CreatedAt)";
using (SqlCommand cmdInsertVersion = new SqlCommand(insertVersionQuery, con))
{
    cmdInsertVersion.Parameters.AddWithValue("@DocumentID", currentDocumentId);
    cmdInsertVersion.Parameters.AddWithValue("@VersionNumber", nextVersion);
    cmdInsertVersion.Parameters.AddWithValue("@Content", docEditor.Value);
    object comment = string.IsNullOrWhiteSpace(txtSaveComment.Text) ? (object)DBNull.Value : txtSaveComment.Text;
    cmdInsertVersion.Parameters.AddWithValue("@Comment", comment);
    int editorId = Session["UserID"] != null ? Convert.ToInt32(Session["UserID"]) : 0;
    cmdInsertVersion.Parameters.AddWithValue("@EditorID", editorId);
    cmdInsertVersion.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
    cmdInsertVersion.ExecuteNonQuery();
}

                    docTitle.InnerText = newTitle;
                    lblStatus.Text = "Save successful";
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                    saveFilesModal.Style["display"] = "none";
                    
                    if (Request.QueryString["docId"] == null)
                    {
                        Response.Redirect("~/Default.aspx?docId=" + currentDocumentId);
                    }
                }
            }
            catch (Exception ex)
            {
                lblSaveModalStatus.Text = "Error saving: " + ex.Message;
                saveFilesModal.Style["display"] = "flex";
            }
        }

        protected void btnCancelSave_Click(object sender, EventArgs e)
        {
            saveFilesModal.Style["display"] = "none";
        }

        protected void btnOpen_Click(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }
            Response.Redirect("~/Default.aspx?open=1");
        }

        private void ShowDocumentList()
        {
            int userId = Convert.ToInt32(Session["UserID"]);
            string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CDE;Integrated Security=True;";

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = "SELECT DISTINCT d.DocumentID, d.Title, d.UpdatedAt " +
                                   "FROM Documents d " +
                                   "LEFT JOIN Collaborators c ON d.DocumentID = c.DocumentID " +
                                   "WHERE d.OwnerID = @UserID OR c.UserID = @UserID " +
                                   "ORDER BY d.UpdatedAt DESC";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gvDocuments.DataSource = dt;
                            gvDocuments.DataBind();
                            lblStatus.Text = string.Empty;
                            openFilesModal.Style["display"] = "flex";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error fetching documents: " + ex.Message;
                lblStatus.ForeColor = System.Drawing.Color.Red;
                openFilesModal.Style["display"] = "flex";
            }
        }

        protected void btnCloseModal_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Default.aspx");
        }

        
        protected void btnVersionHistory_Click(object sender, EventArgs e)
        {
            int currentDocumentId = GetCurrentDocumentId();
            versionComparePanel.Visible = false;
            lblVersionStatus.Text = string.Empty;

            if (currentDocumentId <= 0)
            {
                gvVersions.DataSource = null;
                gvVersions.DataBind();
                lblVersionStatus.Text = "Save the document to view version history.";
                lblVersionStatus.ForeColor = System.Drawing.Color.Red;
                versionHistoryModal.Style["display"] = "flex";
                return;
            }

            BindVersionHistory(currentDocumentId);
            versionHistoryModal.Style["display"] = "flex";
        }

        protected void gvVersions_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "CompareVersion")
            {
                return;
            }

            int versionNumber;
            if (!int.TryParse(e.CommandArgument.ToString(), out versionNumber))
            {
                return;
            }

            int documentId = GetCurrentDocumentId();
            if (documentId <= 0)
            {
                lblVersionStatus.Text = "No document selected.";
                lblVersionStatus.ForeColor = System.Drawing.Color.Red;
                versionHistoryModal.Style["display"] = "flex";
                return;
            }

            LoadVersionCompare(documentId, versionNumber);
            BindVersionHistory(documentId);
            versionHistoryModal.Style["display"] = "flex";
        }

        private void BindVersionHistory(int documentId)
        {
            string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CDE;Integrated Security=True;";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT dv.VersionNumber, dv.CreatedAt, dv.Comment, u.DisplayName AS EditorName " +
                               "FROM DocumentVersions dv " +
                               "INNER JOIN Users u ON dv.EditorID = u.UserID " +
                               "WHERE dv.DocumentID = @DocumentID ORDER BY dv.VersionNumber DESC";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DocumentID", documentId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        gvVersions.DataSource = dt;
                        gvVersions.DataBind();
                    }
                }
            }
        }

        private void LoadVersionCompare(int documentId, int versionNumber)
        {
            string afterContent = GetVersionContent(documentId, versionNumber);
            string beforeContent = string.Empty;

            if (versionNumber > 1)
            {
                beforeContent = GetVersionContent(documentId, versionNumber - 1);
            }
            else
            {
                beforeContent = "(No previous version — this is the first save.)";
            }

            txtBeforeContent.Text = beforeContent;
            txtAfterContent.Text = afterContent ?? string.Empty;
            lblCompareTitle.Text = "Version " + versionNumber + " — before and after";
            versionComparePanel.Visible = true;
            lblVersionStatus.Text = string.Empty;
        }

        private string GetVersionContent(int documentId, int versionNumber)
        {
            string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CDE;Integrated Security=True;";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT Content FROM DocumentVersions WHERE DocumentID = @DocumentID AND VersionNumber = @VersionNumber";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DocumentID", documentId);
                    cmd.Parameters.AddWithValue("@VersionNumber", versionNumber);
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    return result != null && result != DBNull.Value ? result.ToString() : string.Empty;
                }
            }
        }

        protected void gvDocuments_RowCommand(object sender, GridViewCommandEventArgs e)
            {
            if (e.CommandName == "OpenDoc")
            {
                string docId = e.CommandArgument.ToString();
                Response.Redirect("~/Default.aspx?docId=" + docId);
            }
            else if (e.CommandName == "DeleteDoc")
            {
                if (Session["UserID"] == null)
                {
                    Response.Redirect("~/Login.aspx");
                    return;
                }
                int userId = Convert.ToInt32(Session["UserID"]);

                int docId;
                if (int.TryParse(e.CommandArgument.ToString(), out docId))
                {
                    string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CDE;Integrated Security=True;";
                    try
                    {
                        using (SqlConnection con = new SqlConnection(connectionString))
                        {
                            con.Open();

                            // Verify if current user is the owner of the document
                            string checkOwnerQuery = "SELECT COUNT(1) FROM Documents WHERE DocumentID = @DocumentID AND OwnerID = @OwnerID";
                            using (SqlCommand cmdCheck = new SqlCommand(checkOwnerQuery, con))
                            {
                                cmdCheck.Parameters.AddWithValue("@DocumentID", docId);
                                cmdCheck.Parameters.AddWithValue("@OwnerID", userId);
                                int count = Convert.ToInt32(cmdCheck.ExecuteScalar());
                                if (count == 0)
                                {
                                    lblStatus.Text = "Only the owner can delete this document.";
                                    lblStatus.ForeColor = System.Drawing.Color.Red;
                                    openFilesModal.Style["display"] = "flex";
                                    return;
                                }
                            }
                            
                            // Delete dependent records first to avoid foreign key constraints.
                            // Some databases may not have all optional tables (for example Comments),
                            // so only delete from tables that actually exist.
                            DeleteByDocumentIdIfTableExists(con, "dbo.DocumentVersions", docId);
                            DeleteByDocumentIdIfTableExists(con, "dbo.Collaborators", docId);
                            DeleteByDocumentIdIfTableExists(con, "dbo.Files", docId);
                            DeleteByDocumentIdIfTableExists(con, "dbo.Comments", docId);

                            // Finally, delete the document itself
                            using (SqlCommand cmd = new SqlCommand("DELETE FROM Documents WHERE DocumentID = @DocumentID", con))
                            {
                                cmd.Parameters.AddWithValue("@DocumentID", docId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        
                        // Refresh the document list grid view
                        btnOpen_Click(sender, e);
                        
                        // Show a success notification
                        docTitle.InnerText = "";
                        docEditor.Value = "";
                        lblStatus.Text = "Document deleted successfully.";
                        lblStatus.ForeColor = System.Drawing.Color.Green;
                    }
                    catch (Exception ex)
                    {
                        lblStatus.Text = "Error deleting document: " + ex.Message;
                        lblStatus.ForeColor = System.Drawing.Color.Red;
                        openFilesModal.Style["display"] = "flex";
                    }
                }
            }
        }

        // Load comments for the current document into gvComments
        private void LoadComments(int docId)
        {
            string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CDE;Integrated Security=True;";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT CommentID, UserName, CommentText, CreatedAt FROM Comments WHERE DocumentID = @DocumentID ORDER BY CreatedAt DESC";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DocumentID", docId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        System.Data.DataTable dt = new System.Data.DataTable();
                        da.Fill(dt);
                        gvComments.DataSource = dt;
                        gvComments.DataBind();
                    }
                }
            }
        }

        protected void btnAddComment_Click(object sender, EventArgs e)
        {
            if (hfCurrentDocId == null || string.IsNullOrEmpty(hfCurrentDocId.Value) || hfCurrentDocId.Value == "0")
            {
                lblCommentStatus.Text = "Open a document before adding a comment.";
                lblCommentStatus.ForeColor = System.Drawing.Color.Red;
                return;
            }

            string commentText = txtComment.Text.Trim();
            if (string.IsNullOrEmpty(commentText))
            {
                lblCommentStatus.Text = "Comment cannot be empty.";
                lblCommentStatus.ForeColor = System.Drawing.Color.Red;
                return;
            }

            int docId = int.Parse(hfCurrentDocId.Value);
            int userId = Session["UserID"] != null ? Convert.ToInt32(Session["UserID"]) : 0;
            string userName = Session["DisplayName"] != null ? Session["DisplayName"].ToString() : "Anonymous";

            string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CDE;Integrated Security=True;";
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string insert = "INSERT INTO Comments (DocumentID, UserID, UserName, CommentText, CreatedAt) VALUES (@DocumentID, @UserID, @UserName, @CommentText, @CreatedAt)";
                    using (SqlCommand cmd = new SqlCommand(insert, con))
                    {
                        cmd.Parameters.AddWithValue("@DocumentID", docId);
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        cmd.Parameters.AddWithValue("@UserName", userName);
                        cmd.Parameters.AddWithValue("@CommentText", commentText);
                        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                lblCommentStatus.Text = "Comment added.";
                lblCommentStatus.ForeColor = System.Drawing.Color.Green;
                txtComment.Text = string.Empty;
                LoadComments(docId);
            }
            catch (Exception ex)
            {
                lblCommentStatus.Text = "Error adding comment: " + ex.Message;
                lblCommentStatus.ForeColor = System.Drawing.Color.Red;
            }
        }

        protected void RefreshComments_Click(object sender, EventArgs e)
        {
            if (hfCurrentDocId != null && !string.IsNullOrEmpty(hfCurrentDocId.Value) && hfCurrentDocId.Value != "0")
            {
                LoadComments(int.Parse(hfCurrentDocId.Value));
            }
        }

        protected void btnSignOut_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Response.Redirect("Default.aspx");
        }

        protected void btnShare_Click(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            int documentId = GetCurrentDocumentId();
            if (documentId <= 0)
            {
                lblShareStatus.Text = "Save the document before inviting others.";
                lblShareStatus.ForeColor = System.Drawing.Color.Red;
                txtInviteEmail.Text = string.Empty;
                gvCollaborators.DataSource = null;
                gvCollaborators.DataBind();
                shareModal.Style["display"] = "flex";
                return;
            }

            int userId = Convert.ToInt32(Session["UserID"]);
            if (!IsDocumentOwner(documentId, userId))
            {
                lblShareStatus.Text = "Only the document owner can invite others.";
                lblShareStatus.ForeColor = System.Drawing.Color.Red;
                shareModal.Style["display"] = "flex";
                return;
            }

            lblShareStatus.Text = string.Empty;
            txtInviteEmail.Text = string.Empty;
            BindCollaborators(documentId);
            shareModal.Style["display"] = "flex";
        }

        protected void btnInvite_Click(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            int documentId = GetCurrentDocumentId();
            if (documentId <= 0)
            {
                lblShareStatus.Text = "Save the document before inviting others.";
                lblShareStatus.ForeColor = System.Drawing.Color.Red;
                shareModal.Style["display"] = "flex";
                return;
            }

            int ownerId = Convert.ToInt32(Session["UserID"]);
            if (!IsDocumentOwner(documentId, ownerId))
            {
                lblShareStatus.Text = "Only the document owner can invite others.";
                lblShareStatus.ForeColor = System.Drawing.Color.Red;
                shareModal.Style["display"] = "flex";
                return;
            }

            string email = txtInviteEmail.Text.Trim();
            if (string.IsNullOrEmpty(email))
            {
                lblShareStatus.Text = "Please enter an email address.";
                lblShareStatus.ForeColor = System.Drawing.Color.Red;
                shareModal.Style["display"] = "flex";
                return;
            }

            string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CDE;Integrated Security=True;";
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    string ownerEmailQuery = "SELECT Email FROM Users WHERE UserID = @UserID";
                    using (SqlCommand cmdOwnerEmail = new SqlCommand(ownerEmailQuery, con))
                    {
                        cmdOwnerEmail.Parameters.AddWithValue("@UserID", ownerId);
                        object ownerEmailResult = cmdOwnerEmail.ExecuteScalar();
                        string ownerEmail = ownerEmailResult != null ? ownerEmailResult.ToString() : string.Empty;
                        if (string.Equals(email, ownerEmail, StringComparison.OrdinalIgnoreCase))
                        {
                            lblShareStatus.Text = "You cannot invite yourself.";
                            lblShareStatus.ForeColor = System.Drawing.Color.Red;
                            shareModal.Style["display"] = "flex";
                            return;
                        }
                    }

                    int inviteeId = 0;
                    string userQuery = "SELECT UserID FROM Users WHERE Email = @Email";
                    using (SqlCommand cmdUser = new SqlCommand(userQuery, con))
                    {
                        cmdUser.Parameters.AddWithValue("@Email", email);
                        object result = cmdUser.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            lblShareStatus.Text = "No account found with that email. They need to sign up first.";
                            lblShareStatus.ForeColor = System.Drawing.Color.Red;
                            shareModal.Style["display"] = "flex";
                            BindCollaborators(documentId);
                            return;
                        }
                        inviteeId = Convert.ToInt32(result);
                    }

                    string existsQuery = "SELECT COUNT(1) FROM Collaborators WHERE DocumentID = @DocumentID AND UserID = @UserID";
                    using (SqlCommand cmdExists = new SqlCommand(existsQuery, con))
                    {
                        cmdExists.Parameters.AddWithValue("@DocumentID", documentId);
                        cmdExists.Parameters.AddWithValue("@UserID", inviteeId);
                        int count = Convert.ToInt32(cmdExists.ExecuteScalar());
                        if (count > 0)
                        {
                            lblShareStatus.Text = "That person already has access to this document.";
                            lblShareStatus.ForeColor = System.Drawing.Color.Orange;
                            shareModal.Style["display"] = "flex";
                            BindCollaborators(documentId);
                            return;
                        }
                    }

                    string insertQuery = "INSERT INTO Collaborators (DocumentID, UserID) VALUES (@DocumentID, @UserID)";
                    using (SqlCommand cmdInsert = new SqlCommand(insertQuery, con))
                    {
                        cmdInsert.Parameters.AddWithValue("@DocumentID", documentId);
                        cmdInsert.Parameters.AddWithValue("@UserID", inviteeId);
                        cmdInsert.ExecuteNonQuery();
                    }
                }

                lblShareStatus.Text = "Invitation sent successfully.";
                lblShareStatus.ForeColor = System.Drawing.Color.Green;
                txtInviteEmail.Text = string.Empty;
                BindCollaborators(documentId);
                shareModal.Style["display"] = "flex";
            }
            catch (Exception ex)
            {
                lblShareStatus.Text = "Error inviting user: " + ex.Message;
                lblShareStatus.ForeColor = System.Drawing.Color.Red;
                shareModal.Style["display"] = "flex";
            }
        }

        private int GetCurrentDocumentId()
        {
            int documentId = 0;
            int qsId;
            int hiddenId;
            if (Request.QueryString["docId"] != null && int.TryParse(Request.QueryString["docId"], out qsId))
            {
                documentId = qsId;
            }
            else if (hfCurrentDocId != null && !string.IsNullOrEmpty(hfCurrentDocId.Value) && int.TryParse(hfCurrentDocId.Value, out hiddenId))
            {
                documentId = hiddenId;
            }
            return documentId;
        }

        private bool IsDocumentOwner(int documentId, int userId)
        {
            string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CDE;Integrated Security=True;";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT COUNT(1) FROM Documents WHERE DocumentID = @DocumentID AND OwnerID = @OwnerID";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DocumentID", documentId);
                    cmd.Parameters.AddWithValue("@OwnerID", userId);
                    con.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        private void DeleteByDocumentIdIfTableExists(SqlConnection con, string qualifiedTableName, int documentId)
        {
            using (SqlCommand cmdExists = new SqlCommand("SELECT OBJECT_ID(@TableName, 'U')", con))
            {
                cmdExists.Parameters.AddWithValue("@TableName", qualifiedTableName);
                object tableId = cmdExists.ExecuteScalar();
                if (tableId == null || tableId == DBNull.Value)
                {
                    return;
                }
            }

            using (SqlCommand cmdDelete = new SqlCommand("DELETE FROM " + qualifiedTableName + " WHERE DocumentID = @DocumentID", con))
            {
                cmdDelete.Parameters.AddWithValue("@DocumentID", documentId);
                cmdDelete.ExecuteNonQuery();
            }
        }

        private void BindCollaborators(int documentId)
        {
            string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CDE;Integrated Security=True;";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "SELECT u.DisplayName, u.Email " +
                               "FROM Collaborators c " +
                               "INNER JOIN Users u ON c.UserID = u.UserID " +
                               "WHERE c.DocumentID = @DocumentID " +
                               "ORDER BY u.DisplayName";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DocumentID", documentId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        gvCollaborators.DataSource = dt;
                        gvCollaborators.DataBind();
                    }
                }
            }
        }

    }
}
