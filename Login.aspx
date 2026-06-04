<%@ Page Title="Login" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="CDE.Login" %>

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

    .login-container {
        display: flex;
        align-items: center;
        justify-content: center;
        min-height: 100vh;
        background: #ffffff;
    }

    .login-form {
        width: 100%;
        max-width: 400px;
        padding: 40px;
        background: #ffffff;
        border: 1px solid #e0e0e0;
        border-radius: 4px;
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .login-title {
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

    .login-button {
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

    .login-button:hover {
        background: #f5f5f5;
    }

    .login-button:active {
        background: #efefef;
    }

    .signup-link {
        text-align: center;
        margin-top: 16px;
        font-size: 13px;
    }

    .signup-link a {
        color: #000;
        text-decoration: none;
        border-bottom: 1px solid #000;
    }

    .signup-link a:hover {
        text-decoration: underline;
    }

    .error-message {
        display: none;
        padding: 12px;
        background: #fff5f5;
        border: 1px solid #e0e0e0;
        border-radius: 4px;
        color: #d32f2f;
        font-size: 13px;
        margin-bottom: 20px;
    }

    .error-message.active {
        display: block;
    }
</style>

<div class="login-container">
    <asp:Panel DefaultButton="loginButton" CssClass="login-form" runat="server">
        <h1 class="login-title">Login</h1>

        <div class="error-message" id="errorMessage" runat="server"></div>

        <div class="form-group">
            <label class="form-label" for="emailInput">Email</label>
            <asp:TextBox ID="emailInput" runat="server" CssClass="form-input" TextMode="Email" placeholder="Enter your email"></asp:TextBox>
        </div>

        <div class="form-group">
            <label class="form-label" for="passwordInput">Password</label>
            <asp:TextBox ID="passwordInput" runat="server" CssClass="form-input" TextMode="Password" placeholder="Enter your password"></asp:TextBox>
        </div>

        <asp:Button ID="loginButton" runat="server" Text="Login" CssClass="login-button" OnClick="LoginButton_Click" />

        <div class="signup-link">
            Don't have an account? <a href="Signup.aspx">Sign up</a>
        </div>
    </asp:Panel>
</div>

</asp:Content>
