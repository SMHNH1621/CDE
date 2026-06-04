<%@ Page Title="Signup" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Signup.aspx.cs" Inherits="CDE.Signup" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

<style>
    * {
        margin: 0;
        padding: 0;
        box-sizing: border-box;
    }

    html, body {
        height: 100%;
        width: 100%;
        background: #ffffff;
    }

    body {
        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
        color: #333;
    }

    .signup-container {
        display: flex;
        align-items: center;
        justify-content: center;
        min-height: 100vh;
        background: #ffffff;
    }

    .signup-form {
        width: 100%;
        max-width: 400px;
        padding: 40px;
        background: #ffffff;
        border: 1px solid #e0e0e0;
        border-radius: 4px;
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .signup-title {
        font-size: 24px;
        font-weight: 500;
        text-align: center;
        margin-bottom: 32px;
        color: #000;
    }

    .form-group {
        margin-bottom: 20px;
    }

    .form-label {
        display: block;
        margin-bottom: 8px;
        font-size: 14px;
        font-weight: 500;
        color: #000;
    }

    .form-input {
        width: 100%;
        padding: 10px 12px;
        border: 1px solid #d0d0d0;
        border-radius: 4px;
        font-size: 14px;
        background: #ffffff;
        color: #333;
        transition: border-color 0.2s;
    }

    .form-input:focus {
        outline: none;
        border-color: #000;
        box-shadow: 0 0 0 2px rgba(0, 0, 0, 0.05);
    }

    .signup-button {
        width: 100%;
        padding: 10px 16px;
        margin-top: 24px;
        background: #ffffff;
        border: 1px solid #000;
        color: #000;
        border-radius: 4px;
        cursor: pointer;
        font-size: 14px;
        font-weight: 500;
        transition: all 0.2s;
    }

    .signup-button:hover {
        background: #f5f5f5;
    }

    .signup-button:active {
        background: #efefef;
    }

    .login-link {
        text-align: center;
        margin-top: 16px;
        font-size: 13px;
    }

    .login-link a {
        color: #000;
        text-decoration: none;
        border-bottom: 1px solid #000;
    }

    .login-link a:hover {
        text-decoration: underline;
    }

    .status-message {
        padding: 12px;
        border-radius: 4px;
        margin-bottom: 20px;
        font-size: 13px;
        display: none;
    }

    .status-message.success {
        display: block;
        background: #f0f9ff;
        border: 1px solid #90CAF9;
        color: #1565C0;
    }

    .status-message.error {
        display: block;
        background: #fff5f5;
        border: 1px solid #e0e0e0;
        color: #d32f2f;
    }

    .required {
        color: #d32f2f;
    }
</style>

<div class="signup-container">
    <div class="signup-form">
        <h1 class="signup-title">Signup</h1>

        <div class="status-message" id="statusMessage" runat="server"></div>

        <div class="form-group">
            <label class="form-label" for="nameInput">Name <span class="required">*</span></label>
            <asp:TextBox ID="nameInput" runat="server" CssClass="form-input" placeholder="Enter your full name"></asp:TextBox>
        </div>

        <div class="form-group">
            <label class="form-label" for="emailInput">Email <span class="required">*</span></label>
            <asp:TextBox ID="emailInput" runat="server" CssClass="form-input" TextMode="Email" placeholder="Enter your email"></asp:TextBox>
        </div>

        <div class="form-group">
            <label class="form-label" for="passwordInput">Password <span class="required">*</span></label>
            <asp:TextBox ID="passwordInput" runat="server" CssClass="form-input" TextMode="Password" placeholder="Enter your password"></asp:TextBox>
        </div>

        <asp:Button ID="signupButton" runat="server" Text="Signup" CssClass="signup-button" OnClick="SignupButton_Click" />

        <div class="login-link">
            Already have an account? <a href="Login.aspx">Login</a>
        </div>
    </div>
</div>

</asp:Content>
