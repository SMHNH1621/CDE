<%@ Page Title="About" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="About.aspx.cs" Inherits="CDE.About" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main class="about-page" aria-labelledby="title">
        <h1 id="title">About Collaborative Document Editor</h1>

        <section class="about-section">
            <h2>System Description</h2>
            <p>
                Collaborative Document Editor (CDE) is a web-based document editing system built with ASP.NET.
                It allows users to create, save, and manage documents online, collaborate with others in real time,
                and track changes over time.
            </p>
            <ul>
                <li>Create and edit documents in a clean, full-page editor</li>
                <li>Save documents to a database and open them from a document list</li>
                <li>Share documents with other users by email invitation</li>
                <li>Edit the same document simultaneously with live cursor tracking and text sync</li>
                <li>View version history and compare before-and-after changes for each save</li>
                <li>Sign up, log in, and manage your profile securely</li>
            </ul>
        </section>

        <section class="about-section">
            <h2>Development Team</h2>
            <p>This system was developed by:</p>
            <ul class="team-list">
                <li>Nolawit Solomon</li>
                <li>Natnael Gezahegn</li>
                <li>Geda Fuad</li>
                <li>Abdurahman</li>
            </ul>
        </section>
    </main>
</asp:Content>
