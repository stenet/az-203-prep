using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AzSqlAccessToken
{
    internal class Program
    {
        private const string CERT_NAME = "CN=test";
        private const string AAD_INSTANCE = "https://login.windows.net/{0}";
        private const string TENANT = "xxx.onmicrosoft.com"; // => anpassen
        private const string CLIENT_ID = "9a0d9c37-c3b9-472c-9daa-cd6a815335ad";
        private const string SQL_DB_RESOURCE_ID = "https://database.windows.net/";

        private static void Main(string[] args)
        {
            var builder = new SqlConnectionStringBuilder();
            builder["Data Source"] = "sql20200202.database.windows.net";
            builder["Initial Catalog"] = "test";
            builder["Connect Timeout"] = 30;

            var accessToken = GetAccessToken().Result;

            if (accessToken == null)
            {
                Console.WriteLine("Fail to acquire the token to the database.");
            }
            using (var connection = new SqlConnection(builder.ConnectionString))
            {
                try
                {
                    connection.AccessToken = accessToken;
                    connection.Open();


                    Console.WriteLine("Connected to the database");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            Console.WriteLine("Please press any key to stop");
            Console.ReadKey();
        }

        private static async Task<string> GetAccessToken()
        {
            var authority = string.Format(CultureInfo.InvariantCulture, AAD_INSTANCE, TENANT);
            var authContext = new AuthenticationContext(authority);

            X509Certificate2 cert = null;
            var store = new X509Store(StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);

                var certCollection = store.Certificates;
                var currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                var signingCert = currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, CERT_NAME, false);

                if (signingCert.Count == 0)
                    throw new Exception("Cannot find certificate: " + CERT_NAME);

                cert = signingCert[0];
            }
            finally
            {
                store.Close();
            }

            var certCred = new ClientAssertionCertificate(CLIENT_ID, cert);
            return await AcquireToken(authContext, certCred);
        }
        private static async Task<string> AcquireToken(AuthenticationContext authContext, ClientAssertionCertificate certCred)
        {
            var result = (AuthenticationResult)null;
            var retryCount = 0;
            var retry = false;

            do
            {
                retry = false;
                try
                {
                    result = await authContext.AcquireTokenAsync(SQL_DB_RESOURCE_ID, certCred);
                }
                catch (AdalException ex)
                {
                    if (ex.ErrorCode == "temporarily_unavailable")
                    {
                        retry = true;
                        retryCount++;
                        Thread.Sleep(3000);
                    }

                    Console.WriteLine(
                        $"An error occurred while acquiring a token\nTime: {DateTime.Now.ToString()}\nError: {ex.ToString()}\nRetry: {retry.ToString()}\n");
                }
            } while ((retry == true) && (retryCount < 3));

            if (result == null)
            {
                Console.WriteLine("Canceling attempt to contact To Do list service.\n");
                return null;
            }

            return result.AccessToken;
        }
    }
}
