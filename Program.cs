using System.IO.Compression;

namespace Mattodev.ProgramDeployer;

class Program {
    public static int Main(string[] args) {
        string uri = args[0];
        string author = args[1];
        string name = args[2];

        string zipOutDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            $"{author}\\{name}\\"
        );
        string zipLocation = Path.Combine(Path.GetTempPath(), $"mtdeploy_{author}_{name}.zip");
        string oldPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)!;

        if (uri == "!!UNDEPLOY") {
            if (!Directory.Exists(zipOutDir)) {
                Console.WriteLine($"mtdeploy: tried to undeploy nondeployed program {author}/{name}");
                return 1;
            }

            Directory.Delete(zipOutDir, true);
            var pathChunks = oldPath.Split(';').ToList();
            pathChunks.Remove(zipOutDir);
            
            Environment.SetEnvironmentVariable("PATH", string.Join(';', pathChunks), EnvironmentVariableTarget.User);
            Console.WriteLine($"mtdeploy: undeployed {author}/{name}");
            return 0;
        }
        HttpClient client = new();
        
        var responseT = client.GetAsync(uri);
        responseT.Wait();
        var response = responseT.Result;
        if (!response.IsSuccessStatusCode) return 1;

        FileStream zip = File.Open(zipLocation, FileMode.Create, FileAccess.ReadWrite);
        Stream zipResponse = response.Content.ReadAsStream();
        zipResponse.CopyTo(zip);
        zipResponse.Close();
        zip.Position = 0;

        Directory.CreateDirectory(zipOutDir);
        ZipFile.ExtractToDirectory(zip, zipOutDir);
        
        string newPath = oldPath + ";" + zipOutDir;
        Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);

        zip.Close();
        File.Delete(zipLocation);
        Console.WriteLine("mtdeploy: deployed to " + zipOutDir);
        return 0;
    }
}
