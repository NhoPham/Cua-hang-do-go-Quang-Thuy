dotnet ef dbcontext scaffold "Server=172.28.182.33;Database=QLBH;User Id=sa;Password=nhattVim2*;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models --force


dotnet aspnet-codegenerator controller -name ProductController -actions -m Product -dc QlbhContext -outDir Controllers --useDefaultLayout --referenceScriptLibraries
