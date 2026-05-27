<%@ Page Title="Collaborative Editor" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="CDE._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <input type="file" id="fileInput" style="display:none;" onchange="handleFileSelect(event)" />

    <div class="editor-page">
    <!-- Document toolbar: File | Title | Version History | Login -->
    <div class="doc-bar">
        <div class="doc-bar-left">
            <div class="dropdown-container">
                <button type="button" class="doc-btn" id="fileBtn" onclick="toggleFileDropdown()">File</button>
                <div class="dropdown-menu dropdown-menu-left" id="fileDropdown">
                    <button type="button" class="dropdown-item" onclick="newDocument()">New</button>
                    <asp:Button ID="btnOpen" runat="server" CssClass="dropdown-item" Text="Open" OnClick="btnOpen_Click" />
                    <button type="button" class="dropdown-item" onclick="openDocumentClick()">Open from Local</button>
                    <asp:Button ID="btnSave" runat="server" CssClass="dropdown-item" Text="Save" OnClick="btnSave_Click" />
                    <button type="button" class="dropdown-item" onclick="downloadDocument()">Download</button>
                    <asp:Button ID="btnShare" runat="server" CssClass="dropdown-item" Text="Share" OnClick="btnShare_Click" />
                    <button type="button" class="dropdown-item" onclick="closeDocument()">Close</button>
                </div>
            </div>
        </div>

        <div class="doc-bar-center">
            <h1 class="doc-title" id="docTitle" runat="server" ClientIDMode="Static">Untitled Document</h1>
            <asp:HiddenField ID="hfCurrentDocId" runat="server" Value="0" ClientIDMode="Static" />
            <asp:HiddenField ID="hfCollabUserId" runat="server" Value="" ClientIDMode="Static" />
            <asp:HiddenField ID="hfCollabUserName" runat="server" Value="" ClientIDMode="Static" />
            <asp:HiddenField ID="hfCollabEnabled" runat="server" Value="false" ClientIDMode="Static" />
        </div>

        <div class="doc-bar-right">
            <asp:Button ID="btnVersionHistory" runat="server" CssClass="doc-btn" Text="Version History" OnClick="btnVersionHistory_Click" />
            <asp:PlaceHolder ID="phAnonymous" runat="server">
                <a href="Login.aspx" class="doc-btn doc-btn-login">Login</a>
            </asp:PlaceHolder>
            <asp:PlaceHolder ID="phLoggedIn" runat="server" Visible="false">
                <div class="dropdown-container">
                    <button type="button" class="profile-avatar" id="profileBtn" onclick="toggleProfileDropdown()">
                        <asp:Literal ID="litAvatarLetter" runat="server"></asp:Literal>
                    </button>
                    <div class="dropdown-menu dropdown-menu-right" id="profileDropdown">
                        <div class="dropdown-user">
                            <asp:Literal ID="litDisplayName" runat="server"></asp:Literal>
                        </div>
                        <asp:LinkButton ID="btnSignOut" runat="server" CssClass="dropdown-item signout" OnClick="btnSignOut_Click">Sign Out</asp:LinkButton>
                    </div>
                </div>
            </asp:PlaceHolder>
        </div>
    </div>

    <!-- Full-width editor area with real-time collaborator cursors -->
    <div class="editor-area">
        <div class="editor-collab-wrap" id="editorCollabWrap">
            <div class="editor-mirror" id="editorMirror" aria-hidden="true"></div>
            <div class="remote-cursors" id="remoteCursors"></div>
            <textarea class="editor-textarea" id="docEditor" runat="server" ClientIDMode="Static"
                placeholder="Start typing here..." spellcheck="false"></textarea>
        </div>
    </div>
    </div>

    <!-- Comments Modal -->
    <asp:Panel ID="commentsModal" runat="server" CssClass="modal-overlay" Style="display:none;">
        <div class="modal-content">
            <h2>Comments</h2>
            <asp:GridView ID="gvComments" runat="server" AutoGenerateColumns="true" CssClass="comments-grid" Font-Size="12px"></asp:GridView>
            <asp:TextBox ID="txtComment" runat="server" TextMode="MultiLine" Rows="4" CssClass="modal-textarea" placeholder="Write a comment..."></asp:TextBox>
            <div class="modal-actions">
                <asp:Button ID="btnAddComment" runat="server" Text="Add Comment" OnClick="btnAddComment_Click" CssClass="doc-btn" />
                <asp:Button ID="btnCloseComments" runat="server" Text="Close" OnClientClick="closeComments(); return false;" CssClass="doc-btn" />
            </div>
            <asp:Label ID="lblCommentStatus" runat="server" CssClass="status-msg" />
            <asp:Button ID="btnRefreshComments" runat="server" Text="Refresh" OnClick="RefreshComments_Click" Style="display:none;" />
        </div>
    </asp:Panel>

    <!-- Open Files Modal -->
    <asp:Panel ID="openFilesModal" runat="server" CssClass="modal-overlay" Style="display:none;">
        <div class="modal-content">
            <div class="modal-header">Open Document</div>
            <div class="modal-body">
                <asp:GridView ID="gvDocuments" runat="server" AutoGenerateColumns="False" CssClass="table"
                    OnRowCommand="gvDocuments_RowCommand" GridLines="None" Width="100%" DataKeyNames="DocumentID">
                    <Columns>
                        <asp:BoundField DataField="Title" HeaderText="Title" />
                        <asp:BoundField DataField="UpdatedAt" HeaderText="Last Updated" DataFormatString="{0:g}" />
                        <asp:TemplateField>
                            <ItemTemplate>
                                <div style="display:flex;gap:8px;">
                                    <asp:Button ID="btnOpenDoc" runat="server" CommandName="OpenDoc" CommandArgument='<%# Eval("DocumentID") %>' Text="Open" CssClass="doc-btn" />
                                    <asp:Button ID="btnDeleteDoc" runat="server" CommandName="DeleteDoc" CommandArgument='<%# Eval("DocumentID") %>' Text="Delete" CssClass="doc-btn danger" OnClientClick="return confirm('Delete this document?');" />
                                </div>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
                <asp:Label ID="lblStatus" runat="server" ForeColor="Red" />
            </div>
            <div class="modal-footer">
                <asp:Button ID="btnCloseModal" runat="server" Text="Cancel" CssClass="doc-btn" OnClick="btnCloseModal_Click" />
            </div>
        </div>
    </asp:Panel>

    <!-- Version History Modal -->
    <asp:Panel ID="versionHistoryModal" runat="server" CssClass="modal-overlay" Style="display:none;">
        <div class="modal-content modal-xwide">
            <div class="modal-header">
                Version History
                <asp:Button ID="btnCloseVersionHistory" runat="server" Text="&times;" CssClass="modal-close" OnClientClick="closeVersionHistoryModal(); return false;" />
            </div>
            <div class="modal-body">
                <asp:GridView ID="gvVersions" runat="server" AutoGenerateColumns="False" CssClass="table" Font-Size="12px"
                    OnRowCommand="gvVersions_RowCommand" DataKeyNames="VersionNumber">
                    <Columns>
                        <asp:BoundField DataField="VersionNumber" HeaderText="Version" />
                        <asp:BoundField DataField="CreatedAt" HeaderText="Date" DataFormatString="{0:g}" />
                        <asp:BoundField DataField="Comment" HeaderText="Comment" />
                        <asp:BoundField DataField="EditorName" HeaderText="Saved By" />
                        <asp:TemplateField HeaderText="Compare">
                            <ItemTemplate>
                                <asp:Button ID="btnCompareVersion" runat="server" Text="View changes" CssClass="doc-btn"
                                    CommandName="CompareVersion" CommandArgument='<%# Eval("VersionNumber") %>' />
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
                <asp:Label ID="lblVersionStatus" runat="server" Font-Size="12px" />

                <asp:Panel ID="versionComparePanel" runat="server" Visible="false" CssClass="version-compare-panel">
                    <asp:Label ID="lblCompareTitle" runat="server" CssClass="version-compare-title" />
                    <div class="version-compare-grid">
                        <div class="version-compare-col">
                            <span class="version-compare-label">Before change</span>
                            <asp:TextBox ID="txtBeforeContent" runat="server" TextMode="MultiLine" ReadOnly="true"
                                CssClass="version-compare-text" Rows="12" />
                        </div>
                        <div class="version-compare-col">
                            <span class="version-compare-label">After change</span>
                            <asp:TextBox ID="txtAfterContent" runat="server" TextMode="MultiLine" ReadOnly="true"
                                CssClass="version-compare-text" Rows="12" />
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
    </asp:Panel>

    <!-- Share / Invite Modal -->
    <asp:Panel ID="shareModal" runat="server" CssClass="modal-overlay" Style="display:none;">
        <div class="modal-content modal-small">
            <div class="modal-header">
                Invite others
                <asp:Button ID="btnCloseShare" runat="server" Text="&times;" CssClass="modal-close" OnClientClick="closeShareModal(); return false;" />
            </div>
            <div class="modal-body">
                <asp:Label ID="lblSharePrompt" runat="server" Text="Enter the email of someone to invite:" style="display:block;margin-bottom:8px;font-weight:500;"></asp:Label>
                <asp:TextBox ID="txtInviteEmail" runat="server" TextMode="Email" placeholder="name@example.com" style="width:100%;padding:8px;border:1px solid #ccc;border-radius:4px;box-sizing:border-box;"></asp:TextBox>
                <asp:Label ID="lblShareStatus" runat="server" style="display:block;margin-top:8px;font-size:13px;"></asp:Label>
                <asp:GridView ID="gvCollaborators" runat="server" AutoGenerateColumns="False" CssClass="table" GridLines="None" Width="100%"
                    style="margin-top:16px;" EmptyDataText="No collaborators yet.">
                    <Columns>
                        <asp:BoundField DataField="DisplayName" HeaderText="Name" />
                        <asp:BoundField DataField="Email" HeaderText="Email" />
                    </Columns>
                </asp:GridView>
            </div>
            <div class="modal-footer">
                <asp:Button ID="btnCancelShare" runat="server" Text="Cancel" CssClass="doc-btn" OnClientClick="closeShareModal(); return false;" />
                <asp:Button ID="btnInvite" runat="server" Text="Invite" CssClass="doc-btn primary" OnClick="btnInvite_Click" />
            </div>
        </div>
    </asp:Panel>

    <!-- Save Modal -->
    <asp:Panel ID="saveFilesModal" runat="server" CssClass="modal-overlay" Style="display:none;">
        <div class="modal-content modal-small">
            <div class="modal-header">Save Document</div>
            <div class="modal-body">
                <asp:Label ID="lblSavePrompt" runat="server" Text="Document Name:" style="display:block;margin-bottom:8px;font-weight:500;"></asp:Label>
                <asp:TextBox ID="txtSaveTitle" runat="server" style="width:100%;padding:8px;border:1px solid #ccc;border-radius:4px;box-sizing:border-box;"></asp:TextBox>
                <asp:TextBox ID="txtSaveComment" runat="server" style="width:100%;padding:8px;border:1px solid #ccc;border-radius:4px;margin-top:8px;box-sizing:border-box;" placeholder="Add a comment (optional)" />
                <asp:Label ID="lblSaveModalStatus" runat="server" ForeColor="Red" style="display:block;margin-top:8px;font-size:13px;"></asp:Label>
            </div>
            <div class="modal-footer">
                <asp:Button ID="btnCancelSave" runat="server" Text="Cancel" CssClass="doc-btn" OnClick="btnCancelSave_Click" />
                <asp:Button ID="btnConfirmSave" runat="server" Text="Save" CssClass="doc-btn primary" OnClick="btnConfirmSave_Click" />
            </div>
        </div>
    </asp:Panel>

    <script src="https://cdnjs.cloudflare.com/ajax/libs/mammoth/1.6.0/mammoth.browser.min.js"></script>
    <script src="<%= ResolveUrl("~/Scripts/jquery.signalR-2.4.3.min.js") %>"></script>
    <script src="<%= ResolveUrl("~/signalr/hubs") %>"></script>
    <script src="<%= ResolveUrl("~/Scripts/collaborative-cursors.js") %>"></script>
    <script>
        /* Close all dropdowns on outside click */
        document.addEventListener('click', function (e) {
            if (!e.target.closest('.dropdown-container')) {
                document.querySelectorAll('.dropdown-menu').forEach(function (m) { m.classList.remove('active'); });
            }
        });

        function toggleFileDropdown() {
            var m = document.getElementById('fileDropdown');
            m.classList.toggle('active');
        }

        function toggleProfileDropdown() {
            var m = document.getElementById('profileDropdown');
            if (m) m.classList.toggle('active');
        }

        function closeShareModal() {
            var modal = document.getElementById('<%= shareModal.ClientID %>');
            if (modal) modal.style.display = 'none';
        }

        function closeVersionHistoryModal() {
            var modal = document.getElementById('<%= versionHistoryModal.ClientID %>');
            if (modal) modal.style.display = 'none';
        }

        function newDocument() {
            window.location.href = 'Default.aspx';
        }

        function openDocumentClick() {
            document.getElementById('fileInput').click();
            document.getElementById('fileDropdown').classList.remove('active');
        }

        function handleFileSelect(event) {
            var file = event.target.files[0];
            if (!file) return;
            if (file.name.endsWith('.docx')) {
                var reader = new FileReader();
                reader.onload = function (e) {
                    mammoth.extractRawText({ arrayBuffer: e.target.result })
                        .then(function (result) {
                            document.getElementById('docEditor').value = result.value;
                            document.getElementById('docTitle').textContent = file.name;
                            showNotification('Opened ' + file.name);
                        }).catch(function () { showNotification('Error reading document'); });
                };
                reader.readAsArrayBuffer(file);
            } else {
                var reader = new FileReader();
                reader.onload = function (e) {
                    document.getElementById('docEditor').value = e.target.result;
                    document.getElementById('docTitle').textContent = file.name;
                    showNotification('Opened ' + file.name);
                };
                reader.readAsText(file);
            }
            event.target.value = '';
        }

        function downloadDocument() {
            var editor = document.getElementById('docEditor');
            var title = document.getElementById('docTitle');
            if (!editor || !editor.value.trim()) { showNotification('No document to download'); return; }
            var blob = new Blob([editor.value], { type: 'text/plain;charset=utf-8' });
            var url = URL.createObjectURL(blob);
            var a = document.createElement('a');
            a.href = url;
            a.download = (title.innerText.trim() || 'Untitled') + '.txt';
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
            showNotification('Downloaded successfully');
            document.getElementById('fileDropdown').classList.remove('active');
        }

        function closeDocument() {
            document.getElementById('docEditor').value = '';
            document.getElementById('docTitle').textContent = 'Untitled Document';
            showNotification('Document closed');
            document.getElementById('fileDropdown').classList.remove('active');
        }

        function showNotification(message) {
            var n = document.createElement('div');
            n.className = 'notification';
            n.textContent = message;
            document.body.appendChild(n);
            setTimeout(function () {
                n.style.opacity = '0';
                setTimeout(function () { n.remove(); }, 300);
            }, 2500);
        }

        /* Auto-save indicator */
        var autoSaveTimeout;
        var editor = document.getElementById('docEditor');
        if (editor) {
            editor.addEventListener('input', function () {
                clearTimeout(autoSaveTimeout);
                autoSaveTimeout = setTimeout(function () { showNotification('Auto-saved'); }, 2000);
            });
        }
    </script>

</asp:Content>