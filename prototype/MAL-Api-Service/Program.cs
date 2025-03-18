namespace MAL_Api_Service;

// TODO: DELETE OR RENAME THIS CLASS
public class Program {
    // Define the certificate password (e.g., as a constant or read from configuration)
    private const string CertificatePassword = "DevPassword"; // Replace with your actual password
    
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            // Attempt to load certificate
            string certPath = GetCertificatePath();
            
            if (certPath != null && File.Exists(certPath)) {
                try
                {
                    // Load the certificate with the password if it's a .pfx file
                    var certificate = Path.GetExtension(certPath).ToLowerInvariant() == ".pfx"
                        ? new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath, CertificatePassword)
                        : new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath);

                    serverOptions.ConfigureHttpsDefaults(httpsOptions => {
                        httpsOptions.ServerCertificate = certificate;
                    });
                    Console.WriteLine($"Certificate loaded from: {certPath}");
                } catch (System.Security.Cryptography.CryptographicException ex) {
                    Console.WriteLine($"Error loading certificate from {certPath}: {ex.Message}. Running on HTTP only.");
                }
            } else {
                Console.WriteLine($"Warning: Certificate not found at {certPath ?? "any expected location"}. Running on HTTP only.");
            }
        });
        
        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment()) {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        
        // Only enable HTTPS redirection if certificate is found
        /*string certPathCheck = GetCertificatePath();
        
        if (certPathCheck != null && File.Exists(certPathCheck)) {
            app.UseHttpsRedirection();
        }*/

        //app.UseAuthorization();
        
        app.MapControllers();

        app.Run();
    }
    
    private static string GetCertificatePath()
    {
        // Check Docker path first
        string dockerCertPath = "/app/.certs/localhost_custom.pfx";
        if (File.Exists(dockerCertPath))
        {
            return dockerCertPath;
        }

        // Check local path (relative to project root)
        string localCertPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".certs", "localhost_custom.crt");
        localCertPath = Path.GetFullPath(localCertPath);
        if (File.Exists(localCertPath))
        {
            return localCertPath;
        }

        return null;
    }
}