using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI;

namespace CDE
{
    public partial class Signup : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void SignupButton_Click(object sender, EventArgs e)
        {
            string name = nameInput.Text.Trim();
            string email = emailInput.Text.Trim();
            string password = passwordInput.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                ShowError("Name is required");
                return;
            }

            if (string.IsNullOrEmpty(email))
            {
                ShowError("Email is required");
                return;
            }

            if (!IsValidEmail(email))
            {
                ShowError("Please enter a valid email address");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Password is required");
                return;
            }

            if (password.Length < 6)
            {
                ShowError("Password must be at least 6 characters long");
                return;
            }

            string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CDE;Integrated Security=True;";
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string checkQuery = "SELECT COUNT(1) FROM Users WHERE Email = @Email";
                    using (SqlCommand cmdCheck = new SqlCommand(checkQuery, con))
                    {
                        cmdCheck.Parameters.AddWithValue("@Email", email);
                        int count = Convert.ToInt32(cmdCheck.ExecuteScalar());
                        if (count > 0)
                        {
                            ShowError("Email already exists");
                            return;
                        }
                    }

                    string hash = ComputeSha256Hash(password);

                    string insertQuery = "INSERT INTO Users (Email, PasswordHash, DisplayName) VALUES (@Email, @PasswordHash, @DisplayName)";
                    using (SqlCommand cmdInsert = new SqlCommand(insertQuery, con))
                    {
                        cmdInsert.Parameters.AddWithValue("@Email", email);
                        cmdInsert.Parameters.AddWithValue("@PasswordHash", hash);
                        cmdInsert.Parameters.AddWithValue("@DisplayName", name);
                        cmdInsert.ExecuteNonQuery();
                    }
                }

                ShowSuccess("Account created successfully! Redirecting to login...");
                nameInput.Text = "";
                emailInput.Text = "";
                passwordInput.Text = "";

                Response.AddHeader("REFRESH", "2;URL=Login.aspx");
            }
            catch (Exception ex)
            {
                ShowError("Database error: " + ex.Message);
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void ShowError(string message)
        {
            statusMessage.InnerText = message;
            statusMessage.Attributes["class"] = "status-message error";
        }

        private void ShowSuccess(string message)
        {
            statusMessage.InnerText = message;
            statusMessage.Attributes["class"] = "status-message success";
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
