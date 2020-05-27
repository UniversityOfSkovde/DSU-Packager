using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

public class CustomPackages
{
    [MenuItem("DSU/Create New Package")]
    private static void NewPackageOption()
    {
        EditorWindow.GetWindow(typeof(CustomPackagesWindow), true, "Create New Package");
    }
}

public class CustomPackagesWindow : EditorWindow
{
    private static readonly string EDITOR_TEST_RUNNER_GUID = "27619889b8ba8c24980f49ee34dbb44a";
    private static readonly string ENGINE_TEST_RUNNER_GUID = "0acc523941302664db1f4e527237feb3";
    private static readonly string LICENSE_FOLDER = "Packages/DSU Packager/Runtime/Licenses";
    
    private string packageName = "";
    private string authorName = "";
    private string authorEmail = "";
    
    private bool mit = false;
    private bool publicDomain = false;
    private bool noCommercialUse = false;
    private bool shareAlike = false;
    
    private string License { get {
        string license;
        if (publicDomain) license = "CC0";
        else if (mit) license = "MIT";
        else
        {
            if (noCommercialUse && shareAlike)
                license = "CC-BY-NC-SA 4.0";
            else if (noCommercialUse)
                license = "CC-BY-NC 4.0";
            else if (shareAlike)
                license = "CC-BY-SA 4.0";
            else
                license = "CC-BY 4.0";
        }

        return license;
    }}

    private Git git;

    public CustomPackagesWindow()
    {
        git = new Git();
        Task.Run(() =>
        {
            if (!git.CheckInstallation() || !git.GetUserAndEmail(out authorName, out authorEmail))
            {
                errorMessage = "git not configured correctly. More info in the log.'";
                return;
            }
        });
    }

    private string PackageFolderName => 
        packageName.ToLower()
            .Replace("\"", "")
            .Replace(" ", "-");
    
    private string FullPackageName => $"se.his.{PackageFolderName}";

    private string RequiredUnityVersion
    {
        get
        {
            var first = Application.unityVersion.IndexOf('.');
            if (first >= 0)
            {
                var second = Application.unityVersion.IndexOf('.', first + 1);
                if (second >= 0)
                {
                    return Application.unityVersion.Substring(0, second);
                }
            }
            return Application.unityVersion;
        }
    }

    private void OnGUI()
    {
        ShowPackageFields();
        ShowAuthorFields();
        ShowLegalFields();
        ShowSubmit();
    }

    private void GeneratePackage()
    {
        var dirName = $"Packages/{PackageFolderName}";
        Directory.CreateDirectory(dirName);

        if (!GenerateRepository(dirName)
        ||  !GenerateGitIgnore(dirName)
        ||  !GenerateGitAttributes(dirName)
        ||  !GenerateLicenseText(dirName)
        ||  !GenerateRuntimeAndTestDir(dirName)
        ||  !GeneratePackageJson(dirName)) 
            return;

        git.AddAll();
        git.Commit("Upload First Version.");
    }

    private bool GenerateRepository(string dirName)
    {
        git.WorkingDir = $"{Directory.GetCurrentDirectory()}/{dirName}";
        if (!git.Init()) return false;

        var readme = $"{dirName}/README.md";
        using (var writer = File.CreateText(readme))
        {
            writer.WriteLine($"# {packageName}");
            writer.WriteLine("Upload this entire package to GitHub or some other hosting site. You can then add it " +
                             "to a unity project by going into `Window -> Package Manager -> + -> Add Package From git URL...`.");
        }

        return git.Add("README.md")
            && git.Commit("Initialized Repository");
    }

    private bool GenerateGitIgnore(string dirName)
    {
        var ignore = $"{dirName}/.gitignore";
        using (var writer = File.CreateText(ignore))
        {
            writer.WriteLine("# Ignored Folders");
            writer.WriteLine("/[Ll]ibrary/");
            writer.WriteLine("/[Tt]emp/");
            writer.WriteLine("/[Oo]bj/");
            writer.WriteLine("/[Bb]uild/");
            writer.WriteLine("/[Bb]uilds/");
            writer.WriteLine("/[Ll]ogs/");
            writer.WriteLine("/[Uu]ser[Ss]ettings/");
            writer.WriteLine();
            writer.WriteLine("# Memory Captures");
            writer.WriteLine("/[Mm]emoryCaptures/");
            writer.WriteLine();
            writer.WriteLine("# Meta data should only be ignored when the corresponding asset is also ignored");
            writer.WriteLine("!/**/*.meta");
            writer.WriteLine();
            writer.WriteLine("# Cache directories of various applications");
            writer.WriteLine(".vs/");
            writer.WriteLine(".gradle/");
            writer.WriteLine();
            writer.WriteLine("# Solution files should not be included");
            writer.WriteLine("# (since this is not the main project but a package.)");
            writer.WriteLine("ExportedObj/");
            writer.WriteLine(".consulo/");
            writer.WriteLine("*.csproj");
            writer.WriteLine("*.unityproj");
            writer.WriteLine("*.sln");
            writer.WriteLine("*.suo");
            writer.WriteLine("*.tmp");
            writer.WriteLine("*.user");
            writer.WriteLine("*.userprefs");
            writer.WriteLine("*.pidb");
            writer.WriteLine("*.pidb.meta");
            writer.WriteLine("*.booproj");
            writer.WriteLine("*.svd");
            writer.WriteLine("*.pdb");
            writer.WriteLine("*.pdb.meta");
            writer.WriteLine("*.mdb");
            writer.WriteLine("*.mdb.meta");
            writer.WriteLine("*.opendb");
            writer.WriteLine("*.VC.db");
            writer.WriteLine();
            writer.WriteLine("# Don't include crash info in repo");
            writer.WriteLine("sysinfo.txt");
            writer.WriteLine("crashlytics-build.properties");
            writer.WriteLine();
            writer.WriteLine("# Build files are often very large");
            writer.WriteLine("*.apk");
            writer.WriteLine("*.aab");
            writer.WriteLine("*.unitypackage");
        }

        return git.Add(".gitignore")
            && git.Commit("Created .gitignore");
    }

    private bool GenerateGitAttributes(string dirName)
    {
        var ignore = $"{dirName}/.gitattributes";
        using (var writer = new GitAttributeWriter(ignore))
        {
            writer.Comment("Don't include tests and git-specific files on export");
            writer.WriteLines(
                ".gitattributes export-ignore",
                ".gitignore export-ignore",
                "README.md export-ignore",
                "Tests/ export-ignore"
            );
            writer.NewLine();
            
            writer.Comment("Hide Some Files on GitHub");
            writer.WriteLines(
                "*.asset linguist-generated",
                "*.mat linguist-generated",
                "*.meta linguist-generated",
                "*.prefab linguist-generated",
                "*.unity linguist-generated"
            );
            writer.NewLine();
            
            writer.Comment("The following is based on https://gist.github.com/FullStackForger/fe2b3da81e60337757fe82d74ebf7d7a");
            writer.Comment("Unity");
            writer.WriteLines(
                "*.cginc   text",
                "*.cs      diff=csharp text eol=lf",
                "*.shader  text eol=lf"
            );
            writer.NewLine();
            writer.Comment("Unity YAML");
            writer.AddYaml("mat", "anim", "unity", "prefab", 
                "physicMaterial2D", "physicMaterial", 
                "asset", "meta", "controller");
            writer.NewLine();
            writer.Comment("Unity LFS");
            writer.AddLfs("cubemap", "unitypackage");
            writer.NewLine();
            writer.Comment("3D Models");
            writer.AddLfs("3dm", "3ds", "blend", "c4d", "collada", "dae", "dxf", "fbx", "jas", "lws", 
                "lxo", "ma", "max", "mb", "obj", "ply", "skp", "stl", "ztl");
            writer.NewLine();
            writer.Comment("Audio");
            writer.AddLfs("aif", "aiff", "it", "mod", "mp3", "ogg", "s3m", "wav", "xm");
            writer.NewLine();
            writer.Comment("Video");
            writer.AddLfs("asf", "avi", "flv", "mov", "mp4", "mpeg", "mpg", "ogv", "wmv");
            writer.NewLine();
            writer.Comment("Images");
            writer.AddLfs("bmp", "exr", "gif", "hdr", "iff", "jpeg", "jpg", "pict", "png", "psd", "tga", 
                "tif", "tiff");
            writer.NewLine();
            writer.Comment("Compressed Archive");
            writer.AddLfs("7z", "bz2", "gz", "rar", "tar", "zip");
            writer.NewLine();
            writer.Comment("Compiled Dynamic Library");
            writer.AddLfs("dll", "pdb", "so");
            writer.NewLine();
            writer.Comment("Fonts");
            writer.AddLfs("otf", "ttf");
            writer.NewLine();
            writer.Comment("Executable/Installer");
            writer.AddLfs("exe", "apk");
            writer.NewLine();
            writer.Comment("Documents");
            writer.AddLfs("pdf");
        }
        
        return git.Add(".gitattributes")
               && git.Commit(".gitattributes use Unity Smart Merging and git LFS");
    }

    private bool GenerateLicenseText(string dirName)
    {
        if (License == "None") return true;
        
        var licenseFile = $"{dirName}/LICENSE.md";
        var year = DateTime.Now.Year;
        
        using (var writer = File.CreateText(licenseFile))
        {
            var copyright = $"Copyright (c) {year} {authorName}";
            switch (License)
            {
                case "MIT":
                {
                    string text;
                    using (var reader = new StreamReader($"{LICENSE_FOLDER}/mit.txt"))
                    {
                        text = reader.ReadToEnd()
                            .Replace("[year]", year.ToString())
                            .Replace("[fullname]", authorName);
                    }
                    writer.Write(text);
                    break;
                }
                case "CC-BY-NC-SA 4.0":
                {
                    string text;
                    using (var reader = new StreamReader($"{LICENSE_FOLDER}/CC-BY-NC-SA-4.0.txt"))
                    {
                        text = reader.ReadToEnd();
                    }
                    writer.Write(text);
                    break;
                }
                case "CC-BY-NC 4.0":
                {
                    string text;
                    using (var reader = new StreamReader($"{LICENSE_FOLDER}/CC-BY-NC-4.0.txt"))
                    {
                        text = reader.ReadToEnd();
                    }
                    writer.Write(text);
                    break;
                }
                case "CC-BY-SA 4.0":
                {
                    string text;
                    using (var reader = new StreamReader($"{LICENSE_FOLDER}/CC-BY-SA-4.0.txt"))
                    {
                        text = reader.ReadToEnd();
                    }
                    writer.Write(text);
                    break;
                }
                case "CC-BY 4.0":
                {
                    string text;
                    using (var reader = new StreamReader($"{LICENSE_FOLDER}/CC-BY-4.0.txt"))
                    {
                        text = reader.ReadToEnd();
                    }
                    writer.Write(text);
                    break;
                }
                case "CC0":
                {
                    string text;
                    using (var reader = new StreamReader($"{LICENSE_FOLDER}/CC0.txt"))
                    {
                        text = reader.ReadToEnd();
                    }
                    writer.Write(text);
                    break;
                }
                case "Apache 2.0":
                {
                    string text;
                    using (var reader = new StreamReader($"{LICENSE_FOLDER}/Apache-2.0.txt"))
                    {
                        text = reader.ReadToEnd()
                            .Replace("[yyyy]", year.ToString())
                            .Replace("[name of copyright owner]", authorName);
                    }
                    writer.Write(text);
                    break;
                }
                default:
                {
                    Debug.LogError($"An unknown license '{License}' was requested.");
                    writer.WriteLine("Unknown license.");
                    break;
                }
            }
            
        }

        return git.Add("LICENSE.md")
               && git.Commit($"Added {License} license file.");
    }

    private bool GenerateRuntimeAndTestDir(string dirName)
    {
        var runtimeDir = $"{dirName}/Runtime";
        Directory.CreateDirectory(runtimeDir);

        var runtimeDirAsmDefGuid = GUID.Generate().ToString();
        var runtimeDirAsmDef = $"{runtimeDir}/{FullPackageName}.asmdef";
        using (var writer = File.CreateText(runtimeDirAsmDef))
        {
            writer.WriteLine("{");
            writer.WriteLine($"    \"name\": \"{FullPackageName}\"");
            writer.WriteLine("}");
        }
        
        if (!git.Add($"Runtime/{FullPackageName}.asmdef"))
            return false;
        
        var runtimeDirAsmDefMeta = $"{runtimeDirAsmDef}.meta";
        using (var writer = File.CreateText(runtimeDirAsmDefMeta))
        {
            writer.WriteLine("fileFormatVersion: 2");
            writer.WriteLine($"guid: {runtimeDirAsmDefGuid}");
            writer.WriteLine("AssemblyDefinitionImporter:");
            writer.WriteLine("  externalObjects: {}");
            writer.WriteLine("  userData: ");
            writer.WriteLine("  assetBundleName: ");
            writer.WriteLine("  assetBundleVariant: ");
        }

        if (!git.Add($"Runtime/{FullPackageName}.asmdef.meta"))
            return false;
        
        var testsDir = $"{dirName}/Tests";
        Directory.CreateDirectory(testsDir);
        
        var testsRuntimeDir = $"{dirName}/Tests/Runtime";
        Directory.CreateDirectory(testsRuntimeDir);
        
        var testsRuntimeDirAsmDef = $"{testsRuntimeDir}/{FullPackageName}.Tests.asmdef";
        using (var writer = File.CreateText(testsRuntimeDirAsmDef))
        {
            writer.WriteLine("{");
            writer.WriteLine($"    \"name\": \"{FullPackageName}.Tests\",");
            
            writer.WriteLine("    \"references\": [");
            writer.WriteLine($"        \"GUID:{runtimeDirAsmDefGuid}\","); // Reference runtime folder
            writer.WriteLine($"        \"GUID:{EDITOR_TEST_RUNNER_GUID}\",");
            writer.WriteLine($"        \"GUID:{ENGINE_TEST_RUNNER_GUID}\"");
            writer.WriteLine("    ],");
            
            writer.WriteLine("    \"includePlatforms\": [");
            writer.WriteLine("        \"Editor\"");
            writer.WriteLine("    ],");

            writer.WriteLine("    \"excludePlatforms\": [],");
            writer.WriteLine("    \"allowUnsafeCode\": false,");
            writer.WriteLine("    \"overrideReferences\": false,");
            writer.WriteLine("    \"precompiledReferences\": [],");
            writer.WriteLine("    \"autoReferenced\": true,");
            writer.WriteLine("    \"defineConstraints\": [],");
            writer.WriteLine("    \"versionDefines\": [],");
            writer.WriteLine("    \"noEngineReferences\": false");
            writer.WriteLine("}");
        }
        
        return git.Add($"Tests/Runtime/{FullPackageName}.Tests.asmdef");
    }

    private bool GeneratePackageJson(string dirName)
    {
        var manifestName = $"{dirName}/package.json";
        using (var writer = File.CreateText(manifestName))
        {
            writer.WriteLine("{");
            writer.WriteLine($"    \"name\": \"{FullPackageName}\",");
            writer.WriteLine("    \"version\": \"1.0.0\",");
            writer.WriteLine($"    \"displayName\": \"{packageName}\",");
            writer.WriteLine("    \"description\": \"Custom Unity Package.\",");
            writer.WriteLine($"    \"unity\": \"{RequiredUnityVersion}\",");
            writer.WriteLine("    \"dependencies\": {},");
            writer.WriteLine("    \"keywords\": [\"dsu\", \"skövde\"],");
            writer.WriteLine("    \"author\": {");
            writer.WriteLine($"        \"name\": \"{authorName}\",");
            writer.WriteLine($"        \"email\": \"{authorEmail}\"");
            writer.WriteLine("    }");
            writer.WriteLine("}");
        }
        
        Console.WriteLine($"Manifest file '{manifestName}' created!");
        AssetDatabase.Refresh();

        return git.Add("package.json");
    }

    private void ShowPackageFields()
    {
        GUILayout.Label ("Create a New Package", EditorStyles.boldLabel);
        packageName = EditorGUILayout.TextField("Display Name", packageName);
        EditorGUILayout.LabelField($"Folder Name: {PackageFolderName}", EditorStyles.miniLabel);
    }

    private void ShowAuthorFields()
    {
        GUILayout.Label ("About You as Author", EditorStyles.boldLabel);
        authorName = EditorGUILayout.TextField ("Your Name", authorName);
        authorEmail = EditorGUILayout.TextField ("Your Email", authorEmail);
    }

    private void ShowLegalFields()
    {
        GUILayout.Label ("Legal Stuff", EditorStyles.boldLabel);
        
        publicDomain = EditorGUILayout.Toggle("Public Domain (CC0)", publicDomain);
        using (new EditorGUI.IndentLevelScope())
        {
            using (new EditorGUI.DisabledScope(publicDomain))
            {
                if (publicDomain) EditorGUILayout.Toggle("MIT License", false);
                else mit = EditorGUILayout.Toggle("MIT License", mit);
                
                using (new EditorGUI.IndentLevelScope())
                {
                    using (new EditorGUI.DisabledScope(mit))
                    {
                        EditorGUILayout.LabelField("Creative Commons");
                        if (publicDomain || mit)
                        {
                            EditorGUILayout.Toggle("Non-Commercial", false);
                            EditorGUILayout.Toggle("Share Alike", false);
                        }
                        else
                        {
                            noCommercialUse = EditorGUILayout.Toggle("Non-Commercial", noCommercialUse);
                            shareAlike = EditorGUILayout.Toggle("Share Alike", shareAlike);
                        }
                        
                    }
                }
            }
        }
        
        
        GUILayout.Label($"Selected License: {License}");
    }

    private void ShowSubmit()
    {
        GUILayout.Label ("Finalize Package", EditorStyles.boldLabel);

        GUILayout.Label(errorMessage, EditorStyles.centeredGreyMiniLabel);

        if (GUILayout.Button("Generate Package Folder!"))
        {
            errorMessage = "";
            
            packageName = packageName.Trim();
            authorName = authorName.Trim();
            authorEmail = authorEmail.Trim();
            
            if (packageName.Length == 0)
                errorMessage += "No package name specified.\n";
            if (authorName.Length == 0)
                errorMessage += "No author name specified.\n";
            if (authorEmail.Length == 0)
                errorMessage += "No author email specified.\n";

            var dirName = $"Packages/{PackageFolderName}";
            if (Directory.Exists(dirName))
            {
                errorMessage += $"Directory '{dirName}' already exists.";
            }
            
            if (errorMessage == "")
            {
                GeneratePackage();
                Close();
            }
        }
    }
    
    private string errorMessage = "";
}

internal class Git
{
    public string WorkingDir { get; set; }

    public bool CheckInstallation()
    {
        if (!Run("--version", out var version))
        {
            Debug.LogError("Could not find git.");
            return false;
        }
        
        Debug.Log(version);
        
        if (!Run("lfs version", out var lfsVersion))
        {
            Debug.LogError("Could not find git LFS.");
            return false;
        }
        
        Debug.Log(lfsVersion);
        
        if (!Run("flow version", out var flowVersion))
        {
            Debug.LogError("Could not find git-flow.");
            return false;
        }
        
        Debug.Log($"git-flow {flowVersion}");
        return true;
    }

    public bool GetUserAndEmail(out string name, out string email)
    {
        name = ""; email = "";
        return Run("config user.name", out name)
            && Run("config user.email", out email);
    }
    
    public bool Init()
    {
        return Run("init");
    }

    public bool Add(params string[] files)
    {
        var args = string.Join("\", \"", files);
        if (args.Length > 0) args = "\"" + args + "\"";
        return Run("add " + args);
    }

    public bool AddAll()
    {
        return Run("add -A");
    }

    public bool Commit(string message)
    {
        return Run($"commit -m \"{message.Replace("\"", "\\\"")}\"");
    }

    private bool Run(string gitArgs)
    {
        return Run(gitArgs, out _);
    }
    
    private bool Run(string gitArgs, out string output)
    {
        if (!Run(gitArgs, out output, out var errorOutput))
        {
            // Check for failure due to no git setup in the project itself or other fatal errors from git.
            if (output.Contains("fatal") || output == "no-git" || output == "") {
                Debug.LogError($"Command: git {gitArgs} Failed.\n {output}{errorOutput}");
            }
        
            // Log any errors.
            if (errorOutput != "") {
                Debug.LogError($"Git Error: {errorOutput}");
            }

            return false;
        }

        return true;
    }

    private bool Run(string gitArgs, out string output, out string errorOutput)
    {
        output = "";
        errorOutput = "";
        
        var info = new ProcessStartInfo("git", gitArgs) {
            CreateNoWindow = true,          // We want no visible pop-ups
            UseShellExecute = false,        // Allows us to redirect input, output and error streams
            RedirectStandardOutput = true,  // Allows us to read the output stream
            RedirectStandardError = true,   // Allows us to read the error stream
            WorkingDirectory = WorkingDir
        };
        
        var process = new Process { StartInfo = info };
        
        try {
            process.Start();  // Try to start it, catching any exceptions if it fails
        } catch (Exception e) {
            // For now just assume its failed cause it can't find git.
            Debug.LogError("Git is not set-up correctly, required to be on PATH, and to be a git project.");
            return false;
        }

        // Read the results back from the process so we can get the output and check for errors
        output = process.StandardOutput.ReadToEnd();
        errorOutput = process.StandardError.ReadToEnd();

        process.WaitForExit();  // Make sure we wait till the process has fully finished.
        process.Close();        // Close the process ensuring it frees it resources.

        if (errorOutput.StartsWith("warning: CRLF "))
        {
            output = "";
            errorOutput = "";
        }
        
        return !output.Contains("fatal") 
               && output != "no-git"
               && errorOutput == "";
    }
}

internal class GitAttributeWriter : IDisposable
{
    private readonly StreamWriter writer;

    public GitAttributeWriter(string fileName)
    {
        writer = File.CreateText(fileName);
    }

    public void Comment(string message)
    {
        writer.WriteLine($"# {message}");
    }

    public void WriteLines(params string[] lines)
    {
        foreach (var line in lines)
        {
            writer.WriteLine(line);
        }
    }

    public void AddYaml(params string[] fileTypes)
    {
        foreach (var fileType in fileTypes)
        {
            writer.WriteLine($"*.{fileType} merge=unityyamlmerge eol=lf");
        }

        NewLine();
    }

    public void AddLfs(params string[] fileTypes)
    {
        foreach (var fileType in fileTypes)
        {
            writer.WriteLine($"*.{fileType} filter=lfs diff=lfs merge=lfs -text");
        }

        NewLine();
    }
    
    public void NewLine()
    {
        writer.WriteLine();
    }

    public void Dispose()
    {
        writer.Dispose();
    }
}